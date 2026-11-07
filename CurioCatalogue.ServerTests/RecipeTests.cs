using Vintagestory.API.Common;

namespace CurioCatalogue.ServerTests;

/// <summary>
/// The grid recipes of <c>recipes/grid/ledger.json</c> as the server resolved them (docs/modding.md, Recipes): one recipe per
/// combination of <c>allowedVariants</c> that exists as an item, with the output, the ingredients, their counts and the shape we
/// wrote. The expected sets are written out here, not read from the JSON, so a change of a recipe has to be made in two places.
/// A recipe the game dropped (a code that does not resolve) is missing from the set and fails the test with its signature.
/// </summary>
[Collection(ServerCollection.Name)]
public class RecipeTests(GameServerFixture game)
{
    static readonly string[] NormalCovers =
        ["brickred", "cherryred", "darkbeige", "darkgray", "darkgreen", "darkolive", "gray", "olive", "orange", "orangebrown", "purple", "purpleorange", "teal"];
    static readonly string[] AgedCovers =
        ["brickred", "cherryred", "darkbeige", "darkgray", "darkgreen", "darkolive", "gray", "olive", "orange", "orangebrown", "purpleorange"];
    static readonly string[] Covers = [.. NormalCovers.Select(c => "normal-" + c), .. AgedCovers.Select(c => "aged-" + c)];
    static readonly string[] Kinds = ["ledger", "materials", "album"];

    // leather colour -> the cover the recolour recipe gives (the output names the cover, the leather names the dye)
    static readonly Dictionary<string, string> Leathers = new()
    {
        ["plain"] = "orangebrown", ["orange"] = "orange", ["black"] = "darkgray", ["red"] = "cherryred", ["blue"] = "teal",
        ["purple"] = "purple", ["pink"] = "purpleorange", ["white"] = "darkbeige", ["yellow"] = "olive", ["gray"] = "gray", ["green"] = "darkgreen",
    };

    const string Ink = "Q=game:inkandquill x1 tool(0)";

    /// <summary>Our recipes of one name, on the server's main thread.</summary>
    List<T> OfName<T>(string name, System.Func<GridRecipe, T> select) => game.OnMain(() =>
        game.Api.World.GridRecipes.Where(r => r.Output?.Code?.Domain == "curiocatalogue" && r.Name?.Path == name).Select(select).ToList());

    static CraftingRecipeIngredient Ingredient(GridRecipe r, string id) => r.ResolvedIngredients!.OfType<CraftingRecipeIngredient>().First(i => i.Id == id);

    /// <summary>Each recipe as "output <- id=code xN, ..." (ingredients sorted by id); the shape is checked on the way.</summary>
    List<string> Actual(string name, int width, int height, bool shapeless, int group) => OfName(name, r =>
    {
        Assert.True((r.Width, r.Height, r.Shapeless, r.RecipeGroup) == (width, height, shapeless, group),
            $"{name} -> {r.Output!.Code}: expected {width}x{height} shapeless={shapeless} group {group}, got {r.Width}x{r.Height} shapeless={r.Shapeless} group {r.RecipeGroup}");
        return Signature(r);
    });

    static string Signature(GridRecipe r)
    {
        // A grid cell list repeats an ingredient that stands in two cells (the nails): distinct by id.
        var ingredients = r.ResolvedIngredients!.OfType<CraftingRecipeIngredient>().DistinctBy(i => i.Id).OrderBy(i => i.Id)
            .Select(i => $"{i.Id}={i.Code} x{i.Quantity}" + (i.IsTool ? $" tool({i.ToolDurabilityCost})" : ""));
        return $"{r.Output!.Code} x{r.Output.Quantity} <- {string.Join(", ", ingredients)}";
    }

    static void AssertSame(string name, IEnumerable<string> expected, List<string> actual)
    {
        var want = expected.ToList();
        Assert.Equal(want.Count, want.Distinct().Count()); // the table itself has no duplicates
        var missing = want.Except(actual).ToList();
        var extra = actual.Except(want).ToList();
        var duplicates = actual.GroupBy(a => a).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.True(missing.Count == 0 && extra.Count == 0 && duplicates.Count == 0,
            $"{name}: {actual.Count} recipes, expected {want.Count}.\nmissing ({missing.Count}):\n  {string.Join("\n  ", missing.Take(5))}\n" +
            $"unexpected ({extra.Count}):\n  {string.Join("\n  ", extra.Take(5))}\nduplicated ({duplicates.Count}):\n  {string.Join("\n  ", duplicates.Take(5))}");
    }

    [Fact]
    public void Fallback_makes_a_ledger_from_a_vanilla_book()
    {
        AssertSame("ledger-fallback",
            from metal in new[] { "iron", "meteoriciron", "steel" }
            from cover in Covers
            select $"curiocatalogue:book-ledger-{cover} x1 <- B=game:book-{cover} x1, N=game:metalnailsandstrips-{metal} x1, {Ink}",
            Actual("ledger-fallback", 2, 3, false, 1));
    }

