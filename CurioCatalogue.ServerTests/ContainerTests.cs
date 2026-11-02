using CurioCatalogue.Adapters;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace CurioCatalogue.ServerTests;

/// <summary>
/// Real containers in the world: <see cref="ContainerReader"/> and <see cref="ContainerAccess"/> against the blocks and block
/// entities the game itself makes. They replace the cases of <c>ContainerReaderTests</c> and <c>ContainerAccessTests</c> that
/// stand for engine behaviour with hand-made <c>TestChest</c>s and mocked claims.
/// </summary>
[Collection(ServerCollection.Name)]
public class ContainerTests(GameServerFixture game)
{
    // Every placement gets its own cell (TestBlocks), so tests do not meet.
    BlockPos FreePos() => TestBlocks.FreePos(game);

    (BlockPos Pos, BlockEntity Entity) Place(string code, Action<ItemStack>? configure = null) => TestBlocks.Place(game, code, configure);

    ItemStack Stack(string code, int size) =>
        new(game.Api.World.GetItem(new AssetLocation("game", code)) ?? throw new InvalidOperationException($"no item game:{code}"), size);

    ContainerReader Reader() => new(game.Api.World);

    static string Sizes(IEnumerable<ItemStack>? stacks) => stacks == null ? "null" : string.Join(",", stacks.Select(s => s.StackSize));

    [Fact]
    public void A_real_chest_gives_its_non_empty_slots()
    {
        var seen = game.OnMain(() =>
        {
            var (_, chest) = Place("chest-east");
            var inventory = ((IBlockEntityContainer)chest).Inventory;
            inventory[0].Itemstack = Stack("ingot-iron", 3);
            inventory[5].Itemstack = Stack("ingot-iron", 5);
            return (chest.GetType().Name, Reader().Classify(chest), Sizes(Reader().Read(chest)));
        });
        Assert.Equal(("BlockEntityGenericTypedContainer", ContainerKind.Supported, "3,5"), seen);
    }

    // The kind is a string: an attribute argument of a type from our DLL is read at discovery, before the module initializer can find it.
    // Each row: a vanilla block, the class of its block entity (so a game update that renames it shows up), and how we classify it.
    [Theory]
    [InlineData("chest-east", "BlockEntityGenericTypedContainer", "Supported")]
    [InlineData("trunk-east", "BlockEntityGenericTypedContainer", "Supported")]
    [InlineData("labeledchest-east", "BlockEntityLabeledChest", "Supported")]
    [InlineData("storagevessel-earthen", "BlockEntityGenericTypedContainer", "Supported")]
    [InlineData("crate", "BlockEntityCrate", "Supported")]
    [InlineData("barrel", "BlockEntityBarrel", "Unsupported")]
    [InlineData("quern-granite", "BlockEntityQuern", "Unsupported")]
    [InlineData("firepit-cold", "BlockEntityFirepit", "Unsupported")]
    [InlineData("sign-ground-north", "BlockEntitySign", "None")]
    public void Vanilla_blocks_are_classified(string block, string entityClass, string kind)
    {
        var seen = game.OnMain(() =>
        {
            var (_, entity) = Place(block);
            return (entity.GetType().Name, Reader().Classify(entity).ToString());
        });
        Assert.Equal((entityClass, kind), seen);
    }

    [Fact]
    public void A_loot_chest_with_a_share_per_player_is_unsupported()
    {
        // World generation places chests and vessels with the stack attribute isPerPlayer (vssurvivalmod BEGenericTypedContainer.cs:100#isPerPlayer).
        var seen = game.OnMain(() =>
        {
            var (_, loot) = Place("chest-east", stack => stack.Attributes.SetBool("isPerPlayer", true));
            return (((IBlockEntityContainer)loot).Inventory.GetType().Name, Reader().Classify(loot).ToString());
        });
        Assert.Equal(("InventoryPerPlayer", "Unsupported"), seen);
    }

