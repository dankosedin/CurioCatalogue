using System.Text.Encodings.Web;
using System.Text.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace CurioCatalogue.GoldenCapture;

// The format of a golden set (Decisions of the plan, "B1: the answers", Format and Ids). The golden tests start from Bytes, replace
// each id by the id its code has in the test world (Ids), and compare what the current code reads with Inputs. Attributes is for
// people and never parsed (ToJsonToken is not strict JSON).

/// <summary>One golden set: the stacks of one release, and what they were made with.</summary>
sealed record GoldenSet(string About, string GameVersion, string ModVersion, string ZipSha256, string Captured, IReadOnlyList<GoldenStack> Stacks);

/// <summary>
/// One stack as the release wrote it: its code, its bytes (<c>ItemStack.ToBytes</c>, base64), the code of every numeric id it holds
/// (<c>"item:4505"</c>, <c>"block:2053"</c>; nested stacks and those in tree arrays included), its attributes as text for people, and
/// what went into it.
/// </summary>
sealed record GoldenStack(string Name, string Code, string Bytes, SortedDictionary<string, string> Ids, string Attributes, Inputs Inputs);

/// <summary>What went into an item, written by the scenario (never read back from the item): the expected values of the golden tests.</summary>
/// <param name="Made">How it was made, for people.</param>
/// <param name="Code">The item's code the way of making gives (the cover is part of it).</param>
sealed record Inputs(string Made, string Code, BookIn? Book = null, InvoiceIn? Invoice = null);

/// <param name="Mode">A <c>LedgerMode</c>: Types, Counts, Album.</param>
/// <param name="Entries">Sorted by code and type; for an album, the members still missing.</param>
sealed record BookIn(string Mode, int Sheets, string Name, IReadOnlyList<EntryIn> Entries, AlbumIn? Album = null);

/// <param name="Collected">The members taken in, sorted; with <see cref="BookIn.Entries"/>, every member of the collection.</param>
sealed record AlbumIn(string Collection, IReadOnlyList<EntryIn> Collected, bool Full);

/// <param name="Kind">An <c>InvoiceKind</c>: Incoming, Outgoing, WantList.</param>
/// <param name="Lines">In the invoice's order.</param>
sealed record InvoiceIn(string Kind, IReadOnlyList<EntryIn> Lines);

/// <summary>An item as the books know it here: its code and its <c>type</c> attribute (a tapestry's piece), and how many.</summary>
sealed record EntryIn(string Code, string? Type, int Count) : IComparable<EntryIn>
{
    public static EntryIn Of(ItemStack stack, int count) => new(stack.Collectible.Code.ToString(), stack.Attributes.GetString("type"), count);

    public int CompareTo(EntryIn? other) => other == null ? 1
        : string.CompareOrdinal(Code, other.Code) is var c and not 0 ? c
        : string.CompareOrdinal(Type, other.Type) is var t and not 0 ? t
        : Count.CompareTo(other.Count);

    public override string ToString() => $"{Code}{(Type == null ? "" : ":" + Type)} x{Count}";
}

static class Golden
{
    public const string StacksFile = "stacks.json";
    public const string ConfigFile = "curiocatalogue.json";

    const string About = "Golden stacks: items made by this release of the mod in a test server, read by UpgradeTests (docs/development.md, Golden stacks). " +
        "Bytes: ItemStack.ToBytes, base64, with the numeric ids of the capture's world; Ids: the code of each of them. Inputs: what went into each item, the expected values. " +
        "Attributes: for people only. Never edit or capture a set again.";

    static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        NewLine = "\n",
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // names in Cyrillic stay readable
    };

    /// <summary>The stack as it is now, for the set (main thread: the ids are resolved in the world).</summary>
    public static GoldenStack Of(IWorldAccessor world, string name, ItemStack stack, Inputs inputs)
    {
        byte[] bytes = stack.ToBytes();
        var ids = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (cls, id, code) in IdsIn(world, new ItemStack(bytes)))
        {
            string key = $"{cls.ToString().ToLowerInvariant()}:{id}";
            if (ids.TryGetValue(key, out var known) && known != code) throw new InvalidOperationException($"{name}: {key} is both {known} and {code}");
            ids[key] = code;
        }
        return new GoldenStack(name, stack.Collectible.Code.ToString(), Convert.ToBase64String(bytes), ids, stack.Attributes.ToJsonToken(), inputs);
    }

    /// <summary>
    /// Every numeric id in a stack read from bytes: its own, and those of the stacks in its attributes (trees, arrays of trees,
    /// stack attributes), resolved here only to name them. The golden reader walks the same places (docs/modding.md, Stacks and item identity).
    /// </summary>
    static IEnumerable<(EnumItemClass Class, int Id, string Code)> IdsIn(IWorldAccessor world, ItemStack stack)
    {
        stack.ResolveBlockOrItem(world);
        yield return (stack.Class, stack.Id, stack.Collectible?.Code?.ToString() ?? throw new InvalidOperationException($"{stack.Class} {stack.Id} is not in this world"));
        foreach (var found in InTree(world, stack.Attributes)) yield return found;
    }

    static IEnumerable<(EnumItemClass, int, string)> InTree(IWorldAccessor world, ITreeAttribute tree)
    {
        foreach (var (_, value) in tree)
        {
            var found = value switch
            {
                ITreeAttribute sub => InTree(world, sub),
                TreeArrayAttribute { value: { } array } => array.SelectMany(t => InTree(world, t)),
                ItemstackAttribute { value: { } inner } => IdsIn(world, inner),
                _ => [],
            };
            foreach (var id in found) yield return id;
        }
    }

    public static void Write(string folder, GameServerFixture server, IReadOnlyList<GoldenStack> stacks, byte[] config)
    {
        var set = new GoldenSet(About, GameVersion.ShortGameVersion, server.Api.ModLoader.GetMod("curiocatalogue").Info.Version,
            TestPaths.Get("ReleaseSha256").ToLowerInvariant(), DateTime.Now.ToString("yyyy-MM-dd"), stacks);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, StacksFile), JsonSerializer.Serialize(set, Json) + "\n");
        File.WriteAllBytes(Path.Combine(folder, ConfigFile), config); // untouched
    }
}
