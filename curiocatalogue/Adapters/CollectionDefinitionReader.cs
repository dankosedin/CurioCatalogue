using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ExhaustiveMatching;
using CurioCatalogue.Core;

namespace CurioCatalogue.Adapters;

/// <summary>Where a collection's points come from: one of the closed set of kinds written in code (docs/decisions.md,
/// Collections).</summary>
[Closed(typeof(CollectionSource.Variants), typeof(CollectionSource.Tapestry))]
public abstract record CollectionSource
{
    /// <summary>The registered items and blocks whose code fits the pattern (<c>game:crystalizedore-{grade}-{ore}-{rock}</c>):
    /// each <c>{name}</c> is a dimension and takes the text between dashes.</summary>
    public sealed record Variants(VariantPattern Pattern) : CollectionSource;

    /// <summary>The pieces of the block's <c>sizes</c> attribute (<c>game:tapestry-north</c>): dimensions <c>picture</c>,
    /// <c>piece</c> and <c>section</c> (its place for words, <see cref="Core.TapestryPiece.Section"/>), the piece's place as
    /// attributes <c>x</c> and <c>y</c>.</summary>
    public sealed record Tapestry(string Block) : CollectionSource;
}

/// <summary>
/// The code of a <see cref="CollectionSource.Variants"/> source made into a test of item codes. Each <c>{name}</c> takes the text
/// between dashes (<c>[^-]+</c>) and is that dimension's value; a name is a letter or <c>_</c>, then letters, digits or <c>_</c>, and
/// appears once.
/// </summary>
public sealed record VariantPattern(string Code, string Domain, System.Text.RegularExpressions.Regex Regex, IReadOnlyList<string> Names)
{
    private static readonly System.Text.RegularExpressions.Regex Placeholder = new(@"\{([^{}]*)\}");
    private static readonly System.Text.RegularExpressions.Regex ValidName = new(@"^[A-Za-z_][A-Za-z0-9_]*$");

    /// <summary>The pattern, or null with the reasons in <paramref name="errors"/>: a name that is not one, or that comes twice,
    /// would make the regex throw or a value ambiguous.</summary>
    public static VariantPattern? Parse(string code, List<string> errors)
    {
        var location = new Vintagestory.API.Common.AssetLocation(code);
        var names = new List<string>();
        int before = errors.Count;
        foreach (System.Text.RegularExpressions.Match m in Placeholder.Matches(location.Path))
        {
            var name = m.Groups[1].Value;
            if (!ValidName.IsMatch(name)) errors.Add($"'{{{name}}}' in the code '{code}' is not a name (a letter, then letters, digits or _)");
            else if (names.Contains(name)) errors.Add($"'{{{name}}}' comes twice in the code '{code}'");
            else names.Add(name);
        }
        if (errors.Count > before) return null;

        var regex = "^" + Placeholder.Replace(System.Text.RegularExpressions.Regex.Escape(location.Path).Replace("\\{", "{"),
            m => $"(?<{m.Groups[1].Value}>[^-]+)") + "$";
        try
        {
            return new VariantPattern(code, location.Domain, new(regex, System.Text.RegularExpressions.RegexOptions.CultureInvariant), names);
        }
        catch (ArgumentException e)
        {
            errors.Add($"The code '{code}' makes no pattern: {e.Message}");
            return null;
        }
    }
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
        var marks = ReadMarks(root["marks"], errors);
        var exclude = Strings(root["exclude"], "exclude", errors);
        var nameKey = Text(root, "nameKey", errors, required: false);
        var albumRecipe = Text(root, "albumRecipe", errors, required: false);
        if (albumRecipe != null && !albumRecipe.Contains(':'))
            errors.Add($"'albumRecipe' must name the recipe's domain ('curiocatalogue:ledger-album', not '{albumRecipe}')");

