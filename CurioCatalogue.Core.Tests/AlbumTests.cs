using CurioCatalogue.Core;

namespace CurioCatalogue.Core.Tests;

/// <summary>
/// The album of format 2 (docs/decisions.md, Collections): what it collected, by member; what is missing follows the
/// collection; the level; taking new by members; the migration of format 1. On made-up collections: tapestry pieces (one level)
/// and seashells (a level by type, any colour, and one per shell).
/// </summary>
public class AlbumTests
{
    private static readonly LedgerLimits Limits = new(32, 500);
    private static readonly BlockPosition Pos = new(0, 0, 0);

    private static Dictionary<string, string> V(params (string, string)[] values) => values.ToDictionary(v => v.Item1, v => v.Item2);

    private static DefinedCollection Built(CollectionDefinition definition, IEnumerable<SourcePoint> points) =>
        DefinedCollection.Build(definition, points) switch
        {
            CollectionBuild.Built b => b.Collection,
            CollectionBuild.Invalid i => throw new Xunit.Sdk.XunitException("Invalid: " + string.Join("; ", i.Errors)),
            _ => throw new InvalidOperationException(),
        };

    // --- Tapestries: ambush is 3 pieces, notice 1; the picture groups (the default layout), so notice comes after ambush ---

    private static ItemKey K(string piece) => new(CollectibleKind.Block, "game:tapestry-north", $"{{\"type\":\"{piece}\"}}");

    private static CollectionDefinition TapestryDefinition(params string[] exclude) => new(
        "tapestries",
        [new DimensionDef("picture", []), new DimensionDef("piece", [])],
        [new LevelDef("pieces", ["piece"], new Dictionary<string, Drop> { ["picture"] = Drop.Any },
            [new LayoutDef("pictures", ["picture"], new Arrangement.Shape())])],
        [],
        exclude);

    private static SourcePoint Piece(string piece, string picture, int x) =>
        new(piece, V(("picture", picture), ("piece", piece)), K(piece), "sample " + piece, V(("x", x.ToString()), ("y", "1")));

    private static IEnumerable<SourcePoint> PiecesOf(params string[] pieces) => pieces.Select(p =>
        p.StartsWith("ambush") ? Piece(p, "ambush", p[^1] - '0') : Piece(p, p.TrimEnd('1', '2', '3'), 1));

    private static DefinedCollection Tapestries(params string[] pieces) =>
        Built(TapestryDefinition(), PiecesOf(pieces.Length > 0 ? pieces : ["ambush1", "ambush2", "ambush3", "notice"]));

    private static FilledBy By(string piece) => new(K(piece), "stack " + piece);

    private static Album Collected(Album album, params string[] pieces) => album.Collect(pieces.Select(p => (p, By(p))));

    private static string[] Ids(IEnumerable<Member> members) => members.Select(m => m.Id).ToArray();

    [Fact]
    public void A_new_album_has_collected_nothing_and_misses_every_member_in_the_order_of_taking_new()
    {
        var tapestries = Tapestries();
        var album = Album.New(tapestries, sheets: 5, name: "Mine");
        var level = album.LevelIn(tapestries)!;

        Assert.True(album.IsEmpty);
        Assert.Equal(("tapestries", "pieces", 5, "Mine"), (album.CollectionId, album.Level, album.Sheets, album.Name));
        Assert.Equal(["ambush1", "ambush2", "ambush3", "notice"], Ids(album.Missing(level)));
        Assert.Equal(new LevelProgress(0, 4, 0, 2), album.Progress(level));
    }

    [Fact]
    public void Taking_new_wants_the_missing_members_the_container_has_in_the_collections_order()
    {
        var tapestries = Tapestries();
        var album = Collected(Album.New(tapestries, 3, ""), "ambush2");
        var level = album.LevelIn(tapestries)!;

        var wanted = album.Wanted(level, new HashSet<ItemKey> { K("notice"), K("ambush2"), K("ambush1"), K("other") });

        Assert.Equal([("ambush1", K("ambush1")), ("notice", K("notice"))], wanted.Select(w => (w.Member.Id, w.Key)));
    }

