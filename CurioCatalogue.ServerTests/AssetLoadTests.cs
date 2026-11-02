using System.Text.RegularExpressions;
using Vintagestory.API.Common;

namespace CurioCatalogue.ServerTests;

/// <summary>
/// The game logs a broken asset and goes on without it (a recipe that fails to parse, an item class that is not registered,
/// a texture or shape it cannot find): nothing crashes, the item is just missing. The server has loaded our assets for the
/// whole run, so its log says whether they loaded cleanly (docs/critical.md, Recipes).
/// </summary>
[Collection(ServerCollection.Name)]
public class AssetLoadTests(GameServerFixture game)
{
    // Vanilla noise that is not ours and that we cannot fix: a regex per line, each with the reason and the game version
    // it was seen in. A start on 1.22.7 with the game's own mods logs none (checked 02.11.2026): every other warning or error
    // is ours to look at.
    static readonly Regex[] KnownNoise =
    [
        // 1.22.7: a player joining (FakeClient) makes the server send its spawn chunks in one tick; the next tick logs this.
        new(@"^Server overloaded\. A tick took \d+ms to complete\.$"),
    ];

    static bool IsProblem(LogLine line) => line.Type is EnumLogType.Warning or EnumLogType.Error or EnumLogType.Fatal;

    [Fact]
    public void The_log_is_captured_from_the_start()
    {
        // A subscription that came too late, or none, would make the next test pass on an empty list.
        var texts = game.LogEntries().Select(l => l.Text).ToList();
        Assert.Contains(texts, t => t.Contains("Entering runphase RunGame"));
        Assert.Contains(texts, t => t.Contains("curiocatalogue", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Our_assets_load_without_warnings_or_errors()
    {
        var problems = game.LogEntries().Where(IsProblem).Where(l => !KnownNoise.Any(n => n.IsMatch(l.Text))).ToList();

        Assert.True(problems.Count == 0, "the server logged problems while loading:\n" + string.Join("\n", problems));
    }
}
