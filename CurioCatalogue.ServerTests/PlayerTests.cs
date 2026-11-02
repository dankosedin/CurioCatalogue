using CurioCatalogue.Adapters;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace CurioCatalogue.ServerTests;

/// <summary>A real player on the server without a game client (<see cref="FakeClient"/>).</summary>
[Collection(ServerCollection.Name)]
public class PlayerTests(GameServerFixture game)
{
    [Fact]
    public void A_player_joins_with_inventories_rights_and_a_position()
    {
        var client = FakeClient.Join(game, "joiner");

        var seen = game.OnMain(() =>
        {
            IServerPlayer player = client.Player;
            return (player.ConnectionState, player.PlayerName, player.WorldData.CurrentGameMode,
                Hotbar: player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName)?.Count,
                Backpack: player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName)?.Count,
                Spawned: game.Api.World.GetEntityById(player.Entity.EntityId) == player.Entity,
                CanBuild: player.HasPrivilege(Privilege.buildblocks),
                Online: game.Api.World.AllOnlinePlayers.Contains(player));
        });

        Assert.Equal((EnumClientState.Playing, "joiner", EnumGameMode.Creative, (int?)12, (int?)4, true, true, true), seen);
    }

    [Fact]
    public void Our_channel_takes_a_request_from_the_player()
    {
        var client = FakeClient.Join(game, "renamer");
        var mod = game.OnMain(() => game.Api.ModLoader.GetModSystem<CurioCatalogueModSystem>());
        int fingerprint = game.OnMain(() =>
        {
            var slot = client.Player.InventoryManager.ActiveHotbarSlot;
            slot.Itemstack = new ItemStack(game.Api.World.GetItem(new AssetLocation("curiocatalogue", "book-ledger-normal-brickred")));
            slot.MarkDirty();
            return LedgerStore.Fingerprint(slot.Itemstack);
        });

        client.SendToMod(new RenameLedgerRequest
        {
            Name = "from the channel",
            Book = new ShownItem { HotbarSlot = 0, Fingerprint = fingerprint },
        });

        client.WaitFor(() =>
        {
            var stack = client.Player.InventoryManager.ActiveHotbarSlot.Itemstack!;
            return mod.Store.ReadLedger(stack, ((ItemLedger)stack.Collectible).LedgerMode).Name == "from the channel";
        }, "the ledger to be renamed");
    }
}