    [Fact]
    public void Collected_members_keep_the_item_that_filled_them_and_follow_the_collection()
    {
        var album = Collected(Album.New(Tapestries(), 3, "Mine"), "ambush2", "notice");

        Assert.Equal(["ambush2", "notice"], album.Collected.Keys);
        Assert.Equal(By("notice"), album.Collected["notice"]);
        Assert.Equal((3, "Mine"), (album.Sheets, album.Name)); // taking new costs no sheet

        // A mod adds a picture, the config excludes notice: the album needs no refresh. Notice keeps its record and comes back.
        var grown = Tapestries("ambush1", "ambush2", "ambush3", "wolf1");
        var level = album.LevelIn(grown)!;
        Assert.Equal(["ambush1", "ambush3", "wolf1"], Ids(album.Missing(level)));
        Assert.Equal(new LevelProgress(1, 4, 0, 2), album.Progress(level));
        Assert.Equal(new LevelProgress(2, 4, 1, 2), album.Progress(Tapestries().Levels[0]));
    }

    [Fact]
    public void A_member_collected_again_keeps_its_first_record()
    {
        var album = Collected(Album.New(Tapestries(), 3, ""), "notice");
        var again = album.Collect([("notice", new FilledBy(K("notice"), "another stack"))]);
        Assert.Equal(By("notice"), again.Collected["notice"]);
    }

    [Fact]
    public void Starting_over_forgets_what_was_collected_and_keeps_sheets_and_name()
    {
        var tapestries = Tapestries();
        var album = Collected(Album.New(tapestries, 3, "Mine"), "notice") with { Sheets = 7 };

        var again = album.StartOver(tapestries);

        Assert.True(again.IsEmpty);
        Assert.Equal(("pieces", 7, "Mine"), (again.Level, again.Sheets, again.Name));
    }

    [Fact]
    public void Want_list_lists_the_missing_in_the_collections_order_and_costs_a_sheet()
    {
        var tapestries = Tapestries();
        var album = Collected(Album.New(tapestries, 3, ""), "ambush2");
        var level = album.LevelIn(tapestries)!;

        var made = Assert.IsType<CreateInvoiceResult.Created>(album.MakeWantList(level, 5, Pos));

        Assert.Equal(InvoiceKind.WantList, made.Invoice.Kind);
        Assert.Equal([K("ambush1"), K("ambush3"), K("notice")], made.Invoice.Entries.Select(e => e.Key));
        Assert.Equal(["sample ambush1", "sample ambush3", "sample notice"], made.Invoice.Entries.Select(e => (string)e.Sample));
        Assert.All(made.Invoice.Entries, e => Assert.Equal(1, e.Count));
        var after = Assert.IsType<Album>(made.Book);
        Assert.Equal((2, album.Collected), (after.Sheets, after.Collected));
    }

    [Fact]
    public void No_want_list_when_nothing_is_missing_or_no_sheets()
    {
        var tapestries = Tapestries();
        var level = tapestries.Levels[0];
        var done = Collected(Album.New(tapestries, 3, ""), "ambush1", "ambush2", "ambush3", "notice");

        Assert.Equal(InvoiceRefusal.NoDifference, Assert.IsType<CreateInvoiceResult.Refused>(done.MakeWantList(level, 0, Pos)).Reason);
        Assert.Equal(InvoiceRefusal.NoSheets,
            Assert.IsType<CreateInvoiceResult.Refused>(Album.New(tapestries, 0, "").MakeWantList(level, 0, Pos)).Reason);
    }

