using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace CurioCatalogue.GoldenCapture;

/// <summary>
/// The fixture world of the manual upgrade pass (plan backward-compat, B4; <c>docs/testing/upgrade-0.1-to-0.2.md</c>): after the
/// scenario, the items it made are laid out where a player leaves things, and the world is kept. Written against the release's
/// types like <see cref="Scenario"/>. Returns a text with the place of everything, to be kept next to the world.
/// </summary>
sealed class Fixture(GameServerFixture game, IReadOnlyDictionary<string, ItemStack> made)
{
    IWorldAccessor World => game.Api.World;

    public string Lay(string version) => game.OnMain(() =>
    {
        var spawn = World.DefaultSpawnPosition.AsBlockPos;
        var lines = new List<string> { $"Fixture world of {version}: blocks float {6} blocks above the spawn ({spawn.X} {spawn.Y} {spawn.Z}); fly there in creative.", "" };

        void Report(string what, BlockPos pos, string contents) =>
            lines.Add($"{what,-34} at {pos.X} {pos.Y} {pos.Z} (spawn + {pos.X - spawn.X} {pos.Y - spawn.Y} {pos.Z - spawn.Z}): {contents}");

        // Invoices in a chest, on a shelf, on the ground (the inventory cannot be kept: the capturing player is not the author).
        var (chest, chestEntity) = TestBlocks.Place(game, "chest-east");
        Fill(chestEntity, "intake-list", "pick-list", "want-list");
        Report("chest with the three invoices", chest, "intake list, pick list, want list");

        var (shelf, shelfEntity) = TestBlocks.Place(game, "shelf-normal-east");
        Fill(shelfEntity, "intake-list", "pick-list", "want-list", "ledger");
        Report("shelf with invoices and a ledger", shelf, "intake list, pick list, want list, ledger");

        var (ground, _) = TestBlocks.Place(game, "chest-east");
        foreach (var name in new[] { "intake-list", "pick-list", "want-list" })
            World.SpawnItemEntity(made[name].Clone(), new Vec3d(ground.X + 0.5, ground.Y + 1.2, ground.Z + 0.5));
        Report("the same three invoices on the ground", ground, "next to an empty chest; ground items despawn if left, so look soon");

        // The books, in a chest instead of the inventory.
        var (books, booksEntity) = TestBlocks.Place(game, "chest-east");
        Fill(booksEntity, "ledger", "materials", "recoloured", "aged", "spent", "named", "album-partial", "album-full");
        Report("chest with every book", books, "ledger, bill of materials, recoloured, aged, spent, named, album partial, album full");

        // A chest to post the invoices on.
        var (target, targetEntity) = TestBlocks.Place(game, "chest-east");
        var inv = ((IBlockEntityContainer)targetEntity).Inventory;
        int i = 0;
        foreach (var (code, size) in new[] { ("ingot-copper", 5), ("ingot-tin", 3), ("ingot-iron", 3), ("stick", 4), ("plank-oak", 8) })
            inv[i++].Itemstack = new ItemStack(World.GetItem(new AssetLocation("game", code)), size);
        Report("chest to post the invoices on", target, "5 copper, 3 tin, 3 iron ingots, 4 sticks, 8 oak planks");
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    });

    void Fill(BlockEntity entity, params string[] names)
    {
        var inventory = ((IBlockEntityContainer)entity).Inventory;
        for (int i = 0; i < names.Length; i++)
        {
            inventory[i].Itemstack = made[names[i]].Clone();
            inventory[i].MarkDirty();
        }
        entity.MarkDirty();
    }
}
