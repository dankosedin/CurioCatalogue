using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ExhaustiveMatching;
using CurioCatalogue.Core;

namespace CurioCatalogue.Adapters;

/// <summary>Where a collection's points come from: one of the closed set of kinds written in code (docs/architecture.md, Part 1,
/// Collections).</summary>
[Closed(typeof(CollectionSource.Variants), typeof(CollectionSource.Tapestry))]
public abstract record CollectionSource
{
    /// <summary>The registered items and blocks whose code fits <paramref name="Code"/> (<c>game:crystalizedore-{grade}-{ore}-{rock}</c>):
    /// each <c>{name}</c> is a dimension and takes the text between dashes.</summary>
    public sealed record Variants(string Code) : CollectionSource;

    /// <summary>The pieces of the block's <c>sizes</c> attribute (<c>game:tapestry-north</c>): dimensions <c>picture</c> and
    /// <c>piece</c>, the piece's place as attributes <c>x</c> and <c>y</c>.</summary>
    public sealed record Tapestry(string Block) : CollectionSource;
}

/// <summary>A definition asset as read: the definition (the core's type) and the source its points come from.</summary>
public sealed record ParsedDefinition(CollectionDefinition Definition, CollectionSource Source);

/// <summary>The result of reading one definition asset; a bad asset is <see cref="Bad"/> and leaves the other collections alone.</summary>
[Closed(typeof(DefinitionRead.Good), typeof(DefinitionRead.Bad))]
public abstract record DefinitionRead
{
    public sealed record Good(ParsedDefinition Definition) : DefinitionRead;

    public sealed record Bad(string Id, IReadOnlyList<string> Errors) : DefinitionRead;
}

/// <summary>
/// Reads the JSON of a definition asset (<c>config/curiocatalogue/collections/&lt;id&gt;.json</c>) into the core's types. The id is
/// the file's name. The shape of the JSON is in <c>docs/architecture.md</c> and the definition <c>tapestries.json</c> is its example.
/// </summary>
public static class CollectionDefinitionReader
{
    public static DefinitionRead Read(string id, string json)
    {
        var errors = new List<string>();
        JObject root;
        try
        {
            root = JObject.Parse(json);
        }
        catch (JsonException e)
        {
            return new DefinitionRead.Bad(id, [$"Not valid JSON: {e.Message}"]);
        }

        var source = ReadSource(root["source"], errors);
        var dimensions = Items(root, "dimensions", errors).Select(d => ReadDimension(d, errors)).ToList();
        var levels = Items(root, "levels", errors).Select(l => ReadLevel(l, errors)).ToList();
        var rules = Items(root, "rules", errors, required: false).Select(r => ReadRule(r, errors)).ToList();
        var exclude = Strings(root["exclude"], "exclude", errors);
        var nameKey = Text(root, "nameKey", errors, required: false);

        if (errors.Count > 0 || source == null) return new DefinitionRead.Bad(id, errors);
        return new DefinitionRead.Good(new ParsedDefinition(
            new CollectionDefinition(id, dimensions!, levels!, rules!, exclude, nameKey), source));
    }

    private static CollectionSource? ReadSource(JToken? token, List<string> errors)
    {
        if (token is not JObject o) { errors.Add("'source' is missing or not an object"); return null; }
        switch (Text(o, "kind", errors))
        {
            case "variants": return Text(o, "code", errors) is { } code ? new CollectionSource.Variants(code) : null;
            case "tapestry": return Text(o, "block", errors) is { } block ? new CollectionSource.Tapestry(block) : null;
            case { } other: errors.Add($"Unknown source kind '{other}' (variants, tapestry)"); return null;
            default: return null;
        }
    }

    private static DimensionDef? ReadDimension(JToken token, List<string> errors)
    {
        if (token is not JObject o) { errors.Add("A dimension is not an object"); return null; }
        var code = Text(o, "code", errors);
        var values = Strings(o["values"], $"dimension '{code}' values", errors);
        return code == null
            ? null
            : new DimensionDef(code, values, Text(o, "nameKey", errors, required: false), Text(o, "icon", errors, required: false),
                Text(o, "anyKey", errors, required: false));
    }

