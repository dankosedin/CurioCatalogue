using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace CurioCatalogue.Adapters;

/// <summary>
/// May the player use the container at this position? Our item handles the click before the block does,
/// so the game's own checks do not run (modding.md, Access); this repeats them.
/// </summary>
public sealed class ContainerAccess
{
    private readonly IWorldAccessor world;
    private readonly System.Func<BlockPos, IPlayer, bool> isLocked;

    /// <param name="isLocked">Lock or reinforcement check; in game ModSystemBlockReinforcement.IsLockedForInteract.</param>
    public ContainerAccess(IWorldAccessor world, System.Func<BlockPos, IPlayer, bool> isLocked)
    {
        this.world = world;
        this.isLocked = isLocked;
    }

    public bool CanUse(IPlayer player, BlockPos pos) => ClaimAllows(player, pos) && !isLocked(pos, player);

    // Assumption: like the engine, Use access is required, but Traverse is enough for blocks that implement
    // IClaimTraverseable (chests, baskets, vessels) and allow it (vsapi Common/IClaimTraverseable.cs).
    // Other protection mods hook OnTestBlockAccess, which TestAccess calls (modding.md, Access).
    private bool ClaimAllows(IPlayer player, BlockPos pos)
    {
        if (world.Claims.TestAccess(player, pos, EnumBlockAccessFlags.Use) == EnumWorldAccessResponse.Granted) return true;
        return IsTraverseable(pos)
            && world.Claims.TestAccess(player, pos, EnumBlockAccessFlags.Traverse) == EnumWorldAccessResponse.Granted;
    }

    private bool IsTraverseable(BlockPos pos)
    {
        var block = world.BlockAccessor.GetBlock(pos);
        var be = world.BlockAccessor.GetBlockEntity(pos);
        object?[] candidates = [block, .. block?.BlockBehaviors ?? [], be, .. be?.Behaviors ?? []];
        return candidates.OfType<IClaimTraverseable>().Any(t => t.AllowTraverse());
    }
}
