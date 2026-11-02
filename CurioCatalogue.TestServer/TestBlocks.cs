using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace CurioCatalogue.TestServer;

/// <summary>
/// Vanilla blocks put into the test world. One counter for all test classes: nothing is removed, so every placement takes a
/// cell of its own near the spawn and no test meets the blocks of an earlier one.
/// </summary>
internal static class TestBlocks
{
    static int nextCell;

    // Cells 4 blocks apart (a chest and the player's reach around it), in a square of Side × Side from the spawn on: 64 × 64 blocks,
    // inside the chunks the server keeps loaded without a player (offsets 0 to 127 seen loaded, 03.11.2026). A block set into a
    // chunk that is not loaded is lost, so the cells end there.
    const int Spacing = 4, Side = 16;

    /// <summary>A free cell near the spawn (the spawn chunks are loaded).</summary>
    public static BlockPos FreePos(GameServerFixture game)
    {
        int n = Interlocked.Increment(ref nextCell);
        if (n >= Side * Side)
            throw new InvalidOperationException($"the {Side * Side} cells near the spawn are used up: widen {nameof(TestBlocks)} only inside the loaded spawn chunks");
        var spawn = game.Api.World.DefaultSpawnPosition.AsBlockPos;
        return new BlockPos(spawn.X + Spacing * (n % Side), spawn.Y + 6, spawn.Z + Spacing * (n / Side), 0);
    }

    /// <summary>
    /// Puts a block into the world (main thread only) and returns where and its block entity. A code without a domain is the
    /// game's (<c>chest-east</c>); another mod's has its domain (<c>upgradablestorage:chest-east</c>).
    /// </summary>
    public static (BlockPos Pos, BlockEntity Entity) Place(GameServerFixture game, string code, Action<ItemStack>? configure = null)
    {
        var pos = FreePos(game);
        var location = new AssetLocation(code);
        var block = game.Api.World.GetBlock(location) ?? throw new InvalidOperationException($"no block {location}");
        var stack = new ItemStack(block);
        configure?.Invoke(stack);
        game.Api.World.BlockAccessor.SetBlock(block.BlockId, pos, stack);
        var entity = game.Api.World.BlockAccessor.GetBlockEntity(pos) ?? throw new InvalidOperationException($"{location} has no block entity at {pos}");
        return (pos, entity);
    }
}
