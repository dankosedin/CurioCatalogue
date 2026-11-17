using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using CurioCatalogue.Core;

namespace CurioCatalogue.Adapters;

/// <summary>
/// Members and values by their names in the player's language (docs/plans/2026-11-09-album-polish.md, L1, "The rule and how
/// the definition carries it"): a want list's line, an album cell's tooltip, the want list window's slots, the album window's
/// headings and tabs. On the client, or on the server in the server's language (docs/critical.md).
/// </summary>
public static class MemberNames
{
    /// <summary>
    /// A value's name for a group's heading, a tab, a table's heading in words and a member's name part: by its dimension's
    /// <see cref="DimensionDef.NameKey"/>, matched with the game's wildcard keys (<c>game:tapestry-{value}</c> names a piece by
    /// its picture's <c>tapestry-nightfall*</c>, as vssurvivalmod BlockTapestry.cs:359#GetMatching), or for a picture of a
    /// <see cref="CollectionSource.Tapestry"/> source the game's own name (the picture has no <c>nameKey</c>, else it would make
    /// tabs: the plan's L1, point 1); the value itself when nothing names it. By the source, not the collection's id (L12):
    /// another mod's collection of tapestries is named alike. The one function for all of them (R1, G3.10).
    /// </summary>
    public static string ValueName(CollectionSource? source, DimensionDef dimension, string value) =>
        Named(source, dimension, value) ?? value;

    /// <summary>As <see cref="ValueName"/>, but null when nothing names the value (no key, or the language does not translate it).</summary>
    private static string? Named(CollectionSource? source, DimensionDef dimension, string value) =>
        (dimension.ValueNameKey(value) is { } key ? Translated(key) : null)
        ?? (source is CollectionSource.Tapestry && dimension.Code == "picture" ? Translated("game:tapestry-" + value) : null);

    /// <summary>A key's text, matched with wildcards; null when the language has none.</summary>
    public static string? Translated(string key) => Lang.GetMatchingIfExists(key);

    /// <summary>
    /// A member's name by its <see cref="DefinedCollection.NameParts"/> that have a name, parts after the first in lower case,
    /// worded by the level's <see cref="LevelDef.MemberKey"/> (what it leaves free written in it), or joined by commas with what it
    /// leaves free after them ("Native copper, bountiful, andesite"; "Native copper, any rock"). Null when no part has a name
    /// (butterflies, a piece a mod added): the member is then named by its item.
    /// </summary>
    public static string? Name(DefinedCollection collection, CollectionLevel level, Member member, CollectionSource? source = null)
    {
        var all = collection.NameParts(level, member);
        string[] parts = all.Select(p => Named(source, p.Dimension, p.Value)).OfType<string>()
            .Select((name, i) => i == 0 ? name : LowerFirst(name)).ToArray();
        if (parts.Length == 0) return null;
        // The key numbers the parts; with one of them untranslated the numbers would not fit, so the plain form instead. It words
        // what the member leaves free, so a member with nothing left free (the admin's Exclude left one colour) is named plainly (R1, G3.11).
        if (level.Definition.MemberKey is { } memberKey && parts.Length == all.Count && collection.Free(level, member).Count > 0)
            return Lang.Get(memberKey, parts.Cast<object>().ToArray());
        string name = string.Join(", ", parts);
        return Freedom(collection, level, member) is { } free ? WithFreedom(name, free) : name;
    }

    /// <summary>A member's name, or with no part named the name of the item shown for it (<paramref name="itemName"/>) and what it
    /// leaves free: a want list's line.</summary>
    public static string NameOr(DefinedCollection collection, CollectionLevel level, Member member, string itemName,
        CollectionSource? source = null) =>
        Name(collection, level, member, source)
        ?? (Freedom(collection, level, member) is { } free ? WithFreedom(itemName, free) : itemName);

    /// <summary>What a member leaves free ("any rock"); null for a member of one item.</summary>
    public static string? Freedom(DefinedCollection collection, CollectionLevel level, Member member)
    {
        var free = collection.Free(level, member);
        if (free.Count == 0) return null;
        if (free.Any(d => d.AnyKey == null)) return Lang.Get("curiocatalogue:invoice-info-any-of", member.Fillers.Count);
        return string.Join(", ", free.Select(d => Lang.Get(d.AnyKey!)));
    }

    private static string WithFreedom(string name, string free) => Lang.Get("curiocatalogue:invoice-info-member-free", name, free);

    private static string LowerFirst(string text) =>
        text.Length == 0 ? text : char.ToLowerInvariant(text[0]) + text[1..];
}
