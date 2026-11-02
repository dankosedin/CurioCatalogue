using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace CurioCatalogue.TestServer;

/// <summary>
/// Vanilla blocks put into the test world. One counter for all test classes: every placement takes its own cell near the
/// spawn, so tests that run side by side never meet.
/// </summary>
internal static class TestBlocks
{
    static int nextCell;

    /// <summary>A free cell near the spawn (the spawn chunks are loaded).</summary>
    public static BlockPos FreePos(GameServerFixture game)
    {
        int n = Interlocked.Increment(ref nextCell);
        var spawn = game.Api.World.DefaultSpawnPosition.AsBlockPos;
        return new BlockPos(spawn.X + 4 * (n % 8), spawn.Y + 6, spawn.Z + 4 * (n / 8), 0);
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
