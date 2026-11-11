using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using CurioCatalogue.Adapters;
using CurioCatalogue.Core;
using ExhaustiveMatching;

namespace CurioCatalogue;

/// <summary>
/// An invoice. Shift + right-click on a container posts it; right-click into the air shows it (domain.md, Posting an invoice; Windows).
/// Rollable like vanilla parchment: on the ground and on a scroll rack it shows its "rolledShape" as a scroll
/// (vssurvivalmod ItemRollable, IContainedMeshSource).
/// </summary>
public class ItemInvoice : ItemRollable
{
    private const int TooltipLines = 5;

    // From the code ("invoice-outgoing" → "outgoing"), not Variant: that is filled only when the game loads the item.
    public string Kind => LastCodePart();

    public InvoiceKind InvoiceKind => Kind switch
    {
        "outgoing" => InvoiceKind.Outgoing,
        "wantlist" => InvoiceKind.WantList,
        _ => InvoiceKind.Incoming,
    };

    private CurioCatalogueModSystem Mod => api.ModLoader.GetModSystem<CurioCatalogueModSystem>();

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        HeldPriorityInteract = true;
    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        // Right-click into the air (no block, no creature) opens the invoice window, on the client.
        if (firstEvent && blockSel == null && entitySel == null)
        {
            handling = EnumHandHandling.PreventDefault;
            if (api is Vintagestory.API.Client.ICoreClientAPI capi)
            {
                ModLog.Debug(api, $"open {Kind} invoice window");
                Gui.InvoiceDialog.Open(capi, Mod, slot);
            }
            return;
        }

