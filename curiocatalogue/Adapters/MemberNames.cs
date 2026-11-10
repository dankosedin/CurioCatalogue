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
    /// <summary>A value's name by its dimension's <see cref="DimensionDef.NameKey"/>, matched with the game's wildcard keys
    /// (<c>game:tapestry-{value}</c> names a piece by its picture's <c>tapestry-nightfall*</c>, as vssurvivalmod
    /// BlockTapestry.cs:359); null when the dimension has no key or the language does not translate the value.</summary>
    public static string? ValueName(DimensionDef dimension, string value) =>
        dimension.ValueNameKey(value) is { } key ? Translated(key) : null;

    /// <summary>A key's text, matched with wildcards; null when the language has none.</summary>
    public static string? Translated(string key) => Lang.GetMatchingIfExists(key);

    /// <summary>
    /// A member's name by its <see cref="DefinedCollection.NameParts"/> that have a name, parts after the first in lower case,
    /// worded by the level's <see cref="LevelDef.MemberKey"/> (what it leaves free written in it), or joined by commas with what it
    /// leaves free after them ("Native copper, bountiful, andesite"; "Native copper, any rock"). Null when no part has a name
    /// (butterflies, a piece a mod added): the member is then named by its item.
    /// </summary>
    public static string? Name(DefinedCollection collection, CollectionLevel level, Member member)
    {
        var all = collection.NameParts(level, member);
        string[] parts = all.Select(p => ValueName(p.Dimension, p.Value)).OfType<string>()
            .Select((name, i) => i == 0 ? name : LowerFirst(name)).ToArray();
        if (parts.Length == 0) return null;
        // The key numbers the parts; with one of them untranslated the numbers would not fit, so the plain form instead.
        if (level.Definition.MemberKey is { } memberKey && parts.Length == all.Count)
            return Lang.Get(memberKey, parts.Cast<object>().ToArray());
        string name = string.Join(", ", parts);
        return Freedom(collection, level, member) is { } free ? WithFreedom(name, free) : name;
    }

    /// <summary>A member's name, or with no part named the name of the item shown for it (<paramref name="itemName"/>) and what it
    /// leaves free: a want list's line.</summary>
    public static string NameOr(DefinedCollection collection, CollectionLevel level, Member member, string itemName) =>
        Name(collection, level, member)
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