    [Fact]
    public void An_album_takes_a_sheet_back_and_recharges_like_any_book()
    {
        var album = Collected(Album.New(Tapestries(), 31, ""), "notice");

        var back = Assert.IsType<Album>(album.ReturnSheet(Limits));
        var (charged, used) = album.AddSheets(5, Limits);

        Assert.Equal((32, album.Collected), (back.Sheets, back.Collected));
        Assert.Null(back.ReturnSheet(Limits));
        Assert.Equal((32, 1), (charged.Sheets, used));
        Assert.IsType<Album>(charged);
    }

    // --- Levels: seashells by type (any colour) or by shell ---

    private static readonly string[] Types = ["scallop", "clam"];
    private static readonly string[] Colours = ["latte", "plain", "seafoam"];

    private static ItemKey Shell(string type, string colour) => new(CollectibleKind.Block, $"game:seashell-{type}-{colour}", "");

    private static readonly DefinedCollection Seashells = Built(
        new CollectionDefinition("seashells",
            [new DimensionDef("type", Types), new DimensionDef("color", Colours)],
            [
                new LevelDef("type", ["type"], new Dictionary<string, Drop> { ["color"] = Drop.Any }, []),
                new LevelDef("all", ["type", "color"], new Dictionary<string, Drop>(), []),
            ],
            [], []),
        Types.SelectMany(t => Colours.Select(c =>
            new SourcePoint($"seashell-{t}-{c}", V(("type", t), ("color", c)), Shell(t, c), $"{t}-{c}"))));

    [Fact]
    public void A_collection_of_several_levels_has_none_chosen_in_a_new_album()
    {
        var album = Album.New(Seashells, 3, "");
        Assert.Null(album.Level);
        Assert.Null(album.LevelIn(Seashells));
    }

    [Fact]
    public void The_level_is_chosen_while_empty_fixed_by_the_first_piece_and_freed_by_starting_over()
    {
        var album = Assert.IsType<LevelChoice.Chosen>(Album.New(Seashells, 3, "").ChooseLevel(Seashells, "all")).Album;
        Assert.Equal("all", album.LevelIn(Seashells)!.Id);
        // Changing one's mind while empty is fine.
        Assert.Equal("type", Assert.IsType<LevelChoice.Chosen>(album.ChooseLevel(Seashells, "type")).Album.Level);

        var filled = album.Collect([("clam-plain", new FilledBy(Shell("clam", "plain"), "stack"))]);
        Assert.Equal(LevelRefusal.NotEmpty, Assert.IsType<LevelChoice.Refused>(filled.ChooseLevel(Seashells, "type")).Reason);
        Assert.Same(filled, Assert.IsType<LevelChoice.Chosen>(filled.ChooseLevel(Seashells, "all")).Album);
        Assert.Equal(LevelRefusal.UnknownLevel, Assert.IsType<LevelChoice.Refused>(album.ChooseLevel(Seashells, "colour")).Reason);

        Assert.Null(filled.StartOver(Seashells).Level);
    }

    [Fact]
    public void A_member_of_several_items_is_filled_by_any_of_them_and_remembers_which()
    {
        var album = Assert.IsType<LevelChoice.Chosen>(Album.New(Seashells, 3, "").ChooseLevel(Seashells, "type")).Album;
        var level = album.LevelIn(Seashells)!;

        // The container has a seafoam and a plain clam, and no scallop: the clam is wanted once, by the first in the member's order.
        var wanted = album.Wanted(level, new HashSet<ItemKey> { Shell("clam", "seafoam"), Shell("clam", "plain") });
        Assert.Equal([("clam", Shell("clam", "plain"))], wanted.Select(w => (w.Member.Id, w.Key)));

        var after = album.Collect([("clam", new FilledBy(Shell("clam", "seafoam"), "the seafoam one"))]);
        Assert.Equal(["scallop"], Ids(after.Missing(level)));
        Assert.Equal(Shell("clam", "seafoam"), after.Collected["clam"].Key);
    }

    // --- Format 1 (0.1): a snapshot of the members and the missing entries, migrated on read ---

