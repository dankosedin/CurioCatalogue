using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CurioCatalogue.Core;

public enum LedgerMode
{
    /// <summary>A set: only whether an item is there. Entry counts are always 1.</summary>
    Types,

    /// <summary>A multiset: item and how many.</summary>
    Counts,

    /// <summary>
    /// An album: a set of what a collection still misses (domain.md, Albums and collections). Like Types, but an outgoing invoice
    /// takes one of each missing item ("new for the collection") and leaves the duplicates. Incoming makes no sense
    /// for it and finds nothing.
    /// </summary>
    Album,
}

/// <summary>Settings that bound a ledger; they come from the config and are not stored in the book.</summary>
public sealed record LedgerLimits(int MaxSheets, int SoftEntryLimit);

/// <summary>One member of an album's snapshot: its key and a sample to store it by (the game's ItemStack; the core never reads it).</summary>
public sealed record SnapshotMember(ItemKey Key, object Sample);

/// <summary>An album remembers which collection it was made from and that collection's members then.</summary>
/// <param name="Members">The collection's members when the album was made or last refreshed; what was collected is these
/// minus the entries. Stored as samples and re-keyed on read, like the entries, so the two always agree. Null for an
/// album without a snapshot (made before it was kept as samples).</param>
public sealed record AlbumInfo(string CollectionId, IReadOnlyList<SnapshotMember>? Members = null);

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
public abstract record CreateInvoiceResult
{
    /// <param name="Ledger">The ledger with one sheet spent.</param>
    public sealed record Created(Ledger Ledger, Invoice Invoice) : CreateInvoiceResult;

    public sealed record Refused(InvoiceRefusal Reason) : CreateInvoiceResult;
}

/// <param name="OverLimit">The ledger now has more entries than the soft limit; warn the player.</param>
public sealed record PostResult(Ledger Ledger, bool OverLimit);

/// <summary>
/// The ledger's contents. Immutable: every operation returns a new ledger, so the caller writes it back
/// to the item only when the whole action succeeded.
/// </summary>
/// <param name="Name">Name given by the player; empty for none.</param>
/// <param name="Album">Set for an album, null for an ordinary ledger.</param>
public sealed record Ledger(
    LedgerMode Mode,
    int Sheets,
    ImmutableDictionary<ItemKey, Entry> Entries,
    string Name = "",
    AlbumInfo? Album = null)
{
    public static Ledger Empty(LedgerMode mode, int sheets) =>
        new(mode, sheets, ImmutableDictionary<ItemKey, Entry>.Empty);

    /// <summary>
    /// Builds a ledger from stored entries. Keys are recomputed on every load, so after a change of the
    /// ignored attributes two stored entries may get the same key: they merge (types: count 1, counts: summed).
    /// </summary>
    public static Ledger FromStored(
        LedgerMode mode, int sheets, IEnumerable<Entry> entries, string name = "", AlbumInfo? album = null)
    {
        var merged = Multiset.Sum(entries).Select(e => mode == LedgerMode.Counts ? e : e.WithCount(1));
        return new Ledger(mode, sheets, merged.ToImmutableDictionary(e => e.Key), name, album);
    }

    public bool IsEmpty => Entries.IsEmpty;

    /// <summary>
    /// An album's "missing" list as a want list for another player (domain.md, Albums and collections), spending one sheet.
    /// Nothing missing: NoDifference; no sheets: NoSheets.
    /// </summary>
    /// <param name="collection">The album's collection, for the order of the lines; without it, by key.</param>
    public CreateInvoiceResult MakeWantList(double createdTotalDays, BlockPosition position, Collection? collection = null)
    {
        if (Entries.IsEmpty) return new CreateInvoiceResult.Refused(InvoiceRefusal.NoDifference);
        if (Sheets <= 0) return new CreateInvoiceResult.Refused(InvoiceRefusal.NoSheets);
        var missing = collection?.InOrder(Entries.Values) ?? Entries.Values.OrderBy(e => e.Key.ToString(), StringComparer.Ordinal);
        var lines = missing.Select(e => e.WithCount(1)).ToImmutableArray();
        return new CreateInvoiceResult.Created(this with { Sheets = Sheets - 1 }, new Invoice(InvoiceKind.WantList, lines, createdTotalDays, position));
    }

    /// <summary>
    /// Compares a container with the ledger and makes an invoice of the difference, spending one sheet.
    /// Incoming: items the ledger lacks (types: C \ B, counts: C − B).
    /// Outgoing: items the ledger lists (types: C ∩ B, counts: min(C, B)). An album makes no invoices: it takes new
    /// items directly (domain.md, Albums and collections).
    /// </summary>
    /// <param name="containerSlots">One entry per non-empty slot; the same item may appear in several slots.
    /// The caller has already left out the mod's own items.</param>
    public CreateInvoiceResult CreateInvoice(
        InvoiceKind kind, IEnumerable<Entry> containerSlots, LedgerLimits limits, double createdTotalDays, BlockPosition position)
    {
        if (Mode == LedgerMode.Album) throw new InvalidOperationException("An album makes no invoices (domain.md, Albums and collections)");
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
    /// Not for an album: it ignores posted invoices (domain.md, Posting an invoice) and changes only by taking new items.
    /// Sheets are handled separately, see <see cref="ReturnSheet"/>.
    /// </summary>
    public PostResult Post(Invoice invoice, LedgerLimits limits)
    {
        // A want list is for another player's collection: it changes no ledger (domain.md, Albums and collections).
        if (invoice.Kind == InvoiceKind.WantList) return new PostResult(this, Entries.Count > limits.SoftEntryLimit);
        if (Mode == LedgerMode.Album) throw new InvalidOperationException("An album takes no posted invoices (domain.md, Posting an invoice)");

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
        return new PostResult(ledger, ledger.Entries.Count > limits.SoftEntryLimit);
    }

    /// <summary>
    /// Takes back the sheet of a posted or cancelled invoice. Returns null when the ledger is already full
    /// of sheets: the caller then turns the invoice into parchment instead.
    /// </summary>
    public Ledger? ReturnSheet(LedgerLimits limits) =>
        Sheets < limits.MaxSheets ? this with { Sheets = Sheets + 1 } : null;

    /// <summary>Recharges the ledger with up to <paramref name="available"/> sheets of parchment.</summary>
    /// <returns>The recharged ledger and how many sheets it took (0 when it is full).</returns>
    public (Ledger Ledger, int Used) AddSheets(int available, LedgerLimits limits)
    {
        int used = Math.Clamp(limits.MaxSheets - Sheets, 0, Math.Max(0, available));
        return (this with { Sheets = Sheets + used }, used);
    }

    /// <summary>Clears all entries; sheets, name and album info stay.</summary>
    /// <summary>An album with these items crossed off as collected (domain.md, Albums and collections). Items it does not list are ignored.</summary>
    public Ledger CrossOff(IEnumerable<ItemKey> collected) => this with { Entries = Entries.RemoveRange(collected) };

    public Ledger Clear() => this with { Entries = ImmutableDictionary<ItemKey, Entry>.Empty };
}
