using Vintagestory.API.Common;
using Xunit.Abstractions;

namespace CurioCatalogue.ServerTests;

/// <summary>The fixture works: the server runs our mod, has our items, and its slots take them.</summary>
[Collection(ServerCollection.Name)]
public class SmokeTests(GameServerFixture game, ITestOutputHelper output)
{
    [Fact]
    public void Our_mod_is_enabled()
    {
        output.WriteLine($"data {game.DataPath}, port {game.Port}, started in {game.StartTime.TotalSeconds:n1} s");
        Assert.True(game.OnMain(() => game.Api.ModLoader.IsModEnabled("curiocatalogue")));
    }

    [Theory]
    [InlineData("book-ledger-normal-brickred", "ItemLedger")]
    [InlineData("book-materials-aged-teal", null)] // no such cover: aged-* has no teal (itemtypes/book.json)
    [InlineData("book-album-aged-purpleorange", "ItemLedger")]
    [InlineData("invoice-wantlist", "ItemInvoice")]
    public void Our_items_are_registered_with_our_classes(string code, string? itemClass)
    {
        // A class missing from RegisterItemClass leaves a plain Item and logs "no such class registered".
        string? actual = game.OnMain(() => game.Api.World.GetItem(new AssetLocation("curiocatalogue", code))?.GetType().Name);
        Assert.Equal(itemClass, actual);
    }

    [Fact]
    public void A_book_goes_into_a_real_slot()
    {
        var (moved, code) = game.OnMain(() =>
        {
            Item book = game.Api.World.GetItem(new AssetLocation("curiocatalogue", "book-ledger-normal-brickred"))
                ?? throw new InvalidOperationException("no ledger book");
            var inventory = new DummyInventory(game.Api, 1);
            var source = new DummySlot(new ItemStack(book));
            int moved = source.TryPutInto(game.Api.World, inventory[0]);
            return (moved, inventory[0].Itemstack?.Collectible.Code.ToString());
        });
        Assert.Equal(1, moved);
        Assert.Equal("curiocatalogue:book-ledger-normal-brickred", code);
    }
}
