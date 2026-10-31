using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using CurioCatalogue.Core;

namespace CurioCatalogue.Adapters;

public enum PostRefusal { Unsupported, NoAccess, Missing, NoRoom, NothingFromList, MoveFailed, Blank }

public abstract record PostInvoiceResult
{
    /// <param name="LedgerBefore">Entries in the off-hand ledger before and after; null without a ledger.</param>
    /// <param name="ListLeft">For a want list: lines still on it.</param>
    /// <param name="NotFitted">For a want list: lines the container had but the inventory had no room for.</param>
    public sealed record Posted(int Lines, int Items, InvoiceFate Fate, int? LedgerBefore, int? LedgerAfter, bool OverLimit,
        int ListLeft = 0, int NotFitted = 0) : PostInvoiceResult;

    /// <param name="Lacking">For <see cref="PostRefusal.Missing"/>: what the container lacks.</param>
    /// <param name="SlotsShort">For <see cref="PostRefusal.NoRoom"/>: how many more free slots would do.</param>
    public sealed record Refused(PostRefusal Reason, IReadOnlyList<Entry>? Lacking = null, int SlotsShort = 0) : PostInvoiceResult;
}

public enum CollectRefusal { Unsupported, NoAccess, NothingNew, NoRoom }

/// <summary>An album taking new items for its collection straight from a container (domain.md, Albums and collections).</summary>
public abstract record CollectResult
{
    /// <param name="Taken">Items taken into the inventory and crossed off the album.</param>
    /// <param name="NotFitted">New items left in the container for lack of room.</param>
    /// <param name="MissingLeft">What the album still misses.</param>
    public sealed record Collected(int Taken, int NotFitted, int MissingLeft) : CollectResult;

    /// <param name="SlotsShort">For <see cref="CollectRefusal.NoRoom"/>: how many more free slots would take all.</param>
    public sealed record Refused(CollectRefusal Reason, int SlotsShort = 0) : CollectResult;
}

/// <summary>
/// Posts an invoice into a container (domain.md, Posting an invoice): moves its items from the container into the player's
/// inventory, updates the ledger in the off-hand and handles the sheet. Runs on the server. All or nothing:
/// the whole transaction is planned and checked first (<see cref="Transfer"/>), by the game's merge rule, and only then
/// carried out; if the game still moves less, every touched slot is put back. A want list and an album (<see cref="Collect"/>)
/// take as much as fits instead, line by line, and record only what really moved.
/// </summary>
public sealed class InvoicePoster
{
    public static readonly AssetLocation ParchmentCode = new("game", "paper-parchment");

    private readonly IWorldAccessor world;
    private readonly LedgerStore store;
    private readonly StackKeys keys;
    private readonly ContainerReader reader;
    private readonly ContainerAccess access;
    private readonly Func<LedgerLimits> limits;
    private readonly Func<bool> consumeSheets;

    public InvoicePoster(IWorldAccessor world, LedgerStore store, StackKeys keys, ContainerReader reader, ContainerAccess access,
        Func<LedgerLimits> limits, Func<bool> consumeSheets)
    {
        this.world = world;
        this.store = store;
        this.keys = keys;
        this.reader = reader;
        this.access = access;
        this.limits = limits;
        this.consumeSheets = consumeSheets;
    }

