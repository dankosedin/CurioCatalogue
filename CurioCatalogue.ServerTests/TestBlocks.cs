using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace CurioCatalogue.ServerTests;

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

    /// <summary>Puts a vanilla block into the world (main thread only) and returns where and its block entity.</summary>
    public static (BlockPos Pos, BlockEntity Entity) Place(GameServerFixture game, string code, Action<ItemStack>? configure = null)
    {
        var pos = FreePos(game);
        var block = game.Api.World.GetBlock(new AssetLocation("game", code)) ?? throw new InvalidOperationException($"no block game:{code}");
        var stack = new ItemStack(block);
        configure?.Invoke(stack);
        game.Api.World.BlockAccessor.SetBlock(block.BlockId, pos, stack);
        var entity = game.Api.World.BlockAccessor.GetBlockEntity(pos) ?? throw new InvalidOperationException($"game:{code} has no block entity at {pos}");
        return (pos, entity);
    }
}
