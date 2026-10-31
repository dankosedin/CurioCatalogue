using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using CurioCatalogue.Adapters;
using CurioCatalogue.Core;
using CurioCatalogue.Gui;
using ExhaustiveMatching;

namespace CurioCatalogue;

/// <summary>
/// The ledger book, and the album (domain.md, Albums and collections).
/// Shift + right-click on a container creates an incoming invoice, Shift + Ctrl + right-click an outgoing one;
/// an album has only Shift + right-click, which takes the new items for its collection straight away (domain.md, Albums and collections).
/// Right-click into the air opens the window.
/// </summary>
public class ItemLedger : Item
{
    // From the code ("book-materials-normal-teal" → "materials", "normal-teal"), not Variant: that is filled only when
    // the game loads the item.
    /// <summary>Which book: "ledger", "materials" (bill of materials) or "album" (domain.md, Books).</summary>
    public string Kind => CodeParts.Length > 1 ? CodeParts[1] : "";

    /// <summary>The cover, a vanilla book texture (domain.md, Covers): "normal-teal", "aged-orange".</summary>
    public string Cover => CodeParts.Length > 2 ? string.Join("-", CodeParts[2..]) : "";

    private string[] CodeParts => Code?.Path?.Split('-') ?? [];

    public bool IsAlbum => Kind == "album";

    public LedgerMode LedgerMode => Kind switch
    {
        "album" => LedgerMode.Album,
        "materials" => LedgerMode.Counts,
        _ => LedgerMode.Types,
    };

    /// <summary>
    /// Which invoice a click makes: Shift incoming, Shift + Ctrl outgoing. An album acts only on Shift (taking the new
    /// items, <see cref="UseOnContainer"/>); Shift + Ctrl does nothing (domain.md, Albums and collections).
    /// </summary>
    internal bool KindFor(bool ctrl, out InvoiceKind kind)
    {
        kind = IsAlbum || ctrl ? InvoiceKind.Outgoing : InvoiceKind.Incoming;
        return !(IsAlbum && ctrl);
    }

    /// <summary>An album's progress against its collection as the game has it now; null for a ledger.</summary>
    internal CollectionProgress? ProgressOf(Ledger ledger) =>
        ledger.Album is { } info && Mod.Collections.Get(info.CollectionId) is { } collection
            ? collection.Progress(ledger.Entries.Keys)
            : null;

    private CurioCatalogueModSystem Mod => api.ModLoader.GetModSystem<CurioCatalogueModSystem>();

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        // Lets Shift + right-click reach the book before the container opens.
        HeldPriorityInteract = true;
    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        // Right-click into the air (no block, no creature) opens the ledger window, on the client.
        if (firstEvent && blockSel == null && entitySel == null)
        {
            handling = EnumHandHandling.PreventDefault;
            if (api is ICoreClientAPI capi && slot.Itemstack != null)
            {
                ModLog.Debug(api, $"open ledger window, kind={Kind}");
                LedgerDialog.Open(capi, Mod, slot, offHand: false);
            }
            return;
        }

