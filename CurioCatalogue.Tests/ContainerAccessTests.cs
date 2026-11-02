using NSubstitute;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using CurioCatalogue.Adapters;

namespace CurioCatalogue.Tests;

/// <summary>
/// The logic of the access check with mocked claims and a mocked lock. The engine's answers (a claim, a shared claim, a padlock,
/// two real players) run on a real server: <c>ServerTests/ContainerTests</c>.
/// </summary>
public class ContainerAccessTests
{
    private readonly GameWorld game = new();
    // IPlayer cannot be mocked or implemented outside the game: it has an internal member (vsapi IPlayer.cs:124#IsInInteractionRangeOf).
    // Our code only passes the player on to claims and locks, which are mocked, so a null stands in for it.
    private readonly IPlayer player = null!;
    private readonly BlockPos pos = new(10, 20, 30);
    private bool locked;
    private readonly ContainerAccess access;

    public ContainerAccessTests()
    {
        access = new ContainerAccess(game.World, (_, _) => locked);
    }

    private void Claim(EnumBlockAccessFlags flag, EnumWorldAccessResponse response) =>
        game.Claims.TestAccess(player, pos, flag).Returns(response);

    [Fact]
    public void Use_access_is_enough()
    {
        game.Place(pos, new TestChest());
        Assert.True(access.CanUse(player, pos));
    }

    [Fact]
    public void Only_use_access_is_asked_traverse_is_the_engines_business()
    {
        // Inside TestAccess the engine itself turns Use into Traverse for blocks that are IClaimTraverseable and allow it
        // (chests, baskets, vessels), and any flag other than Use is checked as build-or-break (decompiled
        // Vintagestory.Common.WorldMap: GetBlockingLandClaimant ~326-329, testBlockAccessInternal ~243-254). So the mock
        // answers for Use alone, as the engine would; the real server agrees (ServerTests/ContainerTests):
        // a claim that lets the visitor "Use" opens the chest to them.
        game.Place(pos, new TestChest());
        Claim(EnumBlockAccessFlags.Use, EnumWorldAccessResponse.NoPrivilege);
        Claim(EnumBlockAccessFlags.Traverse, EnumWorldAccessResponse.Granted);

        Assert.False(access.CanUse(player, pos));
        game.Claims.DidNotReceive().TestAccess(Arg.Any<IPlayer>(), Arg.Any<BlockPos>(), EnumBlockAccessFlags.Traverse);
    }

    [Fact]
    public void Locked_container_is_refused_even_with_claim_access()
    {
        game.Place(pos, new TestChest());
        locked = true;
        Assert.False(access.CanUse(player, pos));
    }
}
