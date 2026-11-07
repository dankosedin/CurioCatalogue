using Vintagestory.API.Common;

namespace CurioCatalogue.ServerTests;

/// <summary>
/// The server twins of assumptions the adapter tests make about vanilla stacks (<c>StackKeysTests</c>, <c>CollectionRegistry</c>):
/// here the game itself makes the stack, so the assumption is checked rather than written into the test.
/// </summary>
[Collection(ServerCollection.Name)]
public class VanillaContentTests(GameServerFixture server) : BookTestBase(server)
{
    [Fact]
    public void A_tapestry_taken_off_a_wall_is_a_north_piece_with_its_picture_and_a_member_of_the_collection()
    {
        // Hung on an east wall; the drop is what the player gets by breaking it (vssurvivalmod BlockTapestry.cs#GetDrops,
        // BehaviorHorizontalAttachable.cs:66#dropBlockFace, "north").
        var seen = game.OnMain(() =>
        {
            var (pos, _) = TestBlocks.Place(game, "tapestry-east", s => s.Attributes.SetString("type", "ambush1"));
            var drops = World.BlockAccessor.GetBlock(pos).GetDrops(World, pos, null);
            return (string.Join(", ", drops.Select(Describe)), Member: drops.Length == 1 && Mod.Collections.Defined("tapestries") is { } c && Mod.Collections.HasMember(c, drops[0]));
        });

        Assert.Equal(("tapestry-north:ambush1 x1", true), seen);
    }

    [Fact]
    public void A_hot_ingot_counts_as_the_same_item_as_a_cold_one()
    {
        // The game keeps the heat where GlobalConstants.IgnoredStackAttributes says, so the key ignores it without our config.
        var seen = game.OnMain(() =>
        {
            var cold = Vanilla("ingot-iron");
            var hot = Vanilla("ingot-iron");
            hot.Collectible.SetTemperature(World, hot, 600);
            return (Heated: hot.Collectible.GetTemperature(World, hot) > 500, Same: Mod.Keys.KeyOf(hot).Equals(Mod.Keys.KeyOf(cold)));
        });

        Assert.Equal((true, true), seen);
    }
}
