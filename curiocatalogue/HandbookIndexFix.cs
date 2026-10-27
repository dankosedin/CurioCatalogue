using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;

namespace CurioCatalogue;

/// <summary>
/// Lets the handbook find our recipes from the pages of their ingredients ("Ingredient for").
/// The handbook first looks a page up in the world's index of recipe ingredients, and only then checks each recipe's
/// own ingredient with its attributes (vssurvivalmod CollectibleBehaviorHandbookTextAndExtraInfo.cs:1117-1126#FastSearchRecipesByIngredient). The
/// index merges ingredients that differ only in attributes into one entry, and its sample stack keeps the attributes of
/// whichever recipe was resolved last, although the game means it to have none (vsapi CraftingRecipeIngredient.cs:151#Attributes, CraftingRecipeIngredient.cs:182#FastSearchRecipesByIngredient).
/// Our two restoration recipes take two kinds of the same clutter block, so the page of one of them never passed the
/// first step (checked in game 26.10.2026: "large-book-open" left in the sample, the closed book's page empty).
/// Giving the entries our recipes use a sample without attributes restores what the game intends; the second step still
/// checks each recipe's own attributes. Client only: only the handbook reads the samples this way.
/// The sample must be replaced, not changed: on the client it is the very stack the last ingredient keeps as its own
/// (vsapi CraftingRecipeIngredient.cs:224-225#ResolvedItemStack). Clearing it made that recipe show plain clutter in the handbook (seen in
/// game 27.10.2026), and the client's crafting preview would have taken any clutter for it.
/// </summary>
internal static class HandbookIndexFix
{
    public static void Apply(ICoreClientAPI capi)
    {
        int cleared = 0;
        foreach (var entry in capi.World.FastSearchRecipesByIngredient)
        {
            if (!entry.Value.Any(r => r.Name?.Domain == CurioCatalogueModSystem.ModId)) continue;
            if (entry.Key.ResolvedItemStack is not { } sample || sample.Attributes.Count == 0) continue;
            var plain = sample.Clone();
            plain.Attributes = new TreeAttribute();
            entry.Key.ResolvedItemStack = plain;
            cleared++;
        }
        ModLog.Debug(capi, $"handbook: cleared the attributes of {cleared} ingredient index samples used by our recipes");
    }
}
