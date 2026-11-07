using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using CurioCatalogue.Core;

namespace CurioCatalogue.Adapters;

/// <summary>
/// The collections the mod knows (domain.md, Albums and collections): the definitions of every domain's
/// <c>config/curiocatalogue/collections/*.json</c> (patched by other mods or by the server's admin), their points found by the
/// definition's source in the game's own data, adjusted by the config's <c>Collections.&lt;id&gt;</c>. Built on first use, the same on
/// the server and the client. A definition that cannot be used is logged and skipped; the others work (docs/architecture.md, Part 1,
/// Collections).
/// </summary>
public sealed class CollectionRegistry
{
    public const string TapestriesId = CurioCatalogueConfig.TapestriesId;

    /// <summary>Where the definitions are, in every domain.</summary>
    public const string DefinitionsPath = "config/curiocatalogue/collections/";

    private readonly IWorldAccessor world;
    private readonly StackKeys keys;
    private readonly IReadOnlyDictionary<string, CollectionSettings> settings;
    private readonly Func<IEnumerable<DefinitionAsset>> definitions;
    private readonly ProblemLog problems;
    private IReadOnlyList<ChosenDefinition>? chosen;
    private IReadOnlyDictionary<AssetLocation, string>? albumRecipes;
    private IReadOnlyDictionary<string, DefinedCollection>? defined;

    /// <param name="definitions">The definition assets of every domain, patches applied (<see cref="DefinitionsPath"/>); read when the
    /// registry is first used. A function of our own, not the game's asset manager: its interfaces cannot be mocked outside the game.</param>
    /// <param name="problems">Where problems with the definitions and settings are written, once per session; by default a log of
    /// this registry's own.</param>
    public CollectionRegistry(IWorldAccessor world, StackKeys keys, IReadOnlyDictionary<string, CollectionSettings> settings,
        Func<IEnumerable<DefinitionAsset>> definitions, ProblemLog? problems = null)
    {
        this.world = world;
        this.keys = keys;
        this.settings = settings;
        this.definitions = definitions;
        this.problems = problems ?? new ProblemLog(world.Api.Logger);
    }

    /// <summary>The collections that are on and have members, by id.</summary>
    public IReadOnlyDictionary<string, DefinedCollection> All => defined ??= Load();

    public DefinedCollection? Defined(string id) => All.GetValueOrDefault(id);

    /// <summary>What the game says of a collection: <see cref="CollectionResolution.Disabled"/> when the config switches it off (even
    /// if no definition is there), else found when it is built, else unknown.</summary>
    public CollectionResolution Resolve(string id) =>
        settings.TryGetValue(id, out var config) && config.Disabled ? new CollectionResolution.Disabled(id)
        : Defined(id) is { } found ? new CollectionResolution.Found(found)
        : new CollectionResolution.Unknown(id);

    /// <summary>The id of the collection whose album <paramref name="recipe"/> makes (the definition's <c>albumRecipe</c>), switched
    /// off or not; null for any other recipe. Needs no collection built, only the definitions.</summary>
    public string? AlbumOf(AssetLocation? recipe) =>
        recipe != null && (albumRecipes ??= AlbumRecipeMap(Chosen, problems)).TryGetValue(recipe, out var id) ? id : null;

    /// <summary>Whether the stack fills a member of the collection at any of its levels (the sample of an album recipe).</summary>
    public bool HasMember(DefinedCollection collection, ItemStack stack)
    {
        var key = keys.KeyOf(stack);
        return collection.Levels.Any(l => l.MemberOf(key) != null);
    }

    private IReadOnlyList<ChosenDefinition> Chosen => chosen ??= Choose(definitions(), problems);

    // ---- the definitions: which one of an id is used

