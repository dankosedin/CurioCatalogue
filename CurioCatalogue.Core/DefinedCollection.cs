using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using ExhaustiveMatching;

namespace CurioCatalogue.Core;

/// <summary>
/// A member of a level: a point in the kept dimensions (docs/decisions.md, Collections).
/// </summary>
/// <param name="Id">Made of the kept dimensions' values (<see cref="CollectionDefinition"/>); what albums store.</param>
/// <param name="Values">Its value in every dimension on which all its points agree: the kept ones always, and a dimension the
/// source derives from a kept one (a piece's picture).</param>
/// <param name="Fillers">The item keys that fill it: one, or any of several (a seashell type in any colour), in the
/// dimensions' order.</param>
/// <param name="Stacks">What to show: one sample per filler, in the same order (the album cycles through them).</param>
/// <param name="Attributes">The source's attributes that all its points share (a piece's x and y), then the definition's rules.</param>
public sealed record Member(
    string Id,
    IReadOnlyDictionary<string, string> Values,
    IReadOnlyList<ItemKey> Fillers,
    IReadOnlyList<object> Stacks,
    IReadOnlyDictionary<string, string> Attributes)
{
    /// <summary>The attribute naming a flat image the album draws in place of the stack (a tapestry piece's own texture, so
    /// that the pieces side by side make the picture; album view, A2, "What the view needs", 5): an asset location.</summary>
    public const string FlatTexture = "texture";
}

/// <summary>A group of a layout: its values in the layout's grouping dimensions (none for a layout without groups) and its
/// members in the layout's order.</summary>
public sealed record MemberGroup(IReadOnlyList<string> Values, IReadOnlyList<Member> Members)
{
    public bool IsComplete(IReadOnlySet<string> collectedIds) => Members.All(m => collectedIds.Contains(m.Id));

    /// <summary>How many of its members are collected: the "21/34" in its heading.</summary>
    public int CollectedCount(IReadOnlySet<string> collectedIds) => Members.Count(m => collectedIds.Contains(m.Id));
}

/// <summary>How far an album is at a level: members, and whole groups of the default layout (tapestry pictures).</summary>
public sealed record LevelProgress(int Collected, int Total, int GroupsComplete, int GroupsTotal);

/// <summary>What building a collection gave: the collection with warnings for the log, or why the definition cannot be used.
/// One bad definition (a mod's, or a wrong patch) leaves the other collections working.</summary>
[Closed(typeof(CollectionBuild.Built), typeof(CollectionBuild.Invalid))]
public abstract record CollectionBuild
{
    public sealed record Built(DefinedCollection Collection, IReadOnlyList<string> Warnings) : CollectionBuild;

    public sealed record Invalid(string Id, IReadOnlyList<string> Errors) : CollectionBuild;
}

/// <summary>One level of a built collection: its members, the member a key fills, groups and order per layout, progress.</summary>
public sealed class CollectionLevel
{
    public LevelDef Definition { get; }
    public string Id => Definition.Id;

    /// <summary>In the level's order: by the kept dimensions, each in its own order, in the order of the definition's dimensions.</summary>
    public IReadOnlyList<Member> Members { get; }

    /// <summary>The layouts; the first is the default.</summary>
    public IReadOnlyList<LayoutDef> Layouts { get; }

    public LayoutDef DefaultLayout => Layouts[0];

    private readonly IReadOnlyDictionary<string, Member> byId;
    private readonly IReadOnlyDictionary<ItemKey, Member> byKey;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<MemberGroup>> groups;

    internal CollectionLevel(LevelDef definition, IReadOnlyList<Member> members, IReadOnlyList<LayoutDef> layouts,
        IReadOnlyDictionary<ItemKey, Member> byKey, IReadOnlyDictionary<string, IReadOnlyList<MemberGroup>> groups)
    {
        Definition = definition;
        Members = members;
        Layouts = layouts;
        this.byKey = byKey;
        this.groups = groups;
        byId = members.ToDictionary(m => m.Id);
    }

    public Member? Find(string id) => byId.GetValueOrDefault(id);

    /// <summary>The member this key fills, if any: at most one in a level (checked when the collection is built).</summary>
    public Member? MemberOf(ItemKey key) => byKey.GetValueOrDefault(key);

    /// <summary>The groups of a layout of this level, in order.</summary>
    public IReadOnlyList<MemberGroup> Groups(LayoutDef layout) =>
        groups.TryGetValue(layout.Id, out var g) ? g : throw new ArgumentException($"Layout '{layout.Id}' is not one of level '{Id}'");

