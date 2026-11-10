using CurioCatalogue.Adapters;
using CurioCatalogue.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace CurioCatalogue.ServerTests;

/// <summary>
/// The seashells (V3 of the vanilla collections plan; what they are: C2 of the collections design plan): our definition
/// <c>seashells.json</c> on the game's real blocks <c>seashell-{type}-{color}</c>, 7 × 6 = 42, all of which occur in survival.
/// Two levels: the type (7, a shell of any colour), the type in each colour (42).
/// </summary>
[Collection(ServerCollection.Name)]
public class SeashellsTests(GameServerFixture server) : BookTestBase(server)
{
    const string Id = "seashells";

    DefinedCollection Shells => Mod.Collections.Defined(Id) ?? throw new InvalidOperationException("no seashell collection");

    static string CodeOf(ItemKey key) => key.Code[(key.Code.IndexOf(':') + 1)..];

    [Fact]
    public void The_levels_have_7_and_42_members_and_a_type_is_filled_by_a_shell_of_any_of_its_6_colours()
    {
        var seen = game.OnMain(() => (
            World.Blocks.Count(b => b.Code is { Domain: "game" } c && c.Path.StartsWith("seashell-")),
            Shells.Level("types")!.Members.Count,
            Shells.Level("colors")!.Members.Count,
            Shells.Level("types")!.Members.Select(m => m.Fillers.Count).Distinct().ToList(),
            Shells.Level("types")!.Members.Single(m => m.Values["type"] == "clam").Fillers.Select(CodeOf).Order().ToList(),
            Shells.Level("colors")!.Members.Select(m => m.Fillers.Count).Distinct().ToList()));

        Assert.Equal(42, seen.Item1);
        Assert.Equal((7, 42), (seen.Item2, seen.Item3));
        Assert.Equal([6], seen.Item4);
        Assert.Equal(["seashell-clam-cinnamon", "seashell-clam-darkpurple", "seashell-clam-latte", "seashell-clam-plain",
            "seashell-clam-seafoam", "seashell-clam-turquoise"], seen.Item5);
        Assert.Equal([1], seen.Item6);
    }

    [Fact]
    public void Seafoam_and_turquoise_shells_are_rare_and_marked_and_the_others_are_not()
    {
        // C2: the game's own split is the colour (BlockShell.cs:13). The rule is data, and so is its mark (L3 of the album polish
        // plan): a golden corner and "Rare colour", from seashells.json and the lang.
        var seen = game.OnMain(() =>
        {
            var members = Shells.Level("colors")!.Members;
            var marked = members.Where(m => Shells.Definition.MarkOf(m) != null).ToList();
            return (string.Join(",", members.Where(m => m.Attributes.GetValueOrDefault("rarity") == "rare").Select(m => m.Values["color"]).Distinct().Order()),
                members.Count(m => m.Attributes.GetValueOrDefault("rarity") == "rare"),
                Shells.Level("types")!.Members.Count(m => m.Attributes.ContainsKey("rarity") || Shells.Definition.MarkOf(m) != null),
                string.Join(",", marked.Select(m => m.Values["color"]).Distinct().Order()),
                marked.Select(m => Shells.Definition.MarkOf(m)).Distinct().Single());
        });

        Assert.Equal(("seafoam,turquoise", 14, 0, "seafoam,turquoise"), (seen.Item1, seen.Item2, seen.Item3, seen.Item4));
        Assert.Equal(new MarkDef("rarity", "rare", unchecked((int)0xFFE0B040), "curiocatalogue:seashells-rarity-rare"), seen.Item5);
        Assert.NotEqual("curiocatalogue:seashells-rarity-rare", game.OnMain(() => Lang.Get("curiocatalogue:seashells-rarity-rare")));
    }