    /// <param name="invoiceSlot">The slot holding the invoice (the main hand); it is one of <paramref name="playerSlots"/>.</param>
    /// <param name="playerSlots">Where the items may go: hotbar without the off-hand, and the backpack.</param>
    /// <param name="offhandSlot">May hold a ledger to update.</param>
    public PostInvoiceResult Post(IPlayer player, ItemSlot invoiceSlot, InvoiceKind kind, BlockPos pos,
        IReadOnlyList<ItemSlot> playerSlots, ItemSlot? offhandSlot)
    {
        pos = reader.MainPos(pos);
        var be = world.BlockAccessor.GetBlockEntity(pos);
        if (reader.Classify(be) != ContainerKind.Supported) return new PostInvoiceResult.Refused(PostRefusal.Unsupported);
        if (!access.CanUse(player, pos)) return new PostInvoiceResult.Refused(PostRefusal.NoAccess);

        var container = reader.Open(be)!;
        var invoice = store.ReadInvoice(invoiceSlot.Itemstack!, kind);
        // A blank invoice (from the creative inventory) has nothing to post; it would post as "0 lines" and give the book a sheet.
        if (invoice.Entries.IsEmpty) return new PostInvoiceResult.Refused(PostRefusal.Blank);
        var merge = new MergeClasses();
        var (sources, samples) = Sources(container, merge);
        if (kind == InvoiceKind.WantList)
            return PostWantList(player, invoiceSlot, invoice, container, sources, samples, playerSlots, merge);

        // What will become of the ledger and the invoice: decides whether the invoice's slot counts as free.
        var lim = limits();
        // An album in the off-hand takes the sheet back, as when cancelling, but its entries stay: it changes only by taking
        // new items (domain.md, Posting an invoice; Albums and collections).
        var ledgerStack = offhandSlot?.Itemstack?.Collectible is ItemLedger ? offhandSlot.Itemstack : null;
        var ledgerItem = (ItemLedger?)ledgerStack?.Collectible;
        var ledger = ledgerStack != null ? store.ReadLedger(ledgerStack, ledgerItem!.LedgerMode) : null;
        var posted = ledgerItem is { IsAlbum: false } ? ledger!.Post(invoice, lim) : null;
        var after = posted?.Ledger ?? ledger;
        var (fate, withSheet) = InvoiceFates.Ended(after, lim, consumeSheets());
        bool invoiceSlotFrees = fate is InvoiceFate.SheetReturned or InvoiceFate.Consumed;

        var targets = Targets(playerSlots, invoiceSlot, invoiceSlotFrees, merge);
        int Capacity(int target, SourceSlot source) =>
            CapacityOf(playerSlots[target], container.Slots[source.Index].Itemstack!, slot => slot == invoiceSlot && invoiceSlotFrees);
        int MaxStack(ItemKey key) => Math.Max(1, samples[key].Collectible.MaxStackSize);

        var plan = Transfer.Plan(invoice.Entries, sources, targets, Capacity, MaxStack);
        switch (plan)
        {
            case TransferPlan.Missing missing:
                return new PostInvoiceResult.Refused(PostRefusal.Missing, Lacking: missing.Lacking);
            case TransferPlan.NoRoom noRoom:
                return new PostInvoiceResult.Refused(PostRefusal.NoRoom, SlotsShort: noRoom.SlotsShort);
        }

        // Carry it out. The invoice leaves its slot first when that slot is planned to take items. If the game moves less
        // than planned (a bug in the plan), every touched slot goes back as it was: an invoice posts whole or not at all.
        var moves = ((TransferPlan.Ready)plan).Moves;
        var before = Snapshot(moves, container, playerSlots, invoiceSlot);
        if (invoiceSlotFrees) invoiceSlot.Itemstack = null;
        var moved = Carry(player, moves, container, playerSlots);
        if (!moved.SequenceEqual(moves))
        {
            Restore(before, container);
            return new PostInvoiceResult.Refused(PostRefusal.MoveFailed);
        }
        int items = moved.Sum(m => m.Amount);

        if ((withSheet ?? posted?.Ledger) is { } written)
        {
            store.WriteLedger(ledgerStack!, written);
            offhandSlot!.MarkDirty();
        }
        if (fate == InvoiceFate.Parchment) invoiceSlot.Itemstack = Parchment();
        invoiceSlot.MarkDirty();

        return new PostInvoiceResult.Posted(invoice.Entries.Length, items, fate,
            posted != null ? ledger!.Entries.Count : null, posted?.Ledger.Entries.Count, posted?.OverLimit ?? false);
    }

