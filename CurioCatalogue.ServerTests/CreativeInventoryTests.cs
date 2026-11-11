using Vintagestory.API.Common;

namespace CurioCatalogue.ServerTests;

/// <summary>
/// C6: which of our items the creative inventory lists. The server resolves <c>creativeinventory</c> of an item type into
/// <c>CollectibleObject.CreativeInventoryTabs</c> (vsessentialsmod ItemType.cs:84, <c>GetCreativeTabs</c>) when it loads the items; the client's
/// creative dialog and the handbook (vsapi Collectible.cs:2474#GetHandBookStacks) read that same field. So the tabs are checked on the
/// server; the dialog's search box and its drawing stay a game check. The handbook needs the tab for the aged covers
/// (docs/modding.md, Handbook), so a cover missing here has no page.
/// </summary>
[Collection(ServerCollection.Name)]
public class CreativeInventoryTests(GameServerFixture game)
{
    static readonly string[] Tabs = ["general", "items"];

    /// <summary>Our items with their creative tabs, by code path.</summary>
    Dictionary<string, string[]> Listed() => game.OnMain(() =>
        game.Api.World.Items.Where(i => i.Code?.Domain == "curiocatalogue")
            .ToDictionary(i => i.Code.Path, i => i.CreativeInventoryTabs ?? []));

    [Theory]
    [InlineData("ledger")]
    [InlineData("materials")]
    [InlineData("album")]
    public void Every_cover_of_a_kind_is_listed_normal_and_aged(string kind)
    {
        var books = Listed().Where(p => p.Key.StartsWith($"book-{kind}-")).ToList();
        Assert.Equal(24, books.Count);
        Assert.Equal(13, books.Count(p => p.Key.Contains("-normal-")));
        Assert.Equal(11, books.Count(p => p.Key.Contains("-aged-")));
        var unlisted = books.Where(p => !Tabs.All(p.Value.Contains)).Select(p => p.Key).ToList();
        Assert.True(unlisted.Count == 0, $"not in the tabs {string.Join("+", Tabs)}: {string.Join(", ", unlisted)}");
    }

    [Fact]
    public void Seventy_two_books_in_the_general_tab()
    {
        var inGeneral = Listed().Where(p => p.Key.StartsWith("book-") && p.Value.Contains("general")).ToList();
        Assert.Equal(72, inGeneral.Count);
    }

    [Fact]
    public void The_blank_lists_are_in_the_tabs_today()
    {
        // ROADMAP.md, "Lists out of the creative inventory": they are to leave the tabs; when that lands, this test changes with it.
        var lists = Listed().Where(p => p.Key.StartsWith("invoice-")).ToList();
        Assert.Equal(["invoice-incoming", "invoice-outgoing", "invoice-wantlist"], lists.Select(p => p.Key).Order());
        Assert.All(lists, p => Assert.Equal(Tabs, Tabs.Where(p.Value.Contains)));
    }

    [Fact]
    public void Nothing_else_of_ours_is_in_the_tabs()
    {
        var other = Listed().Where(p => p.Value.Length > 0 && !p.Key.StartsWith("book-") && !p.Key.StartsWith("invoice-")).Select(p => p.Key).ToList();
        Assert.True(other.Count == 0, string.Join(", ", other));
    }
}
