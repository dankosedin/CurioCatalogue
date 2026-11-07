using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ExhaustiveMatching;

namespace CurioCatalogue.Core;

// The definition of a collection: data, read from an asset config/curiocatalogue/collections/<id>.json of any domain
// (docs/architecture.md, Part 1, Collections). The members come from a source in the adapters, as SourcePoints; DefinedCollection
// builds them per level. Nothing here knows the asset's JSON: the adapters parse it into these records.

/// <summary>
/// One dimension of a collection (ore, rock, grade; a seashell's type and colour; a tapestry's picture and piece).
/// </summary>
/// <param name="Values">The order of the values, lowest first. A value the source finds but the list lacks comes after the
/// listed ones, by code, ranks below all of them for <see cref="Drop.Max"/>, and gives a warning. An empty list means the
/// order in which the source gives its points, with no warning (tapestries: the order of the block's <c>sizes</c>).</param>
/// <param name="NameKey">A lang key for a value's name, <c>{value}</c> replaced by the value (<c>game:ore-{value}</c>); null
/// when the values have no names.</param>
/// <param name="Icon">An item code for a value's icon, <c>{value}</c> replaced (<c>game:rock-{value}</c>); null when none.</param>
/// <param name="AnyKey">A lang key for what a member of several items leaves free in this dimension, a phrase in lower case
/// ("any rock", "будь-яка порода"; whole, for the grammar of each language); null when none, and a generic "any of N" is said.</param>
public sealed record DimensionDef(string Code, IReadOnlyList<string> Values, string? NameKey = null, string? Icon = null,
    string? AnyKey = null)
{
    public const string ValuePlaceholder = "{value}";

    public string? ValueNameKey(string value) => NameKey?.Replace(ValuePlaceholder, value);

    public string? ValueIcon(string value) => Icon?.Replace(ValuePlaceholder, value);
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
    /// <summary>A table of two dimensions: rows and columns, each a dimension's values in order.</summary>
    public sealed record Table(string Rows, string Columns) : Arrangement;

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
/// </summary>
public sealed record LevelDef(
    string Id,
    IReadOnlyList<string> Keep,
    IReadOnlyDictionary<string, Drop> Drop,
    IReadOnlyList<LayoutDef> Layouts,
    string? NameKey = null);

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
public sealed record CollectionDefinition(
    string Id,
    IReadOnlyList<DimensionDef> Dimensions,
    IReadOnlyList<LevelDef> Levels,
    IReadOnlyList<AttributeRule> Rules,
    IReadOnlyList<string> Exclude,
    string? NameKey = null);

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
