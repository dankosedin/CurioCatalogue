using CurioCatalogue.Core;

namespace CurioCatalogue.Tests;

// The chat line after a cancel names the list by its kind (S11 of the 0.1.1 plan: a want list was called an invoice).
public class CancelMessageTests
{
    private static readonly InvoiceFate[] Cancellable = [InvoiceFate.SheetReturned, InvoiceFate.Parchment, InvoiceFate.Consumed];

    [Theory]
    [InlineData(InvoiceFate.Consumed, false, "wantlist-cancelled")]
    [InlineData(InvoiceFate.SheetReturned, false, "wantlist-cancelled-sheet")]
    [InlineData(InvoiceFate.SheetReturned, true, "wantlist-cancelled-sheet-album")]
    [InlineData(InvoiceFate.Parchment, false, "wantlist-cancelled-parchment")]
    public void A_cancelled_want_list_has_its_own_texts(InvoiceFate fate, bool toAlbum, string key)
    {
        Assert.Equal(key, PlayerMessages.CancelledKey(InvoiceKind.WantList, fate, toAlbum));
    }

    [Theory]
    [InlineData(InvoiceKind.Incoming)]
    [InlineData(InvoiceKind.Outgoing)]
    public void Intake_and_pick_lists_share_the_invoice_texts(InvoiceKind kind)
    {
        Assert.Equal("invoice-cancelled", PlayerMessages.CancelledKey(kind, InvoiceFate.Consumed, false));
        Assert.Equal("invoice-cancelled-sheet", PlayerMessages.CancelledKey(kind, InvoiceFate.SheetReturned, false));
        Assert.Equal("invoice-cancelled-sheet-album", PlayerMessages.CancelledKey(kind, InvoiceFate.SheetReturned, true));
        Assert.Equal("invoice-cancelled-parchment", PlayerMessages.CancelledKey(kind, InvoiceFate.Parchment, false));
    }

    [Fact]
    public void A_cancelled_list_is_never_kept()
    {
        Assert.ThrowsAny<Exception>(() => PlayerMessages.CancelledKey(InvoiceKind.WantList, InvoiceFate.Kept, false));
    }

    public static TheoryData<string> CancelKeys() => new(
        Enum.GetValues<InvoiceKind>()
            .SelectMany(kind => Cancellable.SelectMany(fate => new[] { false, true }.Select(album => PlayerMessages.CancelledKey(kind, fate, album))))
            .Distinct());

    [Theory]
    [MemberData(nameof(CancelKeys))]
    public void Every_cancel_text_exists_in_every_language(string key)
    {
        // Lang_file_has_the_keys_of_en makes the languages equal, so en is enough here.
        var en = File.ReadAllText(Path.Combine(AssetsTests.AssetsFolder, "lang", "en.json"));
        Assert.Contains($"\"curiocatalogue:{key}\"", en);
    }
}
