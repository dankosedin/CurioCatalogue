using CurioCatalogue.Adapters;
using CurioCatalogue.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace CurioCatalogue.CompatTests;

/// <summary>
/// Upgradeable Storage (<c>upgradablestorage</c>, pinned in mods.json): its chests, trunks and storage vessels are vanilla block
/// classes with its own block entity classes or vanilla ones, so our books should work on them as on vanilla chests
/// (<c>docs/testing/functional.md</c>, K2). The gestures are the ones of <c>BookFlowTests</c>, on its blocks.
/// </summary>
[Collection(CompatCollection.Name)]
public class UpgradeableStorageTests(CompatServerFixture server) : BookTestBase(server)
{
    const string Domain = "upgradablestorage";

    /// <summary>Its block, with a metal type (every block of the mod has <c>copper</c>, its creative stacks).</summary>
    (Vintagestory.API.MathTools.BlockPos Pos, BlockEntity Entity) PlaceTheirs(string code) =>
        TestBlocks.Place(game, $"{Domain}:{code}", stack => stack.Attributes.SetString("type", "copper"));

    [Fact]
    public void Both_mods_load_without_warnings_or_errors()
    {
        var enabled = game.OnMain(() => (game.Api.ModLoader.IsModEnabled(CurioCatalogueModSystem.ModId), game.Api.ModLoader.IsModEnabled(Domain)));
        var problems = LogProblems.AtStart(game);

        Assert.Equal((true, true), enabled);
        Assert.True(problems.Count == 0, "the server logged problems:\n" + string.Join("\n", problems));
    }

    // Each row: its block, the class of its block entity (so a new version of the mod that changes it shows up), and our kind.
    [Theory]
    [InlineData("chest-east", "BlockEntityGenericTypedContainer")]
    [InlineData("trunk-east", "BlockEntityGenericTypedContainer")]
    [InlineData("storagevessel-black", "BlockEntityGenericTypedContainer")]
    [InlineData("labeledchest-east", "BlockEntityLabeledChest")]
    [InlineData("labeledtrunk-east", "BlockEntityReinforceableLabeledContainer")]
    [InlineData("labeledstoragevessel-black", "BlockEntityReinforceableLabeledContainer")]
    public void Its_storage_is_supported(string block, string entityClass)
    {
        var seen = game.OnMain(() =>
        {
            var (_, entity) = PlaceTheirs(block);
            return (entity.GetType().Name, new ContainerReader(World).Classify(entity).ToString());
        });
        Assert.Equal((entityClass, "Supported"), seen);
    }

    [Theory]
    [InlineData("chest-east", "us-chest")]
    [InlineData("trunk-east", "us-trunk")]
    [InlineData("storagevessel-black", "us-vessel")]
    [InlineData("labeledchest-east", "us-lchest")]
    [InlineData("labeledtrunk-east", "us-ltrunk")]
    [InlineData("labeledstoragevessel-black", "us-lvessel")]
    public void Every_gesture_works_on_its_storage(string block, string playerName)
    {
        // K1 on its blocks, one after another on the same container: an intake list (Shift) and a pick list (Shift + Ctrl) made,
        // the intake list posted with the ledger in the off-hand, the album taking a piece, a want list filled.
        var client = FakeClient.Join(game, playerName);
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, container) = PlaceTheirs(block);
            var slots = ((IBlockEntityContainer)container).Inventory;
            slots[0].Itemstack = Vanilla("ingot-iron", 3);
            slots[1].Itemstack = Vanilla("ingot-copper", 5);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            var ledger = Book("book-ledger-normal-brickred", 8, Vanilla("ingot-iron"));
            hand.Itemstack = ledger;

            Click(player, hand, pos);
            var intake = TakeOurs(player, "invoice-incoming");
            Click(player, hand, pos, ctrl: true);
            var pick = TakeOurs(player, "invoice-outgoing");
            var made = (Intake: intake == null ? "" : Lines(Mod, intake), Pick: pick == null ? "" : Lines(Mod, pick).Split(" x")[0], Chest: Contents(container));

            player.Entity.LeftHandItemSlot.Itemstack = hand.Itemstack;
            hand.Itemstack = intake;
            Click(player, hand, pos);
            var posted = (Inventory(player), Contents(container), BookState(player.Entity.LeftHandItemSlot.Itemstack!).Entries);

            slots[2].Itemstack = Tapestry("ambush1");
            hand.Itemstack = Album(8);
            Click(player, hand, pos);

            slots[3].Itemstack = Tapestry("ambush2");
            hand.Itemstack = Invoice(InvoiceKind.WantList, Tapestry("ambush2"));
            Click(player, hand, pos);

            return (made, posted, Final: (Inventory(player), Contents(container)));
        });

        Assert.Equal(("game:ingot-copper x5", "game:ingot-iron", "ingot-copper x5, ingot-iron x3"), seen.made);
        Assert.Equal(("ingot-copper x5", "ingot-iron x3", 2), seen.posted);
        Assert.Equal(("ingot-copper x5, paper-parchment x1, tapestry-north:ambush1 x1, tapestry-north:ambush2 x1", "ingot-iron x3"), seen.Final);
    }

    [Fact]
    public void A_ledger_from_the_off_hand_makes_a_list_through_the_channel()
    {
        // The server checks reach and access against its block too (a request names a position, not a block entity).
        var client = FakeClient.Join(game, "us-offhand");
        var pos = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, container) = PlaceTheirs("labeledtrunk-east");
            ((IBlockEntityContainer)container).Inventory[0].Itemstack = Vanilla("ingot-copper", 5);
            player.Entity.LeftHandItemSlot.Itemstack = Book("book-ledger-normal-brickred", 8);
            Stand(player, pos);
            return pos;
        });

        client.SendToMod(new MakeInvoiceRequest { X = pos.X, Y = pos.Y, Z = pos.Z });

        client.WaitFor(() => client.Player.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible is ItemInvoice, "the list in the empty hand");
        Assert.Equal("game:ingot-copper x5", game.OnMain(() => Lines(Mod, client.Player.InventoryManager.ActiveHotbarSlot.Itemstack!)));
    }

    [Fact]
    public void A_padlock_on_its_storage_refuses_the_visitor_but_not_its_owner()
    {
        // Its labeled trunks and vessels have a block entity of its own ("Reinforceable"): the padlock is the game's, checked by us.
        var owner = FakeClient.Join(game, "us-lock-owner");
        var visitor = FakeClient.Join(game, "us-lock-guest");
        var seen = game.OnMain(() =>
        {
            var (pos, container) = PlaceTheirs("labeledstoragevessel-black");
            ((IBlockEntityContainer)container).Inventory[0].Itemstack = Vanilla("ingot-copper", 5);
            bool locked = game.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>().TryLock(pos, owner.Player, "game:padlock");

            (string Inventory, string Chest) Try(IServerPlayer player)
            {
                var hand = player.InventoryManager.ActiveHotbarSlot;
                hand.Itemstack = Invoice(InvoiceKind.Incoming, Vanilla("ingot-copper", 5));
                Click(player, hand, pos);
                return (Inventory(player), Contents(container));
            }
            return (locked, Visitor: Try(visitor.Player), Owner: Try(owner.Player));
        });
        Assert.Equal((true, ("", "ingot-copper x5"), ("ingot-copper x5, paper-parchment x1", "")), seen);
        visitor.WaitFor(() => Errors(visitor).Contains("noaccess"), "the refusal 'no access'");
    }
}
