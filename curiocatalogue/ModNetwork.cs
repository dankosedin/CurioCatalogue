using System;
using ExhaustiveMatching;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using CurioCatalogue.Adapters;
using CurioCatalogue.Core;

namespace CurioCatalogue;

/// <summary>Client → server: "make an invoice from this container with the ledger in my off-hand" (<see cref="EmptyHandGesture"/>).</summary>
[ProtoContract]
public class MakeInvoiceRequest
{
    [ProtoMember(1)] public int X;
    [ProtoMember(2)] public int Y;
    [ProtoMember(3)] public int Z;
    /// <summary>Ctrl was held: outgoing for a ledger; an album has no such action.</summary>
    [ProtoMember(4)] public bool Outgoing;
}

/// <summary>
/// The item a window request is about, as the window shows it: the hand, the hotbar slot it was opened from (main
/// hand only) and its data. The hotbar can switch while a window is open, so "the book in this hand" is not enough.
/// </summary>
[ProtoContract]
public class ShownItem
{
    [ProtoMember(1)] public bool OffHand;
    [ProtoMember(2)] public int HotbarSlot;
    /// <summary><see cref="LedgerStore.Fingerprint"/> of the shown data.</summary>
    [ProtoMember(3)] public int Fingerprint;
}

/// <summary>Client → server, from the ledger window: give the shown ledger a new name (empty for none).</summary>
[ProtoContract]
public class RenameLedgerRequest
{
    [ProtoMember(1)] public string Name = "";
    [ProtoMember(2)] public ShownItem? Book;
}

/// <summary>Client → server, from the ledger window: clear the entries of the shown ledger.</summary>
[ProtoContract]
public class ClearLedgerRequest
{
    [ProtoMember(1)] public ShownItem? Book;
}

/// <summary>Client → server, from the invoice window: cancel the shown invoice (main hand).</summary>
[ProtoContract]
public class CancelInvoiceRequest
{
    [ProtoMember(1)] public ShownItem? Invoice;
}

/// <summary>Client → server, from the album window: make a want list from the shown album.</summary>
[ProtoContract]
public class MakeWantListRequest
{
    [ProtoMember(1)] public ShownItem? Book;
}

/// <summary>
/// Server → client: posting is refused for lack of these items. The client names them: a name depends on the player's
/// language, and the server translates only into its own (checked on a dedicated server 27.10.2026).
/// </summary>
[ProtoContract]
public class MissingItemsRefusal
{
    /// <summary>The first lacking items, as <c>ItemStack.ToBytes</c>.</summary>
    [ProtoMember(1)] public List<byte[]> Stacks = [];
    [ProtoMember(2)] public List<int> Counts = [];
    /// <summary>How many more lacking items there are beyond those listed.</summary>
    [ProtoMember(3)] public int More;
}

/// <summary>
/// The mod's one network channel. Every message the client sends is a request: the server checks hands, reach
/// and access again, as the client may be out of date or lying.
/// </summary>
internal static class ModNetwork
{
    public const string Channel = CurioCatalogueModSystem.ModId;

    public const int MaxNameLength = 40;

    /// <summary>A book name as stored: trimmed, at most <see cref="MaxNameLength"/> chars, never ending in half a surrogate pair.</summary>
    internal static string LimitName(string? name)
    {
        name = (name ?? "").Trim();
        if (name.Length <= MaxNameLength) return name;
        int length = char.IsHighSurrogate(name[MaxNameLength - 1]) ? MaxNameLength - 1 : MaxNameLength;
        return name[..length].TrimEnd();
    }

    /// <summary>Both sides, in the same order: the channel and its message types must match.</summary>
    public static void Register(ICoreAPI api) =>
        api.Network.RegisterChannel(Channel)
            .RegisterMessageType<MakeInvoiceRequest>()
            .RegisterMessageType<RenameLedgerRequest>()
            .RegisterMessageType<ClearLedgerRequest>()
            .RegisterMessageType<CancelInvoiceRequest>()
            .RegisterMessageType<MakeWantListRequest>()
            .RegisterMessageType<MissingItemsRefusal>();

    public static void StartClient(ICoreClientAPI capi) =>
        capi.Network.GetChannel(Channel).SetMessageHandler<MissingItemsRefusal>(refusal =>
        {
            var text = MissingText(capi.World, refusal);
            // The game does not log in-game errors; this line is the only trace of the refusal.
            ModLog.Debug(capi, $"refused, missing: {text}");
            capi.TriggerIngameError(typeof(ModNetwork), "missing", text);
        });