    [Fact]
    public void Taking_new_follows_the_default_layout_not_the_order_of_the_members()
    {
        // QA K7 (Q4). The piece dimension has no list of values, so the members keep the source's order; the layout's shape puts
        // them by place.
        var tapestries = Built(TapestryDefinition(), PiecesOf("ambush3", "ambush1", "ambush2"));
        var level = tapestries.Levels[0];
        Assert.Equal(["ambush3", "ambush1", "ambush2"], Ids(level.Members)); // the premise: the two orders differ
        var album = Album.New(tapestries, 3, "");

        Assert.Equal(["ambush1", "ambush2", "ambush3"], Ids(album.Missing(level)));
        Assert.Equal(["ambush1", "ambush2", "ambush3"],
            album.Wanted(level, new HashSet<ItemKey> { K("ambush3"), K("ambush2"), K("ambush1") }).Select(w => w.Member.Id));
    }

    private static List<FilledBy> Snapshot(params string[] pieces) => pieces.Select(By).ToList();

    /// <summary>The tapestries with a level of whole pictures patched in before the pieces (M1, M3 of K7).</summary>
    private static DefinedCollection PicturesFirst(string piecesId = "pieces")
    {
        var basic = TapestryDefinition();
        var pictures = new LevelDef("pictures", ["picture"], new Dictionary<string, Drop> { ["piece"] = Drop.Any }, []);
        var pieces = basic.Levels[0] with { Id = piecesId };
        return Built(basic with { Levels = [pictures, pieces] }, PiecesOf("ambush1", "ambush2", "ambush3", "notice"));
    }

    [Fact]
    public void Format_1_is_placed_on_the_level_of_pieces_even_when_a_patch_puts_another_level_first()
    {
        // QA K7 (M1). Format 1 knew the pieces only; placed on "pictures", the three ambush pieces would fall on one member and two
        // of them would be lost at the next write.
        var album = Album.FromFormat1("tapestries", 4, "", Snapshot("ambush1", "ambush2", "ambush3", "notice"), [K("notice")],
            PicturesFirst());

        Assert.Equal("pieces", album.Level);
        Assert.Equal(["ambush1", "ambush2", "ambush3"], album.Collected.Keys);
        Assert.Empty(album.Unplaced);
    }

    [Fact]
    public void An_album_whose_level_was_renamed_goes_to_the_level_that_takes_most_of_what_it_collected()
    {
        // M3 of K7: a patch renames "pieces"; the filled album would be left with no level, its pieces shown nowhere.
        var album = Collected(Album.New(Tapestries(), 4, ""), "ambush1", "ambush3", "notice");

        var placed = album.Place(PicturesFirst(piecesId: "bypiece"));

        Assert.Equal("bypiece", placed.Level);
        Assert.Equal(["ambush1", "ambush3", "notice"], placed.Collected.Keys);
        Assert.Equal(By("ambush3"), placed.Collected["ambush3"]);
        Assert.Empty(placed.Unplaced);
    }

    [Fact]
    public void An_album_whose_level_is_gone_keeps_what_its_new_level_lacks_unplaced()
    {
        var withoutNotice = Built(TapestryDefinition("notice") with { Levels = [TapestryDefinition().Levels[0] with { Id = "bypiece" }] },
            PiecesOf("ambush1", "ambush2", "notice"));
        var album = Collected(Album.New(Tapestries(), 4, ""), "ambush1", "notice");

        var placed = album.Place(withoutNotice);

        Assert.Equal("bypiece", placed.Level);
        Assert.Equal(["ambush1"], placed.Collected.Keys);
        Assert.Equal([By("notice")], placed.Unplaced);
    }

    [Fact]
    public void Two_levels_that_take_as_much_go_to_the_first_and_none_leaves_the_album_as_it_is()
    {
        // One scallop: one member on either level of the seashells.
        var shell = new FilledBy(Shell("scallop", "latte"), "stack");
        var album = new Album("seashells", "colour", Album.NoneCollected.Add("scallop-latte", shell), [], 3);

        Assert.Equal("type", album.Place(Seashells).Level);
        Assert.Equal(["scallop"], album.Place(Seashells).Collected.Keys);

        var stranger = album with { Collected = Album.NoneCollected.Add("wolf1", By("wolf1")) };
        Assert.Same(stranger, stranger.Place(Seashells));
    }

