using System.Diagnostics;
using System.Collections.Immutable;
using System.Text;
using CurioCatalogue.Adapters;
using CurioCatalogue.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;
using Xunit.Abstractions;

namespace CurioCatalogue.ServerTests;

/// <summary>
/// C6 of the collections-design plan: what it costs to read a book. Everything that reads a whole album runs through
/// <c>LedgerStore.ReadAlbum</c>: the tooltip (<c>GetHeldItemInfo</c>), the crafting checks (<c>MatchesForCrafting</c>, every time
/// the grid changes), and the open window (<c>Fingerprint</c> every 500 ms, a read when the data changed). The collection grows
/// by the config's <c>TapestryAdd</c> (88 real pieces + made-up names), so the whole path is the real one: registry, store, core.
/// The client's drawing is not here (A1 of the album-view plan). The numbers are a median of many runs on the main thread of the
/// test server (a Release-like JIT is not claimed: this is the Debug build the tests use), written to the test output; the
/// asserted budgets are loose, to catch a read that turns quadratic, not to rate the machine.
/// </summary>
[Collection(ServerCollection.Name)]
public class ReadingCostTests(GameServerFixture server, ITestOutputHelper output) : BookTestBase(server)
{
    static string[] Extra(int count) => Enumerable.Range(0, count).Select(i => $"cost-piece-{i}").ToArray();

    /// <summary>Median of <paramref name="runs"/> runs of <paramref name="action"/>, in milliseconds (after a warm-up).</summary>
    static double Ms(int runs, Action action)
    {
        for (int i = 0; i < Math.Max(3, runs / 10); i++) action();
        var times = new double[runs];
        for (int i = 0; i < runs; i++)
        {
            long start = Stopwatch.GetTimestamp();
            action();
            times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
        Array.Sort(times);
        return times[runs / 2];
    }

    static InventoryCraftingGrid Grid(IServerPlayer player) =>
        (InventoryCraftingGrid)player.InventoryManager.GetOwnInventory(GlobalConstants.craftingInvClassName);

    /// <summary>An album of the collection with the first <paramref name="collected"/> members collected (format 2 stores what it collected).</summary>
    ItemStack AlbumWith(DefinedCollection collection, int collected)
    {
        var stack = Ours("book-album-normal-brickred");
        var members = collection.Levels[0].Order.Take(collected);
        Mod.Store.WriteAlbum(stack, Core.Album.New(collection, 8, "").Collect(members.Select(m => (m.Id, new FilledBy(m.Fillers[0], m.Stacks[0])))));
        return stack;
    }

    [Theory]
    [InlineData(0)]   // 88: the real tapestries
    [InlineData(81)]  // 169: the butterflies' number (C3)
    [InlineData(312)] // 400: the order of ore crystals' levels together (C1)
    public void Reading_an_album_stays_cheap_as_the_collection_grows(int extra)
    {
        var client = FakeClient.Join(game, "cost-" + extra);
        var report = new StringBuilder();
        double budget = 0;

        game.OnMain(() => WithConfig(c => c.Collections["tapestries"].Add = Extra(extra), () =>
        {
            var collection = Mod.Collections.ForSample(Tapestry("ambush1"))!;
            int members = collection.Levels[0].Members.Count;
            // The largest album is the one that collected everything.
            var full = AlbumWith(collection, members);
            var half = AlbumWith(collection, members / 2);
            var empty = Ours("book-ledger-normal-teal");
            var info = new StringBuilder();

            int bytesFull = ((Vintagestory.API.Datastructures.TreeAttribute)full.Attributes.GetTreeAttribute(LedgerStore.RootKey)).ToBytes().Length;
            double read = Ms(200, () => Mod.Store.ReadAlbum(full));
            double readHalf = Ms(200, () => Mod.Store.ReadAlbum(half));
            double tooltip = Ms(200, () => { info.Clear(); full.Collectible.GetHeldItemInfo(new DummySlot(full), info, World, false); });
            double fingerprint = Ms(200, () => LedgerStore.Fingerprint(full));
            // What the game itself does with the stack while the tooltip is up: GuiElementItemstackInfo.SetSourceSlot compares the
            // hovered stack with its copy (ItemStack.Equals over all attributes) and, on a change, clones it (vsapi
            // GuiElementItemstackInfo.cs:252-270). Ours is the largest tree it meets.
            var copy = full.Clone();
            bool same = full.Equals(World, copy, GlobalConstants.IgnoredStackAttributes); double equals = Ms(200, () => full.Equals(World, copy, GlobalConstants.IgnoredStackAttributes));
            double clone = Ms(200, () => full.Clone());
            double progress =Ms(200, () => Mod.Store.ReadAlbum(full).Progress(collection.Levels[0]));

            // The grid: an album put into a slot makes the grid ask every recipe with a ledger ingredient (MatchesForCrafting).
            var grid = Grid(client.Player);
            double InGrid(ItemStack stack) => Ms(100, () =>
            {
                grid[4].Itemstack = stack;
                grid[4].MarkDirty();
                grid[4].Itemstack = null;
                grid[4].MarkDirty();
            });
            double gridEmpty = InGrid(empty), gridAlbum = InGrid(full);

            report.AppendLine($"{members} members, album {bytesFull / 1024.0:F0} KB: read {read:F2} ms (half collected {readHalf:F2}), tooltip {tooltip:F2}, " +
                              $"fingerprint {fingerprint:F2}, game compare of the stack with its own clone {equals:F2} (equal: {same}), clone {clone:F2}, read+progress {progress:F2}; grid put+clear: empty ledger {gridEmpty:F2}, album {gridAlbum:F2}");
            budget = new[] { read, tooltip, fingerprint, gridAlbum, clone }.Max();
            return 0;
        }));

        output.WriteLine(report.ToString());
        Assert.True(budget < 100, $"a read of a book takes {budget:F1} ms");
    }
}
