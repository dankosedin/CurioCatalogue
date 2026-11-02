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

    /// <summary>Writes <paramref name="text"/> as the config file, runs the mod's loader, and returns what it made, what it logged and what the file says now.</summary>
    (CurioCatalogueConfig Config, List<LogLine> Log, string File) Load(string text)
    {
        string? original = File.Exists(FilePath) ? File.ReadAllText(FilePath) : null; // none if the mod's start did not write it
        int mark = game.LogMark();
        try
        {
            Directory.CreateDirectory(GamePaths.ModConfig);
            File.WriteAllText(FilePath, text);
            var config = game.OnMain(() => (CurioCatalogueConfig)typeof(CurioCatalogueModSystem)
                .GetMethod("LoadConfig", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [game.Api])!);
            return (config, game.Withdraw(mark), File.ReadAllText(FilePath));
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
        var (config, log, file) = Load("""{ "MaxSheets": 0, "SoftEntryLimit": -5, "TapestryAdd": null }""");

        Assert.Equal((1, 1, 0), (config.MaxSheets, config.SoftEntryLimit, config.TapestryAdd.Length));
        var warnings = log.Where(l => l.Type == EnumLogType.Warning).Select(l => l.Text).ToList();
        Assert.Contains(warnings, w => w.Contains("curiocatalogue.json") && w.Contains("MaxSheets 0 is below 1, using 1"));
        Assert.Contains(warnings, w => w.Contains("SoftEntryLimit -5 is below 1, using 1"));
        Assert.Contains(warnings, w => w.Contains("TapestryAdd is null, using an empty list"));
        Assert.DoesNotContain(log, l => l.Type is EnumLogType.Error or EnumLogType.Fatal);

        var written = JsonNode.Parse(file)!;
        Assert.Equal((1, 1), ((int)written["MaxSheets"]!, (int)written["SoftEntryLimit"]!));
        Assert.Equal(0, written["TapestryAdd"]!.AsArray().Count);
    }

    [Fact]
    public void A_good_file_loads_without_a_warning_and_gets_the_missing_settings()
    {
        // "Written back every time": a setting a newer version added appears in the file with its default.
        var (config, log, file) = Load("""{ "MaxSheets": 12 }""");

        Assert.Equal(12, config.MaxSheets);
        Assert.Empty(log.Where(l => l.Type is EnumLogType.Warning or EnumLogType.Error or EnumLogType.Fatal));
        var written = JsonNode.Parse(file)!;
        Assert.Equal(12, (int)written["MaxSheets"]!);
        Assert.Equal(500, (int)written["SoftEntryLimit"]!);
    }

    [Fact]
    public void A_broken_file_is_kept_as_it_is_and_the_mod_runs_with_defaults()
    {
        // So the player can fix it: one error line that names the file, the defaults, and no write-back over the broken text.
        const string broken = "{ \"MaxSheets\": ";
        var (config, log, file) = Load(broken);

        Assert.Equal(new CurioCatalogueConfig().MaxSheets, config.MaxSheets);
        var error = Assert.Single(log, l => l.Type == EnumLogType.Error);
        Assert.Contains("Could not read curiocatalogue.json, using defaults", error.Text);
        Assert.Equal(broken, file);
    }
}