    [Fact]
    public void Format_1_becomes_the_members_collected_on_the_first_level()
    {
        var tapestries = Tapestries();

        var album = Album.FromFormat1("tapestries", 4, "Mine", Snapshot("ambush1", "ambush2", "ambush3", "notice"),
            [K("ambush1"), K("ambush3")], tapestries);

        Assert.Equal(("pieces", 4, "Mine"), (album.Level, album.Sheets, album.Name));
        Assert.Equal(["ambush2", "notice"], album.Collected.Keys);
        Assert.Equal(By("notice"), album.Collected["notice"]); // the snapshot's sample is the stack it keeps
        Assert.Empty(album.Unplaced);
    }

    [Fact]
    public void Format_1_with_nothing_collected_is_an_empty_album_with_its_level()
    {
        var album = Album.FromFormat1("tapestries", 4, "", Snapshot("ambush1", "notice"), [K("ambush1"), K("notice")], Tapestries());
        Assert.True(album.IsEmpty);
        Assert.Equal("pieces", album.Level);
    }

    [Fact]
    public void Format_1_keeps_a_collected_piece_the_collection_no_longer_has_and_places_it_when_it_is_back()
    {
        var withoutNotice = Built(TapestryDefinition("notice"), PiecesOf("ambush1", "ambush2", "ambush3", "notice"));

        var album = Album.FromFormat1("tapestries", 4, "", Snapshot("ambush1", "ambush2", "ambush3", "notice"),
            [K("ambush1"), K("ambush3")], withoutNotice);

        Assert.Equal(["ambush2"], album.Collected.Keys);
        Assert.Equal([By("notice")], album.Unplaced);
        Assert.False(album.IsEmpty);
        Assert.Equal(["ambush2", "notice"], album.Place(Tapestries()).Collected.Keys);
        Assert.Empty(album.Place(Tapestries()).Unplaced);
    }

    [Fact]
    public void Format_1_of_an_unknown_collection_keeps_everything_it_collected_until_the_collection_is_known()
    {
        var album = Album.FromFormat1("tapestries", 4, "", Snapshot("ambush1", "ambush2", "notice"), [K("ambush1")], null);

        Assert.Null(album.Level);
        Assert.Empty(album.Collected);
        Assert.Equal([By("ambush2"), By("notice")], album.Unplaced);

        var placed = album.Place(Tapestries());
        Assert.Equal("pieces", placed.Level);
        Assert.Equal(["ambush2", "notice"], placed.Collected.Keys);
    }

    [Fact]
    public void Format_1_without_a_snapshot_counts_as_made_from_the_collection_now()
    {
        // Albums of the development builds before 0.1.0 kept the snapshot as key strings, read as none (0.1 did the same).
        var album = Album.FromFormat1("tapestries", 1, "", null, [K("ambush2")], Tapestries());
        Assert.Equal(["ambush1", "ambush3", "notice"], album.Collected.Keys);
    }

    [Fact]
    public void Placing_a_piece_whose_member_is_collected_adds_nothing_and_loses_nothing_else()
    {
        var album = Collected(Album.New(Tapestries(), 1, ""), "notice") with { Unplaced = [new FilledBy(K("notice"), "other"), By("wolf1")] };

        var placed = album.Place(Tapestries());

        Assert.Equal(By("notice"), placed.Collected["notice"]);
        Assert.Equal([By("wolf1")], placed.Unplaced);
    }

    // --- Want lists of any book ---

