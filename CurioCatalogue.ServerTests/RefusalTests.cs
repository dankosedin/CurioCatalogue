using CurioCatalogue.Adapters;
using CurioCatalogue.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace CurioCatalogue.ServerTests;

/// <summary>
/// Refusals the game cannot show by playing (<c>functional.md</c>, "Not reproducible in game"), made on the server with stacks whose
/// attributes the test writes: <c>newerversion</c> (data from a newer build of the mod: every gesture and every window request is
/// refused and the stack is left untouched) and <c>nocollection</c> ("Start over" for an album whose collection the server does not know).
/// <c>movefailed</c> stays a unit test: no container of the server accepts only part of what is put in.
/// </summary>
[Collection(ServerCollection.Name)]
public class RefusalTests(GameServerFixture server) : BookTestBase(server)
{
    /// <summary>Marks the stack's data as written by a newer build (<see cref="LedgerStore.IsNewer"/>).</summary>
    static ItemStack Newer(ItemStack stack)
    {
        stack.Attributes.GetTreeAttribute(LedgerStore.RootKey).SetInt("v", LedgerStore.Version + 1);
        return stack;
    }

    /// <summary>Everything the stack's attributes hold, to see that a refusal left them as they were.</summary>
    static string Bytes(ItemStack stack) => stack.Attributes.ToJsonToken();

    // ---- newerversion

    [Fact]
    public void The_data_of_a_newer_build_is_recognised_by_the_test_helper()
    {
        // Guards the helper: without it every test below would pass on an ordinary book, only because the click did nothing.
        game.OnMain(() =>
        {
            Assert.False(LedgerStore.IsNewer(Book("book-ledger-normal-brickred", 8)));
            Assert.True(LedgerStore.IsNewer(Newer(Book("book-ledger-normal-brickred", 8))));
            return 0;
        });
    }

