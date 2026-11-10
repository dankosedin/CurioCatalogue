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
        {
            string name = NameOf((ItemStack)line.Sample);
            if (AfterName(wantedIn, line) is { } more) name = Lang.Get("curiocatalogue:invoice-info-member-free", name, more);
            dsc.AppendLine(Lang.Get("curiocatalogue:invoice-info-line", line.Count, name));
        }
        if (invoice.Entries.Length > TooltipLines)
            dsc.AppendLine(Lang.Get("curiocatalogue:invoice-info-more", invoice.Entries.Length - TooltipLines));
    }

    /// <summary>
    /// What a want list's line says after the item's name: the member's values its item's name does not say ("Basalt", by
    /// <see cref="Listed"/>), then what it leaves free ("any rock", by <see cref="Freedom(DefinedCollection, CollectionLevel, Member)"/>);
    /// null when neither (or for a line of format 1, or of a collection the game does not have: the line then wants exactly the
    /// item it shows).
    /// </summary>
    internal static string? AfterName((DefinedCollection Collection, CollectionLevel Level)? wantedIn, Entry line)
    {
        if (line.Member == null || wantedIn is not var (collection, level) || level.Find(line.Member) is not { } member) return null;
        string[] parts = [.. new[] { Listed(collection, level, member), Freedom(collection, level, member) }.OfType<string>()];
        return parts.Length == 0 ? null : string.Join(", ", parts);
    }

    /// <summary>What a want list's line leaves free ("any rock"), for its slot's tooltip in the window: the item shown is one of
    /// those that fill the line, and its own text names its rock. Null when the line leaves nothing free.</summary>
    internal static string? FreeOf((DefinedCollection Collection, CollectionLevel Level)? wantedIn, Entry line) =>
        line.Member == null || wantedIn is not var (collection, level) || level.Find(line.Member) is not { } member
            ? null : Freedom(collection, level, member);

    /// <summary>A member's name parts in the dimensions its level does not keep, by their names ("Basalt"); null when none. The want
    /// list's line (its sheet's text; the window's slots show the item's own text, which names its rock).
    /// Until L1b names the whole member (docs/plans/2026-11-09-album-polish.md).</summary>
    internal static string? Listed(DefinedCollection collection, CollectionLevel level, Member member)
    {
        var listed = collection.NameParts(level, member)
            .Where(p => !level.Definition.Keep.Contains(p.Dimension.Code))
            .Select(p => Lang.GetMatchingIfExists(p.Dimension.ValueNameKey(p.Value)!))
            .OfType<string>().ToList();
        return listed.Count == 0 ? null : string.Join(", ", listed);
    }

    /// <summary>What a member leaves free, as in a want list's line; null for a member of one item (the album window's tooltip).</summary>
    internal static string? Freedom(DefinedCollection collection, CollectionLevel level, Member member)
    {
        var free = collection.Free(level, member);
        if (free.Count == 0) return null;
        if (free.Any(d => d.AnyKey == null)) return Lang.Get("curiocatalogue:invoice-info-any-of", member.Fillers.Count);
        return string.Join(", ", free.Select(d => Lang.Get(d.AnyKey!)));
    }

    // An item of a removed mod has no block or item to ask for a name.
    internal static string NameOf(ItemStack stack) =>
        stack.Collectible == null ? Lang.Get("curiocatalogue:unknown-item", stack.Id) : stack.GetName();
}