    /// <summary>The members in the default layout's order, group by group: the order of taking new, so that taking part of a
    /// list brings whole groups (domain.md, Albums and collections).</summary>
    public IReadOnlyList<Member> Order => order ??= Groups(DefaultLayout).SelectMany(g => g.Members).ToList();

    private IReadOnlyList<Member>? order;

    /// <summary>Progress of an album that collected these members; ids the level does not have are not counted.</summary>
    public LevelProgress Progress(IEnumerable<string> collectedIds)
    {
        var collected = collectedIds.Where(byId.ContainsKey).ToHashSet();
        var defaultGroups = Groups(DefaultLayout);
        return new LevelProgress(collected.Count, Members.Count, defaultGroups.Count(g => g.IsComplete(collected)), defaultGroups.Count);
    }
}

/// <summary>
/// A collection built from its definition and the points its source found (docs/decisions.md, Collections): every
/// level's members, groups and order. Immutable; built again when the definition, the config or the loaded items change.
/// </summary>
public sealed class DefinedCollection
{
    public CollectionDefinition Definition { get; }
    public string Id => Definition.Id;
    public IReadOnlyList<CollectionLevel> Levels { get; }

    /// <summary>For each dimension, the values that occur in the collection, in the dimension's order (table headings, tabs).</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Values { get; }

    private DefinedCollection(CollectionDefinition definition, IReadOnlyList<CollectionLevel> levels,
        IReadOnlyDictionary<string, IReadOnlyList<string>> values)
    {
        Definition = definition;
        Levels = levels;
        Values = values;
    }

    public CollectionLevel? Level(string id) => Levels.FirstOrDefault(l => l.Id == id);

    /// <summary>The level of this id; for a collection of one level, that level whatever the id (a level renamed by a patch). What
    /// an album and a want list made at a level find of it now.</summary>
    public CollectionLevel? LevelOrOnly(string? id) => (id != null ? Level(id) : null) ?? (Levels.Count == 1 ? Levels[0] : null);

    /// <summary>
    /// What a member of several items leaves free: the dimensions the level drops as <see cref="Drop.Any"/> in which the member has
    /// no value of its own (a seashell type: its colour), in the definition's order, for "any colour". Empty for a member of one item.
    /// </summary>
    public IReadOnlyList<DimensionDef> Free(CollectionLevel level, Member member) =>
        member.Fillers.Count < 2
            ? []
            : Definition.Dimensions
                .Where(d => level.Definition.Drop.TryGetValue(d.Code, out var drop) && drop == Drop.Any && !member.Values.ContainsKey(d.Code))
                .ToList();

    /// <summary>
    /// What a listed member says after the item's name: its value in each dimension marked <see cref="DimensionDef.InLists"/>, in
    /// the definition's order. A value all its items share counts, kept by the level or not (the richest native copper, found in
    /// basalt only: "basalt"); a dimension the member leaves free is said by <see cref="Free"/> instead.
    /// </summary>
    public IReadOnlyList<(DimensionDef Dimension, string Value)> Listed(Member member) =>
        Definition.Dimensions
            .Where(d => d.InLists && member.Values.ContainsKey(d.Code))
            .Select(d => (d, member.Values[d.Code]))
            .ToList();

    public const string IdSeparator = "-";

    private static readonly LayoutDef ImplicitLayout = new("order", [], new Arrangement.InOrder());

