using System;
using ExhaustiveMatching;

namespace CurioCatalogue.Core;

/// <summary>
/// What every book has, whatever it registers (domain.md, Books): sheets for invoices and a name. A <see cref="Ledger"/> (the
/// ledger and the bill of materials) or an <see cref="Album"/>: a step towards books as a sum type (ROADMAP.md, Technical).
/// Immutable: every operation returns a new book of the same kind.
/// </summary>
/// <param name="Name">Name given by the player; empty for none.</param>
[Closed(typeof(Ledger), typeof(Album))]
public abstract record Book(int Sheets, string Name)
{
    /// <summary>Nothing registered: a ledger without entries, an album with nothing collected.</summary>
    public abstract bool IsEmpty { get; }

    /// <summary>
    /// Takes back the sheet of a posted or cancelled invoice. Returns null when the book is already full of sheets: the caller then
    /// turns the invoice into parchment instead.
    /// </summary>
    public Book? ReturnSheet(LedgerLimits limits) =>
        Sheets < limits.MaxSheets ? this with { Sheets = Sheets + 1 } : null;

    /// <summary>Recharges the book with up to <paramref name="available"/> sheets of parchment.</summary>
    /// <returns>The recharged book and how many sheets it took (0 when it is full).</returns>
    public (Book Book, int Used) AddSheets(int available, LedgerLimits limits)
    {
        int used = Math.Clamp(limits.MaxSheets - Sheets, 0, Math.Max(0, available));
        return (this with { Sheets = Sheets + used }, used);
    }
}