    [Fact]
    public void A_container_gives_what_it_has_of_a_want_list_of_format_1_exactly()
    {
        // Format 1 (0.1): lines with no member, each its own item, whatever the collection says.
        var invoice = new Invoice(InvoiceKind.WantList, [new(K("ambush1"), 1, ""), new(K("ambush2"), 1, ""), new(K("notice"), 1, "")], 0, Pos);

        var wanted = invoice.Wanted(Tapestries().Levels[0], new HashSet<ItemKey> { K("ambush2"), K("notice"), K("other") });

        Assert.Equal([new WantedLine(1, K("ambush2")), new WantedLine(2, K("notice"))], wanted);
    }

    [Fact]
    public void Delivered_lines_are_crossed_off_a_want_list_by_their_place()
    {
        var invoice = new Invoice(InvoiceKind.WantList, [new(K("ambush1"), 1, ""), new(K("ambush2"), 1, ""), new(K("notice"), 1, "")], 0, Pos);

        var left = invoice.CrossOff([1]);

        Assert.Equal([K("ambush1"), K("notice")], left.Entries.Select(e => e.Key));
        Assert.Equal(InvoiceKind.WantList, left.Kind);
    }

    // --- Want lists of format 2: a line names a member (K5 of the collections core plan) ---

    private static Album OfLevel(string level) =>
        Assert.IsType<LevelChoice.Chosen>(Album.New(Seashells, 3, "").ChooseLevel(Seashells, level)).Album;

    [Fact]
    public void A_want_list_names_its_album_level_and_members()
    {
        var album = OfLevel("type");
        var level = album.LevelIn(Seashells)!;

        var list = Assert.IsType<CreateInvoiceResult.Created>(album.MakeWantList(level, 5, Pos)).Invoice;

        Assert.Equal(new WantedFrom("seashells", "type"), list.From);
        Assert.Equal([("scallop", Shell("scallop", "latte"), "scallop-latte"), ("clam", Shell("clam", "latte"), "clam-latte")],
            list.Entries.Select(e => (e.Member, e.Key, (string)e.Sample)));
    }

    [Fact]
    public void A_line_naming_a_member_of_several_items_is_given_by_any_of_them()
    {
        var album = OfLevel("type");
        var level = album.LevelIn(Seashells)!;
        var list = Assert.IsType<CreateInvoiceResult.Created>(album.MakeWantList(level, 5, Pos)).Invoice;

        // No latte shell anywhere: the clam is given by a seafoam or a plain one, the first in the member's order.
        var wanted = list.Wanted(level, new HashSet<ItemKey> { Shell("clam", "seafoam"), Shell("clam", "plain") });

        Assert.Equal([new WantedLine(1, Shell("clam", "plain"))], wanted);
        Assert.Equal(["scallop"], list.CrossOff(wanted.Select(w => w.Line)).Entries.Select(e => e.Member));
    }

    [Fact]
    public void A_line_whose_member_is_gone_or_whose_collection_is_unknown_wants_exactly_its_item()
    {
        var list = new Invoice(InvoiceKind.WantList,
            [new(Shell("clam", "latte"), 1, "", Member: "clam"), new(Shell("scallop", "latte"), 1, "", Member: "no-such-member")], 0, Pos,
            new WantedFrom("seashells", "type"));
        var chest = new HashSet<ItemKey> { Shell("clam", "plain"), Shell("scallop", "plain"), Shell("scallop", "latte") };

        // The collection unknown: both lines exact, so only the latte scallop.
        Assert.Equal([new WantedLine(1, Shell("scallop", "latte"))], list.Wanted(null, chest));
        // Known: the clam by any colour; the gone member still by its own item.
        Assert.Equal([new WantedLine(0, Shell("clam", "plain")), new WantedLine(1, Shell("scallop", "latte"))],
            list.Wanted(Seashells.Level("type"), chest));
    }

