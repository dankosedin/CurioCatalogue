using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ExhaustiveMatching;

namespace CurioCatalogue.Core;

// The definition of a collection: data, read from an asset config/curiocatalogue/collections/<id>.json of any domain
// (docs/decisions.md, Collections). The members come from a source in the adapters, as SourcePoints; DefinedCollection
// builds them per level. Nothing here knows the asset's JSON: the adapters parse it into these records.

/// <summary>
/// One dimension of a collection (ore, rock, grade; a seashell's type and colour; a tapestry's picture and piece).
/// </summary>
/// <param name="Values">The order of the values, lowest first. A value the source finds but the list lacks comes after the
/// listed ones, by code, ranks below all of them for <see cref="Drop.Max"/>, and gives a warning. An empty list means the
/// order in which the source gives its points, with no warning (tapestries: the order of the block's <c>sizes</c>); such a
/// dimension cannot be dropped as <see cref="Drop.Max"/>, which needs an order of its own.</param>
/// <param name="NameKey">A lang key for a value's name, <c>{value}</c> replaced by the value (<c>game:ore-{value}</c>); null
/// when the values have no names.</param>
/// <param name="Icon">A value's icon, <c>{value}</c> replaced: an item or block code (<c>game:rock-{value}</c>), or an SVG of
/// ours by its path (<c>curiocatalogue:textures/icons/grade-{value}.svg</c>, <see cref="IsSvg"/>); null when none.</param>
/// <param name="AnyKey">A lang key for what a member of several items leaves free in this dimension, a phrase in lower case
/// ("any rock", "будь-яка порода"; whole, for the grammar of each language); null when none, and a generic "any of N" is said.</param>
/// <param name="InLists">Whether a member's value here is said after the item's name where a missing piece is listed (the want
/// list's lines): the item's name does not say it, and pieces of two values share a name (a crystal's
/// rock). Said by <see cref="NameKey"/>.</param>
/// <param name="ShortNameKey">A lang key for a value's short name in a table heading, <c>{value}</c> replaced; null: the <see cref="NameKey"/>.
/// The full name stays the heading's tooltip.</param>
/// <param name="Icons">The icons of the values <see cref="Icon"/> does not fit, value → icon in the same forms (a crystal's
/// <c>quartz_nativegold</c> → <c>game:nugget-nativegold</c>); without <see cref="Icon"/>, a whole table by hand. Null when none.</param>
public sealed record DimensionDef(string Code, IReadOnlyList<string> Values, string? NameKey = null, string? Icon = null,
    string? AnyKey = null, bool InLists = false, string? ShortNameKey = null, IReadOnlyDictionary<string, string>? Icons = null)
{
    public const string ValuePlaceholder = "{value}";

    public string? ValueNameKey(string value) => NameKey?.Replace(ValuePlaceholder, value);

    /// <summary>The key of a value's short name, for a table heading with little room: <see cref="ShortNameKey"/>, else the full name's.</summary>
    public string? ValueShortNameKey(string value) => (ShortNameKey ?? NameKey)?.Replace(ValuePlaceholder, value);

    /// <summary>A value's icon: its own in <see cref="Icons"/>, else <see cref="Icon"/> with the value; null when none.</summary>
    public string? ValueIcon(string value) =>
        Icons != null && Icons.TryGetValue(value, out var own) ? own : Icon?.Replace(ValuePlaceholder, value);

    /// <summary>The values are headed by icons, not words (a table's heading is then a cell, not a line of text).</summary>
    public bool HasIcons => Icon != null || Icons is { Count: > 0 };

    /// <summary>An icon that is an SVG drawn by us, not an item or block: its path ends in <c>.svg</c>.</summary>
    public static bool IsSvg(string icon) => icon.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
}

/// <summary>What a level does with a dimension it does not keep.</summary>
public enum Drop
{
    /// <summary>Any value fills the member (seashells by type: a shell of any colour).</summary>
    Any,

    /// <summary>Only the highest value that occurs among the member's points fills it, in the dimension's order (crystals:
    /// the richest grade of each ore).</summary>
    Max,
}

