using Vintagestory.API.Common;
using CurioCatalogue.Adapters;

namespace CurioCatalogue.Tests;

public class AlbumRecipesTests
{
    // A recipe's Name is an AssetLocation, our recipes' "name" field made into one in our domain (RecipeLoader.cs:124#recipe.Name,
    // docs/modding.md, Recipes). GridRecipe is plain data: Name and Enabled are public fields.
    private static GridRecipe Recipe(string domain, string name) => new() { Name = new AssetLocation(domain, name), Enabled = true };

    [Fact]
    public void A_disabled_collections_album_recipes_leave_the_list_and_are_switched_off_and_nothing_else_does()
    {
        var album = Recipe("curiocatalogue", "ledger-album");
        var other = Recipe("curiocatalogue", "ledger-fallback");
        var alien = Recipe("othermod", "ledger-album"); // same name, not ours
        var vanilla = Recipe("game", "ledger-album");
        List<GridRecipe> list = [album, other, alien, vanilla];

        int removed = AlbumRecipes.Remove(list, ["tapestries"]);

        Assert.Equal(1, removed);
        Assert.Equal([other, alien, vanilla], list);
        Assert.False(album.Enabled);
        Assert.True(other.Enabled && alien.Enabled && vanilla.Enabled);
    }

    [Fact]
    public void Nothing_is_removed_for_collections_that_are_on_or_unknown()
    {
        var album = Recipe("curiocatalogue", "ledger-album");
        List<GridRecipe> list = [album];

        Assert.Equal(0, AlbumRecipes.Remove(list, []));
        Assert.Equal(0, AlbumRecipes.Remove(list, ["vanished"]));
        Assert.Equal([album], list);
        Assert.True(album.Enabled);
    }

    [Fact]
    public void Every_recipe_the_table_names_is_in_the_recipe_file()
    {
        var file = File.ReadAllText(Path.Combine(AssetsTests.AssetsFolder, "recipes", "grid", "ledger.json"));
        foreach (var name in AlbumRecipes.OfCollection.Values.SelectMany(n => n))
            Assert.Contains($"name: \"{name}\"", file);
    }
}
