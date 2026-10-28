using NSubstitute;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using CurioCatalogue.Adapters;
using CurioCatalogue.Core;

namespace CurioCatalogue.Tests;

public class InvoiceMakerTests
{
    private readonly GameWorld game = new();
    // IPlayer cannot be mocked or implemented outside the game: it has an internal member (vsapi IPlayer.cs:124#IsInInteractionRangeOf).
    // Our code only passes the player on to claims and locks, which are mocked, so a null stands in for it.
    private readonly IPlayer player = null!;
    private readonly BlockPos pos = new(512010, 110, 511990);
    private readonly LedgerStore store;
    private readonly InvoiceMaker maker;
    private readonly Item ledgerItem;
    private readonly Block tapestry;
    private bool locked;

    public InvoiceMakerTests()
    {
        store = new LedgerStore(new StackKeys(game.World, ["durability"]));
        var reader = new ContainerReader(game.World, _ => null, TestInventoryBlock.IsStorage);
        var access = new ContainerAccess(game.World, (_, _) => locked);
        maker = new InvoiceMaker(game.World, store, reader, access, () => new LedgerLimits(32, 500));

        ledgerItem = game.Item("curiocatalogue:book-ledger-normal-darkgreen");
        game.Item("curiocatalogue:invoice-incoming");
        game.Item("curiocatalogue:invoice-outgoing");
        game.Item("curiocatalogue:invoice-wantlist");
        tapestry = game.Block("tapestry-north");
        game.Calendar.TotalDays.Returns(42.5);
    }

    private ItemStack Tapestry(string type, int size = 1)
    {
        var stack = new ItemStack(tapestry, size);
        stack.Attributes.SetString("type", type);
        return stack;
    }

    private ItemStack LedgerWith(int sheets, params ItemStack[] known)
    {
        var stack = new ItemStack(ledgerItem);
        store.WriteLedger(stack, Ledger.FromStored(LedgerMode.Types, sheets, known.Select(store.ToEntry)));
        return stack;
    }

    private MakeInvoiceResult Make(ItemStack ledgerStack, InvoiceKind kind = InvoiceKind.Incoming) =>
        maker.Make(player, ledgerStack, LedgerMode.Types, kind, pos);

    [Fact]
    public void Incoming_invoice_lists_new_items_and_spends_a_sheet()
    {
        game.Place(pos, new TestChest(Tapestry("ambush1"), Tapestry("ambush2", 2)));
        var ledgerStack = LedgerWith(3, Tapestry("ambush1"));

        var made = Assert.IsType<MakeInvoiceResult.Made>(Make(ledgerStack));

        Assert.Equal("curiocatalogue:invoice-incoming", made.InvoiceStack.Collectible.Code.ToString());
        var invoice = store.ReadInvoice(made.InvoiceStack, InvoiceKind.Incoming);
        Assert.Equal([("block:game:tapestry-north{\"type\":\"ambush2\"}", 2)], invoice.Entries.Select(e => (e.Key.ToString(), e.Count)));
        Assert.Equal(42.5, invoice.CreatedTotalDays);
        Assert.Equal(new BlockPosition(512010, 110, 511990), invoice.Position);
        Assert.Equal(2, made.SheetsLeft);
        Assert.Equal(2, store.ReadLedger(ledgerStack, LedgerMode.Types).Sheets); // written back into the held ledger
    }

    [Fact]
    public void Other_half_of_a_trunk_reads_the_trunk()
    {
        // The filler half has no block entity; its OffsetInv points to the main block (vsessentialsmod BlockMultiblock.cs:59#BlockMultiblock).
        game.Place(pos, new TestChest(Tapestry("ambush1")));
        var half = pos.EastCopy();
        game.Place(half, null, new Vintagestory.GameContent.BlockMultiblock { OffsetInv = new Vec3i(-1, 0, 0) });

        var made = Assert.IsType<MakeInvoiceResult.Made>(maker.Make(player, LedgerWith(1), LedgerMode.Types, InvoiceKind.Incoming, half));
        Assert.Equal(new BlockPosition(pos.X, pos.Y, pos.Z), store.ReadInvoice(made.InvoiceStack, InvoiceKind.Incoming).Position);
    }

    [Fact]
    public void Outgoing_invoice_uses_its_own_item()
    {
        game.Place(pos, new TestChest(Tapestry("ambush1")));
        var made = Assert.IsType<MakeInvoiceResult.Made>(Make(LedgerWith(1, Tapestry("ambush1")), InvoiceKind.Outgoing));
        Assert.Equal("curiocatalogue:invoice-outgoing", made.InvoiceStack.Collectible.Code.ToString());
    }