        // Clicks that are not ours go to the base, which runs the item's behaviors: GroundStorable puts the book
        // down on Shift + right-click (vsapi Collectible.cs:1625#OnHeldInteractStart). Without Shift the click belongs to the block
        // anyway (chests open). Blocks without an inventory are not ours either. Both sides decide the same,
        // since the client has the block entities too. An album has no Ctrl action, so that click is the game's as well
        // (domain.md, Albums and collections), as from the off-hand.
        if (!firstEvent || !byEntity.Controls.ShiftKey || !TargetsInventory(blockSel) || !KindFor(byEntity.Controls.CtrlKey, out var kind))
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            return;
        }

        handling = EnumHandHandling.PreventDefault;
        // The client only claims the click; the server does the work and sends back the items and messages.
        if (api.Side != EnumAppSide.Server) return;
        if ((byEntity as EntityPlayer)?.Player is not IServerPlayer player || slot.Itemstack == null) return;
        UseOnContainer(player, slot, kind, blockSel!.Position, intoSlot: null);
    }

    /// <summary>
    /// The ledger in <paramref name="ledgerSlot"/> used on the container at <paramref name="pos"/>: a ledger makes an
    /// invoice, an album takes the new items for its collection straight away (domain.md, Albums and collections). Tells the player.
    /// Server only. Shared by both gestures: the ledger in the main hand, and the ledger in the off-hand with an
    /// empty main hand (<see cref="EmptyHandGesture"/>).
    /// </summary>
    /// <param name="intoSlot">The empty main hand, for the new invoice; null for the ledger in the main hand.</param>
    internal void UseOnContainer(IServerPlayer player, ItemSlot ledgerSlot, InvoiceKind kind, BlockPos pos, ItemSlot? intoSlot)
    {
        if (ledgerSlot.Itemstack == null || PlayerMessages.RefuseNewer(player, ledgerSlot.Itemstack)) return;
        if (IsAlbum)
        {
            Collect(player, ledgerSlot, pos, handSlot: intoSlot ?? ledgerSlot);
            return;
        }
        var result = Mod.InvoiceMaker.Make(player, ledgerSlot.Itemstack, LedgerMode, kind, pos);
        ModLog.Debug(api, $"{player.PlayerName}: make {kind} invoice from {api.World.BlockAccessor.GetBlockEntity(Mod.Containers.MainPos(pos))?.GetType().Name} at {pos}, mode={LedgerMode}, into {(intoSlot == null ? "inventory" : "hand")}: {Describe(result)}");

        switch (result)
        {
            case MakeInvoiceResult.Refused refused:
                // "No difference" in the player's words depends on the invoice: nothing the book lacks, or nothing from it.
                var code = refused.Reason == MakeRefusal.NoDifference
                    ? "nodifference-" + (kind == InvoiceKind.Incoming ? "incoming" : "outgoing")
                    : refused.Reason.ToString().ToLowerInvariant();
                PlayerMessages.Refuse(player, code, Mod.Config.SoftEntryLimit);
                break;

            case MakeInvoiceResult.Made made:
                ledgerSlot.MarkDirty();
                if (intoSlot is { Empty: true })
                {
                    // Straight into the empty hand: the next Shift + right-click posts it.
                    intoSlot.Itemstack = made.InvoiceStack;
                    intoSlot.MarkDirty();
                }
                else if (!player.InventoryManager.TryGiveItemstack(made.InvoiceStack, true))
                {
                    api.World.SpawnItemEntity(made.InvoiceStack, player.Entity.Pos.XYZ);
                }
                PlayerMessages.Tell(player, "invoice-made",
                    PlayerMessages.ItemName(player, made.InvoiceStack), made.Invoice.Entries.Length, made.SheetsLeft, Mod.Config.MaxSheets);
                break;

            default:
                throw ExhaustiveMatch.Failed(result);
        }
    }

    private void Collect(IServerPlayer player, ItemSlot albumSlot, BlockPos pos, ItemSlot handSlot)
    {
        var result = Mod.InvoicePoster.Collect(player, albumSlot, pos, ItemInvoice.PlayerSlots(player), handSlot);
        ModLog.Debug(api, $"{player.PlayerName}: collect from {api.World.BlockAccessor.GetBlockEntity(Mod.Containers.MainPos(pos))?.GetType().Name} at {pos}, album in {(albumSlot == handSlot ? "hand" : "off-hand")}: {result}");

        switch (result)
        {
            case CollectResult.Refused refused:
                if (refused.Reason == CollectRefusal.NoRoom) PlayerMessages.Refuse(player, "noroom", refused.SlotsShort);
                else PlayerMessages.Refuse(player, refused.Reason.ToString().ToLowerInvariant());
                break;
            case CollectResult.Collected collected:
                var text = collected.MissingLeft == 0
                    ? PlayerMessages.Text(player, "album-collected-done", collected.Taken)
                    : PlayerMessages.Text(player, "album-collected", collected.Taken, collected.MissingLeft);
                if (collected.NotFitted > 0) text += " " + PlayerMessages.Text(player, "notfitted", collected.NotFitted);
                PlayerMessages.TellText(player, text);
                break;

            default:
                throw ExhaustiveMatch.Failed(result);
        }
    }

    private bool TargetsInventory(BlockSelection? blockSel) => Mod.Containers.Classify(api.World, blockSel) != ContainerKind.None;

    // Our hints are shown over the container, not at the bottom of the screen: see LedgerHintsBehavior.

    private static string Describe(MakeInvoiceResult result) => result switch
    {
        MakeInvoiceResult.Made made => $"made, {made.Invoice.Entries.Length} lines, {made.SheetsLeft} sheets left",
        MakeInvoiceResult.Refused refused => $"refused, {refused.Reason}",
        _ => throw ExhaustiveMatch.Failed(result),
    };

    // --- Crafting (domain.md, Recipes; recipes in recipes/grid/ledger.json, told apart by their name) ---

    private const string FallbackRecipe = "ledger-fallback";
    private const string RestoreRecipe = "ledger-restore";
    private const string SwitchModeRecipe = "ledger-switchmode";
    private const string RechargeRecipe = "ledger-recharge";
    private const string AlbumRecipe = "ledger-album";
    private const string RecolorRecipe = "ledger-recolor";

    /// <summary>Sheets of a new ledger (domain.md, Recipes).</summary>
    private const int FallbackSheets = 8;
    private const int RestoreSheets = 32;

    private static bool Is(IRecipeBase recipe, string name) => recipe.Name?.Path == name;

    private Ledger Read(ItemStack stack) => Mod.Store.ReadLedger(stack, ((ItemLedger)stack.Collectible).LedgerMode);

    /// <summary>
    /// Called for the ledger lying in the grid as an ingredient. Only an empty ledger switches its mode; a ledger
    /// full of sheets takes no parchment, so the recharge recipe does not show.
    /// </summary>
    public override bool MatchesForCrafting(ItemStack inputStack, IRecipeBase recipe, IRecipeIngredient ingredient)
    {
        if (!base.MatchesForCrafting(inputStack, recipe, ingredient)) return false;
        // Data from a newer build of the mod: our recipes would rewrite it without what this build does not know.
        if (Adapters.LedgerStore.IsNewer(inputStack)) return false;
        if (Is(recipe, SwitchModeRecipe)) return Read(inputStack) is { IsEmpty: true, Album: null };
        if (Is(recipe, RechargeRecipe)) return Read(inputStack).Sheets < Mod.Config.MaxSheets;
        // An album is made from an empty ledger (domain.md, Recipes).
        if (Is(recipe, AlbumRecipe)) return Read(inputStack).IsEmpty;
        // Dyed leather for the colour the book already has would only be wasted.
        if (Is(recipe, RecolorRecipe)) return recipe.RecipeOutput?.ResolvedItemStack?.Collectible is ItemLedger output && output.Cover != Cover;
        return true;
    }

    /// <summary>
    /// Called for the ledger in the output slot. A new ledger gets its sheets here, not from attributes in the recipe's
    /// output: with attributes there the handbook cannot tie the recipe to the ledger's page. For recharging, the
    /// recipe copied the ingredient ledger's attributes, and this adds as many sheets as the parchment stack gives.
    /// </summary>
    public override void OnCreatedByCrafting(ItemSlot[] allInputSlots, ItemSlot outputSlot, IRecipeBase byRecipe)
    {
        base.OnCreatedByCrafting(allInputSlots, outputSlot, byRecipe);
        if (outputSlot.Itemstack is not { } output) return;

        // The handbook builds recipe outputs in a DummySlot and calls this too (vssurvivalmod
        // CollectibleBehaviorHandbookTextAndExtraInfo.cs:1128-1141#DummySlot). It links to a page by code and attributes
        // (GuiHandbookItemStackPage.PageCodeForStack), so an output with our data would link to no page. Real
        // crafting uses the grid's own output slot.
        if (outputSlot is DummySlot) return;

        int newSheets = Is(byRecipe, FallbackRecipe) ? FallbackSheets : Is(byRecipe, RestoreRecipe) ? RestoreSheets : 0;
        if (newSheets > 0)
        {
            Mod.Store.WriteLedger(output, Ledger.Empty(LedgerMode, System.Math.Min(newSheets, Mod.Config.MaxSheets)));
            return;
        }

        if (Is(byRecipe, AlbumRecipe))
        {
            MakeAlbum(allInputSlots, outputSlot);
            return;
        }

        if (!Is(byRecipe, RechargeRecipe) || RechargeSlots(allInputSlots) is not { } found) return;
        var (charged, _) = Read(output).AddSheets(found.Paper.StackSize, Mod.Limits);
        Mod.Store.WriteLedger(output, charged);
    }

    /// <summary>
    /// The album of the sample's collection, every member missing; sheets and name come from the ledger it is made
    /// from (the recipe copied them). An album needs no recipe to catch up with its collection: it does so by itself
    /// on every read (LedgerStore, Collection.RefreshIfStale). No collection for the sample (should not happen): no output.
    /// </summary>
    private void MakeAlbum(ItemSlot[] inputSlots, ItemSlot outputSlot)
    {
        var sample = System.Array.Find(inputSlots, s => s.Itemstack is { } st && st.Collectible is not ItemLedger)?.Itemstack;
        var collection = sample == null ? null : Mod.Collections.ForSample(sample);
        if (collection == null)
        {
            outputSlot.Itemstack = null;
            return;
        }
        var from = Read(outputSlot.Itemstack!);
        Mod.Store.WriteLedger(outputSlot.Itemstack!, collection.NewAlbum(from.Sheets, from.Name));
        ModLog.Debug(api, $"album of {collection.Id} made, {collection.Members.Count} entries");
    }

    /// <summary>Recharging uses exactly the sheets added, not the one parchment the recipe names; the rest stays.</summary>
    public override bool ConsumeCraftingIngredients(ItemSlot[] slots, ItemSlot outputSlot, IRecipeBase matchingRecipe)
    {
        if (!Is(matchingRecipe, RechargeRecipe) || outputSlot.Itemstack == null || RechargeSlots(slots) is not { } found)
            return base.ConsumeCraftingIngredients(slots, outputSlot, matchingRecipe);
        var (ledgerSlot, paper) = found;

        int used = Read(outputSlot.Itemstack).Sheets - Read(ledgerSlot.Itemstack!).Sheets;
        ledgerSlot.TakeOut(1);
        paper.TakeOut(System.Math.Max(0, used));
        ledgerSlot.MarkDirty();
        paper.MarkDirty();
        ModLog.Debug(api, $"ledger recharged with {used} sheets");
        return true;
    }

    /// <summary>The grid slots holding the ledger and the parchment of a recharge recipe.</summary>
    private static (ItemSlot Ledger, ItemSlot Paper)? RechargeSlots(ItemSlot[] slots)
    {
        var ledger = System.Array.Find(slots, s => s.Itemstack?.Collectible is ItemLedger);
        var paper = System.Array.Find(slots, s => s.Itemstack?.Collectible?.Code == Adapters.InvoicePoster.ParchmentCode);
        return ledger != null && paper != null ? (ledger, paper) : null;
    }

    /// <summary>
    /// A named ledger shows its name instead of "Ledger"; the kind stays in the tooltip. An unnamed
    /// album shows its collection, "Album: tapestries", in the viewer's language (so no default name is stored).
    /// </summary>
    public override string GetHeldItemName(ItemStack itemStack)
    {
        string name = LedgerStore.ReadName(itemStack);
        if (name.Length > 0) return name;
        if (IsAlbum && LedgerStore.ReadCollectionId(itemStack) is { Length: > 0 } id)
            return Lang.Get("curiocatalogue:album-name-" + id);
        return base.GetHeldItemName(itemStack);
    }

    /// <summary>The hint at the bottom of the screen while the book is in hand; the gestures on a container show over it (LedgerHintsBehavior).</summary>
    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot) =>
    [
        new() { ActionLangCode = "curiocatalogue:heldhelp-open-" + Kind, MouseButton = EnumMouseButton.Right },
        .. base.GetHeldInteractionHelp(inSlot),
    ];

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        if (inSlot.Itemstack == null) return;

        var ledger = Mod.Store.ReadLedger(inSlot.Itemstack, LedgerMode);
        var config = Mod.Config;
        // The title already shows the name; then say what the item is.
        if (ledger.Name.Length > 0 || ledger.Album != null)
            dsc.AppendLine(Lang.Get("curiocatalogue:ledger-info-kind", base.GetHeldItemName(inSlot.Itemstack)));
        // Shown because the recolouring and ageing recipes depend on it (domain.md, Covers).
        if (Cover.Split('-') is [var age, var colour])
            dsc.AppendLine(Lang.Get(age == "aged" ? "curiocatalogue:ledger-info-cover-aged" : "curiocatalogue:ledger-info-cover",
                Lang.Get("curiocatalogue:cover-" + colour)));
        dsc.AppendLine(Lang.Get("curiocatalogue:ledger-info-sheets", ledger.Sheets, config.MaxSheets));
        if (ProgressOf(ledger) is { } progress)
            dsc.AppendLine(Lang.Get("curiocatalogue:ledger-info-album", progress.Collected, progress.Total, progress.GroupsComplete, progress.GroupsTotal));
        // An album's entries are what it still misses: the progress above says it, and the limit does not matter.
        else if (!IsAlbum)
            dsc.AppendLine(Lang.Get("curiocatalogue:ledger-info-entries", ledger.Entries.Count, config.SoftEntryLimit));
        dsc.AppendLine(Lang.Get(LedgerMode switch
        {
            LedgerMode.Album => "curiocatalogue:ledger-info-howto-album",
            LedgerMode.Counts => "curiocatalogue:ledger-info-howto-counts",
            _ => "curiocatalogue:ledger-info-howto-types",
        }, KeyName(world, "shift", "Shift"), KeyName(world, "ctrl", "Ctrl")));
        // The game shows the bottom-of-screen hint only for the main hand, so the off-hand way is told here.
        dsc.AppendLine(Lang.Get("curiocatalogue:ledger-info-open"));
    }

    /// <summary>
    /// The key the player has bound to a hotkey, as the handbook's &lt;hk&gt; tag shows it (vsapi HotkeyComponent.cs:19#HotKeys),
    /// so the tooltip follows a rebinding; the fallback where there is no client.
    /// </summary>
    private static string KeyName(IWorldAccessor world, string hotkeyCode, string fallback) =>
        world.Api is ICoreClientAPI capi && capi.Input.HotKeys.TryGetValue(hotkeyCode, out var hotkey)
            ? hotkey.CurrentMapping.ToString()
            : fallback;
}
