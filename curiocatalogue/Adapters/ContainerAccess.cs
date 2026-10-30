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

    // Use access, as the engine asks for opening a container. For blocks that are IClaimTraverseable and allow it
    // (chests, baskets, vessels) TestAccess itself accepts traverse rights instead (decompiled WorldMap.GetBlockingLandClaimant).
    // Other protection mods hook OnTestBlockAccess, which TestAccess calls (modding.md, Access).
    private bool ClaimAllows(IPlayer player, BlockPos pos) =>
        world.Claims.TestAccess(player, pos, EnumBlockAccessFlags.Use) == EnumWorldAccessResponse.Granted;
}
