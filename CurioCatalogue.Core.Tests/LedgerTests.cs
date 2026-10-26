using System.Collections.Immutable;
using CurioCatalogue.Core;

namespace CurioCatalogue.Core.Tests;

public class LedgerTests
{
    private static readonly LedgerLimits Limits = new(MaxSheets: 32, SoftEntryLimit: 5);
    private static readonly BlockPosition Pos = new(1, 2, 3);

    private static ItemKey K(string name) => new(CollectibleKind.Item, "game:" + name, "");
    private static Entry E(string name, int count) => new(K(name), count, new object());

    private static Ledger LedgerOf(LedgerMode mode, int sheets, params Entry[] entries) =>
        Ledger.Empty(mode, sheets) with { Entries = entries.ToImmutableDictionary(e => e.Key) };

    private static Invoice Created(CreateInvoiceResult result, out Ledger ledger)
    {
        var created = Assert.IsType<CreateInvoiceResult.Created>(result);
        ledger = created.Ledger;
        return created.Invoice;
    }

    private static InvoiceRefusal Refused(CreateInvoiceResult result) =>
        Assert.IsType<CreateInvoiceResult.Refused>(result).Reason;

    private static (string, int)[] Lines(Invoice invoice) =>
        invoice.Entries.Select(e => (e.Key.Code[5..], e.Count)).ToArray();

    private static (string, int)[] Lines(Ledger ledger) =>
        ledger.Entries.Values.Select(e => (e.Key.Code[5..], e.Count)).OrderBy(l => l.Item1).ToArray();

    // --- Creating invoices: the four set operations ---

    [Fact]
    public void Incoming_by_type_is_container_minus_ledger()
    {
        var ledger = LedgerOf(LedgerMode.Types, 3, E("a", 1));
        var invoice = Created(ledger.CreateInvoice(InvoiceKind.Incoming, [E("a", 2), E("b", 4)], Limits, 10, Pos), out var after);

        Assert.Equal([("b", 4)], Lines(invoice));
        Assert.Equal(2, after.Sheets);
    }

    [Fact]
    public void Incoming_by_count_is_the_surplus()
    {
        var ledger = LedgerOf(LedgerMode.Counts, 3, E("a", 2), E("c", 9));
        var invoice = Created(ledger.CreateInvoice(InvoiceKind.Incoming, [E("a", 5), E("b", 1), E("c", 3)], Limits, 10, Pos), out _);

        Assert.Equal([("a", 3), ("b", 1)], Lines(invoice));
    }

    [Fact]
    public void Outgoing_by_type_is_the_intersection_with_full_container_counts()
    {
        var ledger = LedgerOf(LedgerMode.Types, 3, E("a", 1), E("c", 1));
        var invoice = Created(ledger.CreateInvoice(InvoiceKind.Outgoing, [E("a", 7), E("b", 1)], Limits, 10, Pos), out _);

        Assert.Equal([("a", 7)], Lines(invoice));
    }

    [Fact]
    public void Outgoing_by_count_is_the_minimum()
    {
        var ledger = LedgerOf(LedgerMode.Counts, 3, E("a", 4), E("b", 5));
        var invoice = Created(ledger.CreateInvoice(InvoiceKind.Outgoing, [E("a", 7), E("b", 2), E("c", 1)], Limits, 10, Pos), out _);

        Assert.Equal([("a", 4), ("b", 2)], Lines(invoice));
    }

    [Fact]
    public void Same_item_in_several_slots_is_summed_in_first_seen_order()
    {
        var ledger = Ledger.Empty(LedgerMode.Counts, 1);
        var invoice = Created(ledger.CreateInvoice(InvoiceKind.Incoming, [E("b", 1), E("a", 2), E("b", 3)], Limits, 10, Pos), out _);

        Assert.Equal([("b", 4), ("a", 2)], Lines(invoice));
    }

    [Fact]
    public void Invoice_records_kind_time_and_position_and_keeps_samples()
    {
        var sample = E("a", 1);
        var invoice = Created(Ledger.Empty(LedgerMode.Types, 1).CreateInvoice(InvoiceKind.Incoming, [sample], Limits, 12.5, Pos), out _);

        Assert.Equal(InvoiceKind.Incoming, invoice.Kind);
        Assert.Equal(12.5, invoice.CreatedTotalDays);
        Assert.Equal(Pos, invoice.Position);
        Assert.Same(sample.Sample, invoice.Entries[0].Sample);
    }

    // --- Refusals ---

    [Fact]
    public void Empty_container_gives_no_invoice_and_spends_no_sheet()
    {
        var ledger = LedgerOf(LedgerMode.Types, 3, E("a", 1));
        Assert.Equal(InvoiceRefusal.NoDifference, Refused(ledger.CreateInvoice(InvoiceKind.Incoming, [], Limits, 0, Pos)));
        Assert.Equal(InvoiceRefusal.NoDifference, Refused(ledger.CreateInvoice(InvoiceKind.Outgoing, [], Limits, 0, Pos)));
    }

