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
        // "TapestryAdd": null in the file reads as null: the game uses JsonConvert.DeserializeObject with default settings
        // (decompiled Vintagestory.Common.APIBase.LoadModConfig), which sets an explicit null over the default.
        var config = new CurioCatalogueConfig { TapestryExclude = null!, TapestryAdd = null!, ExtraIgnoredAttributes = null! };

        var fixes = config.Correct();

        Assert.Equal(3, fixes.Count);
        Assert.Empty(config.TapestryExclude);
        Assert.Empty(config.TapestryAdd);
        Assert.Empty(config.ExtraIgnoredAttributes);
    }
}