    /// <summary>
    /// Builds the collection. Points with a name the definition excludes are left out; of points with the same name the first
    /// is kept. Any error in the definition, or a key that would fill two members of a level, makes the whole collection
    /// <see cref="CollectionBuild.Invalid"/>; values missing from a dimension's list are warnings.
    /// </summary>
    public static CollectionBuild Build(CollectionDefinition definition, IEnumerable<SourcePoint> points)
    {
        var errors = CheckDefinition(definition);
        if (errors.Count > 0) return new CollectionBuild.Invalid(definition.Id, errors);

        var warnings = new List<string>();
        var dims = definition.Dimensions;

        // The points: first of each name, not excluded, a value in every dimension.
        var excluded = NamePattern.Matcher(definition.Exclude);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var kept = new List<SourcePoint>();
        foreach (var p in points)
        {
            if (!seen.Add(p.Name)) { warnings.Add($"Point '{p.Name}' is given twice; the first is kept"); continue; }
            if (excluded(p.Name)) continue;
            var lacking = dims.Where(d => !p.Values.ContainsKey(d.Code)).Select(d => d.Code).ToList();
            if (lacking.Count > 0) errors.Add($"Point '{p.Name}' has no value for {string.Join(", ", lacking)}");
            else kept.Add(p);
        }
        if (errors.Count > 0) return new CollectionBuild.Invalid(definition.Id, errors);

        var orders = dims.ToDictionary(d => d.Code, d => DimensionOrder.Of(d, kept.Select(p => p.Values[d.Code]), warnings));
        // Stable: points equal in every dimension keep the source's order.
        var sorted = kept.OrderBy(p => dims.Select(d => orders[d.Code].Index(p.Values[d.Code])).ToArray(), Lexicographic.Instance).ToList();

        var levels = new List<CollectionLevel>();
        foreach (var levelDef in definition.Levels)
        {
            var level = BuildLevel(definition, levelDef, sorted, orders, errors);
            if (level != null) levels.Add(level);
        }
        if (errors.Count > 0) return new CollectionBuild.Invalid(definition.Id, errors);

        var values = dims.ToDictionary(d => d.Code, d => orders[d.Code].Values);
        return new CollectionBuild.Built(new DefinedCollection(definition, levels, values), warnings);
    }

    private static CollectionLevel? BuildLevel(CollectionDefinition definition, LevelDef levelDef, IReadOnlyList<SourcePoint> points,
        IReadOnlyDictionary<string, DimensionOrder> orders, List<string> errors)
    {
        var dims = definition.Dimensions;
        var keptDims = dims.Where(d => levelDef.Keep.Contains(d.Code)).Select(d => d.Code).ToList();
        var maxDims = dims.Where(d => levelDef.Drop.GetValueOrDefault(d.Code) == Drop.Max && !levelDef.Keep.Contains(d.Code))
            .Select(d => d.Code).ToList();
        string IdOf(SourcePoint p) => string.Join(IdSeparator, keptDims.Select(d => p.Values[d]));

        var members = new List<Member>();
        foreach (var memberPoints in points.GroupBy(IdOf, StringComparer.Ordinal))
        {
            IReadOnlyList<SourcePoint> filling = memberPoints.ToList();
            // Each Max dimension in turn keeps only the points of its highest value.
            foreach (var d in maxDims)
            {
                int top = filling.Max(p => orders[d].Rank(p.Values[d]));
                filling = filling.Where(p => orders[d].Rank(p.Values[d]) == top).ToList();
            }
            members.Add(MakeMember(definition, memberPoints.Key, filling));
        }

        members = members
            .OrderBy(m => keptDims.Select(d => orders[d].Index(m.Values[d])).ToArray(), Lexicographic.Instance)
            .ToList();

        // A key fills at most one member in a level: else taking new could not tell which member a stack is for.
        var byKey = new Dictionary<ItemKey, Member>();
        foreach (var m in members)
        foreach (var key in m.Fillers)
        {
            if (byKey.TryGetValue(key, out var other))
                errors.Add($"Level '{levelDef.Id}': {key} fills both '{other.Id}' and '{m.Id}'");
            else byKey[key] = m;
        }

        var layouts = levelDef.Layouts.Count > 0 ? levelDef.Layouts : [ImplicitLayout];
        var groups = new Dictionary<string, IReadOnlyList<MemberGroup>>();
        foreach (var layout in layouts)
        {
            var layoutErrors = CheckLayoutOnMembers(levelDef, layout, members);
            if (layoutErrors.Count > 0) errors.AddRange(layoutErrors);
            else groups[layout.Id] = Arrange(layout, members, orders);
        }

        return errors.Count > 0 ? null : new CollectionLevel(levelDef, members, layouts, byKey, groups);
    }

    private static Member MakeMember(CollectionDefinition definition, string id, IReadOnlyList<SourcePoint> points)
    {
        var values = definition.Dimensions
            .Where(d => points.Select(p => p.Values[d.Code]).Distinct().Count() == 1)
            .ToImmutableDictionary(d => d.Code, d => points[0].Values[d.Code]);

        var fillers = new List<ItemKey>();
        var stacks = new List<object>();
        foreach (var p in points)
        {
            if (fillers.Contains(p.Key)) continue;
            fillers.Add(p.Key);
            stacks.Add(p.Sample);
        }

        var shared = points
            .Select(p => p.Attributes ?? ImmutableDictionary<string, string>.Empty)
            .Aggregate((a, b) => a.Where(kv => b.TryGetValue(kv.Key, out var v) && v == kv.Value).ToImmutableDictionary())
            .ToImmutableDictionary();
        var attributes = definition.Rules
            .Where(r => r.Where.All(w => values.TryGetValue(w.Key, out var v) && w.Value.Contains(v)))
            .Aggregate(shared, (acc, r) => acc.SetItems(r.Set));

        return new Member(id, values, fillers, stacks, attributes);
    }

