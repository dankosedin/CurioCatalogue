using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

namespace CurioCatalogue.Adapters;

/// <summary>
/// The album recipes of the collections: each definition names its own (<c>albumRecipe</c>, any domain), so a mod brings a collection
/// with its album by JSON alone. A collection the server switched off gets none: its recipes are taken out of the server's registries
/// at load, so no client ever receives them (the handbook and the grid do not show them) and the server's grid does not match them
/// (docs/modding.md, Recipes, "Taking a grid recipe away on the server").
/// </summary>
public static class AlbumRecipes
{
    /// <summary>
    /// Takes the album recipes of every switched-off collection out of the server's grid recipes; logs what it did. Reads the
    /// definitions itself, as the registry does (<see cref="CollectionRegistry.Choose"/>), but builds no collection: at
    /// <c>AssetsFinalize</c> the world's blocks are not for that yet.
    /// </summary>
    public static void RemoveOfDisabled(ICoreAPI api, IReadOnlyDictionary<string, CollectionSettings> settings,
        IEnumerable<DefinitionAsset> definitions, ProblemLog problems)
    {
        var disabled = settings.Where(s => s.Value.Disabled).Select(s => s.Key).ToHashSet();
        if (disabled.Count == 0) return;
        var albums = CollectionRegistry.AlbumRecipeMap(CollectionRegistry.Choose(definitions, problems), problems);
        int removed = Remove(api.World.GridRecipes, albums, disabled);
        api.Logger.Notification("[CurioCatalogue] {0}", $"collections switched off: {string.Join(", ", disabled)}; {removed} album recipes removed");
    }

    /// <summary>
    /// Removes the album recipes of the switched-off collections from the list and switches them off (the server's grid searches its
    /// own index, which only <c>Enabled</c> keeps them out of). A recipe is found by its <c>Name</c> (domain and path) in
    /// <paramref name="albums"/> (recipe → collection id). Returns how many.
    /// </summary>
    public static int Remove(IList<GridRecipe> recipes, IReadOnlyDictionary<AssetLocation, string> albums, ISet<string> disabledCollections)
    {
        int removed = 0;
        for (int i = recipes.Count - 1; i >= 0; i--)
        {
            var recipe = recipes[i];
            if (recipe.Name is not { } name || !albums.TryGetValue(name, out var collection) || !disabledCollections.Contains(collection)) continue;
            recipe.Enabled = false;
            recipes.RemoveAt(i);
            removed++;
        }
        return removed;
    }
}
