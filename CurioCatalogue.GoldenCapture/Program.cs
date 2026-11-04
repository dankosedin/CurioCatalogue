using System.Text;
using CurioCatalogue;
using CurioCatalogue.GoldenCapture;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;

// B1 spike (docs/plans/2026-11-04-backward-compat.md): the test server with our mod of the release zip; one ledger made the way a
// player makes it (crafted in the grid, an intake list made from a chest, the list posted with the book in the off-hand); then
// what its bytes hold. Run: dotnet run --project CurioCatalogue.GoldenCapture

var release = new OtherMod("curiocatalogue", TestPaths.Get("ReleaseVersion"), TestPaths.Get("ReleaseZip"));
var server = new CaptureServer(release);
string dataPath = server.DataPath;
try
{
    Console.WriteLine($"server up in {server.StartTime.TotalSeconds:0.0} s; our mod {server.Api.ModLoader.GetMod("curiocatalogue").Info.Version} from {server.Api.ModLoader.GetMod("curiocatalogue").SourcePath}");
    Console.WriteLine(new Spike(server).Run());
}
finally
{
    server.Dispose();
}
Console.WriteLine($"data folder kept: {dataPath}");

namespace CurioCatalogue.GoldenCapture
{
    sealed class CaptureServer(OtherMod release) : GameServerFixture([], release, keepData: true);

    sealed class Spike(GameServerFixture server) : BookTestBase(server)
    {
        const int OutputSlot = 9;

        public string Run()
        {
            var client = FakeClient.Join(game, "capturer");
            return game.OnMain(() =>
            {
                var player = client.Player;
                var report = new StringBuilder();

                // 1. A ledger crafted in the grid: book, nails, ink and quill (ledger-fallback, 8 sheets by OnCreatedByCrafting).
                var grid = (InventoryCraftingGrid)player.InventoryManager.GetOwnInventory(Vintagestory.API.Config.GlobalConstants.craftingInvClassName);
                foreach (var (slot, stack) in new[] { (0, Vanilla("metalnailsandstrips-iron")), (3, Vanilla("book-normal-teal")), (4, Vanilla("inkandquill")), (6, Vanilla("metalnailsandstrips-iron")) })
                {
                    grid[slot].Itemstack = stack;
                    grid[slot].MarkDirty();
                }
                var mouse = player.InventoryManager.MouseItemSlot;
                var op = new ItemStackMoveOperation(World, EnumMouseButton.Left, 0, EnumMergePriority.AutoMerge) { ActingPlayer = player };
                grid.ActivateSlot(OutputSlot, mouse, ref op);
                var book = mouse.Itemstack ?? throw new InvalidOperationException($"nothing crafted (recipe {grid.MatchingRecipe?.Name})");
                mouse.Itemstack = null;
                report.AppendLine($"crafted: {Describe(book)}, sheets {ReadBook(book).Sheets}");

                // 2. An intake list from a chest: book in the main hand, Shift + RMB.
                var (pos, chest) = TestBlocks.Place(game, "chest-east");
                var inv = ((IBlockEntityContainer)chest).Inventory;
                inv[0].Itemstack = Vanilla("ingot-copper", 5);
                inv[1].Itemstack = Tapestry("ambush1");
                var hand = player.InventoryManager.ActiveHotbarSlot;
                hand.Itemstack = book;
                Stand(player, pos);
                Click(player, hand, pos);
                var list = TakeOurs(player, "invoice-incoming") ?? throw new InvalidOperationException($"no intake list; inventory: {Inventory(player)}");
                report.AppendLine($"list: {Describe(list)}, lines: {Lines(Mod, list)}; book sheets {ReadBook(hand.Itemstack!).Sheets}");

                // 3. The list posted: book in the off-hand, list in the main hand, Shift + RMB on the chest.
                player.Entity.LeftHandItemSlot.Itemstack = hand.Itemstack;
                hand.Itemstack = list;
                Click(player, hand, pos);
                var posted = player.Entity.LeftHandItemSlot.Itemstack!;
                var ledger = ReadBook(posted);
                report.AppendLine($"posted: book sheets {ledger.Sheets}, entries {string.Join(", ", ledger.Entries.Values.Select(e => $"{e.Key} x{e.Count}"))}; inventory: {Inventory(player)}");

                // 4. What the book's bytes hold.
                byte[] bytes = posted.ToBytes();
                report.AppendLine($"bytes: {bytes.Length}, base64 {Convert.ToBase64String(bytes)}");
                report.AppendLine($"json: {posted.Attributes.ToJsonToken()}");
                report.AppendLine("ids in the stack (walked):");
                foreach (var (path, cls, id, code) in Ids(new ItemStack(bytes), "")) report.AppendLine($"  {path}: {cls} {id} = {code}");

                // 5. What the engine's own mapping walk (a schematic export) finds in the same stack.
                var blocks = new Dictionary<int, AssetLocation>();
                var items = new Dictionary<int, AssetLocation>();
                posted.Collectible.OnStoreCollectibleMappings(World, player.Entity.LeftHandItemSlot, blocks, items);
                report.AppendLine($"OnStoreCollectibleMappings: items [{string.Join(", ", items.Select(p => $"{p.Key}={p.Value}"))}], blocks [{string.Join(", ", blocks.Select(p => $"{p.Key}={p.Value}"))}]");

                // 6. The bytes read back, resolved in this world, read by the mod: the same ledger.
                var back = new ItemStack(bytes);
                back.ResolveBlockOrItem(World);
                var reread = ReadBook(back);
                report.AppendLine($"read back: sheets {reread.Sheets}, entries {reread.Entries.Count}, same bytes {Convert.ToBase64String(back.ToBytes()) == Convert.ToBase64String(bytes)}");

                // 7. The world keeps them: the book in a chest, saved by the stop.
                inv[5].Itemstack = posted.Clone();
                inv[5].MarkDirty();
                report.Append($"book left in the chest at {pos}");
                return report.ToString();
            });
        }

        /// <summary>Every numeric id in a stack read from bytes (unresolved): its own, and those of the stacks in its attributes, arrays of trees included.</summary>
        IEnumerable<(string Path, EnumItemClass Class, int Id, string Code)> Ids(ItemStack stack, string path)
        {
            stack.ResolveBlockOrItem(World);
            yield return (path == "" ? "(the stack)" : path, stack.Class, stack.Id, stack.Collectible?.Code?.ToString() ?? "?");
            foreach (var found in InTree(stack.Attributes, path)) yield return found;
        }

        IEnumerable<(string, EnumItemClass, int, string)> InTree(ITreeAttribute tree, string path)
        {
            foreach (var (key, value) in tree)
            {
                string at = path + "/" + key;
                switch (value)
                {
                    case ITreeAttribute sub:
                        foreach (var found in InTree(sub, at)) yield return found;
                        break;
                    case TreeArrayAttribute array when array.value != null:
                        for (int i = 0; i < array.value.Length; i++)
                            foreach (var found in InTree(array.value[i], $"{at}[{i}]")) yield return found;
                        break;
                    case ItemstackAttribute { value: { } inner }:
                        foreach (var found in Ids(inner, at)) yield return found;
                        break;
                }
            }
        }
    }
}
