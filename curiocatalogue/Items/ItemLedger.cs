using System.Linq;
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

    /// <summary>Where the album stands against its collection as the game has it now (what its tooltip and window say).</summary>
    internal AlbumStanding StandingOf(Album album) => Mod.Store.StandingOf(album);

    /// <summary>An album's progress as its level words it (the level's <c>progressKey</c>; a generic line when it has none).</summary>
    internal static string ProgressText(CollectionLevel level, LevelProgress progress) =>
        Lang.Get(level.Definition.ProgressKey ?? "curiocatalogue:ledger-info-progress", progress.Collected, progress.Total, progress.GroupsComplete, progress.GroupsTotal);

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
                BookWindow.Open(capi, Mod, slot, offHand: false);
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

            case MakeInvoiceResult.AlbumRefused albumRefused:
                PlayerMessages.Refuse(player, albumRefused.Reason);
                break;

            case MakeInvoiceResult.Made made:
                ledgerSlot.MarkDirty();
                // Straight into the empty hand if it can: the next Shift + right-click posts it.
                HandOut.Give(player, made.InvoiceStack, intoSlot);
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
            case CollectResult.AlbumRefused albumRefused:
                PlayerMessages.Refuse(player, albumRefused.Reason);
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

    internal static string Describe(MakeInvoiceResult result) => result switch
    {
        MakeInvoiceResult.Made made => $"made, {made.Invoice.Entries.Length} lines, {made.SheetsLeft} sheets left",
        MakeInvoiceResult.Refused refused => $"refused, {refused.Reason}",
        MakeInvoiceResult.AlbumRefused refused => $"refused, {refused.Reason}",
        _ => throw ExhaustiveMatch.Failed(result),
    };

    // --- Crafting (domain.md, Recipes; recipes in recipes/grid/ledger.json, told apart by their name) ---

    private const string FallbackRecipe = "ledger-fallback";
    private const string RestoreRecipe = "ledger-restore";
    private const string SwitchModeRecipe = "ledger-switchmode";
    private const string RechargeRecipe = "ledger-recharge";
    private const string RecolorRecipe = "ledger-recolor";

    /// <summary>Sheets of a new ledger (domain.md, Recipes).</summary>
    private const int FallbackSheets = 8;
    private const int RestoreSheets = 32;

    private static bool Is(IRecipeBase recipe, string name) => recipe.Name?.Path == name;

    /// <summary>An album recipe of any collection, of any domain: the one a definition names (<c>albumRecipe</c>).</summary>
    private bool IsAlbumRecipe(IRecipeBase recipe) => Mod.Collections.AlbumOf(recipe.Name) != null;

    private Book Read(ItemStack stack) => Mod.Store.ReadBook(stack, ((ItemLedger)stack.Collectible).LedgerMode);

    /// <summary>
    /// Called for the ledger lying in the grid as an ingredient. Only an empty ledger switches its mode; a ledger
    /// full of sheets takes no parchment, so the recharge recipe does not show.
    /// </summary>
    public override bool MatchesForCrafting(ItemStack inputStack, IRecipeBase recipe, IRecipeIngredient ingredient)
    {
        if (!base.MatchesForCrafting(inputStack, recipe, ingredient)) return false;
        // Data from a newer build of the mod: our recipes would rewrite it without what this build does not know.
        if (Adapters.LedgerStore.IsNewer(inputStack)) return false;
        if (Is(recipe, SwitchModeRecipe)) return Read(inputStack) is Ledger { IsEmpty: true };
        if (Is(recipe, RechargeRecipe)) return Read(inputStack).Sheets < Mod.Config.MaxSheets;
        // An album is made from an empty ledger (domain.md, Recipes).
        if (IsAlbumRecipe(recipe)) return Read(inputStack).IsEmpty;
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
            // Both recipes make a ledger (domain.md, Recipes).
            Mod.Store.WriteLedger(output, Ledger.Empty(LedgerMode, System.Math.Min(newSheets, Mod.Config.MaxSheets)));
            return;
        }

        if (Mod.Collections.AlbumOf(byRecipe.Name) is { } collectionId)
        {
            MakeAlbum(allInputSlots, outputSlot, collectionId);
            return;
        }

        if (!Is(byRecipe, RechargeRecipe) || RechargeSlots(allInputSlots) is not { } found) return;
        var (charged, _) = Read(output).AddSheets(found.Paper.StackSize, Mod.Limits);
        Mod.Store.WriteBook(output, charged);
    }

    /// <summary>
    /// A new album of the recipe's collection, nothing collected (the level chosen only if the collection has one); sheets and name
    /// come from the ledger it is made from (the recipe copied them). An album follows its collection by itself: it stores what it
    /// collected, and what is missing is computed. The sample must be a member of that collection (a recipe's ingredient may be wider
    /// than the collection: an excluded piece); else, or with the collection off or gone, no output.
    /// </summary>
    private void MakeAlbum(ItemSlot[] inputSlots, ItemSlot outputSlot, string collectionId)
    {
        var sample = System.Array.Find(inputSlots, s => s.Itemstack is { } st && st.Collectible is not ItemLedger)?.Itemstack;
        if (sample == null || Mod.Collections.Resolve(collectionId) is not CollectionResolution.Found { Value: var collection }
            || !Mod.Collections.HasMember(collection, sample))
        {
            outputSlot.Itemstack = null;
            return;
        }
        // The output has the ledger's attributes (copyAttributesFrom): read them as the ledger they were.
        var from = Mod.Store.ReadLedger(outputSlot.Itemstack!, LedgerMode.Types);
        Mod.Store.WriteAlbum(outputSlot.Itemstack!, Album.New(collection, from.Sheets, from.Name));
        ModLog.Debug(api, $"album of {collection.Id} made, levels {string.Join(", ", collection.Levels.Select(l => $"{l.Id} {l.Members.Count}"))}");
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
        if (IsAlbum && LedgerStore.ReadCollectionId(itemStack) is { Length: > 0 } id && Mod.Collections.Defined(id)?.Definition.NameKey is { } nameKey)
            return Lang.Get("curiocatalogue:album-name", Lang.Get(nameKey));
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

        var book = Mod.Store.ReadBook(inSlot.Itemstack, LedgerMode);
        var config = Mod.Config;
        // The title already shows the name; then say what the item is.
        if (book.Name.Length > 0 || book is Album)
            dsc.AppendLine(Lang.Get("curiocatalogue:ledger-info-kind", base.GetHeldItemName(inSlot.Itemstack)));
        // Shown because the recolouring and ageing recipes depend on it (domain.md, Covers).
        if (Cover.Split('-') is [var age, var colour])
            dsc.AppendLine(Lang.Get(age == "aged" ? "curiocatalogue:ledger-info-cover-aged" : "curiocatalogue:ledger-info-cover",
                Lang.Get("curiocatalogue:cover-" + colour)));
        dsc.AppendLine(Lang.Get("curiocatalogue:ledger-info-sheets", book.Sheets, config.MaxSheets));
        switch (book)
        {
            // An album's progress against its level; nothing when the game lacks its collection, "no level" until one is chosen.
            case Album album:
                switch (StandingOf(album))
                {
                    case AlbumStanding.AtLevel at:
                        dsc.AppendLine(ProgressText(at.Level, album.Progress(at.Level)));
                        break;
                    case AlbumStanding.Refused { Reason: AlbumRefusal.Disabled }:
                        dsc.AppendLine(Lang.Get("curiocatalogue:ledger-info-disabled"));
                        break;
                    case AlbumStanding.Refused { Reason: AlbumRefusal.NoLevel }:
                        dsc.AppendLine(Lang.Get("curiocatalogue:ledger-info-nolevel"));
                        break;
                }
                break;
            case Ledger ledger:
                dsc.AppendLine(Lang.Get("curiocatalogue:ledger-info-entries", ledger.Entries.Count, config.SoftEntryLimit));
                break;
            default:
                throw ExhaustiveMatch.Failed(book);
        }
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