    [Fact]
    public void An_album_of_types_takes_a_shell_of_any_colour_once_and_leaves_the_second_of_a_type()
    {
        var client = FakeClient.Join(game, "shells-take");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            var slots = ((IBlockEntityContainer)chest).Inventory;
            slots[0].Itemstack = Vanilla("seashell-conch-turquoise");
            slots[1].Itemstack = Vanilla("seashell-conch-latte");
            slots[2].Itemstack = Vanilla("seashell-volute-plain");
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Ours("book-album-normal-brickred");
            var album = Assert.IsType<LevelChoice.Chosen>(Core.Album.New(Shells, 8, "").ChooseLevel(Shells, "types")).Album;
            Mod.Store.WriteAlbum(hand.Itemstack, album);
            Click(player, hand, pos);
            var read = ReadAlbum(hand.Itemstack!);
            return (read.Collected.Keys.Order().ToList(), Inventory(player).Split(", ").Order().ToList(), Contents(chest));
        });

        // One conch of the first colour in the member's order fills "conch"; the other conch stays with the chest.
        Assert.Equal(["conch", "volute"], seen.Item1);
        Assert.Equal(2, seen.Item2.Count);
        Assert.Contains("seashell-volute-plain x1", seen.Item2);
        Assert.Single(seen.Item2, s => s.StartsWith("seashell-conch-"));
        Assert.Single(seen.Item3.Split(", "));
        Assert.StartsWith("seashell-conch-", seen.Item3);
    }

    [Fact]
    public void A_want_list_of_types_says_any_colour_and_is_given_by_a_shell_of_another_colour()
    {
        var client = FakeClient.Join(game, "shells-wants");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var album = Assert.IsType<LevelChoice.Chosen>(Core.Album.New(Shells, 8, "").ChooseLevel(Shells, "types")).Album;
            var list = Assert.IsType<CreateInvoiceResult.Created>(album.MakeWantList(album.LevelIn(Shells)!, 0, new BlockPosition(0, 0, 0))).Invoice;
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = new ItemStack(World.GetItem(InvoiceMaker.InvoiceCode(InvoiceKind.WantList)));
            Mod.Store.WriteInvoice(hand.Itemstack, list);

            var text = new System.Text.StringBuilder();
            hand.Itemstack.Collectible.GetHeldItemInfo(hand, text, World, false);

            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("seashell-clam-cinnamon");
            Click(player, hand, pos);

            var left = Mod.Store.ReadInvoice(hand.Itemstack!, InvoiceKind.WantList);
            return (list.Entries.Length, left.Entries.Select(e => e.Member).ToList(), Inventory(player), text.ToString());
        });

        Assert.Equal(7, seen.Item1);
        Assert.Equal(6, seen.Item2.Count);
        Assert.DoesNotContain("clam", seen.Item2);
        Assert.Equal("seashell-clam-cinnamon x1", seen.Item3);
        // The first line, the scallop, worded by the level's member key: "Scallop, any colour" (L1).
        Assert.Contains(LangText("en", "invoice-info-line", 1, LangText("en", "seashells-member-type", LangText("en", "seashells-type-scallop"))),
            seen.Item4.Split(["\r\n", "\n"], StringSplitOptions.None));
    }

    [Fact]
    public void A_levels_member_key_words_the_whole_name()
    {
        // Our en key says what the plain form says ("Scallop, any colour"); a key of other words shows it is the key that speaks.
        var name = game.OnMain(() =>
        {
            var levels = Shells.Definition.Levels.Select(l => l.Id == "types" ? l with { MemberKey = "curiocatalogue:album-name" } : l).ToList();
            var points = Shells.Level("colors")!.Members.Select(m => new SourcePoint(m.Id, m.Values, m.Fillers[0], m.Stacks[0]));
            var built = Assert.IsType<CollectionBuild.Built>(DefinedCollection.Build(Shells.Definition with { Levels = levels }, points)).Collection;
            return MemberNames.Name(built, built.Level("types")!, built.Level("types")!.Find("scallop")!);
        });
        Assert.Equal(LangText("en", "album-name", LangText("en", "seashells-type-scallop")), name);
    }

    [Fact]
    public void A_shell_of_a_colour_is_named_by_its_type_and_colour_in_lower_case()
    {
        var name = game.OnMain(() => MemberNames.Name(Shells, Shells.Level("colors")!, Shells.Level("colors")!.Find("scallop-seafoam")!));
        Assert.Equal($"{LangText("en", "seashells-type-scallop")}, {LangText("en", "seashells-color-seafoam").ToLowerInvariant()}", name);
    }

    [Fact]
    public void Every_type_and_colour_has_a_name_and_an_any_word_in_the_lang()
    {
        var missing = game.OnMain(() => Shells.Definition.Dimensions
            .SelectMany(d => d.Values.Select(d.ValueNameKey).Append(d.AnyKey).Append("curiocatalogue:seashells-level-types").Append("curiocatalogue:seashells-member-type"))
            .Where(key => key != null && Lang.Get(key) == key).ToList());
        Assert.Empty(missing);
    }
}
