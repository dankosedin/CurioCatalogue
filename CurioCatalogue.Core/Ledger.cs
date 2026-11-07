using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ExhaustiveMatching;

namespace CurioCatalogue.Core;

public enum LedgerMode
{
    /// <summary>A set: only whether an item is there. Entry counts are always 1.</summary>
    Types,

    /// <summary>A multiset: item and how many.</summary>
    Counts,

    /// <summary>
    /// The album (domain.md, Albums and collections): the kind of book, not a ledger's mode. Its data is an <see cref="Core.Album"/>,
    /// never a <see cref="Ledger"/>.
    /// </summary>
    Album,
}

/// <summary>Settings that bound a ledger; they come from the config and are not stored in the book.</summary>
public sealed record LedgerLimits(int MaxSheets, int SoftEntryLimit);

public enum InvoiceRefusal
{
    /// <summary>The ledger has reached its entry limit, so it makes no incoming invoices.</summary>
    LedgerFull,

    /// <summary>The difference is empty: nothing to put on an invoice. No sheet is spent.</summary>
    NoDifference,

    /// <summary>The ledger has no sheets left.</summary>
    NoSheets,
}

/// <summary>Result of <see cref="Ledger.CreateInvoice"/>: either a new invoice or the reason there is none.</summary>
[Closed(typeof(CreateInvoiceResult.Created), typeof(CreateInvoiceResult.Refused))]
public abstract record CreateInvoiceResult
{
    /// <param name="Book">The ledger or album with one sheet spent.</param>
    public sealed record Created(Book Book, Invoice Invoice) : CreateInvoiceResult;

    public sealed record Refused(InvoiceRefusal Reason) : CreateInvoiceResult;
}

/// <param name="OverLimit">The ledger is now at or over the soft limit, so it makes no more incoming invoices; warn the player.</param>
public sealed record PostResult(Ledger Ledger, bool OverLimit);

/// <summary>
/// The contents of a ledger or a bill of materials (an album is an <see cref="Core.Album"/>). Immutable: every operation returns a
/// new ledger, so the caller writes it back to the item only when the whole action succeeded.
/// </summary>
/// <param name="Mode">Types or Counts; never Album.</param>
public sealed record Ledger(
    LedgerMode Mode,
    int Sheets,
    ImmutableDictionary<ItemKey, Entry> Entries,
    string Name = "") : Book(Sheets, Name)
{
    public static Ledger Empty(LedgerMode mode, int sheets) =>
        new(mode, sheets, ImmutableDictionary<ItemKey, Entry>.Empty);

    /// <summary>
    /// Builds a ledger from stored entries. Keys are recomputed on every load, so after a change of the
    /// ignored attributes two stored entries may get the same key: they merge (types: count 1, counts: summed).
    /// </summary>
    public static Ledger FromStored(LedgerMode mode, int sheets, IEnumerable<Entry> entries, string name = "")
    {
        var merged = Multiset.Sum(entries).Select(e => mode == LedgerMode.Counts ? e : e.WithCount(1));
        return new Ledger(mode, sheets, merged.ToImmutableDictionary(e => e.Key), name);
    }

    public override bool IsEmpty => Entries.IsEmpty;

    /// <summary>
    /// Compares a container with the ledger and makes an invoice of the difference, spending one sheet.
    /// Incoming: items the ledger lacks (types: C \ B, counts: C − B).
    /// Outgoing: items the ledger lists (types: C ∩ B, counts: min(C, B)).
    /// </summary>
    /// <param name="containerSlots">One entry per non-empty slot; the same item may appear in several slots.
    /// The caller has already left out the mod's own items.</param>
    public CreateInvoiceResult CreateInvoice(
        InvoiceKind kind, IEnumerable<Entry> containerSlots, LedgerLimits limits, double createdTotalDays, BlockPosition position)
    {
        if (kind == InvoiceKind.Incoming && Entries.Count >= limits.SoftEntryLimit)
            return new CreateInvoiceResult.Refused(InvoiceRefusal.LedgerFull);

        var lines = Multiset.Sum(containerSlots)
            .Select(c => c.WithCount(InvoiceCount(kind, c)))
            .Where(c => c.Count > 0)
            .ToImmutableArray();

        if (lines.IsEmpty) return new CreateInvoiceResult.Refused(InvoiceRefusal.NoDifference);
        if (Sheets <= 0) return new CreateInvoiceResult.Refused(InvoiceRefusal.NoSheets);

        return new CreateInvoiceResult.Created(
            this with { Sheets = Sheets - 1 },
            new Invoice(kind, lines, createdTotalDays, position));
    }

    /// <summary>How many of a container's item go on the invoice.</summary>
    private int InvoiceCount(InvoiceKind kind, Entry inContainer)
    {
        int inLedger = Entries.TryGetValue(inContainer.Key, out var e) && e.Kind == EntryKind.Exact ? e.Count : 0;
        return (kind, Mode) switch
        {
            (InvoiceKind.Incoming, LedgerMode.Types) => inLedger > 0 ? 0 : inContainer.Count,
            (InvoiceKind.Incoming, LedgerMode.Counts) => Math.Max(0, inContainer.Count - inLedger),
            (InvoiceKind.Outgoing, LedgerMode.Types) => inLedger > 0 ? inContainer.Count : 0,
            (InvoiceKind.Outgoing, LedgerMode.Counts) => Math.Min(inContainer.Count, inLedger),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    /// <summary>
    /// Applies a posted invoice to the ledger: incoming adds its items, outgoing removes them
    /// (never below zero; items the ledger does not list are ignored). Never fails, even over the limit.
    /// Sheets are handled separately, see <see cref="Book.ReturnSheet"/>.
    /// </summary>
    public PostResult Post(Invoice invoice, LedgerLimits limits)
    {
        // A want list is for another player's collection: it changes no ledger (domain.md, Albums and collections).
        if (invoice.Kind == InvoiceKind.WantList) return new PostResult(this, Entries.Count >= limits.SoftEntryLimit);

        var entries = Entries.ToBuilder();
        foreach (var line in invoice.Entries)
        {
            entries.TryGetValue(line.Key, out var current);
            int count = current?.Count ?? 0;
            int updated = (invoice.Kind, Mode) switch
            {
                (InvoiceKind.Incoming, LedgerMode.Types) => 1,
                (InvoiceKind.Incoming, LedgerMode.Counts) => count + line.Count,
                (InvoiceKind.Outgoing, LedgerMode.Types) => 0,
                (InvoiceKind.Outgoing, LedgerMode.Counts) => Math.Max(0, count - line.Count),
                _ => throw new ArgumentOutOfRangeException(nameof(invoice)),
            };

            if (updated > 0) entries[line.Key] = (current ?? line).WithCount(updated);
            else entries.Remove(line.Key);
        }

        var ledger = this with { Entries = entries.ToImmutable() };
        return new PostResult(ledger, ledger.Entries.Count >= limits.SoftEntryLimit);
    }

    /// <summary>Clears all entries; sheets and name stay. An album starts over instead (<see cref="Album.StartOver"/>).</summary>
    public Ledger Clear() => this with { Entries = ImmutableDictionary<ItemKey, Entry>.Empty };
}