    [Fact]
    public void Mod_items_in_the_container_are_left_out()
    {
        var otherLedger = new ItemStack(ledgerItem);
        game.Place(pos, new TestChest(otherLedger, Tapestry("ambush1")));

        var made = Assert.IsType<MakeInvoiceResult.Made>(Make(LedgerWith(1)));
        Assert.Single(store.ReadInvoice(made.InvoiceStack, InvoiceKind.Incoming).Entries);
    }

    [Fact]
    public void Machine_is_refused_and_nothing_happens()
    {
        game.Place(pos, new TestMachine(Tapestry("ambush1")));
        var ledgerStack = LedgerWith(3);

        Assert.Equal(MakeRefusal.Unsupported, Refusal(Make(ledgerStack)));
        Assert.Equal(3, store.ReadLedger(ledgerStack, LedgerMode.Types).Sheets);
    }

    [Fact]
    public void Block_without_inventory_is_refused_if_it_ever_gets_here()
    {
        // The ledger does not take such clicks at all; this guards against a block that changed meanwhile.
        game.Place(pos, new Vintagestory.GameContent.BlockEntitySign());
        Assert.Equal(MakeRefusal.Unsupported, Refusal(Make(LedgerWith(1))));
    }

    [Fact]
    public void Same_click_twice_makes_two_invoices()
    {
        // Making an invoice changes neither the container nor the ledger's entries; only posting does (domain.md, Making an invoice).
        game.Place(pos, new TestChest(Tapestry("ambush1")));
        var ledgerStack = LedgerWith(3);

        Assert.IsType<MakeInvoiceResult.Made>(Make(ledgerStack));
        var second = Assert.IsType<MakeInvoiceResult.Made>(Make(ledgerStack));
        Assert.Equal(1, second.SheetsLeft);
    }

    [Fact]
    public void Empty_chest_gives_no_difference()
    {
        game.Place(pos, new TestChest());
        Assert.Equal(MakeRefusal.NoDifference, Refusal(Make(LedgerWith(3))));
    }

    [Fact]
    public void Without_access_nothing_happens()
    {
        game.Place(pos, new TestChest(Tapestry("ambush1")));
        locked = true;
        var ledgerStack = LedgerWith(3);

        Assert.Equal(MakeRefusal.NoAccess, Refusal(Make(ledgerStack)));
        Assert.Equal(3, store.ReadLedger(ledgerStack, LedgerMode.Types).Sheets);
    }

    [Fact]
    public void No_difference_spends_no_sheet()
    {
        game.Place(pos, new TestChest(Tapestry("ambush1")));
        var ledgerStack = LedgerWith(3, Tapestry("ambush1"));

        Assert.Equal(MakeRefusal.NoDifference, Refusal(Make(ledgerStack)));
        Assert.Equal(3, store.ReadLedger(ledgerStack, LedgerMode.Types).Sheets);
    }

    [Fact]
    public void Ledger_without_sheets_is_refused()
    {
        game.Place(pos, new TestChest(Tapestry("ambush1")));
        Assert.Equal(MakeRefusal.NoSheets, Refusal(Make(LedgerWith(0))));
    }

    [Fact]
    public void Full_ledger_is_refused_for_incoming()
    {
        var full = new InvoiceMaker(game.World, store, new ContainerReader(game.World, _ => null, TestInventoryBlock.IsStorage),
            new ContainerAccess(game.World, (_, _) => false), () => new LedgerLimits(32, 1));
        game.Place(pos, new TestChest(Tapestry("ambush2")));

        var result = full.Make(player, LedgerWith(3, Tapestry("ambush1")), LedgerMode.Types, InvoiceKind.Incoming, pos);
        Assert.Equal(MakeRefusal.LedgerFull, Refusal(result));
    }

    [Fact]
    public void Want_list_lists_what_the_album_misses_for_a_sheet()
    {
        var albumItem = game.Item("curiocatalogue:book-album-aged-orange", new ItemLedger());
        var albumStack = new ItemStack(albumItem);
        var collection = new Collection("tapestries", new[] { "a", "b" }.Select(t => new CollectionMember(store.ToEntry(Tapestry(t)).Key, t, Tapestry(t))));
        store.WriteLedger(albumStack, collection.NewAlbum(2, ""));

        var made = Assert.IsType<MakeInvoiceResult.Made>(maker.MakeWantList(albumStack, pos));

        Assert.Equal("curiocatalogue:invoice-wantlist", made.InvoiceStack.Collectible.Code.ToString());
        Assert.Equal(2, store.ReadInvoice(made.InvoiceStack, InvoiceKind.WantList).Entries.Length);
        Assert.Equal(1, store.ReadLedger(albumStack, LedgerMode.Album).Sheets);
    }

    private static MakeRefusal Refusal(MakeInvoiceResult result) => Assert.IsType<MakeInvoiceResult.Refused>(result).Reason;
}
