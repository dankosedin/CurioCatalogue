using System.Reflection;
using NSubstitute;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace CurioCatalogue.Tests;

/// <summary>
/// A tiny game world behind a mocked <see cref="IWorldAccessor"/> and <see cref="ICoreAPI"/>: a registry of
/// blocks and items, block entities at positions, land claims and a calendar. Everything not set up returns defaults.
/// </summary>
internal sealed class GameWorld
{
    public IWorldAccessor World { get; } = Substitute.For<IWorldAccessor>();
    public ICoreAPI Api { get; } = Substitute.For<ICoreAPI>();
    public IBlockAccessor Blocks { get; } = Substitute.For<IBlockAccessor>();
    public ILandClaimAPI Claims { get; } = Substitute.For<ILandClaimAPI>();
    public IGameCalendar Calendar { get; } = Substitute.For<IGameCalendar>();

    private int nextId = 1;

    public GameWorld()
    {
        World.BlockAccessor.Returns(Blocks);
        World.Claims.Returns(Claims);
        World.Calendar.Returns(Calendar);
        World.Api.Returns(Api);
        World.Side.Returns(EnumAppSide.Server);
        World.Logger.Returns(Substitute.For<ILogger>());
        Api.World.Returns(World);
        Api.Side.Returns(EnumAppSide.Server);
        Api.Logger.Returns(World.Logger);
        // Everything is accessible unless a test says otherwise.
        Claims.TestAccess(default!, default!, default).ReturnsForAnyArgs(EnumWorldAccessResponse.Granted);
    }

    // Assumption: a stack read from bytes knows only its class and numeric id and finds its block or item
    // through world.GetBlock(id) / world.GetItem(id) (vsapi ItemStack.cs:354#ResolveBlockOrItem, ResolveBlockOrItem).

    public Block Block(string code)
    {
        var block = new Block { Code = AssetLocation.Create(code, "game"), BlockId = nextId++ };
        Register(block);
        World.GetBlock(block.BlockId).Returns(block);
        World.GetBlock(block.Code).Returns(block);
        return block;
    }

    public Item Item(string code, Item? item = null)
    {
        item ??= new Item();
        item.Code = AssetLocation.Create(code, "game");
        item.ItemId = nextId++;
        Register(item);
        World.GetItem(item.ItemId).Returns(item);
        World.GetItem(item.Code).Returns(item);
        return item;
    }

    public void Place(BlockPos pos, BlockEntity? be, Block? block = null)
    {
        Blocks.GetBlockEntity(pos).Returns(be);
        if (block != null) Blocks.GetBlock(pos).Returns(block);
    }

    // The game sets a collectible's api in OnLoaded, which needs a full game; comparing stacks uses it
    // (vsapi Collectible.cs:3387#api.World, Equals → api.World). Set just the field.
    private static readonly FieldInfo ApiField =
        typeof(CollectibleObject).GetField("api", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private void Register(CollectibleObject collectible) => ApiField.SetValue(collectible, Api);
}
