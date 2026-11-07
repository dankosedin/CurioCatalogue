namespace CurioCatalogue.Core;

public enum EntryKind
{
    /// <summary>Matches exactly one item: the one with this <see cref="ItemKey"/>.</summary>
    Exact,

    /// <summary>Reserved for template entries ("any plank"); not matched by anything yet.</summary>
    Pattern,
}

/// <summary>
/// A line of a ledger or an invoice: an item, how many of it, and a sample stack to show it in the GUI.
/// </summary>
/// <param name="Sample">A sample of the item for the GUI: the game's ItemStack. The core never reads it; only the adapter does.</param>
/// <param name="Member">On a want list of format 2: the id of the member of its album's level the line asks for (any item that
/// fills the member does, <see cref="Invoice.Wanted"/>); the key and the sample are then the member's first item, for the line
/// to show and to fall back on. Null for every other line: exactly its item.</param>
public sealed record Entry(ItemKey Key, int Count, object Sample, EntryKind Kind = EntryKind.Exact, string? Member = null)
{
    public Entry WithCount(int count) => this with { Count = count };
}

/// <summary>Block coordinates, for information only (where an invoice was made).</summary>
public sealed record BlockPosition(int X, int Y, int Z);