    [Fact]
    public void A_backpack_in_a_chest_is_one_item()
    {
        var seen = game.OnMain(() =>
        {
            var (_, chest) = Place("chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Stack("backpack-normal", 1);
            return Reader().Read(chest)!.Select(s => s.Collectible.Code.ToString()).ToList().Single();
        });
        Assert.Equal("game:backpack-normal", seen);
    }

    [Fact]
    public void A_pile_on_the_ground_is_left_to_the_game()
    {
        var kind = game.OnMain(() =>
        {
            var (_, ground) = Place("groundstorage");
            ((BlockEntityGroundStorage)ground).Inventory[0].Itemstack = Stack("ingot-iron", 10);
            return Reader().Classify(ground);
        });
        Assert.Equal(ContainerKind.None, kind);
    }

    [Fact]
    public void Items_put_into_a_backpack_on_the_ground_are_read_back()
    {
        // The whole path of a want list taken from / given to a bag: Open makes the bag's slots, Save writes a changed one
        // into the bag's attributes, Read gets it from the stack again.
        var seen = game.OnMain(() =>
        {
            var (_, ground) = Place("groundstorage");
            var storage = (BlockEntityGroundStorage)ground;
            storage.Inventory[0].Itemstack = Stack("backpack-normal", 1);
            var reader = Reader();
            var kind = reader.Classify(ground);
            var before = Sizes(reader.Read(ground));

            var opened = reader.Open(ground)!;
            var slot = opened.Slots[0];
            slot.Itemstack = Stack("ingot-iron", 7);
            opened.Save(slot);
            return (kind, before, Sizes(reader.Read(ground)));
        });
        Assert.Equal((ContainerKind.Supported, "", "7"), seen);
    }

    [Fact]
    public void A_trunk_is_found_from_its_second_cell()
    {
        // A trunk is two cells; the second is a BlockMultiblock filler without a block entity (modding.md, Containers and inventories).
        var client = FakeClient.Join(game, "trunk-placer");
        var seen = game.OnMain(() =>
        {
            var pos = FreePos();
            var blocks = game.Api.World.BlockAccessor;
            var trunk = game.Api.World.GetBlock(new AssetLocation("game", "trunk-east"));
            var stack = new ItemStack(trunk);
            var selection = new BlockSelection { Position = pos, Face = BlockFacing.UP, HitPosition = new Vec3d(0.5, 0, 0.5) };
            bool placed = trunk.DoPlaceBlock(game.Api.World, client.Player, selection, stack);

            var fillers = new List<BlockPos>();
            foreach (var face in BlockFacing.HORIZONTALS)
            {
                var near = pos.AddCopy(face);
                if (blocks.GetBlock(near) is BlockMultiblock) fillers.Add(near);
            }
            var filler = fillers.SingleOrDefault();
            var main = filler == null ? null : ContainerReader.MainPos(blocks, filler);
            return (placed, FillerCount: fillers.Count, FillerHasEntity: filler != null && blocks.GetBlockEntity(filler) != null,
                MainIsTrunk: main != null && main.Equals(pos), Kind: Reader().Classify(blocks.GetBlockEntity(main ?? pos)).ToString());
        });
        Assert.Equal((true, 1, false, true, "Supported"), seen);
    }

    // Access, with the mod's own wiring (CurioCatalogueModSystem.Access) and two real players: the owner and a visitor.
    // The item handles the click before the block, so these checks are ours (docs/critical.md, "Access is our job").

    (bool Owner, bool Visitor) CanUse(IServerPlayer owner, IServerPlayer visitor, BlockPos pos)
    {
        var access = game.Api.ModLoader.GetModSystem<CurioCatalogueModSystem>().Access;
        return (access.CanUse(owner, pos), access.CanUse(visitor, pos));
    }

    [Fact]
    public void An_unclaimed_unlocked_chest_is_open_to_everyone()
    {
        var owner = FakeClient.Join(game, "free-owner");
        var visitor = FakeClient.Join(game, "free-visitor");
        var seen = game.OnMain(() => CanUse(owner.Player, visitor.Player, Place("chest-east").Pos));
        Assert.Equal((true, true), seen);
    }

    [Fact]
    public void A_land_claim_keeps_the_visitor_out_unless_it_allows_use()
    {
        var owner = FakeClient.Join(game, "claim-owner");
        var visitor = FakeClient.Join(game, "claim-visitor");
        var seen = game.OnMain(() =>
        {
            var pos = Place("chest-east").Pos;
            var closed = TestBlocks.WithClaim(game, owner.Player, pos, _ => { }, () => CanUse(owner.Player, visitor.Player, pos));
            var useEveryone = TestBlocks.WithClaim(game, owner.Player, pos, c => c.AllowUseEveryone = true, () => CanUse(owner.Player, visitor.Player, pos));
            var shared = TestBlocks.WithClaim(game, owner.Player, pos, c => c.PermittedPlayerUids[visitor.Player.PlayerUID] = EnumBlockAccessFlags.Use,
                () => CanUse(owner.Player, visitor.Player, pos));
            return (closed, useEveryone, shared, After: CanUse(owner.Player, visitor.Player, pos));
        });
        Assert.Equal(((true, false), (true, true), (true, true), (true, true)), seen);
    }

    [Fact]
    public void A_locked_chest_keeps_the_visitor_out_but_not_its_owner()
    {
        var owner = FakeClient.Join(game, "lock-owner");
        var visitor = FakeClient.Join(game, "lock-visitor");
        var seen = game.OnMain(() =>
        {
            var pos = Place("chest-east").Pos;
            bool locked = game.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>().TryLock(pos, owner.Player, "game:padlock");
            return (locked, CanUse(owner.Player, visitor.Player, pos));
        });
        Assert.Equal((true, (true, false)), seen);
    }
}
