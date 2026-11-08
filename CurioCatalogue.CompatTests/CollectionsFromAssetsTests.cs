using CurioCatalogue.Adapters;
using CurioCatalogue.Core;
using Vintagestory.API.Common;

namespace CurioCatalogue.CompatTests;

/// <summary>
/// K3 of the collections core plan: the registry reads the definitions of every domain, with the patches applied. Our test mod
/// <c>cccollections</c> (<c>testmods/</c>) brings a collection of the game's ingots by a definition of its own domain, and a patch
/// of that definition (the way an admin or another mod changes one) leaves out iron. The vanilla tapestries stay the collection
/// of 0.1: the same pieces and pictures.
/// </summary>
[Collection(CompatCollection.Name)]
public class CollectionsFromAssetsTests(CompatServerFixture server) : BookTestBase(server)
{
    [Fact]
    public void A_definition_of_another_domain_is_a_collection_of_the_registered_variants()
    {
        var seen = game.OnMain(() =>
        {
            var members = Mod.Collections.Defined("ingots")!.Level("ingots")!.Members.Select(m => m.Id).Order().ToList();
            var registered = World.Collectibles.Where(c => c.Code.Domain == "game" && c.Code.Path.StartsWith("ingot-"))
                .Select(c => c.Code.Path["ingot-".Length..]).Where(m => !m.Contains('-')).Order().ToList();
            return (members, registered);
        });

        Assert.Contains("copper", seen.registered); // the game's ingots are there to compare with
        // Iron is the patch's doing: the definition itself leaves nothing out.
        Assert.Equal(seen.registered.Where(m => m != "iron"), seen.members);
    }

    [Fact]
    public void The_ores_of_the_test_mod_are_a_collection_of_the_allowed_ore_blocks()
    {
        // ccores is there for looking at the album's tabs and tables in game (album view plan, A4): an ore a tab, grade × rock a
        // table. Its members are the game's graded ore blocks (ore-graded.json, allowedVariants). The gems' blocks have the same
        // shape with a potential in the grade's place (ore-gem.json, "ore-high-emerald-shale"), so the definition excludes them.
        string[] gems = ["diamond", "emerald", "olivine_peridot"];
        var seen = game.OnMain(() =>
        {
            var ores = Mod.Collections.Defined("ccores")!;
            var members = ores.Level("deposits")!.Members
                .Select(m => $"ore-{m.Values["grade"]}-{m.Values["ore"]}-{m.Values["rock"]}").Order().ToList();
            var registered = World.Blocks.Where(b => b.Code is { Domain: "game" } code
                    && code.Path.Split('-') is ["ore", "poor" or "medium" or "rich" or "bountiful", var ore, _] && !gems.Contains(ore))
                .Select(b => b.Code.Path).Distinct().Order().ToList();
            int kinds = ores.Level("deposits")!.Members.Select(m => m.Values["ore"]).Distinct().Count();
            return (members, registered, kinds, oreMembers: ores.Level("ores")!.Members.Count);
        });

        Assert.True(seen.registered.Count > 100, $"only {seen.registered.Count} graded ore blocks");
        Assert.Equal(seen.registered, seen.members);
        Assert.Equal(seen.kinds, seen.oreMembers); // a member of the level "ores" for every ore
    }

    [Fact]
    public void A_patch_of_a_definition_is_in_what_the_registry_reads()
    {
        var exclude = game.OnMain(() => Mod.Collections.Defined("ingots")!.Definition.Exclude);

        Assert.Equal(["ingot-iron"], exclude);
    }

    [Fact]
    public void The_vanilla_tapestries_are_the_collection_of_0_1_with_the_picture_of_the_test_mod()
    {
        // 88 pieces in 31 pictures in the game (modding.md, Vanilla content we rely on) and cctapestry's 2×1 picture.
        var (pieces, pictures) = game.OnMain(() =>
        {
            var members = Pieces.Members;
            return (members.Count, members.Select(m => m.Values["picture"]).Distinct().Count());
        });

        Assert.Equal((88 + 2, 31 + 1), (pieces, pictures));
    }

    [Fact]
    public void Every_collection_of_the_registry_is_found_by_its_samples_and_by_nothing_else()
    {
        string? Of(ItemStack s) => Mod.Collections.All.Values.FirstOrDefault(c => Mod.Collections.HasMember(c, s))?.Id;
        var seen = game.OnMain(() => (
            Of(Tapestry("ambush1")),
            Of(Vanilla("ingot-copper")),
            Of(Vanilla("ingot-iron"))));

        Assert.Equal(("tapestries", "ingots", null), seen);
    }