    [Fact]
    public void No_item_is_given_to_two_lines()
    {
        // A list a player could not make, but data may hold: two lines for one item, and two members one item could fill.
        var list = new Invoice(InvoiceKind.WantList,
            [new(Shell("clam", "latte"), 1, ""), new(Shell("clam", "latte"), 1, "", Member: "clam")], 0, Pos, new WantedFrom("seashells", "type"));

        Assert.Equal([new WantedLine(0, Shell("clam", "latte"))], list.Wanted(Seashells.Level("type"), new HashSet<ItemKey> { Shell("clam", "latte") }));
    }

    [Fact]
    public void A_want_list_finds_the_only_level_of_its_collection_under_another_id()
    {
        // As an album does: a patch renamed the level of a collection of one level.
        Assert.Equal("pieces", new WantedFrom("tapestries", "old-name").LevelIn(Tapestries())!.Id);
        Assert.Null(new WantedFrom("seashells", "old-name").LevelIn(Seashells));
    }

    [Fact]
    public void A_member_of_several_items_leaves_free_the_dimensions_its_level_drops_as_any()
    {
        var byType = Seashells.Level("type")!;
        var all = Seashells.Level("all")!;
        var tapestries = Tapestries();
        var pieces = tapestries.Levels[0];

        Assert.Equal(["color"], Seashells.Free(byType, byType.Find("clam")!).Select(d => d.Code));
        Assert.Empty(Seashells.Free(all, all.Find("clam-plain")!)); // one item
        Assert.Empty(tapestries.Free(pieces, pieces.Find("ambush1")!)); // the picture is dropped as any, but a piece has its own
    }

    [Fact]
    public void Posting_a_want_list_into_a_ledger_changes_nothing()
    {
        var ledger = Ledger.FromStored(LedgerMode.Counts, 3, [new(K("ambush1"), 2, "")]);
        var invoice = new Invoice(InvoiceKind.WantList, [new(K("ambush1"), 1, "")], 0, Pos);
        Assert.Same(ledger.Entries, ledger.Post(invoice, Limits).Ledger.Entries);
    }

    // --- Names in exclusions ---

    [Theory]
    [InlineData("aid-*", "aid-arm1", true)]
    [InlineData("aid-*", "said-arm1", false)]
    [InlineData("glider*", "glider2", true)]
    [InlineData("symptoms11", "symptoms11", true)]
    [InlineData("symptoms11", "symptoms12", false)]
    public void Exclusion_patterns_use_a_star(string pattern, string name, bool matches)
    {
        Assert.Equal(matches, NamePattern.Matcher([pattern])(name));
    }

    // --- What the window shows when the game does not have the album's collection (album view, A3c) ---

    [Fact]
    public void Collected_only_is_one_group_of_what_was_collected_each_shown_by_the_item_put_in()
    {
        var album = Collected(Album.New(Tapestries(), 3, "Mine"), "notice", "ambush2");

        var shown = album.CollectedOnly()!;
        var level = album.LevelIn(shown)!;

        Assert.Equal(Album.CollectedOnlyLevel, level.Id);
        var group = Assert.Single(level.Groups(level.DefaultLayout));
        Assert.Equal(["ambush2", "notice"], Ids(group.Members));
        Assert.Equal(["stack ambush2", "stack notice"], group.Members.Select(m => album.StacksToShow(m).Single()));
        Assert.Equal(2, album.Progress(level).Collected); // every piece is shown collected
        Assert.Null(Album.New(Tapestries(), 3, "Mine").CollectedOnly());
    }

    [Fact]
    public void Collected_only_is_nothing_when_one_item_filled_two_members()
    {
        var album = Album.New(Tapestries(), 3, "Mine").Collect([("notice", By("notice")), ("ambush1", By("notice"))]);

        Assert.Null(album.CollectedOnly());
    }

    [Fact]
    public void Any_of_several_patterns_matches_and_none_matches_nothing()
    {
        var excluded = NamePattern.Matcher(["aid-*", "GLIDER*"]);

        Assert.True(excluded("aid-leg2"));
        Assert.True(excluded("glider1")); // case ignored
        Assert.False(excluded("ambush1"));
        Assert.False(NamePattern.Matcher([])("ambush1"));
    }
}
