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

/// <summary>
/// A self-contained document: real stacks with counts, not tied to the ledger that made it.
/// </summary>
/// <param name="Entries">One entry per item, in the order the items were found in the container.</param>
/// <param name="CreatedTotalDays">In-game time of creation, in days since the world began.</param>
public sealed record Invoice(InvoiceKind Kind, ImmutableArray<Entry> Entries, double CreatedTotalDays, BlockPosition Position)
{
    /// <summary>
    /// For a want list: the lines a container can give now (one of each, as far as it has them) and the
    /// invoice that is left after taking them. The list keeps its order.
    /// </summary>
    /// <param name="inContainer">The items the container has.</param>
    public (ImmutableArray<Entry> Take, Invoice Left) SplitBy(IReadOnlySet<ItemKey> inContainer)
    {
        var take = Entries.Where(e => inContainer.Contains(e.Key)).Select(e => e.WithCount(1)).ToImmutableArray();
        var rest = Entries.Where(e => !inContainer.Contains(e.Key)).ToImmutableArray();
        return (take, this with { Entries = rest });
    }

    /// <summary>The invoice with these items crossed off its list (what a want list has delivered).</summary>
    public Invoice CrossOff(IEnumerable<ItemKey> taken)
    {
        var done = taken.ToHashSet();
        return this with { Entries = Entries.Where(e => !done.Contains(e.Key)).ToImmutableArray() };
    }
}