/// <summary>How a group of members is laid out in the album (docs/plans/2026-11-04-album-view.md, A2).</summary>
[Closed(typeof(Arrangement.Table), typeof(Arrangement.Shape), typeof(Arrangement.InOrder))]
public abstract record Arrangement
{
    /// <summary>A table of two dimensions: rows and columns, each a dimension's values in order; with
    /// <paramref name="ReverseRows"/> the rows highest first (a crystal's richest grade on top, L2b), the dimension's own order
    /// (lowest first, for <see cref="Drop.Max"/>) unchanged.</summary>
    public sealed record Table(string Rows, string Columns, bool ReverseRows = false) : Arrangement;

    /// <summary>The group's own shape: each member at its integer attributes <c>x</c> and <c>y</c> (a tapestry piece in
    /// its picture), in reading order (row by row).</summary>
    public sealed record Shape : Arrangement
    {
        public const string X = "x", Y = "y";
    }

    /// <summary>Members one after another, in the level's order.</summary>
    public sealed record InOrder : Arrangement;
}

/// <summary>
/// One way to look at a level: which dimensions group the members (none: one group; several: nested, the first the outermost)
/// and how a group is laid out. Changes nothing in the data.
/// </summary>
public sealed record LayoutDef(string Id, IReadOnlyList<string> GroupBy, Arrangement Arrange, string? NameKey = null);

/// <summary>
/// A level: a projection of the collection's points. It keeps some dimensions (a member per combination of their values) and
/// says for every other dimension whether any value fills the member or only the highest. The first layout is the default,
/// which gives the order of taking new; with none, one group in order.
/// <param name="ProgressKey">A lang key of the line that tells an album's progress at this level, whole, for each language's grammar:
/// <c>{0}</c> members collected, <c>{1}</c> of all, <c>{2}</c> groups complete, <c>{3}</c> groups ("Pieces {0} of {1} · whole
/// pictures {2} of {3}"). Null: a generic line.</param>
/// </summary>
public sealed record LevelDef(
    string Id,
    IReadOnlyList<string> Keep,
    IReadOnlyDictionary<string, Drop> Drop,
    IReadOnlyList<LayoutDef> Layouts,
    string? NameKey = null,
    string? ProgressKey = null);

/// <summary>
/// Sets attributes on the members whose values match: each dimension in <paramref name="Where"/> must be one the member has a
/// value in (kept by the level, or shared by all its points) and that value one of those listed. Later rules win.
/// </summary>
public sealed record AttributeRule(IReadOnlyDictionary<string, IReadOnlyList<string>> Where, IReadOnlyDictionary<string, string> Set);

/// <summary>
/// A collection as data. The member's id is made of the kept dimensions' values in the order of <paramref name="Dimensions"/>,
/// joined by <c>-</c>: albums store it, so the order of the dimensions must not change once released.
/// </summary>
/// <param name="Exclude">Patterns (<c>*</c> for any text) on the source's own name of a point (a code without domain, a
/// tapestry piece's name); points that match are not in the collection.</param>
/// <param name="AlbumRecipe">The grid recipe that makes an album of this collection, by its name with the domain
/// (<c>curiocatalogue:ledger-album</c>); null when the collection has none. The core only carries it: the adapters find the
/// recipe and take it away when the collection is switched off.</param>
public sealed record CollectionDefinition(
    string Id,
    IReadOnlyList<DimensionDef> Dimensions,
    IReadOnlyList<LevelDef> Levels,
    IReadOnlyList<AttributeRule> Rules,
    IReadOnlyList<string> Exclude,
    string? NameKey = null,
    string? AlbumRecipe = null);

/// <summary>A point the source found: its own name (for exclusions), its value in each dimension, the item that fills it, a
/// sample stack to show (the game's ItemStack; the core never reads it) and attributes the source knows (a piece's x and y).</summary>
public sealed record SourcePoint(
    string Name,
    IReadOnlyDictionary<string, string> Values,
    ItemKey Key,
    object Sample,
    IReadOnlyDictionary<string, string>? Attributes = null);

/// <summary>Names matching patterns with <c>*</c> for any text (exclusions in definitions and in the config).</summary>
public static class NamePattern
{
    /// <summary>A test for names that match any of the patterns. Built once; case is ignored the same way in every culture (no
    /// Turkish dotless i).</summary>
    public static Func<string, bool> Matcher(IEnumerable<string> patterns)
    {
        var alternatives = patterns.Select(p => Regex.Escape(p).Replace("\\*", ".*")).ToList();
        if (alternatives.Count == 0) return _ => false;
        var regex = new Regex("^(?:" + string.Join("|", alternatives) + ")$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return regex.IsMatch;
    }
}
