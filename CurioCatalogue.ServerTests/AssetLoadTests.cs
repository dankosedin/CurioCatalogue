namespace CurioCatalogue.ServerTests;

/// <summary>
/// The game logs a broken asset and goes on without it (a recipe that fails to parse, an item class that is not registered,
/// a texture or shape it cannot find): nothing crashes, the item is just missing. The server has loaded our assets for the
/// whole run, so its log says whether they loaded cleanly (docs/critical.md, Recipes). The noise that is not ours: <see cref="LogProblems"/>.
/// </summary>
[Collection(ServerCollection.Name)]
public class AssetLoadTests(GameServerFixture game)
{
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
        var problems = LogProblems.Of(game);

        Assert.True(problems.Count == 0, "the server logged problems while loading:\n" + string.Join("\n", problems));
    }
}