    private static LevelDef? ReadLevel(JToken token, List<string> errors)
    {
        if (token is not JObject o) { errors.Add("A level is not an object"); return null; }
        var id = Text(o, "id", errors);
        var keep = Strings(o["keep"], $"level '{id}' keep", errors);
        var drop = new Dictionary<string, Drop>();
        if (o["drop"] is JObject drops)
        {
            foreach (var (dimension, value) in drops)
            {
                switch (value?.Type == JTokenType.String ? ((string)value!).ToLowerInvariant() : null)
                {
                    case "any": drop[dimension] = Drop.Any; break;
                    case "max": drop[dimension] = Drop.Max; break;
                    default: errors.Add($"Level '{id}': drop of '{dimension}' must be \"any\" or \"max\""); break;
                }
            }
        }
        else if (o["drop"] != null) errors.Add($"Level '{id}': 'drop' is not an object");
        var layouts = Items(o, "layouts", errors, required: false).Select(l => ReadLayout(l, id, errors)).ToList();
        return id == null ? null : new LevelDef(id, keep, drop, layouts!, Text(o, "nameKey", errors, required: false));
    }

    private static LayoutDef? ReadLayout(JToken token, string? level, List<string> errors)
    {
        if (token is not JObject o) { errors.Add($"Level '{level}': a layout is not an object"); return null; }
        var id = Text(o, "id", errors);
        var groupBy = Strings(o["groupBy"], $"layout '{id}' groupBy", errors);
        Arrangement? arrange = null;
        if (o["arrange"] is JObject a)
        {
            switch (Text(a, "kind", errors))
            {
                case "table":
                    var rows = Text(a, "rows", errors);
                    var columns = Text(a, "columns", errors);
                    if (rows != null && columns != null) arrange = new Arrangement.Table(rows, columns);
                    break;
                case "shape": arrange = new Arrangement.Shape(); break;
                case "order": arrange = new Arrangement.InOrder(); break;
                case { } other: errors.Add($"Layout '{id}': unknown arrangement '{other}' (table, shape, order)"); break;
            }
        }
        else if (o["arrange"] == null) arrange = new Arrangement.InOrder();
        else errors.Add($"Layout '{id}': 'arrange' is not an object");
        return id == null || arrange == null ? null : new LayoutDef(id, groupBy, arrange, Text(o, "nameKey", errors, required: false));
    }

    private static AttributeRule ReadRule(JToken token, List<string> errors)
    {
        var where = new Dictionary<string, IReadOnlyList<string>>();
        var set = new Dictionary<string, string>();
        if (token is not JObject o) { errors.Add("A rule is not an object"); return new AttributeRule(where, set); }
        if (o["where"] is JObject w)
            foreach (var (dimension, values) in w) where[dimension] = Strings(values, $"rule where '{dimension}'", errors);
        else errors.Add("A rule has no 'where' object");
        if (o["set"] is JObject s)
        {
            foreach (var (name, value) in s)
            {
                if (value?.Type == JTokenType.String) set[name] = (string)value!;
                else errors.Add($"A rule sets '{name}' to something that is not a text");
            }
        }
        else errors.Add("A rule has no 'set' object");
        return new AttributeRule(where, set);
    }

    private static IEnumerable<JToken> Items(JObject o, string name, List<string> errors, bool required = true)
    {
        if (o[name] is JArray a) return a;
        if (o[name] != null) errors.Add($"'{name}' is not an array");
        else if (required) errors.Add($"'{name}' is missing");
        return [];
    }

    private static string? Text(JObject o, string name, List<string> errors, bool required = true)
    {
        if (o[name] is { Type: JTokenType.String } t) return (string)t!;
        if (o[name] != null) errors.Add($"'{name}' is not a text");
        else if (required) errors.Add($"'{name}' is missing");
        return null;
    }

    private static List<string> Strings(JToken? token, string what, List<string> errors)
    {
        if (token == null) return [];
        if (token is not JArray a || a.Any(i => i.Type != JTokenType.String)) { errors.Add($"{what} is not a list of texts"); return []; }
        return a.Select(i => (string)i!).ToList();
    }
}