    /// <summary>
    /// The definition used for each id: of the assets that read well (<see cref="DefinitionRead.Good"/>), ours (domain
    /// <c>curiocatalogue</c>) first, then in the order the game gives them. A bad one is logged and does not take the id; a good one
    /// after the first is logged and ignored. The registry and the album recipes at load both choose this way, so they agree.
    /// </summary>
    public static IReadOnlyList<ChosenDefinition> Choose(IEnumerable<DefinitionAsset> assets, ProblemLog problems)
    {
        var chosen = new Dictionary<string, ChosenDefinition>();
        var order = new List<string>();
        var jsons = assets
            .Where(a => a.Location.Path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.Location.Domain == CurioCatalogueModSystem.ModId ? 0 : 1); // stable: the game's order otherwise
        foreach (var asset in jsons)
        {
            var id = System.IO.Path.GetFileNameWithoutExtension(asset.Location.Path);
            var origin = asset.Location.ToString();
            switch (CollectionDefinitionReader.Read(id, asset.Text))
            {
                case DefinitionRead.Bad bad:
                    problems.Error($"collection '{id}' ({origin}) cannot be used: {string.Join("; ", bad.Errors)}");
                    break;
                case DefinitionRead.Good when chosen.TryGetValue(id, out var first):
                    problems.Error($"collection '{id}' is defined twice ({first.Origin} and {origin}); the second is ignored");
                    break;
                case DefinitionRead.Good good:
                    chosen[id] = new ChosenDefinition(good.Definition, origin);
                    order.Add(id);
                    break;
            }
        }
        return order.Select(id => chosen[id]).ToList();
    }

    /// <summary>The album recipes of the definitions, recipe → collection id; a recipe two definitions name is the first's.</summary>
    public static IReadOnlyDictionary<AssetLocation, string> AlbumRecipeMap(IEnumerable<ChosenDefinition> chosen, ProblemLog problems)
    {
        var map = new Dictionary<AssetLocation, string>();
        foreach (var c in chosen)
        {
            if (c.Parsed.Definition.AlbumRecipe is not { } name) continue;
            var recipe = new AssetLocation(name);
            if (map.TryGetValue(recipe, out var other))
                problems.Error($"collection '{c.Parsed.Definition.Id}' ({c.Origin}) names the album recipe {recipe}, which is already '{other}''s; it gets none");
            else map[recipe] = c.Parsed.Definition.Id;
        }
        return map;
    }

    // ---- building

    private Dictionary<string, DefinedCollection> Load()
    {
        var result = new Dictionary<string, DefinedCollection>();
        foreach (var id in settings.Keys.Where(id => Chosen.All(c => c.Parsed.Definition.Id != id)))
            problems.Warning($"Collections.{id} in the config: no collection has this id (a mod removed, or a typo?)");

        foreach (var c in Chosen)
        {
            // A guard: whatever a definition or the game's data does, the other collections still work.
            try
            {
                if (Build(c.Parsed, c.Origin) is { } collection) result[collection.Id] = collection;
            }
            catch (Exception e)
            {
                problems.Error($"collection '{c.Parsed.Definition.Id}' ({c.Origin}) cannot be used: {e.GetType().Name}: {e.Message}");
            }
        }
        return result;
    }

    private DefinedCollection? Build(ParsedDefinition parsed, string origin)
    {
        var id = parsed.Definition.Id;
        var config = settings.GetValueOrDefault(id) ?? new CollectionSettings();
        if (config.Disabled)
        {
            ModLog.Debug(world.Api, $"collection {id} is disabled in the config");
            return null;
        }
        if (parsed.Source is CollectionSource.Variants && config.Add.Length > 0)
            problems.Warning($"Collections.{id}.Add in the config is ignored: only a tapestry collection can add a member by name");

        var points = Points(parsed.Source, config.Add);
        if (points == null || points.Count == 0)
        {
            ModLog.Debug(world.Api, $"collection {id}: its source finds nothing, so there is no collection");
            return null;
        }

        var definition = parsed.Definition with { Exclude = [.. parsed.Definition.Exclude, .. config.Exclude] };
        var build = DefinedCollection.Build(definition, points);
        switch (build)
        {
            case CollectionBuild.Invalid invalid:
                problems.Error($"collection '{id}' ({origin}) cannot be used: {string.Join("; ", invalid.Errors)}");
                return null;
            case CollectionBuild.Built built:
                foreach (var warning in built.Warnings) problems.Warning($"collection '{id}': {warning}");
                ModLog.Debug(world.Api, $"collection {id}: {string.Join(", ", built.Collection.Levels.Select(l => $"{l.Id} {l.Members.Count} members"))}");
                return built.Collection;
            default:
                throw ExhaustiveMatching.ExhaustiveMatch.Failed(build);
        }
    }

