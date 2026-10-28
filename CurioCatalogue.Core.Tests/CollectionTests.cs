using CurioCatalogue.Core;

namespace CurioCatalogue.Core.Tests;

public class CollectionTests
{
    private static readonly LedgerLimits Limits = new(32, 500);
    private static readonly BlockPosition Pos = new(0, 0, 0);

    private static ItemKey K(string piece) => new(CollectibleKind.Block, "game:tapestry-north", $"{{\"type\":\"{piece}\"}}");
    private static Entry E(string piece, int count) => new(K(piece), count, new object());

    // ambush is 3 pieces, notice is 1.
    private static readonly Collection Tapestries = new("tapestries",
    [
        new(K("ambush1"), "ambush", new object()),
        new(K("ambush2"), "ambush", new object()),
        new(K("ambush3"), "ambush", new object()),
        new(K("notice"), "notice", new object()),
    ]);

    [Fact]
    public void New_album_lists_every_member_once()
    {
        var album = Tapestries.NewAlbum(sheets: 5, name: "Album: tapestries");

        Assert.Equal(LedgerMode.Album, album.Mode);
        Assert.Equal(4, album.Entries.Count);
        Assert.All(album.Entries.Values, e => Assert.Equal(1, e.Count));
        Assert.Equal("tapestries", album.Album!.CollectionId);
        Assert.Equal(4, album.Album.Members!.Count);
        Assert.Equal((5, "Album: tapestries"), (album.Sheets, album.Name));
    }

    [Fact]
    public void A_album_neither_makes_nor_takes_ordinary_invoices()
    {
        // It takes new items directly (domain.md, Albums and collections) and ignores invoices posted with it in the off-hand;
        // the adapters never ask, so asking is a bug.
        var album = Tapestries.NewAlbum(3, "");
        Assert.Throws<InvalidOperationException>(() => album.CreateInvoice(InvoiceKind.Outgoing, [E("ambush1", 1)], Limits, 0, Pos));
        Assert.Throws<InvalidOperationException>(() => album.Post(new Invoice(InvoiceKind.Outgoing, [E("ambush1", 1)], 0, Pos), Limits));
    }

    [Fact]
    public void Progress_counts_pieces_and_complete_pictures()
    {
        Assert.Equal(new CollectionProgress(0, 4, 0, 2), Tapestries.Progress(Tapestries.Members.Select(m => m.Key)));
        Assert.Equal(new CollectionProgress(2, 4, 1, 2), Tapestries.Progress([K("ambush1"), K("ambush2")]));
        Assert.Equal(new CollectionProgress(4, 4, 2, 2), Tapestries.Progress([]));
    }

    // --- Refreshing an album after the collection changed ---

    private static Collection Of(params string[] pieces) =>
        new("tapestries", pieces.Select(p => new CollectionMember(K(p), p[..^1], new object())));

    private static Ledger Collect(Ledger album, params string[] pieces) => album.CrossOff(pieces.Select(K));

    [Fact]
    public void Refresh_keeps_progress_adds_new_members_and_drops_gone_ones()
    {
        var album = Collect(Of("a1", "a2", "b1").NewAlbum(7, "Mine"), "a1");

        // The collection now has c1 more and b1 less.
        var refreshed = Of("a1", "a2", "c1").Refresh(album);

        Assert.Equal([K("a2"), K("c1")], refreshed.Entries.Keys.OrderBy(k => k.Attributes)); // a1 stays collected
        Assert.Equal((7, "Mine", 3), (refreshed.Sheets, refreshed.Name, refreshed.Album!.Members!.Count));
        Assert.Equal(new CollectionProgress(1, 3, 0, 2), Of("a1", "a2", "c1").Progress(refreshed.Entries.Keys)); // groups a (a2 missing), c
    }

    [Fact]
    public void Refresh_with_the_same_collection_changes_nothing()
    {
        var collection = Of("a1", "a2");
        var album = Collect(collection.NewAlbum(3, ""), "a2");
        Assert.Equal(album.Entries.Keys, collection.Refresh(album).Entries.Keys);
    }

    [Fact]
    public void Refresh_of_an_empty_album_lists_only_new_members()
    {
        var album = Collect(Of("a1").NewAlbum(3, ""), "a1");
        Assert.True(album.IsEmpty);
        Assert.Equal([K("b1")], Of("a1", "b1").Refresh(album).Entries.Keys);
    }

    [Fact]
    public void Refresh_if_stale_leaves_a_current_album_alone_and_updates_a_stale_one()
    {
        var album = Collect(Of("a1", "a2").NewAlbum(3, ""), "a1");

        Assert.Same(album, Of("a1", "a2").RefreshIfStale(album));
        Assert.Equal([K("a2"), K("b1")], Of("a1", "a2", "b1").RefreshIfStale(album).Entries.Keys.OrderBy(k => k.Attributes));
    }

    [Fact]
    public void Album_without_a_snapshot_counts_as_made_from_the_current_collection()
    {
        var old = Ledger.FromStored(LedgerMode.Album, 3, [E("a2", 1)], "", new AlbumInfo("tapestries"));
        Assert.Equal([K("a2")], Of("a1", "a2").Refresh(old).Entries.Keys);
    }

    // --- Want lists (domain.md, Albums and collections) ---

    [Fact]
    public void Want_list_carries_the_missing_list_and_costs_a_sheet()
    {
        var album = Collect(Tapestries.NewAlbum(3, ""), "notice");

        var made = Assert.IsType<CreateInvoiceResult.Created>(album.MakeWantList(5, Pos));

        Assert.Equal(InvoiceKind.WantList, made.Invoice.Kind);
        Assert.Equal(3, made.Invoice.Entries.Length);
        Assert.All(made.Invoice.Entries, e => Assert.Equal(1, e.Count));
        Assert.Equal(2, made.Ledger.Sheets);
        Assert.Equal(3, made.Ledger.Entries.Count); // the album itself is unchanged
    }

