using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

namespace CurioCatalogue.Adapters;

/// <summary>
/// The album recipes of the collections. A collection the server disabled gets none: its recipes are taken out of the server's
/// registries at load, so no client ever receives them (the handbook and the grid do not show them) and the server's grid does not
/// match them (docs/modding.md, Recipes, "Taking a grid recipe away on the server").
/// </summary>
public static class AlbumRecipes
{
    /// <summary>The names (in <c>recipes/grid/ledger.json</c>) of the recipes that make an album of the collection. The album
    /// recipe is the tapestries' in 0.2 until each collection of item 4 adds its own.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> OfCollection = new Dictionary<string, string[]>
    {
        [CollectionRegistry.TapestriesId] = ["ledger-album"],
    };

    /// <summary>Takes the recipes of every disabled collection out of the server's grid recipes; logs what it did.</summary>
    public static void RemoveOfDisabled(ICoreAPI api, IReadOnlyDictionary<string, CollectionSettings> settings)
    {
        var disabled = settings.Where(s => s.Value.Disabled).Select(s => s.Key).ToList();
        if (disabled.Count == 0) return;
        int removed = Remove(api.World.GridRecipes, disabled);
        api.Logger.Notification("[CurioCatalogue] {0}", $"collections switched off: {string.Join(", ", disabled)}; {removed} album recipes removed");
    }

    /// <summary>
    /// Removes the recipes of the disabled collections from the list and switches them off (the server's grid searches its own
    /// index, which only <c>Enabled</c> keeps them out of). Ours only, found by their domain and <c>name</c>. Returns how many.
    /// </summary>
    public static int Remove(IList<GridRecipe> recipes, IEnumerable<string> disabledCollections)
    {
        var names = disabledCollections.SelectMany(id => OfCollection.GetValueOrDefault(id) ?? []).ToHashSet();
        int removed = 0;
        for (int i = recipes.Count - 1; i >= 0; i--)
        {
            var recipe = recipes[i];
            if (recipe.Name is not { } name || name.Domain != CurioCatalogueModSystem.ModId || !names.Contains(name.Path)) continue;
            recipe.Enabled = false;
            recipes.RemoveAt(i);
            removed++;
        }
        return removed;
    }
}
