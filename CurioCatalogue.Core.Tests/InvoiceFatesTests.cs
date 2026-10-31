namespace CurioCatalogue.Core.Tests;

/// <summary>The one table of how an invoice ends (domain.md, Posting an invoice; Cancelling).</summary>
public class InvoiceFatesTests
{
    private static readonly LedgerLimits Limits = new(MaxSheets: 4, SoftEntryLimit: 500);

    private static Ledger Book(int sheets) => Ledger.Empty(LedgerMode.Types, sheets);

    [Fact]
    public void A_book_with_room_takes_the_sheet_back()
    {
        var end = InvoiceFates.Ended(Book(3), Limits, consumeSheets: false);

        Assert.Equal(InvoiceFate.SheetReturned, end.Fate);
        Assert.Equal(4, end.Book!.Sheets);
    }

    [Fact]
    public void A_full_book_or_none_makes_parchment()
    {
        Assert.Equal(new InvoiceEnd(InvoiceFate.Parchment, null), InvoiceFates.Ended(Book(4), Limits, consumeSheets: false));
        Assert.Equal(new InvoiceEnd(InvoiceFate.Parchment, null), InvoiceFates.Ended(null, Limits, consumeSheets: false));
    }

    [Fact]
    public void Consume_sheets_takes_the_invoice_away_whatever_is_in_the_off_hand()
    {
        Assert.Equal(new InvoiceEnd(InvoiceFate.Consumed, null), InvoiceFates.Ended(Book(1), Limits, consumeSheets: true));
        Assert.Equal(new InvoiceEnd(InvoiceFate.Consumed, null), InvoiceFates.Ended(null, Limits, consumeSheets: true));
    }

    [Fact]
    public void A_cancelled_blank_invoice_gives_no_sheet_and_no_parchment()
    {
        Assert.Equal(new InvoiceEnd(InvoiceFate.Consumed, null), InvoiceFates.Cancelled(blank: true, Book(1), Limits, consumeSheets: false));
        Assert.Equal(new InvoiceEnd(InvoiceFate.Consumed, null), InvoiceFates.Cancelled(blank: true, null, Limits, consumeSheets: false));
    }

    [Fact]
    public void A_cancelled_invoice_with_lines_ends_as_a_posted_one()
    {
        Assert.Equal(InvoiceFate.SheetReturned, InvoiceFates.Cancelled(blank: false, Book(3), Limits, consumeSheets: false).Fate);
        Assert.Equal(InvoiceFate.Parchment, InvoiceFates.Cancelled(blank: false, null, Limits, consumeSheets: false).Fate);
    }

    [Theory]
    [InlineData(false, false, InvoiceFate.Kept)]
    [InlineData(false, true, InvoiceFate.Kept)]
    [InlineData(true, false, InvoiceFate.Parchment)]
    [InlineData(true, true, InvoiceFate.Consumed)]
    public void A_posted_want_list_stays_until_done_and_goes_back_into_no_book(bool done, bool consumeSheets, InvoiceFate fate)
    {
        Assert.Equal(fate, InvoiceFates.WantListPosted(done, consumeSheets));
    }
}
