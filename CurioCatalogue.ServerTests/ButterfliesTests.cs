using CurioCatalogue.Adapters;
using CurioCatalogue.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace CurioCatalogue.ServerTests;

/// <summary>
/// The butterflies (V4 of the vanilla collections plan; what they are: C3 of the collections design plan): our definition
/// <c>butterflies.json</c> on the game's real items <c>butterfly-dead-{type}</c>, 169 of them (<c>reedbutterfly</c> has no item).
/// One level, every dead butterfly a member of its own, no grouping.
/// </summary>
[Collection(ServerCollection.Name)]
public class ButterfliesTests(GameServerFixture server) : BookTestBase(server)
{
    const string Id = "butterflies";

    DefinedCollection Butterflies => Mod.Collections.Defined(Id) ?? throw new InvalidOperationException("no butterfly collection");

    [Fact]
    public void The_only_level_has_a_member_for_each_of_the_169_dead_butterflies_filled_by_that_butterfly_alone()
    {
        var seen = game.OnMain(() =>
        {
            var items = World.Items.Where(i => i.Code is { Domain: "game" } c && c.Path.StartsWith("butterfly-dead-")).Select(i => i.Code.Path).Order().ToList();
            var level = Butterflies.Levels.Single();
            return (items.Count, Butterflies.Levels.Count, level.Id, level.Members.Count, string.Join(",", level.Members.Select(m => m.Fillers.Count).Distinct()),
                level.Members.Select(m => "butterfly-dead-" + m.Values["type"]).Order().SequenceEqual(items));
        });

        Assert.Equal((169, 1, "all", 169, "1", true), seen);
    }

    [Fact]
    public void The_members_follow_the_order_of_the_games_list()
    {
        // The order is the definition's own, that of the item's variant group (not alphabetical: "smallcoppermale" before "smallcopperabschmidtiifemale").
        var order = game.OnMain(() => Butterflies.Levels.Single().Members.Select(m => m.Values["type"]).ToList());
        Assert.Equal("acmonbluefemale", order[0]);
        Assert.Equal("yellowpansymale", order[^1]);
    }

    [Fact]
    public void The_definitions_list_is_exactly_the_games_types_so_the_log_has_no_warning_of_an_unlisted_one()
    {
        var seen = game.OnMain(() => (
            Butterflies.Definition.Dimensions.Single().Values.Order().ToList(),
            World.Items.Where(i => i.Code is { Domain: "game" } c && c.Path.StartsWith("butterfly-dead-")).Select(i => i.Code.Path["butterfly-dead-".Length..]).Order().ToList()));
        Assert.Equal(seen.Item2, seen.Item1);
    }

    [Fact]
    public void An_album_takes_a_butterfly_from_a_chest_once_and_leaves_the_rest()
    {
        var client = FakeClient.Join(game, "bfly-take");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            var slots = ((IBlockEntityContainer)chest).Inventory;
            slots[0].Itemstack = Vanilla("butterfly-dead-monarch", 3);
            slots[1].Itemstack = Vanilla("butterfly-dead-peacock");
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Ours("book-album-normal-brickred");
            Mod.Store.WriteAlbum(hand.Itemstack, Core.Album.New(Butterflies, 8, ""));
            Click(player, hand, pos);
            var read = ReadAlbum(hand.Itemstack!);
            return (read.Collected.Keys.Order().ToList(), Inventory(player), Contents(chest));
        });

        Assert.Equal(["monarch", "peacock"], seen.Item1);
        Assert.Equal("butterfly-dead-monarch x1, butterfly-dead-peacock x1", string.Join(", ", seen.Item2.Split(", ").Order()));
        Assert.Equal("butterfly-dead-monarch x2", seen.Item3);
    }

    [Fact]
    public void A_want_list_names_each_missing_butterfly_and_is_given_by_that_butterfly()
    {
        var client = FakeClient.Join(game, "bfly-wants");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var album = Core.Album.New(Butterflies, 8, "");
            var list = Assert.IsType<CreateInvoiceResult.Created>(album.MakeWantList(album.LevelIn(Butterflies)!, 0, new BlockPosition(0, 0, 0))).Invoice;
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = new ItemStack(World.GetItem(InvoiceMaker.InvoiceCode(InvoiceKind.WantList)));
            Mod.Store.WriteInvoice(hand.Itemstack, list);

            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("butterfly-dead-redadmiral");
            Click(player, hand, pos);

            var left = Mod.Store.ReadInvoice(hand.Itemstack!, InvoiceKind.WantList);
            return (list.Entries.Length, left.Entries.Select(e => e.Member).ToList(), Inventory(player));
        });

        Assert.Equal(169, seen.Item1);
        Assert.Equal(168, seen.Item2.Count);
        Assert.DoesNotContain("redadmiral", seen.Item2);
        Assert.Equal("butterfly-dead-redadmiral x1", seen.Item3);
    }

    [Fact]
    public void The_collection_and_its_progress_have_names_in_the_lang()
    {
        var missing = game.OnMain(() => new[] { Butterflies.Definition.NameKey, Butterflies.Definition.Levels.Single().ProgressKey }
            .Where(key => key != null && Lang.Get(key) == key).ToList());
        Assert.Empty(missing);
    }
}