    private List<SourcePoint>? Points(CollectionSource source, IReadOnlyList<string> add) => source switch
    {
        CollectionSource.Tapestry t => TapestryPoints(t, add),
        CollectionSource.Variants v => VariantPoints(v.Pattern),
        _ => throw ExhaustiveMatching.ExhaustiveMatch.Failed(source),
    };

    /// <summary>
    /// All tapestry pieces: from the tapestry block's "sizes" attribute, plus the config's additions (each its own picture).
    /// Assumption: a piece is the block with its name in the string attribute "type". A tapestry taken off a
    /// wall of any side drops as tapestry-north (vssurvivalmod BehaviorHorizontalAttachable.cs:66, dropBlockFace defaults to
    /// "north"; modding.md, Vanilla content we rely on). Only creative pick block gives another side (vssurvivalmod
    /// BlockTapestry.cs#OnPickBlock, "new ItemStack(this)"), and such a stack is not a member. A "sizes" of another shape throws,
    /// and the guard in <see cref="Load"/> logs it.
    /// </summary>
    private List<SourcePoint>? TapestryPoints(CollectionSource.Tapestry source, IReadOnlyList<string> add)
    {
        var block = world.GetBlock(new AssetLocation(source.Block));
        var sizes = block?.Attributes?["sizes"].AsObject<Dictionary<string, string>>();
        if (block == null || sizes == null) return null;

        return TapestryPieces.WithPictures(sizes)
            .Concat(add.Select(name => new TapestryPiece(name, name)))
            .Select(p =>
            {
                var stack = new ItemStack(block);
                stack.Attributes.SetString("type", p.Piece);
                return new SourcePoint(p.Piece,
                    new Dictionary<string, string> { ["piece"] = p.Piece, ["picture"] = p.Picture },
                    keys.KeyOf(stack), stack,
                    new Dictionary<string, string>
                    {
                        [Arrangement.Shape.X] = p.X.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        [Arrangement.Shape.Y] = p.Y.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    });
            }).ToList();
    }

    /// <summary>Every registered item and block whose code fits the pattern. The point's name is the code without domain.</summary>
    private List<SourcePoint> VariantPoints(VariantPattern pattern)
    {
        var points = new List<SourcePoint>();
        foreach (var collectible in world.Collectibles)
        {
            if (collectible?.Code == null || collectible.Code.Domain != pattern.Domain) continue;
            var match = pattern.Regex.Match(collectible.Code.Path);
            if (!match.Success) continue;
            var stack = new ItemStack(collectible);
            points.Add(new SourcePoint(collectible.Code.Path, pattern.Names.ToDictionary(n => n, n => match.Groups[n].Value), keys.KeyOf(stack), stack));
        }
        return points;
    }
}

/// <summary>The definition used for an id, and the asset it came from (for the log).</summary>
public sealed record ChosenDefinition(ParsedDefinition Parsed, string Origin);

/// <summary>A definition asset: where it is and what it says.</summary>
public sealed record DefinitionAsset(AssetLocation Location, string Text)
{
    /// <summary>
    /// Every definition of every domain: <c>GetMany</c> without a domain finds the path in all of them, and the patches are in
    /// when it is read after <c>AssetsLoaded</c> (<c>docs/modding.md</c>, Config and logging).
    /// </summary>
    public static IEnumerable<DefinitionAsset> OfAllDomains(IAssetManager assets) =>
        assets.GetMany(CollectionRegistry.DefinitionsPath).Select(a => new DefinitionAsset(a.Location, a.ToText()));
}
