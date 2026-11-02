using CurioCatalogue.Adapters;
using Vintagestory.API.Common;

namespace CurioCatalogue.CompatTests;

/// <summary>
/// A11: a tapestry another mod adds to the game's tapestry list (the attribute <c>sizes</c> of <c>game:tapestry</c>) is a member of the
/// album's collection. No real mod does it today (Craftable Companion and Astria Porta add blocks of their own, which the album does
/// not count; plan 2026-11-02-server-tests, T17), so our test mod <c>cctapestry</c> (<c>testmods/</c>) patches in a 2×1 picture.
/// </summary>
[Collection(CompatCollection.Name)]
public class ModdedTapestryTests(CompatServerFixture server) : BookTestBase(server)
{
    const string Picture = "cctestpicture";

    [Fact]
    public void The_patched_picture_is_in_the_collection()
    {
        var pieces = game.OnMain(() => Mod.Collections.Get(CollectionRegistry.TapestriesId)!.Members
            .Where(m => m.Group == Picture)
            .Select(m => ((ItemStack)m.Sample).Attributes.GetString("type"))
            .ToList());
        Assert.Equal([Picture + "1", Picture + "2"], pieces);
    }

    [Fact]
    public void A_new_album_misses_the_patched_pieces_and_takes_one_from_a_chest()
    {
        var client = FakeClient.Join(game, "cc-tapestry");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Tapestry(Picture + "2");
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Album(8);
            bool missingBefore = Missing(hand.Itemstack).Contains(Picture + "2");

            Click(player, hand, pos);

            return (missingBefore, MissingAfter: Missing(hand.Itemstack).Contains(Picture + "2"), Inventory(player), Contents(chest));
        });
        Assert.Equal((true, false, $"tapestry-north:{Picture}2 x1", ""), seen);
    }

    /// <summary>The album's missing pieces: their keys' attributes, one per line (a key holds the tapestry's <c>type</c>).</summary>
    string Missing(ItemStack album) =>
        string.Join("\n", Mod.Store.ReadLedger(album, ((ItemLedger)album.Collectible).LedgerMode).Entries.Select(e => e.Key.Attributes));
}
