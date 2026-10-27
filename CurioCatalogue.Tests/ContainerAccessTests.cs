using NSubstitute;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using CurioCatalogue.Adapters;

namespace CurioCatalogue.Tests;

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
    public void Without_use_access_a_traverseable_container_needs_traverse_access()
    {
        game.Place(pos, new TraverseableChest());
        Claim(EnumBlockAccessFlags.Use, EnumWorldAccessResponse.NoPrivilege);

        Claim(EnumBlockAccessFlags.Traverse, EnumWorldAccessResponse.Granted);
        Assert.True(access.CanUse(player, pos));

        Claim(EnumBlockAccessFlags.Traverse, EnumWorldAccessResponse.NoPrivilege);
        Assert.False(access.CanUse(player, pos));
    }

    [Fact]
    public void Traverse_access_is_not_enough_for_other_containers()
    {
        // E.g. a crate: BlockEntityCrate does not implement IClaimTraverseable (vssurvivalmod BECrate.cs:15#BlockEntityCrate).
        game.Place(pos, new TestChest());
        Claim(EnumBlockAccessFlags.Use, EnumWorldAccessResponse.NoPrivilege);
        Claim(EnumBlockAccessFlags.Traverse, EnumWorldAccessResponse.Granted);
        Assert.False(access.CanUse(player, pos));
    }

    [Fact]
    public void Locked_container_is_refused_even_with_claim_access()
    {
        game.Place(pos, new TestChest());
        locked = true;
        Assert.False(access.CanUse(player, pos));
    }
}
