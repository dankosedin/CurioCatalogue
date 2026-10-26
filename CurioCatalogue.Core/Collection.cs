using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace CurioCatalogue.Core;

/// <summary>An item of a collection; <paramref name="Group"/> is what it is part of (a tapestry piece's picture).</summary>
/// <param name="Sample">A sample stack for the album's entry (the game's ItemStack; the core never reads it).</param>
public sealed record CollectionMember(ItemKey Key, string Group, object Sample);

/// <summary>How far an album is: members and whole groups (e.g. tapestry pieces and complete pictures).</summary>
public sealed record CollectionProgress(int Collected, int Total, int GroupsComplete, int GroupsTotal);

/// <summary>
/// A named set of items to collect (domain.md, Albums and collections). An album is made from it: a ledger in album mode listing every
/// member, from which collected items are crossed off.
/// </summary>
public sealed class Collection
{
    public string Id { get; }
    public IReadOnlyList<CollectionMember> Members { get; }

    public Collection(string id, IEnumerable<CollectionMember> members)
    {
        Id = id;
        // One member per key; a repeated key (e.g. added in the config and found again) keeps its first group.
        Members = members.GroupBy(m => m.Key).Select(g => g.First()).ToList();
    }

    public bool Contains(ItemKey key) => Members.Any(m => m.Key == key);

    /// <summary>
    /// Entries in the collection's order (for tapestries: picture by picture, piece by piece), so that taking only
    /// part of a list brings whole pictures (domain.md, Albums and collections). Items outside the collection come last, by key.
    /// </summary>
    public IEnumerable<Entry> InOrder(IEnumerable<Entry> entries) =>
        entries.OrderBy(e => Index.TryGetValue(e.Key, out int i) ? i : int.MaxValue).ThenBy(e => e.Key.ToString(), StringComparer.Ordinal);

    private IReadOnlyDictionary<ItemKey, int> Index => index ??= Members.Select((m, i) => (m.Key, i)).ToDictionary(p => p.Key, p => p.i);

    private IReadOnlyDictionary<ItemKey, int>? index;

    /// <summary>The members' keys as text, sorted: what an album remembers of the collection it follows.</summary>
    public IReadOnlyList<string> Snapshot => snapshot ??= Members.Select(m => m.Key.ToString()).Order(StringComparer.Ordinal).ToList();

    private IReadOnlyList<string>? snapshot;

    /// <summary>A new album: every member missing, count 1.</summary>
    public Ledger NewAlbum(int sheets, string name) => AlbumMissing(Members, sheets, name);

    /// <summary>
    /// The album as it should be for the collection now: the same object when the collection has not changed
    /// since the album's snapshot, else <see cref="Refresh"/>ed. Albums follow their collection by themselves
    /// (domain.md, Albums and collections): every read goes through this.
    /// </summary>
    public Ledger RefreshIfStale(Ledger album) =>
        album.Album?.Members is { } members && members.SequenceEqual(Snapshot) ? album : Refresh(album);

    /// <summary>
    /// The album brought up to the collection as it is now (after a config change or a mod with new tapestries),
    /// keeping its progress: what was collected stays collected, new members come in as missing, members the
    /// collection no longer has drop out. Sheets and name stay. An album without a member snapshot counts as made
    /// from the current collection.
    /// </summary>
    public Ledger Refresh(Ledger album)
    {
        var missing = album.Entries.Keys.Select(k => k.ToString()).ToHashSet();
        var before = album.Album?.Members ?? Snapshot;
        var collected = before.Where(k => !missing.Contains(k)).ToHashSet();
        return AlbumMissing(Members.Where(m => !collected.Contains(m.Key.ToString())), album.Sheets, album.Name);
    }

    private Ledger AlbumMissing(IEnumerable<CollectionMember> missing, int sheets, string name) =>
        new(LedgerMode.Album, sheets,
            missing.ToImmutableDictionary(m => m.Key, m => new Entry(m.Key, 1, m.Sample)),
            name, new AlbumInfo(Id, Members.Count, Snapshot));

    /// <summary>
    /// Progress of an album whose entries are <paramref name="missing"/>. Counted against the collection as it is now;
    /// a group is complete when none of its members is missing.
    /// </summary>
    public CollectionProgress Progress(IEnumerable<ItemKey> missing)
    {
        var missingSet = missing.ToHashSet();
        var groups = Members.GroupBy(m => m.Group).ToList();
        int collected = Members.Count(m => !missingSet.Contains(m.Key));
        int complete = groups.Count(g => g.All(m => !missingSet.Contains(m.Key)));
        return new CollectionProgress(collected, Members.Count, complete, groups.Count);
    }

    /// <summary>Does a name match a pattern with <c>*</c> for any text (config exclusions like "aid-*")?</summary>
    public static bool Matches(string pattern, string name) =>
        Regex.IsMatch(name, "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$", RegexOptions.IgnoreCase);
}
