using CurioCatalogue.Adapters;
using CurioCatalogue.Core;
using Vintagestory.API.Common;

namespace CurioCatalogue.ServerTests;

/// <summary>Tests call our code with our own types, and those are the types the game's mod loader loaded.</summary>
[Collection(ServerCollection.Name)]
public class OurCodeTests(GameServerFixture game)
{
    [Fact]
    public void The_tests_and_the_game_share_one_copy_of_our_types()
    {
        var (modAssembly, item) = game.OnMain(() =>
        (
            game.Api.ModLoader.GetMod("curiocatalogue").Systems.First().GetType().Assembly,
            game.Api.World.GetItem(new AssetLocation("curiocatalogue", "book-ledger-normal-brickred"))
        ));
        Assert.Same(modAssembly, typeof(ItemLedger).Assembly);
        Assert.IsType<ItemLedger>(item);
        // A second copy of either DLL would make the game's objects foreign to the tests' types.
        foreach (string name in new[] { "curiocatalogue", "CurioCatalogue.Core" })
        {
            var copies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name == name).Select(a => a.Location).ToList();
            Assert.True(copies.Count == 1,$"{name}: {copies.Count} copies loaded: {string.Join(", ", copies)}");
        }
    }

    [Fact]
    public void StackKeys_keys_a_real_book()
    {
        ItemKey key = game.OnMain(() =>
        {
            Item book = game.Api.World.GetItem(new AssetLocation("curiocatalogue", "book-ledger-normal-brickred"))
                ?? throw new InvalidOperationException("no ledger book");
            return new StackKeys(game.Api.World, []).KeyOf(new ItemStack(book));
        });
        Assert.Equal(new ItemKey(CollectibleKind.Item, "curiocatalogue:book-ledger-normal-brickred", ""), key);
    }
}