    // ---- an album of a collection of several levels (K4; the ingots have two, "ingots" and "bars", the same members)

    [Fact]
    public void An_album_of_several_levels_takes_nothing_until_a_level_is_chosen_then_keeps_it()
    {
        // A2 of the album view plan: no default level; the window's request chooses one while the album is empty, the server
        // re-checks it (a level the collection lacks is ignored, a filled album refuses), the first piece fixes it.
        var client = FakeClient.Join(game, "cc-levels");
        var (pos, fingerprint, firstClick) = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("ingot-copper", 2);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Ours("book-album-normal-brickred");
            Mod.Store.WriteAlbum(hand.Itemstack, Core.Album.New(Mod.Collections.Defined("ingots")!, 8, ""));
            Click(player, hand, pos);
            return (pos, LedgerStore.Fingerprint(hand.Itemstack), (Inventory(player), Contents(chest)));
        });
        Assert.Equal(("", "ingot-copper x2"), firstClick);
        client.WaitFor(() => Errors(client).Contains("nolevel"), "the refusal 'no level'");

        var shown = new ShownItem { HotbarSlot = client.Player.InventoryManager.ActiveHotbarSlotNumber, Fingerprint = fingerprint };
        client.SendToMod(new ChooseLevelRequest { Book = shown, Level = "no-such-level" }); // ignored, the album unchanged
        client.SendToMod(new ChooseLevelRequest { Book = shown, Level = "bars" });
        client.WaitFor(() => ReadAlbum(client.Player.InventoryManager.ActiveHotbarSlot.Itemstack!).Level == "bars", "the level chosen");

        var (taken, refingerprint) = game.OnMain(() =>
        {
            var hand = client.Player.InventoryManager.ActiveHotbarSlot;
            Click(client.Player, hand, pos);
            return (ReadAlbum(hand.Itemstack!).Collected.Keys.ToList(), LedgerStore.Fingerprint(hand.Itemstack!));
        });
        Assert.Equal(["copper"], taken);

        client.SendToMod(new ChooseLevelRequest { Book = new ShownItem { HotbarSlot = shown.HotbarSlot, Fingerprint = refingerprint }, Level = "ingots" });
        client.WaitFor(() => Errors(client).Contains("levelfixed"), "the refusal 'level fixed'");
        Assert.Equal("bars", game.OnMain(() => ReadAlbum(client.Player.InventoryManager.ActiveHotbarSlot.Itemstack!).Level));
    }

    // ---- a want list of members of several items (K5; the test mod's seashells: a level by type, any colour)

    [Fact]
    public void A_want_list_by_type_says_any_colour_and_is_given_by_a_shell_of_another_colour()
    {
        var client = FakeClient.Join(game, "cc-wants");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var shells = Mod.Collections.Defined("ccshells")!;
            var album = Assert.IsType<LevelChoice.Chosen>(Core.Album.New(shells, 8, "").ChooseLevel(shells, "type")).Album;
            var list = Assert.IsType<CreateInvoiceResult.Created>(album.MakeWantList(album.LevelIn(shells)!, 0, new BlockPosition(0, 0, 0))).Invoice;
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = new ItemStack(World.GetItem(InvoiceMaker.InvoiceCode(InvoiceKind.WantList)));
            Mod.Store.WriteInvoice(hand.Itemstack, list);

            var text = new System.Text.StringBuilder();
            hand.Itemstack.Collectible.GetHeldItemInfo(hand, text, World, false);

            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("seashell-clam-turquoise");
            Click(player, hand, pos);

            var left = Mod.Store.ReadInvoice(hand.Itemstack!, InvoiceKind.WantList);
            return (text.ToString(), Vanilla("seashell-scallop-latte").GetName(), left.Entries.Select(e => e.Member).ToList(), Inventory(player));
        });
        var (info, scallop, left, inventory) = seen;

        // The first line is the scallop, shown by its first colour, any of them will do; the test mod's lang says "any colour".
        Assert.Contains(LangText("en", "invoice-info-line", 1, LangText("en", "invoice-info-member-free", scallop, "any colour")), info);
        Assert.Equal(["scallop", "sundial", "turritella", "conch", "seastar", "volute"], left);
        Assert.Equal("seashell-clam-turquoise x1", inventory);
    }
}