    /// <summary>How many lacking items a refusal names; the rest are counted.</summary>
    public const int LackingShown = 3;

    public static void SendMissing(ICoreServerAPI sapi, IServerPlayer player, IReadOnlyList<Entry> lacking) =>
        sapi.Network.GetChannel(Channel).SendPacket(MissingRefusal(lacking), player);

    internal static MissingItemsRefusal MissingRefusal(IReadOnlyList<Entry> lacking)
    {
        var refusal = new MissingItemsRefusal { More = Math.Max(0, lacking.Count - LackingShown) };
        foreach (var entry in lacking.Take(LackingShown))
        {
            refusal.Stacks.Add(((ItemStack)entry.Sample).ToBytes());
            refusal.Counts.Add(entry.Count);
        }
        return refusal;
    }

    /// <summary>"Missing: 2 × Ambush, 1 × Nightfall …and 3 more", in the client's language.</summary>
    internal static string MissingText(IWorldAccessor world, MissingItemsRefusal refusal)
    {
        var lines = refusal.Stacks.Select((bytes, i) =>
        {
            var stack = new ItemStack(bytes);
            stack.ResolveBlockOrItem(world);
            return Lang.Get("curiocatalogue:invoice-info-line", refusal.Counts[i], ItemInvoice.NameOf(stack));
        });
        var text = string.Join(", ", lines);
        if (refusal.More > 0) text += " " + Lang.Get("curiocatalogue:invoice-info-more", refusal.More);
        return Lang.Get("curiocatalogue:ingameerror-missing", text);
    }

    public static void StartServer(ICoreServerAPI sapi, CurioCatalogueModSystem mod)
    {
        var channel = sapi.Network.GetChannel(Channel);
        channel.SetMessageHandler<MakeInvoiceRequest>((player, request) => EmptyHandGesture.OnRequest(sapi, player, request));
        channel.SetMessageHandler<RenameLedgerRequest>((player, request) => Rename(sapi, mod, player, request));
        channel.SetMessageHandler<ClearLedgerRequest>((player, request) => Clear(sapi, mod, player, request));
        channel.SetMessageHandler<CancelInvoiceRequest>((player, request) => Cancel(sapi, mod, player, request));
        channel.SetMessageHandler<MakeWantListRequest>((player, request) => MakeWantList(sapi, mod, player, request));
    }

    /// <summary>
    /// The slot that still holds the item the window showed: the off-hand, or the active hotbar slot if the window was
    /// opened from it; of the right kind and with the same data. Null if anything differs.
    /// </summary>
    internal static ItemSlot? ShownSlot(ShownItem? shown, ItemSlot? offhand, ItemSlot? active, int activeIndex, System.Func<CollectibleObject, bool> isKind)
    {
        if (shown == null) return null;
        var slot = shown.OffHand ? offhand : shown.HotbarSlot == activeIndex ? active : null;
        if (slot?.Itemstack is not { } stack || !isKind(stack.Collectible)) return null;
        return LedgerStore.Fingerprint(stack) == shown.Fingerprint ? slot : null;
    }

    /// <summary>
    /// The shown item's slot on the server; null, with the player told, if it is gone or its data is from a newer
    /// build of the mod.
    /// </summary>
    private static ItemSlot? Target(ICoreServerAPI sapi, IServerPlayer player, ShownItem? shown, System.Func<CollectibleObject, bool> isKind, string action)
    {
        var inventory = player.InventoryManager;
        var slot = ShownSlot(shown, player.Entity?.LeftHandItemSlot, inventory.ActiveHotbarSlot, inventory.ActiveHotbarSlotNumber, isKind);
        if (slot == null)
        {
            PlayerMessages.Refuse(player, "notshown");
            ModLog.Debug(sapi, $"{player.PlayerName}: {action} refused: the hand no longer holds the item shown");
            return null;
        }
        return PlayerMessages.RefuseNewer(player, slot.Itemstack) ? null : slot;
    }

    private static ItemSlot? LedgerSlot(ICoreServerAPI sapi, IServerPlayer player, ShownItem? shown, string action) =>
        Target(sapi, player, shown, c => c is ItemLedger, action);

    private static void Rename(ICoreServerAPI sapi, CurioCatalogueModSystem mod, IServerPlayer player, RenameLedgerRequest request)
    {
        if (LedgerSlot(sapi, player, request.Book, "rename") is not { } slot) return;

        string name = LimitName(request.Name);
        var item = (ItemLedger)slot.Itemstack!.Collectible;
        var ledger = mod.Store.ReadLedger(slot.Itemstack, item.LedgerMode);
        mod.Store.WriteLedger(slot.Itemstack, ledger with { Name = name });
        slot.MarkDirty();
        ModLog.Debug(sapi, $"{player.PlayerName}: ledger renamed to '{name}'");
    }

