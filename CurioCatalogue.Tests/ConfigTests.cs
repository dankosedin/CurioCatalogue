namespace CurioCatalogue.Tests;

/// <summary>A hand-edited config is put right on load, with a line in the log for each correction (code review 28.10.2026).</summary>
public class ConfigTests
{
    [Fact]
    public void Defaults_need_no_correction()
    {
        Assert.Empty(new CurioCatalogueConfig().Correct());
    }

    [Fact]
    public void Limits_below_one_become_one()
    {
        var config = new CurioCatalogueConfig { MaxSheets = 0, SoftEntryLimit = -5 };

        var fixes = config.Correct();

        Assert.Equal((1, 1), (config.MaxSheets, config.SoftEntryLimit));
        Assert.Equal(2, fixes.Count);
        Assert.Contains(fixes, f => f.Contains("MaxSheets"));
        Assert.Contains(fixes, f => f.Contains("SoftEntryLimit"));
    }

    [Fact]
    public void Missing_lists_become_empty()
    {
        // "ExtraIgnoredAttributes": null in the file reads as null: the game uses JsonConvert.DeserializeObject with default settings
        // (decompiled Vintagestory.Common.APIBase.LoadModConfig), which sets an explicit null over the default.
        var config = new CurioCatalogueConfig
        {
            ExtraIgnoredAttributes = null!,
            Collections = new() { ["tapestries"] = new() { Exclude = null!, Add = null! }, ["ores"] = null! },
        };

        var fixes = config.Correct();

        Assert.Equal(4, fixes.Count);
        Assert.Empty(config.ExtraIgnoredAttributes);
        Assert.Empty(config.Collections["tapestries"].Exclude);
        Assert.Empty(config.Collections["tapestries"].Add);
        Assert.False(config.Collections["ores"].Disabled);
    }

    [Fact]
    public void A_null_inside_a_collections_list_is_dropped()
    {
        // K7 (R1, C1): [ "aid-*", null ] in the file (a stray comma) reads with the null kept (Newtonsoft's default), and a null
        // pattern would throw when the collection is built.
        var config = Newtonsoft.Json.JsonConvert.DeserializeObject<CurioCatalogueConfig>(
            """{ "Collections": { "tapestries": { "Exclude": [ "aid-*", null ], "Add": [ null ] } } }""")!;

        var fixes = config.Correct();

        Assert.Equal(2, fixes.Count);
        Assert.Equal(["aid-*"], config.Collections["tapestries"].Exclude);
        Assert.Empty(config.Collections["tapestries"].Add);
    }

    [Fact]
    public void The_tapestry_settings_of_0_1_move_into_the_collections_section_and_are_cleared()
    {
        var config = new CurioCatalogueConfig { TapestryExclude = ["aid-*"], TapestryAdd = ["mymod-banner"] };
        config.Collections["tapestries"].Exclude = ["glider*"];

        var moved = config.MoveLegacy();

        Assert.Equal(2, moved.Count);
        Assert.Equal(["glider*", "aid-*"], config.Collections["tapestries"].Exclude);
        Assert.Equal(["mymod-banner"], config.Collections["tapestries"].Add);
        Assert.Null(config.TapestryExclude);
        Assert.Null(config.TapestryAdd);
        Assert.Empty(config.MoveLegacy()); // once
    }

    [Fact]
    public void The_legacy_keys_leave_the_file_once_moved()
    {
        var config = new CurioCatalogueConfig { TapestryExclude = ["aid-*"] };
        config.MoveLegacy();

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(config);

        Assert.DoesNotContain("TapestryExclude", json);
        Assert.DoesNotContain("TapestryAdd", json);
        Assert.Contains("\"Exclude\":[\"aid-*\"]", json);
    }
}
