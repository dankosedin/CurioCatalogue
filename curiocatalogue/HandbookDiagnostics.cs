using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace CurioCatalogue;

/// <summary>
/// Diagnostics for the handbook (docs/plans/2026-10-25-mvp.md, stage 9): the large book's page does not list the restoration recipe under
/// "Ingredient for", although crafting works. Logs the page's stacks and, for each ingredient of our restoration
/// recipes, whether the handbook's own checks accept the page (vssurvivalmod CollectibleBehaviorHandbookTextAndExtraInfo.cs:1117#FastSearchRecipesByIngredient).
/// Client only, once the world is in; only with Diagnostics on.
/// </summary>
internal static class HandbookDiagnostics
{
    public static void Log(ICoreClientAPI capi)
    {
        if (!ModLog.DebugEnabled) return;

        var clutter = capi.World.GetBlock(new AssetLocation("game", "clutter"));
        var pages = clutter?.CreativeInventoryStacks?.SelectMany(t => t.Stacks).Select(j => j.ResolvedItemstack)
            .Where(s => s?.Attributes.GetString("type")?.Contains("large-book") == true).ToList() ?? [];
        foreach (var page in pages) ModLog.Debug(capi, $"handbook: large book page {page!.Collectible.Code} {page.Attributes.ToJsonToken()}");

        var restore = capi.World.GridRecipes.Where(r => r.Name?.Domain == CurioCatalogueModSystem.ModId && r.Name.Path == "ledger-restore").ToList();
        ModLog.Debug(capi, $"handbook: {restore.Count} restoration recipes on the client");
        foreach (var recipe in restore)
        {
            foreach (var ingredient in recipe.ResolvedIngredients?.Where(i => i != null) ?? [])
            {
                var stack = ingredient!.ResolvedItemStack;
                string accepts = string.Join(", ", pages.Select(p => ingredient.SatisfiesAsIngredient(p, false)));
                ModLog.Debug(capi, $"handbook:   {ingredient.Code} resolved={stack?.Collectible?.Code} attrs={stack?.Attributes.ToJsonToken()} accepts pages: {accepts}");
            }
            ModLog.Debug(capi, $"handbook:   output {recipe.Output?.ResolvedItemStack?.Collectible?.Code}");
        }

        // The handbook's first, coarse step: an entry of the fast search index that accepts the page.
        foreach (var page in pages)
        {
            var found = capi.World.FastSearchRecipesByIngredient
                .Where(e => e.Key.SatisfiesAsIngredient(page!))
                .SelectMany(e => e.Value).OfType<GridRecipe>()
                .Count(r => r.Name?.Domain == CurioCatalogueModSystem.ModId);
            ModLog.Debug(capi, $"handbook: fast search finds {found} of our grid recipes for {page!.Attributes.GetString("type")}");
        }
    }
}