        if (errors.Count > 0 || source == null) return new DefinitionRead.Bad(id, errors);
        var definition = new CollectionDefinition(id, dimensions!, levels!, rules!, exclude, nameKey, albumRecipe, marks);
        // Good means the core accepts the definition too, whatever the game has: the registry and the recipes choose among the
        // good ones (docs/decisions.md, Collections).
        var invalid = DefinedCollection.CheckDefinition(definition);
        return invalid.Count > 0 ? new DefinitionRead.Bad(id, invalid) : new DefinitionRead.Good(new ParsedDefinition(definition, source));
    }

    private static CollectionSource? ReadSource(JToken? token, List<string> errors)
    {
        if (token is not JObject o) { errors.Add("'source' is missing or not an object"); return null; }
        switch (Text(o, "kind", errors))
        {
            case "variants":
                return Text(o, "code", errors) is { } code && VariantPattern.Parse(code, errors) is { } pattern
                    ? new CollectionSource.Variants(pattern) : null;
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
                Text(o, "anyKey", errors, required: false), Text(o, "shortNameKey", errors, required: false),
                TextMap(o, "icons", $"dimension '{code}'", errors), Text(o, "from", errors, required: false),
                ListMap(o, "map", $"dimension '{code}'", errors));
    }

    /// <summary>An optional object of lists of texts (a derived dimension's <c>map</c>, value → its source values); null when absent.</summary>
    private static Dictionary<string, IReadOnlyList<string>>? ListMap(JObject o, string name, string what, List<string> errors)
    {
        if (o[name] == null) return null;
        if (o[name] is not JObject map) { errors.Add($"{what}: '{name}' is not an object"); return null; }
        var result = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var (key, value) in map) result[key] = Strings(value, $"{what}: '{name}' of '{key}'", errors);
        return result;
    }

    /// <summary>An optional object of texts (a dimension's <c>icons</c>, value → icon); null when absent.</summary>
    private static Dictionary<string, string>? TextMap(JObject o, string name, string what, List<string> errors)
    {
        if (o[name] == null) return null;
        if (o[name] is not JObject map) { errors.Add($"{what}: '{name}' is not an object"); return null; }
        var result = new Dictionary<string, string>();
        foreach (var (key, value) in map)
        {
            if (value?.Type == JTokenType.String) result[key] = (string)value!;
            else errors.Add($"{what}: '{name}' gives '{key}' something that is not a text");
        }
        return result;
    }

    private static bool Flag(JObject o, string name, List<string> errors)
    {
        if (o[name] is { Type: JTokenType.Boolean } t) return (bool)t;
        if (o[name] != null) errors.Add($"'{name}' is not true or false");
        return false;
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
        return id == null
            ? null
            : new LevelDef(id, keep, drop, layouts!, Text(o, "nameKey", errors, required: false), Text(o, "progressKey", errors, required: false),
                Text(o, "memberKey", errors, required: false), Text(o, "icon", errors, required: false));
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
                    bool reverseRows = Flag(a, "reverseRows", errors);
                    if (rows != null && columns != null) arrange = new Arrangement.Table(rows, columns, reverseRows);
                    break;
                case "shape": arrange = new Arrangement.Shape(); break;
                case "order": arrange = new Arrangement.InOrder(); break;
                case { } other: errors.Add($"Layout '{id}': unknown arrangement '{other}' (table, shape, order)"); break;
            }
        }
        else if (o["arrange"] == null) arrange = new Arrangement.InOrder();
        else errors.Add($"Layout '{id}': 'arrange' is not an object");
        return id == null || arrange == null ? null : new LayoutDef(id, groupBy, arrange, Text(o, "nameKey", errors, required: false),
            Text(o, "icon", errors, required: false));
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

    /// <summary><c>marks</c>: attribute → value → <c>{ "color": "#E0B040", "nameKey": … }</c>, in the file's order (the first that
    /// fits a member is shown). The colour as <c>#RRGGBB</c> or <c>#AARRGGBB</c>.</summary>
    private static List<MarkDef> ReadMarks(JToken? token, List<string> errors)
    {
        var marks = new List<MarkDef>();
        if (token == null) return marks;
        if (token is not JObject attributes) { errors.Add("'marks' is not an object"); return marks; }
        foreach (var (attribute, values) in attributes)
        {
            if (values is not JObject byValue) { errors.Add($"'marks' gives '{attribute}' something that is not an object"); continue; }
            foreach (var (value, mark) in byValue)
            {
                string at = $"The mark of '{attribute}' = '{value}'";
                if (mark is not JObject o) { errors.Add($"{at} is not an object"); continue; }
                int? argb = Colour(Text(o, "color", errors) ?? "");
                if (argb == null) { errors.Add($"{at}: 'color' is not #RRGGBB or #AARRGGBB"); continue; }
                marks.Add(new MarkDef(attribute, value, argb.Value, Text(o, "nameKey", errors, required: false)));
            }
        }
        return marks;
    }

    /// <summary>A colour <c>#RRGGBB</c> (opaque) or <c>#AARRGGBB</c> as ARGB; null when it is neither.</summary>
    internal static int? Colour(string text)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(text, "^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$")) return null;
        uint v = uint.Parse(text.AsSpan(1), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
        return unchecked((int)(text.Length == 7 ? 0xFF000000u | v : v));
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