    [Fact]
    public void Restore_makes_a_ledger_from_an_old_book_on_a_shelf()
    {
        // An old book is the clutter block, closed or open; the jug must hold 1 litre of limewater.
        var books = OfName("ledger-restore", r => Ingredient(r, "B").Attributes?["type"].AsString()).Order().ToList();
        Assert.Equal(["bookshelves/large-book-closed", "bookshelves/large-book-open"], books);

        var jugs = OfName("ledger-restore", r => $"{Ingredient(r, "J").RecipeAttributes?["requiresContent"]["code"].AsString()} {Ingredient(r, "J").RecipeAttributes?["requiresLitres"].AsInt()}");
        Assert.Equal(["game:limewaterportion 1", "game:limewaterportion 1"], jugs);

        // The two recipes differ only in the clutter's attributes, checked above.
        string one = $"curiocatalogue:book-ledger-normal-darkgreen x1 <- B=game:clutter x1, J=game:jug-*-fired x1, {Ink}";
        Assert.Equal([one, one], Actual("ledger-restore", 3, 1, true, 1));
    }

    [Fact]
    public void Switchmode_turns_a_ledger_into_a_bill_of_materials_and_back()
    {
        AssertSame("ledger-switchmode",
            from cover in Covers
            from pair in new[] { ("ledger", "materials"), ("materials", "ledger") }
            select $"curiocatalogue:book-{pair.Item2}-{cover} x1 <- L=curiocatalogue:book-{pair.Item1}-{cover} x1",
            Actual("ledger-switchmode", 1, 1, false, 2));
    }

    [Fact]
    public void Recharge_takes_parchment_for_any_book_including_aged_ones()
    {
        AssertSame("ledger-recharge",
            from kind in Kinds
            from cover in Covers
            select $"curiocatalogue:book-{kind}-{cover} x1 <- L=curiocatalogue:book-{kind}-{cover} x1, P=game:paper-parchment x1",
            Actual("ledger-recharge", 2, 1, true, 3));
    }

    [Fact]
    public void Album_is_made_from_a_ledger_or_a_bill_of_materials_and_a_tapestry()
    {
        AssertSame("ledger-album",
            from kind in new[] { "ledger", "materials" }
            from cover in Covers
            select $"curiocatalogue:book-album-{cover} x1 <- L=curiocatalogue:book-{kind}-{cover} x1, T=game:tapestry-* x1 tool(0)",
            Actual("ledger-album", 2, 1, true, 4));
    }

    [Fact]
    public void Recolour_gives_the_cover_of_the_leather_and_keeps_the_kind()
    {
        AssertSame("ledger-recolor",
            from leather in Leathers
            from kind in Kinds
            from cover in Covers
            select $"curiocatalogue:book-{kind}-normal-{leather.Value} x1 <- L=curiocatalogue:book-{kind}-{cover} x1, S=game:leather-normal-{leather.Key} x1",
            Actual("ledger-recolor", 2, 1, true, 5));
    }

    [Fact]
    public void Age_takes_a_normal_cover_and_a_jug_of_sulfuric_acid()
    {
        AssertSame("ledger-age",
            from kind in Kinds
            from color in AgedCovers
            select $"curiocatalogue:book-{kind}-aged-{color} x1 <- J=game:jug-*-fired x1, L=curiocatalogue:book-{kind}-normal-{color} x1",
            Actual("ledger-age", 2, 1, true, 6));

        var acid = OfName("ledger-age", r => $"{Ingredient(r, "J").RecipeAttributes?["requiresContent"]["code"].AsString()} {Ingredient(r, "J").RecipeAttributes?["requiresLitres"].AsInt()}").Distinct().ToList();
        Assert.Equal(["game:acid-full-sulfuric 1"], acid);
    }

    [Fact]
    public void Switching_tapestries_off_takes_exactly_their_album_recipes_out_of_the_loaded_ones()
    {
        // K7 (P3): the removal at load (AlbumRecipes.RemoveOfDisabled) on a copy of the server's real list, with the album recipes
        // read from the real definitions. The recipes themselves are shared with the server (CraftingTests needs them), so the
        // ones switched off are switched on again.
        var (removed, albumNames, left, total) = game.OnMain(() =>
        {
            var problems = new CurioCatalogue.ProblemLog(game.Api.Logger);
            var albums = CurioCatalogue.Adapters.CollectionRegistry.AlbumRecipeMap(
                CurioCatalogue.Adapters.CollectionRegistry.Choose(CurioCatalogue.Adapters.DefinitionAsset.OfAllDomains(game.Api.Assets), problems), problems);
            var copy = game.Api.World.GridRecipes.ToList();
            int total = copy.Count;
            var before = copy.ToList();
            int removed = CurioCatalogue.Adapters.AlbumRecipes.Remove(copy, albums, new HashSet<string> { "tapestries" });
            var gone = before.Except(copy).ToList();
            var result = (removed, gone.Select(r => r.Name?.ToString()).Distinct().ToList(), copy.Count(r => r.Name?.Path == "ledger-album"), total);
            foreach (var r in gone) r.Enabled = true;
            return result;
        });

        Assert.Equal(OfName("ledger-album", r => r).Count, removed);
        Assert.True(removed > 0, "no album recipe was found");
        Assert.Equal(["curiocatalogue:ledger-album"], albumNames);
        Assert.Equal(0, left);
        Assert.True(game.OnMain(() => game.Api.World.GridRecipes.Count) == total, "the server's own list is untouched");
    }

    [Fact]
    public void All_our_recipes_are_covered_by_a_test_above()
    {
        // A new recipe name in the JSON without a test here would pass unseen.
        var names = game.OnMain(() => game.Api.World.GridRecipes.Where(r => r.Output?.Code?.Domain == "curiocatalogue").Select(r => r.Name?.Path).Distinct().Order().ToList());
        Assert.Equal(["ledger-age", "ledger-album", "ledger-fallback", "ledger-recharge", "ledger-recolor", "ledger-restore", "ledger-switchmode"], names);
    }
}