    [Fact]
    public void Equal_sets_give_no_incoming_difference()
    {
        var ledger = LedgerOf(LedgerMode.Counts, 3, E("a", 2));
        Assert.Equal(InvoiceRefusal.NoDifference, Refused(ledger.CreateInvoice(InvoiceKind.Incoming, [E("a", 2)], Limits, 0, Pos)));
    }

    [Fact]
    public void Empty_ledger_gives_no_outgoing_difference()
    {
        var ledger = Ledger.Empty(LedgerMode.Types, 3);
        Assert.Equal(InvoiceRefusal.NoDifference, Refused(ledger.CreateInvoice(InvoiceKind.Outgoing, [E("a", 1)], Limits, 0, Pos)));
    }

    [Fact]
    public void No_sheets_refuses_only_when_there_is_a_difference()
    {
        var ledger = Ledger.Empty(LedgerMode.Types, 0);
        Assert.Equal(InvoiceRefusal.NoSheets, Refused(ledger.CreateInvoice(InvoiceKind.Incoming, [E("a", 1)], Limits, 0, Pos)));
        Assert.Equal(InvoiceRefusal.NoDifference, Refused(ledger.CreateInvoice(InvoiceKind.Incoming, [], Limits, 0, Pos)));
    }

    [Fact]
    public void Full_ledger_makes_no_incoming_but_still_outgoing()
    {
        var ledger = LedgerOf(LedgerMode.Types, 3, E("a", 1), E("b", 1), E("c", 1), E("d", 1), E("e", 1));
        Assert.Equal(InvoiceRefusal.LedgerFull, Refused(ledger.CreateInvoice(InvoiceKind.Incoming, [E("z", 1)], Limits, 0, Pos)));
        Created(ledger.CreateInvoice(InvoiceKind.Outgoing, [E("a", 1)], Limits, 0, Pos), out _);
    }

    [Fact]
    public void Pattern_entries_do_not_match_yet()
    {
        var pattern = E("a", 1) with { Kind = EntryKind.Pattern };
        var ledger = LedgerOf(LedgerMode.Types, 3, pattern);
        var invoice = Created(ledger.CreateInvoice(InvoiceKind.Incoming, [E("a", 1)], Limits, 0, Pos), out _);
        Assert.Equal([("a", 1)], Lines(invoice));
    }

    // --- Posting invoices into a ledger ---

    private static Invoice InvoiceOf(InvoiceKind kind, params Entry[] lines) => new(kind, [.. lines], 0, Pos);

    [Fact]
    public void Incoming_into_types_ledger_adds_types_with_count_one()
    {
        var ledger = LedgerOf(LedgerMode.Types, 0, E("a", 1));
        var result = ledger.Post(InvoiceOf(InvoiceKind.Incoming, E("a", 3), E("b", 4)), Limits);

        Assert.Equal([("a", 1), ("b", 1)], Lines(result.Ledger));
        Assert.False(result.OverLimit);
    }

    [Fact]
    public void Incoming_into_counts_ledger_adds_counts()
    {
        var ledger = LedgerOf(LedgerMode.Counts, 0, E("a", 1));
        var result = ledger.Post(InvoiceOf(InvoiceKind.Incoming, E("a", 3), E("b", 4)), Limits);

        Assert.Equal([("a", 4), ("b", 4)], Lines(result.Ledger));
    }

    [Fact]
    public void Outgoing_from_types_ledger_removes_types_and_ignores_missing()
    {
        var ledger = LedgerOf(LedgerMode.Types, 0, E("a", 1), E("b", 1));
        var result = ledger.Post(InvoiceOf(InvoiceKind.Outgoing, E("a", 5), E("z", 1)), Limits);

        Assert.Equal([("b", 1)], Lines(result.Ledger));
    }

    [Fact]
    public void Outgoing_from_counts_ledger_subtracts_down_to_zero()
    {
        var ledger = LedgerOf(LedgerMode.Counts, 0, E("a", 5), E("b", 2));
        var result = ledger.Post(InvoiceOf(InvoiceKind.Outgoing, E("a", 3), E("b", 9), E("z", 1)), Limits);

        Assert.Equal([("a", 2)], Lines(result.Ledger));
    }

    [Fact]
    public void Posting_keeps_the_ledgers_own_sample()
    {
        var mine = E("a", 1);
        var ledger = LedgerOf(LedgerMode.Counts, 0, mine);
        var result = ledger.Post(InvoiceOf(InvoiceKind.Incoming, E("a", 2)), Limits);

        Assert.Same(mine.Sample, result.Ledger.Entries[K("a")].Sample);
    }

