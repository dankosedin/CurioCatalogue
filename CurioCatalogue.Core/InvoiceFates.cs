namespace CurioCatalogue.Core;

/// <summary>What becomes of an invoice when it is posted or cancelled (domain.md, Posting an invoice; Cancelling).</summary>
public enum InvoiceFate
{
    /// <summary>The book in the off-hand took its sheet back; the invoice is gone.</summary>
    SheetReturned,

    /// <summary>No book in the off-hand, or it is full of sheets: the invoice is now parchment.</summary>
    Parchment,

    /// <summary>Hardcore option ConsumeSheets, or a blank invoice cancelled: the invoice is gone, no sheet comes back.</summary>
    Consumed,

    /// <summary>A want list with lines still left: it stays in the hand, shorter (domain.md, Albums and collections).</summary>
    Kept,
}

/// <param name="Book">The book in the off-hand with the sheet back, to be written; null when no sheet goes back.</param>
public sealed record InvoiceEnd(InvoiceFate Fate, Ledger? Book);

/// <summary>
/// The one table of how an invoice ends; the caller carries it out:
/// <code>
///                       ConsumeSheets   book with room   full book / none
/// posted invoice        consumed        sheet returned   parchment
/// cancelled (any kind)  consumed        sheet returned   parchment
/// cancelled blank       consumed        consumed         consumed
/// posted want list      list done: consumed or parchment (never into a book); lines left: kept
/// </code>
/// A blank invoice (no lines: from the creative inventory) was never a book's sheet, so it gives none back.
/// "Book" is any book in the off-hand, an album included: an album takes the sheet, its entries stay.
/// </summary>
public static class InvoiceFates
{
    /// <summary>A posted ordinary invoice, or any cancelled one.</summary>
    /// <param name="book">The book in the off-hand as it will be (after posting, if it takes the entries), or null.</param>
    public static InvoiceEnd Ended(Ledger? book, LedgerLimits limits, bool consumeSheets)
    {
        if (consumeSheets) return new InvoiceEnd(InvoiceFate.Consumed, null);
        return book?.ReturnSheet(limits) is { } withSheet
            ? new InvoiceEnd(InvoiceFate.SheetReturned, withSheet)
            : new InvoiceEnd(InvoiceFate.Parchment, null);
    }

    /// <summary>A cancelled invoice of any kind; a blank one is just gone.</summary>
    public static InvoiceEnd Cancelled(bool blank, Ledger? book, LedgerLimits limits, bool consumeSheets) =>
        blank ? new InvoiceEnd(InvoiceFate.Consumed, null) : Ended(book, limits, consumeSheets);

    /// <summary>A posted want list: it was made for someone else's collection, so its sheet goes back into no book.</summary>
    public static InvoiceFate WantListPosted(bool done, bool consumeSheets) =>
        !done ? InvoiceFate.Kept : consumeSheets ? InvoiceFate.Consumed : InvoiceFate.Parchment;
}
