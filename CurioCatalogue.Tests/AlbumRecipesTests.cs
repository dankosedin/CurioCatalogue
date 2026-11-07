using Vintagestory.API.Common;
using CurioCatalogue.Adapters;

namespace CurioCatalogue.Tests;

public class AlbumRecipesTests
{
    // A recipe's Name is an AssetLocation, our recipes' "name" field made into one in our domain (RecipeLoader.cs:124#recipe.Name,
    // docs/modding.md, Recipes). GridRecipe is plain data: Name and Enabled are public fields.
    private static GridRecipe Recipe(string domain, string name) => new() { Name = new AssetLocation(domain, name), Enabled = true };

    /// <summary>The album recipes as the definitions name them (CollectionRegistry.AlbumRecipeMap).</summary>
    private static readonly Dictionary<AssetLocation, string> Albums = new()
    {
        [new AssetLocation("curiocatalogue", "ledger-album")] = "tapestries",
        [new AssetLocation("othermod", "album-ores")] = "ores",
    };

    [Fact]
    public void A_disabled_collections_album_recipes_leave_the_list_and_are_switched_off_and_nothing_else_does()
    {
        var album = Recipe("curiocatalogue", "ledger-album");
        var other = Recipe("curiocatalogue", "ledger-fallback");
        var alien = Recipe("othermod", "ledger-album"); // same name, another domain
        var ores = Recipe("othermod", "album-ores"); // another collection's, which is on
        List<GridRecipe> list = [album, other, alien, ores];

        int removed = AlbumRecipes.Remove(list, Albums, new HashSet<string> { "tapestries" });

        Assert.Equal(1, removed);
        Assert.Equal([other, alien, ores], list);
        Assert.False(album.Enabled);
        Assert.True(other.Enabled && alien.Enabled && ores.Enabled);
    }

    [Fact]
    public void Another_domains_album_recipe_leaves_when_its_collection_is_off()
    {
        var ores = Recipe("othermod", "album-ores");
        List<GridRecipe> list = [ores];

        Assert.Equal(1, AlbumRecipes.Remove(list, Albums, new HashSet<string> { "ores" }));
        Assert.Empty(list);
    }

    [Fact]
    public void Nothing_is_removed_for_collections_that_are_on_or_unknown()
    {
        var album = Recipe("curiocatalogue", "ledger-album");
        List<GridRecipe> list = [album];

        Assert.Equal(0, AlbumRecipes.Remove(list, Albums, new HashSet<string>()));
        Assert.Equal(0, AlbumRecipes.Remove(list, Albums, new HashSet<string> { "vanished" }));
        Assert.Equal([album], list);
        Assert.True(album.Enabled);
    }

    [Fact]
    public void The_album_recipe_our_definitions_name_is_in_the_recipe_file()
    {
        var file = File.ReadAllText(Path.Combine(AssetsTests.AssetsFolder, "recipes", "grid", "ledger.json"));
        var definition = File.ReadAllText(Path.Combine(AssetsTests.AssetsFolder, "config", "curiocatalogue", "collections", "tapestries.json"));
        var read = Assert.IsType<DefinitionRead.Good>(CollectionDefinitionReader.Read("tapestries", definition));
        var recipe = new AssetLocation(read.Definition.Definition.AlbumRecipe!);

        Assert.Equal("curiocatalogue", recipe.Domain);
        Assert.Contains($"name: \"{recipe.Path}\"", file);
    }
}