    private static IReadOnlyList<MemberGroup> Arrange(LayoutDef layout, IReadOnlyList<Member> members,
        IReadOnlyDictionary<string, DimensionOrder> orders)
    {
        // Members come in the level's order; OrderBy is stable, so it breaks every tie below.
        Func<Member, int[]> within = layout.Arrange switch
        {
            Arrangement.Table t => m =>
                [(t.ReverseRows ? -1 : 1) * orders[t.Rows].Index(m.Values[t.Rows]), orders[t.Columns].Index(m.Values[t.Columns])],
            Arrangement.Shape _ => m => [Coordinate(m, Arrangement.Shape.Y)!.Value, Coordinate(m, Arrangement.Shape.X)!.Value],
            Arrangement.InOrder _ => _ => [],
            _ => throw ExhaustiveMatch.Failed(layout.Arrange),
        };
        return members
            .GroupBy(m => string.Join("\n", layout.GroupBy.Select(d => m.Values[d])), StringComparer.Ordinal)
            .Select(g => new MemberGroup(layout.GroupBy.Select(d => g.First().Values[d]).ToList(),
                g.OrderBy(within, Lexicographic.Instance).ToList()))
            .OrderBy(g => layout.GroupBy.Select((d, i) => orders[d].Index(g.Values[i])).ToArray(), Lexicographic.Instance)
            .ToList();
    }

    /// <summary>What can be checked before any point is read: the errors that make the definition itself unusable, whatever the
    /// game has. The adapters' reader calls it too, so a definition it calls good is one the core accepts.</summary>
    public static List<string> CheckDefinition(CollectionDefinition def)
    {
        var errors = new List<string>();
        var codes = def.Dimensions.Select(d => d.Code).ToList();
        var known = codes.ToHashSet();

        if (codes.Count == 0) errors.Add("No dimensions");
        foreach (var dup in codes.GroupBy(c => c).Where(g => g.Count() > 1)) errors.Add($"Dimension '{dup.Key}' is defined twice");
        foreach (var d in def.Dimensions)
        foreach (var dup in d.Values.GroupBy(v => v).Where(g => g.Count() > 1))
            errors.Add($"Dimension '{d.Code}' lists '{dup.Key}' twice");

        if (def.Levels.Count == 0) errors.Add("No levels");
        foreach (var dup in def.Levels.GroupBy(l => l.Id).Where(g => g.Count() > 1)) errors.Add($"Level '{dup.Key}' is defined twice");

        foreach (var level in def.Levels)
        {
            string at = $"Level '{level.Id}'";
            // An album with no level stores "": a level with that id would read as none chosen.
            if (string.IsNullOrEmpty(level.Id)) errors.Add("A level has an empty id");
            if (level.Keep.Count == 0) errors.Add($"{at} keeps no dimension");
            // Max needs an order to find the highest; without a list it would be the order of the source's points, which for
            // variants follows the world's item ids.
            foreach (var d in def.Dimensions.Where(d => d.Values.Count == 0 && level.Drop.GetValueOrDefault(d.Code) == Drop.Max && !level.Keep.Contains(d.Code)))
                errors.Add($"{at} drops '{d.Code}' as max, but the dimension lists no values to tell the highest");
            foreach (var d in level.Keep.Concat(level.Drop.Keys).Where(d => !known.Contains(d)).Distinct())
                errors.Add($"{at} names an unknown dimension '{d}'");
            foreach (var d in level.Keep.Where(level.Drop.ContainsKey)) errors.Add($"{at} both keeps and drops '{d}'");
            foreach (var d in codes.Where(c => !level.Keep.Contains(c) && !level.Drop.ContainsKey(c)))
                errors.Add($"{at} neither keeps nor drops '{d}' (say any or max)");

            foreach (var dup in level.Layouts.GroupBy(l => l.Id).Where(g => g.Count() > 1))
                errors.Add($"{at}: layout '{dup.Key}' is defined twice");
            foreach (var layout in level.Layouts)
            foreach (var d in LayoutDimensions(layout).Where(d => !known.Contains(d)).Distinct())
                errors.Add($"{at}, layout '{layout.Id}': unknown dimension '{d}'");
        }

        foreach (var rule in def.Rules)
        foreach (var d in rule.Where.Keys.Where(d => !known.Contains(d)))
            errors.Add($"A rule names an unknown dimension '{d}'");

        return errors;
    }