    /// <summary>
    /// A want list takes what the container has of its list, as much as fits, and keeps the rest
    /// (domain.md, Albums and collections). It touches no ledger; once its list is done it ends by <see cref="InvoiceFates.WantListPosted"/>.
    /// </summary>
    private PostInvoiceResult PostWantList(IPlayer player, ItemSlot invoiceSlot, Invoice invoice, ContainerSlots container,
        List<SourceSlot> sources, Dictionary<ItemKey, ItemStack> samples, IReadOnlyList<ItemSlot> playerSlots, MergeClasses merge)
    {
        var (lines, _) = invoice.SplitBy(samples.Keys.ToHashSet());
        if (lines.IsEmpty) return new PostInvoiceResult.Refused(PostRefusal.NothingFromList);

        var plan = PlanWhatFits(lines, sources, container, playerSlots, invoiceSlot, merge);
        if (plan.Taken.Count == 0)
            return new PostInvoiceResult.Refused(PostRefusal.NoRoom, SlotsShort: SlotsShort(lines, sources, samples, container, playerSlots, invoiceSlot, merge));

        // A want list takes as much as fits anyway: what the game did not move just stays on the list.
        var moved = Carry(player, plan.Moves, container, playerSlots);
        var taken = FullyMoved(plan.Taken, moved, sources);
        var listLeft = invoice.CrossOff(taken.Select(e => e.Key));
        var fate = InvoiceFates.WantListPosted(done: listLeft.Entries.IsEmpty, consumeSheets());
        if (fate == InvoiceFate.Kept) store.WriteInvoice(invoiceSlot.Itemstack!, listLeft);
        else invoiceSlot.Itemstack = fate == InvoiceFate.Parchment ? Parchment() : null;
        invoiceSlot.MarkDirty();

        return new PostInvoiceResult.Posted(taken.Count, moved.Sum(m => m.Amount), fate, null, null, false, listLeft.Entries.Length,
            plan.Left.Count + plan.Taken.Count - taken.Count);
    }

    /// <summary>
    /// An album takes new items for its collection straight from a container (domain.md, Albums and collections): one of each item it
    /// misses, in the collection's order, as much as fits; what is taken is crossed off. No invoice, no sheet.
    /// </summary>
    /// <param name="albumSlot">The album, in the main hand or the off-hand.</param>
    /// <param name="playerSlots">Where the items may go: hotbar without the off-hand, and the backpack.</param>
    /// <param name="handSlot">The main hand; filled last, so it stays free when there is room elsewhere.</param>
    public CollectResult Collect(IPlayer player, ItemSlot albumSlot, BlockPos pos, IReadOnlyList<ItemSlot> playerSlots, ItemSlot? handSlot)
    {
        pos = reader.MainPos(pos);
        var be = world.BlockAccessor.GetBlockEntity(pos);
        if (reader.Classify(be) != ContainerKind.Supported) return new CollectResult.Refused(CollectRefusal.Unsupported);
        if (!access.CanUse(player, pos)) return new CollectResult.Refused(CollectRefusal.NoAccess);

        var container = reader.Open(be)!;
        var merge = new MergeClasses();
        var (sources, samples) = Sources(container, merge);
        var album = store.ReadLedger(albumSlot.Itemstack!, LedgerMode.Album);
        var wanted = album.Entries.Values.Where(e => samples.ContainsKey(e.Key)).Select(e => e.WithCount(1));
        var lines = (store.CollectionOf(album)?.InOrder(wanted) ?? wanted.OrderBy(e => e.Key.ToString(), StringComparer.Ordinal)).ToList();
        if (lines.Count == 0) return new CollectResult.Refused(CollectRefusal.NothingNew);

        var plan = PlanWhatFits(lines, sources, container, playerSlots, handSlot, merge);
        if (plan.Taken.Count == 0)
            return new CollectResult.Refused(CollectRefusal.NoRoom, SlotsShort(lines, sources, samples, container, playerSlots, handSlot, merge));

        // Only what really moved is crossed off: an item left in the container is still missing.
        var moved = Carry(player, plan.Moves, container, playerSlots);
        var taken = FullyMoved(plan.Taken, moved, sources);
        var after = album.CrossOff(taken.Select(e => e.Key));
        store.WriteLedger(albumSlot.Itemstack!, after);
        albumSlot.MarkDirty();
        return new CollectResult.Collected(moved.Sum(m => m.Amount), plan.Left.Count + plan.Taken.Count - taken.Count, after.Entries.Count);
    }

    /// <summary>
    /// Cancels an invoice of any kind (domain.md, Cancelling): no items move, the ledger's entries stay; the sheet goes back
    /// into the ledger or album in the off-hand, else the invoice becomes parchment; ConsumeSheets: gone.
    /// </summary>
    public InvoiceFate Cancel(ItemSlot invoiceSlot, ItemSlot? offhandSlot)
    {
        var ledgerStack = offhandSlot?.Itemstack?.Collectible is ItemLedger ? offhandSlot.Itemstack : null;
        var book = ledgerStack != null ? store.ReadLedger(ledgerStack, ((ItemLedger)ledgerStack.Collectible).LedgerMode) : null;
        var (fate, withSheet) = InvoiceFates.Ended(book, limits(), consumeSheets());
        if (withSheet != null)
        {
            store.WriteLedger(ledgerStack!, withSheet);
            offhandSlot!.MarkDirty();
        }
        invoiceSlot.Itemstack = fate == InvoiceFate.Parchment ? Parchment() : null;
        invoiceSlot.MarkDirty();
        return fate;
    }

