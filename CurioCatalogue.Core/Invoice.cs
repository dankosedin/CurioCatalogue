using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CurioCatalogue.Core;

public enum InvoiceKind
{
    /// <summary>Items the ledger does not have yet; posting adds them to the ledger.</summary>
    Incoming,

    /// <summary>Items the ledger lists; posting removes them from the ledger.</summary>
    Outgoing,

    /// <summary>
    /// A want list: an album's "missing" list for another player (domain.md, Albums and collections). Posting takes what a container
    /// has of it, one of each, and crosses that off the list; it does not touch any ledger.
    /// </summary>
    WantList,
}

/// <summary>The album a want list of format 2 was made from: its collection and level. Its lines name members of that level.</summary>
public sealed record WantedFrom(string CollectionId, string Level)
{
    /// <summary>The level in the collection as it is now (<see cref="DefinedCollection.LevelOrOnly"/>); null when it has none such.</summary>
    public CollectionLevel? LevelIn(DefinedCollection collection) => collection.LevelOrOnly(Level);
}

/// <summary>A line of a want list a container can give now (its index in the list) and the item there that gives it.</summary>
public sealed record WantedLine(int Line, ItemKey Key);

/// <summary>
/// A self-contained document: real stacks with counts, not tied to the ledger that made it.
/// </summary>
/// <param name="Entries">One entry per item, in the order the items were found in the container (a want list: the album's order
/// of taking new).</param>
/// <param name="CreatedTotalDays">In-game time of creation, in days since the world began.</param>
/// <param name="From">A want list of format 2: the album's collection and level, whose members its lines name; null for other
/// invoices and for want lists of format 1 (0.1), whose lines are exact items.</param>
public sealed record Invoice(InvoiceKind Kind, ImmutableArray<Entry> Entries, double CreatedTotalDays, BlockPosition Position,
    WantedFrom? From = null)
{
    /// <summary>
    /// For a want list: the lines a container that has <paramref name="available"/> can give now, in the list's order, one item
    /// each. A line naming a member of <paramref name="level"/> goes the album's way (<see cref="Album.Wanted"/>): any item that
    /// fills the member, the first in the member's order. A line of format 1, or one whose member the level no longer has (or no
    /// level: the collection unknown), wants exactly its own item. No item is given to two lines.
    /// </summary>
    /// <param name="level">The level of <see cref="From"/> as the collection is now; null when unknown.</param>
    public IReadOnlyList<WantedLine> Wanted(CollectionLevel? level, IReadOnlySet<ItemKey> available)
    {
        var used = new HashSet<ItemKey>();
        var wanted = new List<WantedLine>();
        for (int i = 0; i < Entries.Length; i++)
        {
            var line = Entries[i];
            IEnumerable<ItemKey> fillers = line.Member != null && level?.Find(line.Member) is { } member ? member.Fillers : [line.Key];
            if (fillers.FirstOrDefault(k => available.Contains(k) && !used.Contains(k)) is { } key)
            {
                used.Add(key);
                wanted.Add(new WantedLine(i, key));
            }
        }
        return wanted;
    }

    /// <summary>The invoice with these lines (by index) crossed off its list (what a want list has delivered).</summary>
    public Invoice CrossOff(IEnumerable<int> lines)
    {
        var done = lines.ToHashSet();
        return this with { Entries = Entries.Where((_, i) => !done.Contains(i)).ToImmutableArray() };
    }
}
