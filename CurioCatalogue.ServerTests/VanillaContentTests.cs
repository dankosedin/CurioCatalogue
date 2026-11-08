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
    public void Every_tapestry_piece_has_the_flat_image_the_album_draws()
    {
        // The album draws a piece's own texture (Member.FlatTexture), the one the game puts on the wall (vssurvivalmod
        // BlockTapestry.cs:40#texturePath). Textures load on the server too (vsapi AssetCategory.cs:23, Universal), but the
        // Linux server archive CI uses ships no block textures (docs/test-server.md, checked 09.11.2026): there only the
        // attribute is checked, the files where the game has them.
        var seen = game.OnMain(() =>
        {
            var members = Mod.Collections.Defined("tapestries")!.Levels.SelectMany(l => l.Members).ToList();
            var paths = members.Select(m => (m.Id, Path: m.Attributes.GetValueOrDefault(CurioCatalogue.Core.Member.FlatTexture))).ToList();
            return (members.Count, paths, Shipped: paths.Any(p => p.Path != null && World.AssetManager.Exists(new AssetLocation(p.Path))),
                Exist: paths.Where(p => p.Path != null && World.AssetManager.Exists(new AssetLocation(p.Path))).Select(p => p.Id).ToHashSet());
        });

        Assert.True(seen.Count > 30, $"only {seen.Count} pieces");
        Assert.Empty(seen.paths.Where(p => p.Path is null || !p.Path.StartsWith("game:textures/block/cloth/tapestry/")).Select(p => p.Id));
        if (seen.Shipped)
            Assert.Empty(seen.paths.Where(p => !seen.Exist.Contains(p.Id)).Select(p => p.Id));
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
