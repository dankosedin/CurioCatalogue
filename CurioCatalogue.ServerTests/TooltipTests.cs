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

    void Rename(ItemStack book, string name) =>
        Mod.Store.WriteLedger(book, Mod.Store.ReadLedger(book, ((ItemLedger)book.Collectible).LedgerMode) with { Name = name });

    [Fact]
    public void A_ledger_says_its_cover_sheets_entries_and_gestures()
    {
        var (name, info, max, limit) = game.OnMain(() =>
        {
            var book = Book("book-ledger-normal-teal", 5, Vanilla("ingot-copper"), Vanilla("ingot-tin"));
            return (book.GetName(), Info(book), Mod.Config.MaxSheets, Mod.Config.SoftEntryLimit);
        });

        Assert.Equal("Ledger", name);
        Assert.Contains("Cover: teal", info);
        Assert.Contains($"Sheets: 5 of {max}", info);
        Assert.Contains($"Entries: 2 (limit {limit})", info);
        Assert.Contains("Shift + right-click a container: intake list for items not in the ledger.", info);
        Assert.Contains("Shift + Ctrl + right-click: pick list for items in the ledger, every stack of them.", info);
        Assert.Contains("Right-click into the air opens it, in your hand or in your off-hand when the main hand is empty.", info);
        Assert.DoesNotContain("Ledger", info); // the kind line is only for a named book
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
        Assert.Contains("Bill of materials", info);
        Assert.Contains("Cover: dark green, aged", info);
        Assert.Contains("Shift + right-click a container: intake list for what is over the listed counts.", info);
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

        Assert.Equal("Album: tapestries", name);
        Assert.Equal("Grandma's walls", named);
        Assert.Contains("Album", info); // the kind line: an album always has one, its title is the collection or a name
        Assert.Contains($"Pieces 0 of {progress.Total} · whole pictures 0 of {progress.GroupsTotal}", info);
        Assert.Contains(info, l => l.StartsWith("Shift + right-click a container: takes one of each missing item right away"));
        Assert.DoesNotContain(info, l => l.StartsWith("Entries:")); // its entries are what it misses, the limit does not matter
    }

    [Fact]
    public void A_blank_list_says_it_is_blank()
    {
        var info = game.OnMain(() => Info(new ItemStack(World.GetItem(Adapters.InvoiceMaker.InvoiceCode(InvoiceKind.Incoming)))));

        Assert.Contains("Blank", info);
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
                $"Made on day 3 at {10 - spawn.X}, 4, {-20 - spawn.Z}",
                "1 × Unknown item #987654",
                .. stacks.Take(4).Select(s => $"2 × {s.GetName()}"),
                "…and 2 more",
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