    [Theory]
    [InlineData("book-ledger-normal-brickred", false)]
    [InlineData("book-ledger-normal-brickred", true)]
    [InlineData("book-materials-normal-teal", false)]
    [InlineData("album", false)]
    public void A_newer_book_in_hand_is_refused_on_a_chest_and_left_as_it_is(string book, bool ctrl)
    {
        var client = FakeClient.Join(game, "nb-" + book.Split('-')[^1] + (ctrl ? "-c" : ""));
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = book == "album" ? Tapestry("ambush1") : Vanilla("ingot-copper", 5);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Newer(book == "album" ? Album(8) : Book(book, 8));
            string before = Bytes(hand.Itemstack);

            Click(player, hand, pos, ctrl);

            return (Unchanged: Bytes(hand.Itemstack) == before, Chest: Contents(chest), Given: Inventory(player), Lists: FindOurs(player, "invoice-") != null);
        });
        Assert.Equal((true, book == "album" ? "tapestry-north:ambush1 x1" : "ingot-copper x5", "", false), seen);
        client.WaitFor(() => Errors(client).Contains("newerversion"), "the refusal 'newer version'");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_newer_invoice_is_not_posted_and_stays(bool inOffhandBook)
    {
        // The invoice itself is newer, or it is fine and the book in the off-hand (the sheet would go back into it) is.
        var client = FakeClient.Join(game, "ni-" + inOffhandBook);
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("ingot-copper", 5);
            var offhand = player.Entity.LeftHandItemSlot;
            offhand.Itemstack = inOffhandBook ? Newer(Book("book-ledger-normal-brickred", 7)) : Book("book-ledger-normal-brickred", 7);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            var invoice = Invoice(InvoiceKind.Incoming, Vanilla("ingot-copper", 5));
            hand.Itemstack = inOffhandBook ? invoice : Newer(invoice);
            string before = Bytes(hand.Itemstack) + Bytes(offhand.Itemstack);

            Click(player, hand, pos);

            return (Unchanged: Bytes(hand.Itemstack) + Bytes(offhand.Itemstack) == before, Chest: Contents(chest), Given: Inventory(player));
        });
        Assert.Equal((true, "ingot-copper x5", ""), seen);
        client.WaitFor(() => Errors(client).Contains("newerversion"), "the refusal 'newer version'");
    }

    [Fact]
    public void A_newer_book_in_the_off_hand_is_refused_through_the_channel()
    {
        // The off-hand gesture arrives as a MakeInvoiceRequest and ends in the same UseOnContainer.
        var client = FakeClient.Join(game, "newer-offhand");
        var (pos, before) = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Tapestry("ambush1");
            player.Entity.LeftHandItemSlot.Itemstack = Newer(Album(8));
            Stand(player, pos);
            return (pos, Bytes(player.Entity.LeftHandItemSlot.Itemstack));
        });

        client.SendToMod(new MakeInvoiceRequest { X = pos.X, Y = pos.Y, Z = pos.Z });

        client.WaitFor(() => Errors(client).Contains("newerversion"), "the refusal 'newer version'");
        Assert.Equal((before, ""), game.OnMain(() => (Bytes(client.Player.Entity.LeftHandItemSlot.Itemstack), Inventory(client.Player))));
    }

    [Theory]
    [InlineData("rename")]
    [InlineData("clear")]
    [InlineData("cancel")]
    [InlineData("wantlist")]
    public void A_window_request_about_newer_data_is_refused_and_changes_nothing(string request)
    {
        // Each window's request, with the item of that window in the main hand.
        var client = FakeClient.Join(game, "nw-" + request);
        var (fingerprint, before) = game.OnMain(() =>
        {
            var hand = client.Player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = request switch
            {
                "cancel" => Newer(Invoice(InvoiceKind.Incoming, Vanilla("ingot-copper", 5))),
                "wantlist" => Newer(Album(8)),
                _ => Newer(Book("book-ledger-normal-brickred", 5, Vanilla("ingot-iron"))),
            };
            return (LedgerStore.Fingerprint(hand.Itemstack), Bytes(hand.Itemstack));
        });
        var shown = new ShownItem { HotbarSlot = 0, Fingerprint = fingerprint };

        switch (request)
        {
            case "rename": client.SendToMod(new RenameLedgerRequest { Name = "new name", Book = shown }); break;
            case "clear": client.SendToMod(new ClearLedgerRequest { Book = shown }); break;
            case "cancel": client.SendToMod(new CancelInvoiceRequest { Invoice = shown }); break;
            default: client.SendToMod(new MakeWantListRequest { Book = shown }); break;
        }

        client.WaitFor(() => Errors(client).Contains("newerversion"), "the refusal 'newer version'");
        Assert.Equal((before, ""), game.OnMain(() => (Bytes(client.Player.InventoryManager.ActiveHotbarSlot.Itemstack), Inventory(client.Player))));
    }

    [Fact]
    public void Cancelling_an_invoice_is_refused_when_the_book_in_the_off_hand_is_newer()
    {
        // The cancelled invoice's sheet would go back into that book, which this build must not write.
        var client = FakeClient.Join(game, "newer-cancel");
        var (fingerprint, before) = game.OnMain(() =>
        {
            var player = client.Player;
            player.Entity.LeftHandItemSlot.Itemstack = Newer(Book("book-ledger-normal-brickred", 7));
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Invoice(InvoiceKind.Incoming, Vanilla("ingot-copper", 5));
            return (LedgerStore.Fingerprint(hand.Itemstack), Bytes(hand.Itemstack) + Bytes(player.Entity.LeftHandItemSlot.Itemstack));
        });

        client.SendToMod(new CancelInvoiceRequest { Invoice = new ShownItem { HotbarSlot = 0, Fingerprint = fingerprint } });

        client.WaitFor(() => Errors(client).Contains("newerversion"), "the refusal 'newer version'");
        Assert.Equal(before, game.OnMain(() => Bytes(client.Player.InventoryManager.ActiveHotbarSlot.Itemstack) + Bytes(client.Player.Entity.LeftHandItemSlot.Itemstack)));
    }

    // ---- nocollection

    [Fact]
    public void Starting_over_an_album_of_an_unknown_collection_is_refused_and_changes_nothing()
    {
        // The album names a collection this build does not have (a mod that added it is gone).
        var client = FakeClient.Join(game, "no-collection");
        var (fingerprint, before) = game.OnMain(() =>
        {
            var hand = client.Player.InventoryManager.ActiveHotbarSlot;
            var stack = Ours("book-album-normal-brickred");
            Mod.Store.WriteLedger(stack, Ledger.FromStored(LedgerMode.Album, 5, [], "gone", new AlbumInfo("vanished-collection")));
            hand.Itemstack = stack;
            return (LedgerStore.Fingerprint(stack), Bytes(stack));
        });

        client.SendToMod(new ClearLedgerRequest { Book = new ShownItem { HotbarSlot = 0, Fingerprint = fingerprint } });

        client.WaitFor(() => Errors(client).Contains("nocollection"), "the refusal 'no collection'");
        Assert.Equal(before, game.OnMain(() => Bytes(client.Player.InventoryManager.ActiveHotbarSlot.Itemstack)));
    }
}