    /// <summary>The container's side, on game-independent copies, and a sample stack of each item.</summary>
    private (List<SourceSlot> Sources, Dictionary<ItemKey, ItemStack> Samples) Sources(ContainerSlots container, MergeClasses merge)
    {
        var sources = new List<SourceSlot>();
        var samples = new Dictionary<ItemKey, ItemStack>();
        for (int i = 0; i < container.Slots.Count; i++)
        {
            if (container.Slots[i].Itemstack is not { StackSize: > 0 } stack) continue;
            var key = keys.KeyOf(stack);
            sources.Add(new SourceSlot(i, key, stack.StackSize, merge.Of(stack)));
            samples.TryAdd(key, stack);
        }
        return (sources, samples);
    }

    /// <summary>
    /// The player's side. <paramref name="last"/> (the main hand) comes last, so the hand stays empty when there is
    /// room elsewhere; with <paramref name="lastFrees"/> it counts as empty (the invoice in it is going away).
    /// </summary>
    private List<TargetSlot> Targets(IReadOnlyList<ItemSlot> playerSlots, ItemSlot? last, bool lastFrees, MergeClasses merge) =>
        playerSlots.Select((slot, i) =>
                slot.Itemstack == null || (slot == last && lastFrees)
                    ? new TargetSlot(i, null, 0)
                    : new TargetSlot(i, keys.KeyOf(slot.Itemstack), slot.Itemstack.StackSize, merge.Of(slot.Itemstack)))
            .OrderBy(t => playerSlots[t.Index] == last)
            .ToList();

    /// <summary>As much of the list as fits; no player slot is freed.</summary>
    private PartialPlan PlanWhatFits(IEnumerable<Entry> lines, List<SourceSlot> sources, ContainerSlots container,
        IReadOnlyList<ItemSlot> playerSlots, ItemSlot? last, MergeClasses merge) =>
        Transfer.PlanWhatFits(lines, sources, Targets(playerSlots, last, lastFrees: false, merge),
            (target, source) => CapacityOf(playerSlots[target], container.Slots[source.Index].Itemstack!, _ => false));

    /// <summary>How many more free slots would take the whole list: for the message when nothing fits.</summary>
    private int SlotsShort(IEnumerable<Entry> lines, List<SourceSlot> sources, Dictionary<ItemKey, ItemStack> samples,
        ContainerSlots container, IReadOnlyList<ItemSlot> playerSlots, ItemSlot? last, MergeClasses merge) =>
        Transfer.Plan(lines, sources, Targets(playerSlots, last, lastFrees: false, merge),
            (target, source) => CapacityOf(playerSlots[target], container.Slots[source.Index].Itemstack!, _ => false),
            key => Math.Max(1, samples[key].Collectible.MaxStackSize)) is TransferPlan.NoRoom noRoom ? noRoom.SlotsShort : 1;

    /// <summary>Carries out the planned moves; returns what really moved, move by move (a move that moved nothing is left out).</summary>
    private List<Move> Carry(IPlayer player, IReadOnlyList<Move> moves, ContainerSlots container, IReadOnlyList<ItemSlot> playerSlots)
    {
        var moved = new List<Move>();
        foreach (var move in moves)
        {
            var from = container.Slots[move.Source];
            var to = playerSlots[move.Target];
            string name = from.Itemstack?.Collectible?.Code?.ToString() ?? "an item"; // the code: names need Lang, absent in tests
            var op = new ItemStackMoveOperation(world, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, move.Amount)
            {
                ActingPlayer = player,
            };
            int amount = from.TryPutInto(to, ref op);
            if (amount > 0) moved.Add(move with { Amount = amount });
            if (amount != move.Amount)
            {
                // The plan follows the game's own checks, so this should not happen; the caller decides what to do.
                // Always logged, it is a bug worth knowing about.
                world.Logger.Error("[CurioCatalogue] Planned to move {0} of {1}, moved {2}", move.Amount, name, amount);
            }
            container.Save(from);
            to.MarkDirty();
        }
        return moved;
    }

