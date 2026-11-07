using System.Text.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace CurioCatalogue.TestServer;

// The reader of a golden set (docs/tests.md, Golden stacks; plan backward-compat, Decisions "Ids"). The format is the one
// CurioCatalogue.GoldenCapture/Golden.cs writes; the records here are its twin. Both use only the game's types, so they could be one
// file; kept apart until the capture is rewritten for 0.2 shows what the format needs (ROADMAP.md). An unknown field fails the load.

/// <summary>One golden set: the stacks of one release, and what they were made with.</summary>
internal sealed record GoldenSet(string About, string GameVersion, string ModVersion, string ZipSha256, string Captured, IReadOnlyList<GoldenStack> Stacks);

/// <param name="Bytes"><c>ItemStack.ToBytes</c>, base64, with the numeric ids of the capture's world.</param>
/// <param name="Ids">The code of every numeric id the bytes hold (<c>"item:4505"</c>, <c>"block:2053"</c>), nested stacks included.</param>
/// <param name="Attributes">For people; never read.</param>
internal sealed record GoldenStack(string Name, string Code, string Bytes, Dictionary<string, string> Ids, string Attributes, GoldenInputs Inputs);

/// <summary>What went into an item: the expected values, written by the capture's scenario, never read back from the item.</summary>
internal sealed record GoldenInputs(string Made, string Code, GoldenBook? Book = null, GoldenInvoice? Invoice = null);

internal sealed record GoldenBook(string Mode, int Sheets, string Name, IReadOnlyList<GoldenEntry> Entries, GoldenAlbum? Album = null);

internal sealed record GoldenAlbum(string Collection, IReadOnlyList<GoldenEntry> Collected, bool Full);

internal sealed record GoldenInvoice(string Kind, IReadOnlyList<GoldenEntry> Lines);

internal sealed record GoldenEntry(string Code, string? Type, int Count)
{
    public override string ToString() => $"{Code}{(Type == null ? "" : ":" + Type)} x{Count}";
}

internal static class GoldenStacks
{
    // Disallow: a field a later capture writes (a new input) and these records do not know fails the load instead of going unchecked.
    static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
    };

    /// <summary>The folder of the sets (<c>CurioCatalogue.ServerTests/Golden</c>), written into the test assembly by its project file.</summary>
    static string Root => TestPaths.Get("GoldenRoot");

    /// <summary>The versions that have a set, e.g. "0.1.1".</summary>
    public static IReadOnlyList<string> Versions() => Directory.GetDirectories(Root, "v*").Select(d => Path.GetFileName(d)[1..]).Order().ToList();

    static string Folder(string version) => Path.Combine(Root, "v" + version);

    public static GoldenSet Load(string version) =>
        JsonSerializer.Deserialize<GoldenSet>(File.ReadAllText(Path.Combine(Folder(version), "stacks.json")), Json)
        ?? throw new InvalidOperationException($"golden set {version} is empty");

    /// <summary>The config file the release wrote, as text.</summary>
    public static string ConfigText(string version) => File.ReadAllText(Path.Combine(Folder(version), "curiocatalogue.json"));

    /// <summary>
    /// The stack as the current code meets it: read from the bytes, and every numeric id of the capture's world replaced by the id
    /// the same code has in this world, in the stack, in the stacks nested in its attributes, in tree arrays. A world keeps its ids
    /// for ever, so a player's book always meets its own world's ids; this emulates that (docs/modding.md, Stacks and item identity).
    /// Main thread (the ids are looked up in the world).
    /// </summary>
    public static ItemStack Read(IWorldAccessor world, GoldenStack golden)
    {
        var stack = new ItemStack(Convert.FromBase64String(golden.Bytes));
        Replace(world, stack, golden.Ids, golden.Name);
        // Only the stack itself is resolved, as a world resolves a slot's stack when it loads (vsapi InventoryBase.cs:290); the
        // stacks nested in its attributes stay unresolved, as the game leaves them (TreeAttribute.cs:736): the current code must resolve them.
        stack.ResolveBlockOrItem(world);
        return stack;
    }

    static void Replace(IWorldAccessor world, ItemStack stack, Dictionary<string, string> ids, string name)
    {
        string key = $"{stack.Class.ToString().ToLowerInvariant()}:{stack.Id}";
        if (!ids.TryGetValue(key, out var code)) throw new InvalidOperationException($"{name}: the set has no code for {key}");
        var location = new AssetLocation(code);
        CollectibleObject collectible = (stack.Class == EnumItemClass.Block ? world.GetBlock(location) : (CollectibleObject?)world.GetItem(location))
            ?? throw new InvalidOperationException($"{name}: {code} ({key}) is not in this world");
        stack.Id = collectible.Id;
        InTree(world, stack.Attributes, ids, name);
    }

    static void InTree(IWorldAccessor world, ITreeAttribute tree, Dictionary<string, string> ids, string name)
    {
        foreach (var (_, value) in tree)
        {
            switch (value)
            {
                case ITreeAttribute sub: InTree(world, sub, ids, name); break;
                case TreeArrayAttribute { value: { } array }: foreach (var item in array) InTree(world, item, ids, name); break;
                case ItemstackAttribute { value: { } inner }: Replace(world, inner, ids, name); break;
            }
        }
    }
}
