using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ExhaustiveMatching;

namespace CurioCatalogue.Core;

/// <summary>The item that filled a member: its key (recomputed on every read, like a ledger's entries) and the stack itself
/// (the game's ItemStack; the core never reads it).</summary>
public sealed record FilledBy(ItemKey Key, object Stack);

/// <summary>A missing member a container can fill now, and the key of the item there that fills it.</summary>
public sealed record WantedMember(Member Member, ItemKey Key);

public enum LevelRefusal
{
    /// <summary>The album has collected something: its level is fixed until "Start over".</summary>
    NotEmpty,

    /// <summary>The collection has no such level (a stale or forged request).</summary>
    UnknownLevel,
}

[Closed(typeof(LevelChoice.Chosen), typeof(LevelChoice.Refused))]
public abstract record LevelChoice
{
    public sealed record Chosen(Album Album) : LevelChoice;

    public sealed record Refused(LevelRefusal Reason) : LevelChoice;
}

/// <summary>
/// An album (domain.md, Albums and collections): what it collected of its collection, not what it misses (format 2,
/// docs/decisions.md, Collections). What is missing is the collection's level minus <see cref="Collected"/>, so the album
/// follows its collection with no refresh: a new member is missing, a member excluded or disabled for a while keeps its record and
/// comes back collected.
/// </summary>
/// <param name="Level">The level chosen in the window while the album was empty; null for none. A collection with one level has
/// it chosen always (<see cref="LevelIn"/>).</param>
/// <param name="Collected">By member id: the item that filled the member.</param>
/// <param name="Unplaced">Items an album of format 1 had collected that are no member of its collection now (the collection is
/// unknown, or the piece is excluded), and those of an album whose level the collection no longer has. Kept, never dropped, and
/// placed when the collection has them again (<see cref="Place"/>).</param>
public sealed record Album(
    string CollectionId,
    string? Level,
    ImmutableSortedDictionary<string, FilledBy> Collected,
    ImmutableList<FilledBy> Unplaced,
    int Sheets,
    string Name = "") : Book(Sheets, Name)
{
    public static readonly ImmutableSortedDictionary<string, FilledBy> NoneCollected =
        ImmutableSortedDictionary.Create<string, FilledBy>(StringComparer.Ordinal);

    /// <summary>A new album of the collection: nothing collected; the level chosen only when there is no choice.</summary>
    public static Album New(DefinedCollection collection, int sheets, string name) =>
        new(collection.Id, collection.Levels.Count == 1 ? collection.Levels[0].Id : null, NoneCollected, [], sheets, name);

    public override bool IsEmpty => Collected.IsEmpty && Unplaced.IsEmpty;

    /// <summary>
    /// The album's level in the collection as it is now: the chosen one; for a collection of one level, that level (a level renamed
    /// by a patch included); otherwise null, and the album takes nothing and makes no want list until a level is chosen.
    /// </summary>
    public CollectionLevel? LevelIn(DefinedCollection collection) => collection.LevelOrOnly(Level);

    /// <summary>"Start over": nothing collected, the level free again (sheets and name stay).</summary>
    public Album StartOver(DefinedCollection collection) => New(collection, Sheets, Name);

    /// <summary>Chooses the level, which only an empty album may do (the level is fixed by the first piece; A2 of the album view plan).</summary>
    public LevelChoice ChooseLevel(DefinedCollection collection, string level)
    {
        if (collection.Level(level) == null) return new LevelChoice.Refused(LevelRefusal.UnknownLevel);
        if (Level == level) return new LevelChoice.Chosen(this);
        if (!IsEmpty) return new LevelChoice.Refused(LevelRefusal.NotEmpty);
        return new LevelChoice.Chosen(this with { Level = level });
    }

    /// <summary>The level's members the album has not collected, in the order of taking new (the default layout's).</summary>
    public IReadOnlyList<Member> Missing(CollectionLevel level) => level.Order.Where(m => !Collected.ContainsKey(m.Id)).ToList();

    public LevelProgress Progress(CollectionLevel level) => level.Progress(Collected.Keys);

    /// <summary>
    /// What to take from a container that has <paramref name="available"/>: each missing member it can fill, in the order of taking
    /// new, with the item that fills it there; of a member filled by any of several items, the first in the member's order. A key
    /// fills at most one member of a level, so no two lines share a key.
    /// </summary>
    public IReadOnlyList<WantedMember> Wanted(CollectionLevel level, IReadOnlySet<ItemKey> available) =>
        Missing(level)
            .Select(m => m.Fillers.FirstOrDefault(available.Contains) is { } key ? new WantedMember(m, key) : null)
            .OfType<WantedMember>()
            .ToList();

    /// <summary>The album with these members collected, each with the item that filled it; a member already collected keeps its record.</summary>
    public Album Collect(IEnumerable<(string MemberId, FilledBy By)> filled)
    {
        var collected = Collected.ToBuilder();
        foreach (var (id, by) in filled) collected.TryAdd(id, by);
        return this with { Collected = collected.ToImmutable() };
    }

    /// <summary>
    /// What the album misses as a want list for another player (domain.md, Albums and collections), spending one sheet. A line per
    /// missing member, naming it (any of its items will do, <see cref="Invoice.Wanted"/>), shown by its first item. Nothing missing:
    /// NoDifference; no sheets: NoSheets.
    /// </summary>
    public CreateInvoiceResult MakeWantList(CollectionLevel level, double createdTotalDays, BlockPosition position)
    {
        var missing = Missing(level);
        if (missing.Count == 0) return new CreateInvoiceResult.Refused(InvoiceRefusal.NoDifference);
        if (Sheets <= 0) return new CreateInvoiceResult.Refused(InvoiceRefusal.NoSheets);
        var lines = missing.Select(m => new Entry(m.Fillers[0], 1, m.Stacks[0], Member: m.Id)).ToImmutableArray();
        return new CreateInvoiceResult.Created(this with { Sheets = Sheets - 1 },
            new Invoice(InvoiceKind.WantList, lines, createdTotalDays, position, new WantedFrom(CollectionId, level.Id)));
    }

    /// <summary>
    /// Puts what the album holds on the members of the collection as it is now (K7, M1 and M3 of the plan's Decisions):
    /// <list type="bullet">
    /// <item>The album's level is the collection's: its unplaced items go to their members there.</item>
    /// <item>It is not (an album of format 1, which has none; a level renamed or removed by a patch): everything it collected,
    /// placed and unplaced, goes to the level that gives it the most members (a tie: the first level), which becomes the album's.
    /// No level takes any of it: the album stays as it is.</item>
    /// </list>
    /// An item whose member is already collected adds nothing; one that is no member of the level stays unplaced, so nothing is
    /// lost and it is placed once the collection has it again.
    /// </summary>
    public Album Place(DefinedCollection collection)
    {
        if (Level != null && collection.Level(Level) is { } own)
            return Unplaced.IsEmpty ? this : PlacedOn(own, Collected, Unplaced);
        if (IsEmpty) return this;

        var all = Collected.Values.Concat(Unplaced).ToList();
        CollectionLevel? best = null;
        int most = 0;
        foreach (var level in collection.Levels)
        {
            int members = all.Select(by => level.MemberOf(by.Key)?.Id).OfType<string>().Distinct().Count();
            if (members > most) (best, most) = (level, members);
        }
        return best == null ? this : PlacedOn(best, NoneCollected, all);
    }

    private Album PlacedOn(CollectionLevel level, ImmutableSortedDictionary<string, FilledBy> collected, IEnumerable<FilledBy> toPlace)
    {
        var placed = collected.ToBuilder();
        var unplaced = ImmutableList.CreateBuilder<FilledBy>();
        foreach (var by in toPlace)
        {
            if (level.MemberOf(by.Key) is { } member) placed.TryAdd(member.Id, by);
            else unplaced.Add(by);
        }
        return this with { Level = level.Id, Collected = placed.ToImmutable(), Unplaced = unplaced.ToImmutable() };
    }

    /// <summary>
    /// An album of format 1 (0.1): a snapshot of the collection's members then and the entries still missing. What it collected is
    /// the snapshot less the missing; without a snapshot (development builds before 0.1.0), the collection's first level now less
    /// the missing, as 0.1 read it. Those items are placed by <see cref="Place"/> (on the level that takes most of them: format 1
    /// knew one level, the pieces, which a patch may have moved from the first place) when the collection is known, else kept
    /// unplaced; nothing collected is lost.
    /// </summary>
    public static Album FromFormat1(string collectionId, int sheets, string name, IReadOnlyList<FilledBy>? snapshot,
        IEnumerable<ItemKey> missing, DefinedCollection? collection)
    {
        var first = collection?.Levels.FirstOrDefault();
        var members = snapshot ?? first?.Members.Select(m => new FilledBy(m.Fillers[0], m.Stacks[0])).ToList() ?? [];
        var stillMissing = missing.ToHashSet();
        var collected = members.Where(m => !stillMissing.Contains(m.Key)).ToImmutableList();
        // The level as a new album's: chosen only when there is no choice.
        var album = new Album(collectionId, collection?.Levels.Count == 1 ? first!.Id : null, NoneCollected, collected, sheets, name);
        return collection != null ? album.Place(collection) : album;
    }
}