    [Fact]
    public void No_want_list_when_nothing_is_missing_or_no_sheets()
    {
        var done = Collect(Tapestries.NewAlbum(3, ""), "ambush1", "ambush2", "ambush3", "notice");
        Assert.Equal(InvoiceRefusal.NoDifference, Assert.IsType<CreateInvoiceResult.Refused>(done.MakeWantList(0, Pos)).Reason);
        Assert.Equal(InvoiceRefusal.NoSheets, Assert.IsType<CreateInvoiceResult.Refused>(Tapestries.NewAlbum(0, "").MakeWantList(0, Pos)).Reason);
    }

    [Fact]
    public void A_container_gives_what_it_has_of_the_list_and_the_rest_stays()
    {
        var invoice = new Invoice(InvoiceKind.WantList, [E("ambush1", 1), E("ambush2", 1), E("notice", 1)], 0, Pos);

        var (take, left) = invoice.SplitBy(new HashSet<ItemKey> { K("ambush2"), K("notice"), K("other") });

        Assert.Equal([K("ambush2"), K("notice")], take.Select(e => e.Key));
        Assert.Equal([K("ambush1")], left.Entries.Select(e => e.Key));
        Assert.Equal(InvoiceKind.WantList, left.Kind);
    }

    [Fact]
    public void Delivered_lines_are_crossed_off_a_want_list()
    {
        var invoice = new Invoice(InvoiceKind.WantList, [E("ambush1", 1), E("ambush2", 1), E("notice", 1)], 0, Pos);
        Assert.Equal([K("ambush1"), K("notice")], invoice.CrossOff([K("ambush2"), K("other")]).Entries.Select(e => e.Key));
    }

    // --- Taking straight into the album (domain.md, Albums and collections) ---

    // Members in an order that differs from the keys' order: notice before ambush.
    private static readonly Collection NoticeFirst = new("tapestries",
    [
        new(K("notice"), "notice", new object()),
        new(K("ambush2"), "ambush", new object()),
        new(K("ambush1"), "ambush", new object()),
    ]);

    [Fact]
    public void Entries_come_in_the_collections_order_with_strangers_last()
    {
        var ordered = NoticeFirst.InOrder([E("zzz", 1), E("ambush1", 1), E("aaa", 1), E("notice", 1), E("ambush2", 1)]);
        Assert.Equal([K("notice"), K("ambush2"), K("ambush1"), K("aaa"), K("zzz")], ordered.Select(e => e.Key));
    }

    [Fact]
    public void Want_list_lists_in_the_collections_order()
    {
        var made = Assert.IsType<CreateInvoiceResult.Created>(NoticeFirst.NewAlbum(1, "").MakeWantList(0, Pos, NoticeFirst));
        Assert.Equal([K("notice"), K("ambush2"), K("ambush1")], made.Invoice.Entries.Select(e => e.Key));
    }

    [Fact]
    public void Collected_items_are_crossed_off_the_album_and_the_rest_is_kept()
    {
        var album = Tapestries.NewAlbum(3, "Mine");

        var after = album.CrossOff([K("ambush2"), K("notice"), K("other")]);

        Assert.Equal([K("ambush1"), K("ambush3")], after.Entries.Keys.OrderBy(k => k.ToString()));
        Assert.Equal((3, "Mine", album.Album), (after.Sheets, after.Name, after.Album)); // no sheet spent
    }

    [Fact]
    public void Posting_a_want_list_into_a_ledger_changes_nothing()
    {
        var ledger = Ledger.FromStored(LedgerMode.Counts, 3, [E("ambush1", 2)]);
        var invoice = new Invoice(InvoiceKind.WantList, [E("ambush1", 1)], 0, Pos);
        Assert.Same(ledger.Entries, ledger.Post(invoice, Limits).Ledger.Entries);
    }

    [Fact]
    public void Stored_album_entries_stay_at_one()
    {
        var ledger = Ledger.FromStored(LedgerMode.Album, 0, [E("ambush1", 1), E("ambush1", 1)]);
        Assert.Equal(1, ledger.Entries[K("ambush1")].Count);
    }

    [Theory]
    [InlineData("aid-*", "aid-arm1", true)]
    [InlineData("aid-*", "said-arm1", false)]
    [InlineData("glider*", "glider2", true)]
    [InlineData("symptoms11", "symptoms11", true)]
    [InlineData("symptoms11", "symptoms12", false)]
    public void Exclusion_patterns_use_a_star(string pattern, string name, bool matches)
    {
        Assert.Equal(matches, Collection.Matcher([pattern])(name));
    }

    [Fact]
    public void Any_of_several_patterns_matches_and_none_matches_nothing()
    {
        var excluded = Collection.Matcher(["aid-*", "GLIDER*"]);

        Assert.True(excluded("aid-leg2"));
        Assert.True(excluded("glider1")); // case ignored
        Assert.False(excluded("ambush1"));
        Assert.False(Collection.Matcher([])("ambush1"));
    }

    [Fact]
    public void Tapestry_pieces_know_their_picture()
    {
        var pieces = TapestryPieces.WithPictures(new Dictionary<string, string> { ["ambush"] = "3x1", ["notice-blackguard"] = "1x1" });
        Assert.Equal(
            [new TapestryPiece("ambush1", "ambush"), new("ambush2", "ambush"), new("ambush3", "ambush"), new("notice-blackguard", "notice-blackguard")],
            pieces);
    }
}