    [Fact]
    public void Posting_over_the_soft_limit_adds_everything_and_warns()
    {
        var ledger = LedgerOf(LedgerMode.Types, 0, E("a", 1), E("b", 1), E("c", 1), E("d", 1));
        var result = ledger.Post(InvoiceOf(InvoiceKind.Incoming, E("e", 1), E("f", 1)), Limits);

        Assert.Equal(6, result.Ledger.Entries.Count);
        Assert.True(result.OverLimit);
    }

    [Fact]
    public void Project_scenario_shopping_list_shrinks()
    {
        // domain.md, Scenarios: a counts ledger as a shopping list, ticked off by outgoing invoices from several chests.
        var list = LedgerOf(LedgerMode.Counts, 2, E("x", 4), E("barrel", 5));

        var first = Created(list.CreateInvoice(InvoiceKind.Outgoing, [E("x", 1), E("barrel", 3), E("other", 8)], Limits, 0, Pos), out list);
        list = list.Post(first, Limits).Ledger;
        var second = Created(list.CreateInvoice(InvoiceKind.Outgoing, [E("x", 10)], Limits, 0, Pos), out list);
        list = list.Post(second, Limits).Ledger;

        Assert.Equal([("barrel", 2)], Lines(list));
        Assert.Equal([("x", 3)], Lines(second));
    }

    [Fact]
    public void Invoices_post_into_a_ledger_of_the_other_mode()
    {
        // An invoice carries real stacks, so a counts invoice works on a types ledger and back (domain.md, Posting an invoice).
        var counts = LedgerOf(LedgerMode.Counts, 1);
        var invoice = Created(counts.CreateInvoice(InvoiceKind.Incoming, [E("a", 3)], Limits, 0, Pos), out _);

        Assert.Equal([("a", 1)], Lines(Ledger.Empty(LedgerMode.Types, 0).Post(invoice, Limits).Ledger));
        Assert.Equal([("a", 3)], Lines(Ledger.Empty(LedgerMode.Counts, 0).Post(invoice, Limits).Ledger));
    }

    // --- Loading ---

    [Fact]
    public void Stored_entries_with_the_same_key_merge()
    {
        var first = E("a", 2);
        Assert.Equal([("a", 5), ("b", 1)], Lines(Ledger.FromStored(LedgerMode.Counts, 0, [first, E("b", 1), E("a", 3)])));
        Assert.Equal([("a", 1)], Lines(Ledger.FromStored(LedgerMode.Types, 0, [E("a", 1), E("a", 1)])));
        Assert.Same(first.Sample, Ledger.FromStored(LedgerMode.Counts, 0, [first, E("a", 3)]).Entries[K("a")].Sample);
    }

    // --- Sheets and clearing ---

    [Fact]
    public void Returned_sheet_goes_back_unless_the_ledger_is_full()
    {
        Assert.Equal(6, Ledger.Empty(LedgerMode.Types, 5).ReturnSheet(Limits)!.Sheets);
        Assert.Null(Ledger.Empty(LedgerMode.Types, 32).ReturnSheet(Limits));
    }

    [Theory]
    [InlineData(30, 5, 32, 2)]
    [InlineData(0, 8, 8, 8)]
    [InlineData(32, 1, 32, 0)]
    [InlineData(10, 0, 10, 0)]
    public void Recharge_takes_only_what_fits(int sheets, int available, int expectedSheets, int expectedUsed)
    {
        var (ledger, used) = Ledger.Empty(LedgerMode.Types, sheets).AddSheets(available, Limits);
        Assert.Equal(expectedSheets, ledger.Sheets);
        Assert.Equal(expectedUsed, used);
    }

    [Fact]
    public void Clear_keeps_sheets_name_and_album()
    {
        var ledger = LedgerOf(LedgerMode.Types, 7, E("a", 1)) with { Name = "Tapestries", Album = new AlbumInfo("tapestries", 88) };
        var cleared = ledger.Clear();

        Assert.True(cleared.IsEmpty);
        Assert.Equal(7, cleared.Sheets);
        Assert.Equal("Tapestries", cleared.Name);
        Assert.Equal(new AlbumInfo("tapestries", 88), cleared.Album);
    }

    [Fact]
    public void Operations_do_not_change_the_original_ledger()
    {
        var ledger = LedgerOf(LedgerMode.Counts, 3, E("a", 1));
        Created(ledger.CreateInvoice(InvoiceKind.Incoming, [E("b", 1)], Limits, 0, Pos), out _);
        ledger.Post(InvoiceOf(InvoiceKind.Outgoing, E("a", 1)), Limits);

        Assert.Equal(3, ledger.Sheets);
        Assert.Equal([("a", 1)], Lines(ledger));
    }
}