    /// <summary>The planned lines whose items all moved.</summary>
    private static List<Entry> FullyMoved(IEnumerable<Entry> planned, IEnumerable<Move> moved, List<SourceSlot> sources)
    {
        var keyOf = sources.ToDictionary(s => s.Index, s => s.Key);
        var amounts = moved.GroupBy(m => keyOf[m.Source]).ToDictionary(g => g.Key, g => g.Sum(m => m.Amount));
        return planned.Where(line => amounts.GetValueOrDefault(line.Key) >= line.Count).ToList();
    }

    /// <summary>Copies of every slot the moves touch, and of <paramref name="extra"/>, to put back with <see cref="Restore"/>.</summary>
    private static List<(ItemSlot Slot, ItemStack? Stack, bool InContainer)> Snapshot(IReadOnlyList<Move> moves, ContainerSlots container,
        IReadOnlyList<ItemSlot> playerSlots, ItemSlot extra)
    {
        var slots = moves.Select(m => (container.Slots[m.Source], true))
            .Concat(moves.Select(m => (playerSlots[m.Target], false)))
            .Append((extra, false))
            .DistinctBy(s => s.Item1);
        return slots.Select(s => (s.Item1, s.Item1.Itemstack?.Clone(), s.Item2)).ToList();
    }

    /// <summary>
    /// Puts the slots back as they were. Safe on the server: posting runs within one tick, so nothing else touched them,
    /// and the moves change only their own two slots (vsapi ItemSlot.cs#TryPutInto).
    /// </summary>
    private static void Restore(List<(ItemSlot Slot, ItemStack? Stack, bool InContainer)> snapshot, ContainerSlots container)
    {
        foreach (var (slot, stack, inContainer) in snapshot)
        {
            slot.Itemstack = stack;
            if (inContainer) container.Save(slot);
            else slot.MarkDirty();
        }
    }

    private ItemStack Parchment() =>
        new(world.GetItem(ParchmentCode) ?? throw new InvalidOperationException($"No item {ParchmentCode}"));

    /// <summary>
    /// How many of the source's item the player slot can hold, following the game's own rules: CanHold (storage
    /// flags of bags), the slot's own stack limit, and for an occupied slot whether the game would merge at all
    /// (vsapi ItemSlot.cs:137#CanHold, ItemSlot.cs:110#CanTakeFrom; <see cref="MergeClasses.Merges"/>).
    /// </summary>
    private static int CapacityOf(ItemSlot slot, ItemStack source, System.Func<ItemSlot, bool> treatAsEmpty)
    {
        var probe = new DummySlot(source);
        if (!slot.CanHold(probe)) return 0;

        int stackLimit = Math.Min(source.Collectible.MaxStackSize, slot.MaxSlotStackSize);
        if (slot.Itemstack == null || treatAsEmpty(slot)) return stackLimit;
        return MergeClasses.Merges(slot.Itemstack, source) ? stackLimit : 0;
    }

    /// <summary>
    /// Numbers stacks by what the game would merge (<see cref="SourceSlot.Stack"/>): our keys ignore attributes the
    /// game does not (the config's ignored attributes), so two stacks of one key may still not go into one slot.
    /// Assumption: the game's rule is an equivalence, so comparing with one stack of each class is enough; vanilla's is
    /// Equals with GlobalConstants.IgnoredStackAttributes; behaviors and some items may change it (vssurvivalmod ItemEgg.cs:12).
    /// </summary>
    private sealed class MergeClasses
    {
        private readonly List<ItemStack> firsts = [];

        public int Of(ItemStack stack)
        {
            int i = firsts.FindIndex(first => Merges(first, stack));
            if (i >= 0) return i + 1;
            firsts.Add(stack);
            return firsts.Count;
        }

        /// <summary>
        /// Would the game put <paramref name="source"/> onto a stack of <paramref name="sink"/>'s item, given room? Asked of
        /// a single item, so that a full stack still answers (vsapi Collectible.cs:2558#GetMergableQuantity). An item that
        /// does not stack (MaxStackSize 1) merges with nothing, which is right too.
        /// </summary>
        public static bool Merges(ItemStack sink, ItemStack source)
        {
            var one = sink.Clone();
            one.StackSize = 1;
            return one.Collectible.GetMergableQuantity(one, source, EnumMergePriority.DirectMerge) > 0;
        }
    }
}
