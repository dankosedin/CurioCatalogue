using Vintagestory.API.Common;
using CurioCatalogue.Core;

namespace CurioCatalogue.Tests;

public class ItemLedgerTests
{
    private static ItemLedger Of(string path) => new() { Code = new AssetLocation("curiocatalogue", path) };

    [Theory]
    [InlineData("book-ledger-normal-teal", LedgerMode.Types, "normal-teal", false)]
    [InlineData("book-materials-aged-orangebrown", LedgerMode.Counts, "aged-orangebrown", false)]
    [InlineData("book-album-normal-darkgreen", LedgerMode.Album, "normal-darkgreen", true)]
    public void Mode_and_cover_come_from_the_code(string path, LedgerMode mode, string cover, bool album)
    {
        // The code is "<item>-<mode>-<cover>" (itemtypes/book.json; domain.md, Covers).
        var item = Of(path);
        Assert.Equal((mode, cover, album), (item.LedgerMode, item.Cover, item.IsAlbum));
    }
}
