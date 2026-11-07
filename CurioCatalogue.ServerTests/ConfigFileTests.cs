using System.Reflection;
using System.Text.Json.Nodes;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace CurioCatalogue.ServerTests;

/// <summary>
/// G1 of <c>docs/testing/functional.md</c>: the wiring of <c>CurioCatalogueModSystem.LoadConfig</c> (the corrections of
/// <c>Correct()</c> have unit tests). The mod's loader is called again on the running server, by reflection, with a config file
/// the test wrote; the file is put back afterwards. The mod's own <c>Config</c> is not touched (LoadConfig returns a new object).
/// The warnings the mod logs on purpose are withdrawn from the log, so <see cref="AssetLoadTests"/> does not see them.
/// </summary>
[Collection(ServerCollection.Name)]
public class ConfigFileTests(GameServerFixture game)
{
    static string FilePath => Path.Combine(GamePaths.ModConfig, CurioCatalogueModSystem.ConfigFileName);

    /// <summary>Writes <paramref name="text"/> as the config file, runs the mod's loader, and returns what it made, what it logged (all of it, and the problems without the known noise) and what the file says now.</summary>
    (CurioCatalogueConfig Config, IReadOnlyList<LogLine> Log, List<LogLine> Problems, string File) Load(string text)
    {
        string? original = File.Exists(FilePath) ? File.ReadAllText(FilePath) : null; // none if the mod's start did not write it
        int mark = game.LogMark();
        try
        {
            Directory.CreateDirectory(GamePaths.ModConfig);
            File.WriteAllText(FilePath, text);
            var config = game.OnMain(() => (CurioCatalogueConfig)typeof(CurioCatalogueModSystem)
                .GetMethod("LoadConfig", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [game.Api])!);
            return (config, game.LogSince(mark), LogProblems.Since(game, mark), File.ReadAllText(FilePath));
        }
        finally
        {
            if (original != null) File.WriteAllText(FilePath, original);
            else File.Delete(FilePath);
        }
    }

    [Fact]
    public void Impossible_values_are_corrected_logged_and_written_back()
    {
        var (config, log, _, file) = Load("""{ "MaxSheets": 0, "SoftEntryLimit": -5, "ExtraIgnoredAttributes": null, "Collections": { "tapestries": { "Exclude": null } } }""");

        Assert.Equal((1, 1, 0), (config.MaxSheets, config.SoftEntryLimit, config.ExtraIgnoredAttributes.Length));
        var warnings = log.Where(l => l.Type == EnumLogType.Warning).Select(l => l.Text).ToList();
        Assert.Contains(warnings, w => w.Contains("curiocatalogue.json") && w.Contains("MaxSheets 0 is below 1, using 1"));
        Assert.Contains(warnings, w => w.Contains("SoftEntryLimit -5 is below 1, using 1"));
        Assert.Contains(warnings, w => w.Contains("ExtraIgnoredAttributes is null, using an empty list"));
        Assert.Contains(warnings, w => w.Contains("Collections.tapestries.Exclude is null, using an empty list"));
        Assert.DoesNotContain(log, l => l.Type is EnumLogType.Error or EnumLogType.Fatal);

        var written = JsonNode.Parse(file)!;
        Assert.Equal((1, 1), ((int)written["MaxSheets"]!, (int)written["SoftEntryLimit"]!));
        Assert.Equal(0, written["ExtraIgnoredAttributes"]!.AsArray().Count);
        Assert.Equal(0, written["Collections"]!["tapestries"]!["Exclude"]!.AsArray().Count);
    }

    [Fact]
    public void The_tapestry_settings_of_0_1_are_moved_into_the_collections_section_and_the_file_loses_them()
    {
        // K3 of the collections core plan: read once, moved, removed from the file; a second load finds nothing to move.
        var (config, log, problems, file) = Load("""{ "TapestryExclude": ["aid-*"], "TapestryAdd": ["somemod-piece"], "MaxSheets": 12 }""");

        var tapestries = config.Collections["tapestries"];
        Assert.Equal(["aid-*"], tapestries.Exclude);
        Assert.Equal(["somemod-piece"], tapestries.Add);
        Assert.False(tapestries.Disabled);
        Assert.Null(config.TapestryExclude);
        Assert.Empty(problems);
        Assert.Contains(log, l => l.Type == EnumLogType.Notification && l.Text.Contains("TapestryExclude [aid-*] moved to Collections.tapestries.Exclude"));
        var written = JsonNode.Parse(file)!.AsObject();
        Assert.False(written.ContainsKey("TapestryExclude"));
        Assert.False(written.ContainsKey("TapestryAdd"));
        Assert.Equal("aid-*", (string)written["Collections"]!["tapestries"]!["Exclude"]![0]!);
        Assert.Equal("somemod-piece", (string)written["Collections"]!["tapestries"]!["Add"]![0]!);
        Assert.Equal(12, (int)written["MaxSheets"]!);

        var (again, againLog, _, _) = Load(file);
        Assert.Equal(["aid-*"], again.Collections["tapestries"].Exclude);
        Assert.DoesNotContain(againLog, l => l.Text.Contains("moved to"));
    }

    [Fact]
    public void A_good_file_loads_without_a_warning_and_gets_the_missing_settings()
    {
        // "Written back every time": a setting a newer version added appears in the file with its default.
        var (config, _, problems, file) = Load("""{ "MaxSheets": 12 }""");

        Assert.Equal(12, config.MaxSheets);
        Assert.Empty(problems);
        var written = JsonNode.Parse(file)!;
        Assert.Equal(12, (int)written["MaxSheets"]!);
        Assert.Equal(500, (int)written["SoftEntryLimit"]!);
    }

    [Fact]
    public void A_broken_file_is_kept_as_it_is_and_the_mod_runs_with_defaults()
    {
        // So the player can fix it: one error line that names the file, the defaults, and no write-back over the broken text.
        const string broken = "{ \"MaxSheets\": ";
        var (config, log, _, file) = Load(broken);

        Assert.Equal(new CurioCatalogueConfig().MaxSheets, config.MaxSheets);
        var error = Assert.Single(log, l => l.Type == EnumLogType.Error);
        Assert.Contains("Could not read curiocatalogue.json, using defaults", error.Text);
        Assert.Equal(broken, file);
    }
}
