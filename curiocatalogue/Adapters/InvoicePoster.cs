using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using CurioCatalogue.Core;

namespace CurioCatalogue.Adapters;

public enum PostRefusal { Unsupported, NoAccess, Missing, NoRoom, NothingFromList }

/// <summary>What became of the invoice after posting (domain.md, Posting an invoice).</summary>
public enum InvoiceFate
{
    /// <summary>The ledger in the off-hand took its sheet back; the invoice is gone.</summary>
    SheetReturned,

    /// <summary>No ledger in the off-hand, or it is full of sheets: the invoice is now parchment.</summary>
    Parchment,

    /// <summary>Hardcore option ConsumeSheets: the invoice is gone, no sheet comes back.</summary>
    Consumed,

    /// <summary>A want list with lines still left: it stays in the hand, shorter (domain.md, Albums and collections).</summary>
    Kept,
}

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
/// the whole transaction is planned and checked first (<see cref="Transfer"/>), and only then carried out.
/// A want list and an album (<see cref="Collect"/>) take as much as fits instead, line by line.
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
        var (sources, samples) = Sources(container);
        if (kind == InvoiceKind.WantList)
            return PostWantList(player, invoiceSlot, invoice, container, sources, samples, playerSlots);

        // What will become of the ledger and the invoice: decides whether the invoice's slot counts as free.
        var lim = limits();
        // An album in the off-hand is left alone: it changes only by taking new items (domain.md, Posting an invoice; Albums and collections).
        var ledgerStack = offhandSlot?.Itemstack?.Collectible is ItemLedger { IsAlbum: false } ? offhandSlot.Itemstack : null;
        var ledger = ledgerStack != null ? store.ReadLedger(ledgerStack, ((ItemLedger)ledgerStack.Collectible).LedgerMode) : null;
        var posted = ledger?.Post(invoice, lim);
        var withSheet = posted != null && !consumeSheets() ? posted.Ledger.ReturnSheet(lim) : null;
        var fate = consumeSheets() ? InvoiceFate.Consumed : withSheet != null ? InvoiceFate.SheetReturned : InvoiceFate.Parchment;
        bool invoiceSlotFrees = fate is InvoiceFate.SheetReturned or InvoiceFate.Consumed;

        var targets = Targets(playerSlots, invoiceSlot, invoiceSlotFrees);
        int Capacity(int target, ItemKey key) => CapacityOf(playerSlots[target], samples[key], slot => slot == invoiceSlot && invoiceSlotFrees);
        int MaxStack(ItemKey key) => Math.Max(1, samples[key].Collectible.MaxStackSize);

        var plan = Transfer.Plan(invoice.Entries, sources, targets, Capacity, MaxStack);
        switch (plan)
        {
            case TransferPlan.Missing missing:
                return new PostInvoiceResult.Refused(PostRefusal.Missing, Lacking: missing.Lacking);
            case TransferPlan.NoRoom noRoom:
                return new PostInvoiceResult.Refused(PostRefusal.NoRoom, SlotsShort: noRoom.SlotsShort);
        }

        // Carry it out. The invoice leaves its slot first when that slot is planned to take items.
        if (invoiceSlotFrees) invoiceSlot.Itemstack = null;
        int items = Carry(player, ((TransferPlan.Ready)plan).Moves, container, playerSlots);

        if (ledgerStack != null && posted != null)
        {
            store.WriteLedger(ledgerStack, withSheet ?? posted.Ledger);
            offhandSlot!.MarkDirty();
        }
        if (fate == InvoiceFate.Parchment) invoiceSlot.Itemstack = Parchment();
        invoiceSlot.MarkDirty();

        return new PostInvoiceResult.Posted(invoice.Entries.Length, items, fate,
            ledger?.Entries.Count, posted?.Ledger.Entries.Count, posted?.OverLimit ?? false);
    }

    /// <summary>
    /// A want list takes what the container has of its list, as much as fits, and keeps the rest
    /// (domain.md, Albums and collections). It touches no ledger and becomes parchment once its list is done.
    /// </summary>
    private PostInvoiceResult PostWantList(IPlayer player, ItemSlot invoiceSlot, Invoice invoice, ContainerSlots container,
        List<SourceSlot> sources, Dictionary<ItemKey, ItemStack> samples, IReadOnlyList<ItemSlot> playerSlots)
    {
        var (lines, _) = invoice.SplitBy(samples.Keys.ToHashSet());
        if (lines.IsEmpty) return new PostInvoiceResult.Refused(PostRefusal.NothingFromList);

        var plan = PlanWhatFits(lines, sources, samples, playerSlots, invoiceSlot);
        if (plan.Taken.Count == 0)
            return new PostInvoiceResult.Refused(PostRefusal.NoRoom, SlotsShort: SlotsShort(lines, sources, samples, playerSlots, invoiceSlot));

        int items = Carry(player, plan.Moves, container, playerSlots);
        var listLeft = invoice.CrossOff(plan.Taken.Select(e => e.Key));
        var fate = listLeft.Entries.IsEmpty ? InvoiceFate.Parchment : InvoiceFate.Kept;
        if (fate == InvoiceFate.Kept) store.WriteInvoice(invoiceSlot.Itemstack!, listLeft);
        else invoiceSlot.Itemstack = Parchment();
        invoiceSlot.MarkDirty();

        return new PostInvoiceResult.Posted(plan.Taken.Count, items, fate, null, null, false, listLeft.Entries.Length, plan.Left.Count);
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
        var (sources, samples) = Sources(container);
        var album = store.ReadLedger(albumSlot.Itemstack!, LedgerMode.Album);
        var wanted = album.Entries.Values.Where(e => samples.ContainsKey(e.Key)).Select(e => e.WithCount(1));
        var lines = (store.CollectionOf(album)?.InOrder(wanted) ?? wanted.OrderBy(e => e.Key.ToString(), StringComparer.Ordinal)).ToList();
        if (lines.Count == 0) return new CollectResult.Refused(CollectRefusal.NothingNew);

        var plan = PlanWhatFits(lines, sources, samples, playerSlots, handSlot);
        if (plan.Taken.Count == 0)
            return new CollectResult.Refused(CollectRefusal.NoRoom, SlotsShort(lines, sources, samples, playerSlots, handSlot));

        int items = Carry(player, plan.Moves, container, playerSlots);
        var after = album.CrossOff(plan.Taken.Select(e => e.Key));
        store.WriteLedger(albumSlot.Itemstack!, after);
        albumSlot.MarkDirty();
        return new CollectResult.Collected(items, plan.Left.Count, after.Entries.Count);
    }

    /// <summary>
    /// Cancels an invoice of any kind (domain.md, Cancelling): no items move, the ledger's entries stay; the sheet goes back
    /// into the ledger or album in the off-hand, else the invoice becomes parchment; ConsumeSheets: gone.
    /// </summary>
    public InvoiceFate Cancel(ItemSlot invoiceSlot, ItemSlot? offhandSlot)
    {
        var ledgerStack = offhandSlot?.Itemstack?.Collectible is ItemLedger ledgerItem ? offhandSlot.Itemstack : null;
        Ledger? withSheet = null;
        if (ledgerStack != null && !consumeSheets())
            withSheet = store.ReadLedger(ledgerStack, ((ItemLedger)ledgerStack.Collectible).LedgerMode).ReturnSheet(limits());

        var fate = consumeSheets() ? InvoiceFate.Consumed : withSheet != null ? InvoiceFate.SheetReturned : InvoiceFate.Parchment;
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
    private (List<SourceSlot> Sources, Dictionary<ItemKey, ItemStack> Samples) Sources(ContainerSlots container)
    {
        var sources = new List<SourceSlot>();
        var samples = new Dictionary<ItemKey, ItemStack>();
        for (int i = 0; i < container.Slots.Count; i++)
        {
            if (container.Slots[i].Itemstack is not { StackSize: > 0 } stack) continue;
            var key = keys.KeyOf(stack);
            sources.Add(new SourceSlot(i, key, stack.StackSize));
            samples.TryAdd(key, stack);
        }
        return (sources, samples);
    }

    /// <summary>
    /// The player's side. <paramref name="last"/> (the main hand) comes last, so the hand stays empty when there is
    /// room elsewhere; with <paramref name="lastFrees"/> it counts as empty (the invoice in it is going away).
    /// </summary>
    private List<TargetSlot> Targets(IReadOnlyList<ItemSlot> playerSlots, ItemSlot? last, bool lastFrees) =>
        playerSlots.Select((slot, i) =>
                slot.Itemstack == null || (slot == last && lastFrees)
                    ? new TargetSlot(i, null, 0)
                    : new TargetSlot(i, keys.KeyOf(slot.Itemstack), slot.Itemstack.StackSize))
            .OrderBy(t => playerSlots[t.Index] == last)
            .ToList();

    /// <summary>As much of the list as fits; no player slot is freed.</summary>
    private PartialPlan PlanWhatFits(IEnumerable<Entry> lines, List<SourceSlot> sources, Dictionary<ItemKey, ItemStack> samples,
        IReadOnlyList<ItemSlot> playerSlots, ItemSlot? last) =>
        Transfer.PlanWhatFits(lines, sources, Targets(playerSlots, last, lastFrees: false),
            (target, key) => CapacityOf(playerSlots[target], samples[key], _ => false));

    /// <summary>How many more free slots would take the whole list: for the message when nothing fits.</summary>
    private int SlotsShort(IEnumerable<Entry> lines, List<SourceSlot> sources, Dictionary<ItemKey, ItemStack> samples,
        IReadOnlyList<ItemSlot> playerSlots, ItemSlot? last) =>
        Transfer.Plan(lines, sources, Targets(playerSlots, last, lastFrees: false),
            (target, key) => CapacityOf(playerSlots[target], samples[key], _ => false),
            key => Math.Max(1, samples[key].Collectible.MaxStackSize)) is TransferPlan.NoRoom noRoom ? noRoom.SlotsShort : 1;

    /// <summary>Carries out the planned moves; returns how many items moved.</summary>
    private int Carry(IPlayer player, IReadOnlyList<Move> moves, ContainerSlots container, IReadOnlyList<ItemSlot> playerSlots)
    {
        int items = 0;
        foreach (var move in moves)
        {
            var from = container.Slots[move.Source];
            var to = playerSlots[move.Target];
            var op = new ItemStackMoveOperation(world, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, move.Amount)
            {
                ActingPlayer = player,
            };
            int moved = from.TryPutInto(to, ref op);
            items += moved;
            if (moved != move.Amount)
            {
                // The plan follows the game's own checks, so this should not happen. Nothing is lost: what did not
                // move stays in the container. Always logged, it is a bug worth knowing about.
                world.Logger.Error("[CurioCatalogue] Planned to move {0} of {1}, moved {2}", move.Amount, from.Itemstack?.GetName() ?? "an item", moved);
            }
            container.Save(from);
            to.MarkDirty();
        }
        return items;
    }

    private ItemStack Parchment() =>
        new(world.GetItem(ParchmentCode) ?? throw new InvalidOperationException($"No item {ParchmentCode}"));

    /// <summary>
    /// How many of the sample's item the player slot can hold, following the game's own rules: CanHold (storage
    /// flags of bags), the slot's own stack limit, and for an occupied slot whether the game would merge at all
    /// (vsapi ItemSlot.cs:137#CanHold, ItemSlot.cs:110#CanTakeFrom, Collectible.cs:2558#Equals for merging).
    /// </summary>
    private static int CapacityOf(ItemSlot slot, ItemStack sample, System.Func<ItemSlot, bool> treatAsEmpty)
    {
        var probe = new DummySlot(sample);
        if (!slot.CanHold(probe)) return 0;

        int stackLimit = Math.Min(sample.Collectible.MaxStackSize, slot.MaxSlotStackSize);
        if (slot.Itemstack == null || treatAsEmpty(slot)) return stackLimit;
        return sample.Collectible.Equals(slot.Itemstack, sample, GlobalConstants.IgnoredStackAttributes) ? stackLimit : 0;
    }
}
