using CurioCatalogue.Gui;
using NSubstitute;
using Vintagestory.API.Client;

namespace CurioCatalogue.Tests;

/// <summary>The album window lays out its vertical tabs with a tab height of its own (<see cref="AlbumTabs.TabHeight"/>), copied from
/// vanilla's: the column is sized by it and the tabs go aside or into a dropdown by it. If a game update changes the engine's
/// numbers, the column no longer fits its tabs and this fails (R1, G5.4).</summary>
public class TabMetricsTests
{
    /// <summary>Reaches the base's protected sizes; no tabs, no fonts (CairoFont needs the native cairo, absent here), nothing drawn.</summary>
    private sealed class Probe() : GuiElementVerticalTabs(Substitute.For<ICoreClientAPI>(), [], null!,
        null!, ElementBounds.Fixed(0, 0, 10, 10), (_, _) => { })
    {
        public double Height => unscaledTabHeight;
        public double Spacing => unscaledTabSpacing;
    }

    [Fact]
    public void The_windows_tab_height_is_the_engines_height_and_spacing()
    {
        // Checked against vsapi GuiElementVerticalTabs.cs:17-18#unscaledTabSpacing: the fields' values at construction.
        var vanilla = new Probe();

        Assert.Equal(vanilla.Height + vanilla.Spacing, AlbumTabs.TabHeight);
    }
}
