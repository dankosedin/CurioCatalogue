using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

    public static readonly AssetLocation TapestryCode = new("game", "tapestry-north");

    private readonly IWorldAccessor world;
    private readonly StackKeys keys;
    private readonly IReadOnlyDictionary<string, CollectionSettings> settings;
    private readonly Func<IEnumerable<DefinitionAsset>> definitions;
    private IReadOnlyDictionary<string, DefinedCollection>? defined;
    private readonly Dictionary<string, Collection> views = [];

    /// <param name="definitions">The definition assets of every domain, patches applied (<see cref="DefinitionsPath"/>); read when the
    /// registry is first used. A function of our own, not the game's asset manager: its interfaces cannot be mocked outside the game.</param>
    public CollectionRegistry(IWorldAccessor world, StackKeys keys, IReadOnlyDictionary<string, CollectionSettings> settings,
        Func<IEnumerable<DefinitionAsset>> definitions)
    {
        this.world = world;
        this.keys = keys;
        this.settings = settings;
        this.definitions = definitions;
    }

    /// <summary>The collections that are on and have members, by id.</summary>
    public IReadOnlyDictionary<string, DefinedCollection> All => defined ??= Load();

    public DefinedCollection? Defined(string id) => All.GetValueOrDefault(id);

    /// <summary>
    /// The collection as the MVP's album code reads it, until K4 of the collections core plan moves the album to members: each member
    /// by its first filler, in the default layout's order, grouped by the default layout's grouping values.
    /// </summary>
    public Collection? Get(string id)
    {
        if (views.TryGetValue(id, out var view)) return view;
        if (Defined(id) is not { } built || built.Levels.Count == 0) return null;
        var level = built.Levels[0];
        var groupBy = level.DefaultLayout.GroupBy;
        return views[id] = new Collection(id, level.Order.Select(m =>
            new CollectionMember(m.Fillers[0], string.Join(DefinedCollection.IdSeparator, groupBy.Select(d => m.Values[d])), m.Stacks[0])));
    }

    /// <summary>The tapestries: the collection of tapestry pieces, or null when the game has no tapestry block or it is off.</summary>
    public Collection? Tapestries => Get(TapestriesId);

    /// <summary>The collection a sample item belongs to (the narrowest, once there are several), or null.</summary>
    public Collection? ForSample(ItemStack sample)
    {
        var key = keys.KeyOf(sample);
        return All.Keys.Select(Get).FirstOrDefault(c => c != null && c.Contains(key));
    }

    private Dictionary<string, DefinedCollection> Load()
    {
        var result = new Dictionary<string, DefinedCollection>();
        var seen = new Dictionary<string, string>();
        foreach (var asset in definitions())
        {
            if (!asset.Location.Path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
            var id = System.IO.Path.GetFileNameWithoutExtension(asset.Location.Path);
            var origin = asset.Location.ToString();
            if (!seen.TryAdd(id, origin))
            {
                Problem(true, $"collection '{id}' is defined twice ({seen[id]} and {origin}); the second is ignored");
                continue;
            }

            switch (CollectionDefinitionReader.Read(id, asset.Text))
            {
                case DefinitionRead.Bad bad:
                    Problem(true, $"collection '{id}' ({origin}) cannot be used: {string.Join("; ", bad.Errors)}");
                    continue;
                case DefinitionRead.Good good:
                    if (Build(good.Definition, origin) is { } collection) result[id] = collection;
                    break;
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

        List<SourcePoint>? points;
        try
        {
            points = Points(parsed.Source, config.Add);
        }
        catch (FormatException e)
        {
            Problem(true, $"collection '{id}' ({origin}) cannot be used: {e.Message}");
            return null;
        }
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
                Problem(true, $"collection '{id}' ({origin}) cannot be used: {string.Join("; ", invalid.Errors)}");
                return null;
            case CollectionBuild.Built built:
                foreach (var warning in built.Warnings) Problem(false, $"collection '{id}': {warning}");
                ModLog.Debug(world.Api, $"collection {id}: {string.Join(", ", built.Collection.Levels.Select(l => $"{l.Id} {l.Members.Count} members"))}");
                return built.Collection;
            default:
                throw ExhaustiveMatching.ExhaustiveMatch.Failed(build);
        }
    }

    private void Problem(bool error, string text)
    {
        if (error) world.Api.Logger.Error("[CurioCatalogue] {0}", text);
        else world.Api.Logger.Warning("[CurioCatalogue] {0}", text);
    }

    private List<SourcePoint>? Points(CollectionSource source, IReadOnlyList<string> add) => source switch
    {
        CollectionSource.Tapestry t => TapestryPoints(t, add),
        CollectionSource.Variants v => VariantPoints(v),
        _ => throw ExhaustiveMatching.ExhaustiveMatch.Failed(source),
    };

    /// <summary>
    /// All tapestry pieces: from the tapestry block's "sizes" attribute, plus the config's additions (each its own picture).
    /// Assumption: a piece is the block with its name in the string attribute "type". A tapestry taken off a
    /// wall of any side drops as tapestry-north (vssurvivalmod BehaviorHorizontalAttachable.cs:66, dropBlockFace defaults to
    /// "north"; modding.md, Vanilla content we rely on). Only creative pick block gives another side (vssurvivalmod
    /// BlockTapestry.cs#OnPickBlock, "new ItemStack(this)"), and such a stack is not a member.
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

    /// <summary>
    /// Every registered item and block whose code fits the pattern; each <c>{name}</c> of the pattern takes the text between dashes
    /// (<c>[^-]+</c>) and is that dimension's value. The point's name is the code without domain.
    /// </summary>
    private List<SourcePoint> VariantPoints(CollectionSource.Variants source)
    {
        var pattern = new AssetLocation(source.Code);
        var names = new List<string>();
        var regex = new Regex("^" + Regex.Replace(Regex.Escape(pattern.Path).Replace("\\{", "{"), @"\{(\w+)\}", m =>
        {
            names.Add(m.Groups[1].Value);
            return $"(?<{m.Groups[1].Value}>[^-]+)";
        }) + "$", RegexOptions.CultureInvariant);

        var points = new List<SourcePoint>();
        foreach (var collectible in world.Collectibles)
        {
            if (collectible?.Code == null || collectible.Code.Domain != pattern.Domain) continue;
            var match = regex.Match(collectible.Code.Path);
            if (!match.Success) continue;
            var stack = new ItemStack(collectible);
            points.Add(new SourcePoint(collectible.Code.Path, names.ToDictionary(n => n, n => match.Groups[n].Value), keys.KeyOf(stack), stack));
        }
        return points;
    }
}

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
