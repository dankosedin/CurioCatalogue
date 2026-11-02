using System.Reflection;
using System.Text;
using CurioCatalogue.Core;
using Vintagestory.API.Common;

namespace CurioCatalogue.ServerTests;

/// <summary>
/// What the player reads about a book or a list: its name (<c>GetHeldItemName</c>), its tooltip (<c>GetHeldItemInfo</c>), and the
/// "Missing: …" text of a refused post (<c>ModNetwork.MissingText</c>). The client calls these; they need no client, so the server runs
/// them here, in its language (English). The key names fall back to "Shift" and "Ctrl" without a client (ItemLedger.KeyName).
/// The drawing of the tooltip and the player's language stay a game check (T1 of <c>functional.md</c>).
/// </summary>
[Collection(ServerCollection.Name)]
public class TooltipTests(GameServerFixture server) : BookTestBase(server)
{
    /// <summary>The tooltip's lines. Main thread.</summary>
    string[] Info(ItemStack stack)
    {
        var text = new StringBuilder();
        stack.Collectible.GetHeldItemInfo(new DummySlot(stack), text, World, false);
        return text.ToString().Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    // The expected texts come from en.json (the server's language); the key names without a client are "Shift" and "Ctrl".
    static string En(string key, params object[] args) => LangText("en", key, args);
    static string[] EnLines(string key) => En(key, "Shift", "Ctrl").Split('\n');

    void Rename(ItemStack book, string name) =>
        Mod.Store.WriteLedger(book, ReadBook(book) with { Name = name });

    [Fact]
    public void A_ledger_says_its_cover_sheets_entries_and_gestures()
    {
        var (name, info, max, limit) = game.OnMain(() =>
        {
            var book = Book("book-ledger-normal-teal", 5, Vanilla("ingot-copper"), Vanilla("ingot-tin"));
            return (book.GetName(), Info(book), Mod.Config.MaxSheets, Mod.Config.SoftEntryLimit);
        });

        Assert.Equal(En("item-book-ledger-*"), name);
        Assert.Contains(En("ledger-info-cover", En("cover-teal")), info);
        Assert.Contains(En("ledger-info-sheets", 5, max), info);
        Assert.Contains(En("ledger-info-entries", 2, limit), info);
        Assert.All(EnLines("ledger-info-howto-types"), line => Assert.Contains(line, info));
        Assert.Contains(En("ledger-info-open"), info);
        Assert.DoesNotContain(En("item-book-ledger-*"), info); // the kind line is only for a named book
        Assert.DoesNotContain(info, l => l.StartsWith("Pieces"));
    }

    [Fact]
    public void A_named_book_shows_its_name_and_says_its_kind_in_the_tooltip()
    {
        var (name, info) = game.OnMain(() =>
        {
            var book = Book("book-materials-aged-darkgreen", 3);
            Rename(book, "Smithy order");
            return (book.GetName(), Info(book));
        });

        Assert.Equal("Smithy order", name);
        Assert.Contains(En("item-book-materials-*"), info);
        Assert.Contains(En("ledger-info-cover-aged", En("cover-darkgreen")), info);
        Assert.All(EnLines("ledger-info-howto-counts"), line => Assert.Contains(line, info));
    }

    [Fact]
    public void An_album_is_named_after_its_collection_and_shows_its_progress()
    {
        var (name, named, info, progress) = game.OnMain(() =>
        {
            var album = Album(8);
            var collection = Mod.Collections.ForSample(Tapestry("ambush1"))!;
            var progress = collection.Progress(collection.Members.Select(m => m.Key));
            var renamed = Album(8);
            Rename(renamed, "Grandma's walls");
            return (album.GetName(), renamed.GetName(), Info(album), progress);
        });

        Assert.Equal(En("album-name-tapestries"), name);
        Assert.Equal("Grandma's walls", named);
        Assert.Contains(En("item-book-album-*"), info); // the kind line: an album always has one, its title is the collection or a name
        Assert.Contains(En("ledger-info-album", 0, progress.Total, 0, progress.GroupsTotal), info);
        Assert.All(EnLines("ledger-info-howto-album"), line => Assert.Contains(line, info));
        Assert.DoesNotContain(info, l => l.StartsWith("Entries:")); // its entries are what it misses, the limit does not matter
    }

    [Fact]
    public void A_blank_list_says_it_is_blank()
    {
        var info = game.OnMain(() => Info(new ItemStack(World.GetItem(Adapters.InvoiceMaker.InvoiceCode(InvoiceKind.Incoming)))));

        Assert.Contains(En("invoice-info-blank"), info);
        Assert.DoesNotContain(info, l => l.StartsWith("Made on day"));
    }

    [Fact]
    public void A_list_shows_where_it_was_made_its_first_five_lines_and_how_many_more()
    {
        var (info, expected) = game.OnMain(() =>
        {
            // An item of a removed mod: a stack whose id the game no longer knows.
            var ghost = new ItemStack(Vanilla("ingot-copper").ToBytes()) { Id = 987654 };
            string[] codes = ["ingot-copper", "ingot-tin", "ingot-iron", "ingot-lead", "ingot-zinc", "ingot-gold"];
            var stacks = codes.Select(c => Vanilla(c, 2)).ToArray();
            var list = new ItemStack(World.GetItem(Adapters.InvoiceMaker.InvoiceCode(InvoiceKind.Outgoing)));
            Entry[] entries = [Mod.Store.ToEntry(ghost), .. stacks.Select(Mod.Store.ToEntry)];
            Mod.Store.WriteInvoice(list, new Invoice(InvoiceKind.Outgoing, [.. entries], 3.7, new BlockPosition(10, 4, -20)));
            var spawn = World.DefaultSpawnPosition.AsBlockPos;
            string[] expected =
            [
                En("invoice-info-origin", 3, 10 - spawn.X, 4, -20 - spawn.Z),
                $"1 × {En("unknown-item", 987654)}",
                .. stacks.Take(4).Select(s => $"2 × {s.GetName()}"),
                En("invoice-info-more", 2),
            ];
            return (Info(list), expected);
        });

        Assert.Equal(expected, info.SkipWhile(l => !l.StartsWith("Made on day")).Take(expected.Length));
    }

    [Fact]
    public void A_refused_post_names_three_missing_items_and_counts_the_rest()
    {
        var missingText = typeof(CurioCatalogueModSystem).Assembly.GetType("CurioCatalogue.ModNetwork")!
            .GetMethod("MissingText", BindingFlags.Static | BindingFlags.NonPublic)!;
        var (text, names) = game.OnMain(() =>
        {
            var stacks = new[] { "ingot-copper", "ingot-tin", "ingot-iron" }.Select(c => Vanilla(c)).ToArray();
            var refusal = new MissingItemsRefusal { More = 2 };
            foreach (var (stack, i) in stacks.Select((s, i) => (s, i)))
            {
                refusal.Stacks.Add(stack.ToBytes());
                refusal.Counts.Add(i + 1);
            }
            return ((string)missingText.Invoke(null, [World, refusal])!, stacks.Select(s => s.GetName()).ToArray());
        });

        Assert.Equal($"Missing: 1 × {names[0]}, 2 × {names[1]}, 3 × {names[2]} …and 2 more", text);
    }
}