    private static void Clear(ICoreServerAPI sapi, CurioCatalogueModSystem mod, IServerPlayer player, ClearLedgerRequest request)
    {
        if (LedgerSlot(sapi, player, request.Book, "clear") is not { } slot) return;

        var item = (ItemLedger)slot.Itemstack!.Collectible;
        var ledger = mod.Store.ReadLedger(slot.Itemstack, item.LedgerMode);

        // An album starts over instead: every member missing again, sheets and name kept (domain.md, Albums and collections).
        if (ledger.Album is { } info)
        {
            if (mod.Collections.Get(info.CollectionId) is not { } collection)
            {
                PlayerMessages.Refuse(player, "nocollection");
                ModLog.Debug(sapi, $"{player.PlayerName}: restart refused: no collection {info.CollectionId}");
                return;
            }
            mod.Store.WriteLedger(slot.Itemstack, collection.NewAlbum(ledger.Sheets, ledger.Name));
            slot.MarkDirty();
            PlayerMessages.Tell(player, "album-restarted", collection.Members.Count);
            ModLog.Debug(sapi, $"{player.PlayerName}: album of {collection.Id} started over");
            return;
        }

        mod.Store.WriteLedger(slot.Itemstack, ledger.Clear());
        slot.MarkDirty();
        PlayerMessages.Tell(player, "ledger-cleared", ledger.Entries.Count);
        ModLog.Debug(sapi, $"{player.PlayerName}: ledger cleared, {ledger.Entries.Count} entries removed");
    }

    /// <summary>A want list from the album in that hand (domain.md, Albums and collections); into the empty main hand if it can.</summary>
    private static void MakeWantList(ICoreServerAPI sapi, CurioCatalogueModSystem mod, IServerPlayer player, MakeWantListRequest request)
    {
        if (Target(sapi, player, request.Book, c => c is ItemLedger { IsAlbum: true }, "want list") is not { } slot) return;

        var result = mod.InvoiceMaker.MakeWantList(slot.Itemstack!, player.Entity.Pos.AsBlockPos);
        ModLog.Debug(sapi, $"{player.PlayerName}: make want list: {result}");
        switch (result)
        {
            case MakeInvoiceResult.Refused refused:
                // Nothing missing reads better as "all collected" than as "no difference".
                PlayerMessages.Refuse(player, refused.Reason == MakeRefusal.NoDifference ? "allcollected" : refused.Reason.ToString().ToLowerInvariant());
                break;

            case MakeInvoiceResult.Made made:
                slot.MarkDirty();
                var hand = player.InventoryManager.ActiveHotbarSlot;
                if (hand.Empty)
                {
                    hand.Itemstack = made.InvoiceStack;
                    hand.MarkDirty();
                }
                else if (!player.InventoryManager.TryGiveItemstack(made.InvoiceStack, true))
                {
                    sapi.World.SpawnItemEntity(made.InvoiceStack, player.Entity.Pos.XYZ);
                }
                PlayerMessages.Tell(player, "wantlist-made", made.Invoice.Entries.Length, made.SheetsLeft, mod.Config.MaxSheets);
                break;

            default:
                throw ExhaustiveMatch.Failed(result);
        }
    }

    private static void Cancel(ICoreServerAPI sapi, CurioCatalogueModSystem mod, IServerPlayer player, CancelInvoiceRequest request)
    {
        // The invoice window opens only for the main hand.
        var shown = request.Invoice is { OffHand: false } ? request.Invoice : null;
        if (Target(sapi, player, shown, c => c is ItemInvoice, "cancel") is not { } hand) return;

        var offhand = player.Entity?.LeftHandItemSlot;
        if (PlayerMessages.RefuseNewer(player, offhand?.Itemstack)) return; // the sheet would go back into it
        bool toAlbum = offhand?.Itemstack?.Collectible is ItemLedger { IsAlbum: true };
        var fate = mod.InvoicePoster.Cancel(hand, offhand);
        PlayerMessages.Tell(player, fate switch
        {
            InvoiceFate.SheetReturned => toAlbum ? "invoice-cancelled-sheet-album" : "invoice-cancelled-sheet",
            InvoiceFate.Parchment => "invoice-cancelled-parchment",
            _ => "invoice-cancelled",
        });
        ModLog.Debug(sapi, $"{player.PlayerName}: invoice cancelled: {fate}");
    }
}