        // Like the ledger: clicks that are not ours go to the base, so the game can put the invoice down.
        if (!firstEvent || !byEntity.Controls.ShiftKey || Mod.Containers.Classify(api.World, blockSel) == ContainerKind.None)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            return;
        }

        handling = EnumHandHandling.PreventDefault;
        // The client only claims the click; the server does the work and sends back the items and messages.
        if (api.Side != EnumAppSide.Server) return;
        if ((byEntity as EntityPlayer)?.Player is not IServerPlayer player || slot.Itemstack == null) return;

        var offhand = player.InventoryManager.OffhandHotbarSlot;
        if (PlayerMessages.RefuseNewer(player, slot.Itemstack, offhand?.Itemstack)) return;
        var result = Mod.InvoicePoster.Post(player, slot, InvoiceKind, blockSel!.Position, PlayerSlots(player), offhand);
        ModLog.Debug(api, $"{player.PlayerName}: post {InvoiceKind} invoice into {api.World.BlockAccessor.GetBlockEntity(Mod.Containers.MainPos(blockSel.Position))?.GetType().Name} at {blockSel.Position}, off-hand={(offhand?.Itemstack?.Collectible is ItemLedger l ? (l.IsAlbum ? "album" : "ledger") : "-")}: {Describe(result)}");
        Tell(player, result, offhand);
    }

    /// <summary>Where taken items may go: the hotbar without the off-hand, and the backpack (its bag slots take only bags).</summary>
    internal static List<ItemSlot> PlayerSlots(IServerPlayer player)
    {
        var inventory = player.InventoryManager;
        var offhand = inventory.OffhandHotbarSlot;
        return inventory.GetHotbarInventory().Where(s => s != offhand)
            .Concat(inventory.GetOwnInventory(GlobalConstants.backpackInvClassName) ?? Enumerable.Empty<ItemSlot>())
            .ToList();
    }

    private void Tell(IServerPlayer player, PostInvoiceResult result, ItemSlot? offhand)
    {
        switch (result)
        {
            case PostInvoiceResult.Refused refused:
                switch (refused.Reason)
                {
                    case PostRefusal.Missing:
                        // Named by the client, in the player's language (ModNetwork.MissingItemsRefusal).
                        ModNetwork.SendMissing((ICoreServerAPI)api, player, refused.Lacking!);
                        break;
                    case PostRefusal.NoRoom:
                        PlayerMessages.Refuse(player, "noroom", refused.SlotsShort);
                        break;
                    default:
                        PlayerMessages.Refuse(player, refused.Reason.ToString().ToLowerInvariant());
                        break;
                }
                break;

            case PostInvoiceResult.Posted posted:
                if (InvoiceKind == InvoiceKind.WantList)
                {
                    var told = PlayerMessages.Text(player, posted.Fate switch
                    {
                        InvoiceFate.Kept => "wantlist-posted",
                        InvoiceFate.Consumed => "wantlist-posted-consumed",
                        InvoiceFate.Parchment => "wantlist-posted-done",
                        // A want list's sheet goes back into no book (InvoiceFates.WantListPosted).
                        InvoiceFate.SheetReturned => throw ExhaustiveMatch.Failed(posted.Fate),
                        _ => throw ExhaustiveMatch.Failed(posted.Fate),
                    }, posted.Items, posted.ListLeft);
                    if (posted.NotFitted > 0) told += " " + PlayerMessages.Text(player, "notfitted", posted.NotFitted);
                    PlayerMessages.TellText(player, told);
                    break;
                }

                var text = PlayerMessages.Text(player, "invoice-posted", posted.Lines, posted.Items);
                if (posted.LedgerBefore != null)
                    text += " " + PlayerMessages.Text(player, "invoice-posted-ledger", posted.LedgerBefore, posted.LedgerAfter!);
                var book = offhand?.Itemstack?.Collectible as ItemLedger; // the book the sheet went back into, or would have
                if (posted.Fate == InvoiceFate.SheetReturned)
                    text += " " + PlayerMessages.Text(player, book is { IsAlbum: true } ? "invoice-posted-sheet-album" : "invoice-posted-sheet");
                if (posted.Fate == InvoiceFate.Parchment)
                    text += " " + PlayerMessages.Text(player, book switch
                    {
                        { IsAlbum: true } => "invoice-posted-parchment-full-album",
                        not null => "invoice-posted-parchment-full",
                        null => "invoice-posted-parchment",
                    }, Mod.Config.MaxSheets);
                PlayerMessages.TellText(player, text);
                if (posted.OverLimit)
                    PlayerMessages.Tell(player, "ledger-overlimit", posted.LedgerAfter!, Mod.Config.SoftEntryLimit);
                break;

            default:
                throw ExhaustiveMatch.Failed(result);
        }
    }

    private static string Describe(PostInvoiceResult result) => result switch
    {
        PostInvoiceResult.Posted p =>
            $"posted, {p.Lines} lines, {p.Items} items, {p.Fate}, ledger {p.LedgerBefore?.ToString() ?? "-"} -> {p.LedgerAfter?.ToString() ?? "-"}{(p.OverLimit ? ", over limit" : "")}{(p.NotFitted > 0 ? $", {p.NotFitted} did not fit" : "")}",
        PostInvoiceResult.Refused r => r.Reason switch
        {
            PostRefusal.Missing => $"refused, Missing: {string.Join(", ", r.Lacking!.Select(e => $"{e.Count} x {e.Key}"))}",
            PostRefusal.NoRoom => $"refused, NoRoom, {r.SlotsShort} slots short",
            _ => $"refused, {r.Reason}",
        },
        _ => throw ExhaustiveMatch.Failed(result),
    };

    /// <summary>The hint at the bottom of the screen while the invoice is in hand; posting shows over a container (LedgerHintsBehavior).</summary>
    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot) =>
    [
        new() { ActionLangCode = "curiocatalogue:heldhelp-open-" + Kind, MouseButton = EnumMouseButton.Right },
        .. base.GetHeldInteractionHelp(inSlot),
    ];

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        if (inSlot.Itemstack == null) return;

        var invoice = Mod.Store.ReadInvoice(inSlot.Itemstack, InvoiceKind);
        if (invoice.Entries.IsEmpty)
        {
            dsc.AppendLine(Lang.Get("curiocatalogue:invoice-info-blank"));
            return;
        }

        // Coordinates as the player sees them: relative to the world spawn, like the game's own position display.
        var spawn = world.DefaultSpawnPosition.AsBlockPos;
        var pos = invoice.Position;
        dsc.AppendLine(Lang.Get("curiocatalogue:invoice-info-origin", (int)invoice.CreatedTotalDays, pos.X - spawn.X, pos.Y, pos.Z - spawn.Z));

        var wantedIn = Mod.Store.WantedIn(invoice);
        foreach (var line in invoice.Entries.Take(TooltipLines))
            dsc.AppendLine(Lang.Get("curiocatalogue:invoice-info-line", line.Count, LineName(wantedIn, line)));
        if (invoice.Entries.Length > TooltipLines)
            dsc.AppendLine(Lang.Get("curiocatalogue:invoice-info-more", invoice.Entries.Length - TooltipLines));
    }

    /// <summary>
    /// What a want list's line names: its member by its values ("Native copper, any rock", <see cref="MemberNames.NameOr"/>), or
    /// with no value named the item it shows and what it leaves free. The item alone for a line of format 1 or of a collection the
    /// game does not have: the line then wants exactly that item.
    /// </summary>
    internal static string LineName((DefinedCollection Collection, CollectionLevel Level)? wantedIn, Entry line)
    {
        string itemName = NameOf((ItemStack)line.Sample);
        return Wanted(wantedIn, line) is var (collection, level, member) ? MemberNames.NameOr(collection, level, member, itemName) : itemName;
    }

    /// <summary>What a want list's slot says under the item's own text in the window: its member's name, or with no value named
    /// only what it leaves free ("any of 3 kinds"); null when neither (and as <see cref="LineName"/>).</summary>
    internal static string? SlotLine((DefinedCollection Collection, CollectionLevel Level)? wantedIn, Entry line) =>
        Wanted(wantedIn, line) is var (collection, level, member)
            ? MemberNames.Name(collection, level, member) ?? MemberNames.Freedom(collection, level, member)
            : null;

    /// <summary>What a want list's slot shows in the window, in turn (L7 of the album polish plan): its member's items, the line's
    /// own first (the want list is made with the member's first, <see cref="Album.MakeWantList"/>); one stack, the line's own, when
    /// the member has one item or the line names none. An item of a removed mod is left out: it cannot be drawn. Stack sizes are
    /// the window's to set.</summary>
    public static IReadOnlyList<ItemStack> SlotStacks((DefinedCollection Collection, CollectionLevel Level)? wantedIn, Entry line)
    {
        var stacks = Wanted(wantedIn, line) is var (_, _, member)
            ? member.Stacks.OfType<ItemStack>().Where(s => s.Collectible != null).ToList()
            : [];
        return stacks.Count > 1 ? stacks : [(ItemStack)line.Sample];
    }

    private static (DefinedCollection, CollectionLevel, Member)? Wanted((DefinedCollection Collection, CollectionLevel Level)? wantedIn, Entry line) =>
        line.Member == null || wantedIn is not var (collection, level) || level.Find(line.Member) is not { } member
            ? null : (collection, level, member);

    // An item of a removed mod has no block or item to ask for a name.
    internal static string NameOf(ItemStack stack) =>
        stack.Collectible == null ? Lang.Get("curiocatalogue:unknown-item", stack.Id) : stack.GetName();
}