    /// <summary>A layout groups and arranges by values each member must have, and a shape needs integer x and y.</summary>
    private static List<string> CheckLayoutOnMembers(LevelDef level, LayoutDef layout, IReadOnlyList<Member> members)
    {
        var errors = new List<string>();
        string at = $"Level '{level.Id}', layout '{layout.Id}'";
        foreach (var d in LayoutDimensions(layout).Distinct())
        {
            var without = members.Where(m => !m.Values.ContainsKey(d)).Select(m => m.Id).ToList();
            if (without.Count > 0) errors.Add($"{at}: '{d}' is not one value in members {string.Join(", ", without.Take(5))}");
        }
        if (layout.Arrange is Arrangement.Shape)
        {
            var without = members
                .Where(m => Coordinate(m, Arrangement.Shape.X) is null || Coordinate(m, Arrangement.Shape.Y) is null)
                .Select(m => m.Id).ToList();
            if (without.Count > 0) errors.Add($"{at}: no integer x and y in members {string.Join(", ", without.Take(5))}");
        }
        return errors;
    }

    /// <summary>A member's integer attribute (a piece's x or y), the same in every culture; null when it has none.</summary>
    private static int? Coordinate(Member m, string attribute) =>
        m.Attributes.TryGetValue(attribute, out var s) && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)
            ? v : null;

    private static IEnumerable<string> LayoutDimensions(LayoutDef layout)
    {
        IEnumerable<string> arranged = layout.Arrange switch
        {
            Arrangement.Table t => [t.Rows, t.Columns],
            Arrangement.Shape _ => [],
            Arrangement.InOrder _ => [],
            _ => throw ExhaustiveMatch.Failed(layout.Arrange),
        };
        return layout.GroupBy.Concat(arranged);
    }

    /// <summary>
    /// The order of one dimension's values (docs/architecture.md, Dimensions). With a list: the listed values first, in its
    /// order, then the others by code; for Max the others rank below every listed one. Without a list: the order in which the
    /// values first come from the source (a level cannot drop such a dimension as Max: <see cref="CheckDefinition"/>).
    /// </summary>
    private sealed class DimensionOrder
    {
        private readonly IReadOnlyDictionary<string, int> index;
        private readonly IReadOnlyDictionary<string, int> rank;

        /// <summary>The values that occur, in order.</summary>
        public IReadOnlyList<string> Values { get; }

        private DimensionOrder(IReadOnlyList<string> values, IReadOnlyDictionary<string, int> rank)
        {
            Values = values;
            index = values.Select((v, i) => (v, i)).ToDictionary(p => p.v, p => p.i);
            this.rank = rank;
        }

        public int Index(string value) => index[value];

        public int Rank(string value) => rank[value];

        public static DimensionOrder Of(DimensionDef dim, IEnumerable<string> occurring, List<string> warnings)
        {
            var found = occurring.Distinct().ToList();
            if (dim.Values.Count == 0)
                return new DimensionOrder(found, found.Select((v, i) => (v, i)).ToDictionary(p => p.v, p => p.i));

            var listed = dim.Values.Where(found.Contains).ToList();
            var unlisted = found.Where(v => !dim.Values.Contains(v)).OrderBy(v => v, StringComparer.Ordinal).ToList();
            if (unlisted.Count > 0)
                warnings.Add($"Dimension '{dim.Code}' does not list {string.Join(", ", unlisted)}: they come last and rank lowest");
            // Rank: unlisted from -count up to -1, listed from 0 by their place in the list.
            var rank = unlisted.Select((v, i) => (v, r: i - unlisted.Count))
                .Concat(listed.Select(v => (v, r: dim.Values.ToList().IndexOf(v))))
                .ToDictionary(p => p.v, p => p.r);
            return new DimensionOrder(listed.Concat(unlisted).ToList(), rank);
        }
    }

    /// <summary>Compares int arrays element by element (a sort key of several dimensions).</summary>
    private sealed class Lexicographic : IComparer<int[]>
    {
        public static readonly Lexicographic Instance = new();

        public int Compare(int[]? a, int[]? b)
        {
            for (int i = 0; i < Math.Min(a!.Length, b!.Length); i++)
                if (a[i] != b[i]) return a[i].CompareTo(b[i]);
            return a.Length.CompareTo(b.Length);
        }
    }
}
