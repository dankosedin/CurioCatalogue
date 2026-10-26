using CurioCatalogue.Core;

namespace CurioCatalogue.Core.Tests;

public class TapestryPiecesTests
{
    [Fact]
    public void Pieces_follow_the_size()
    {
        var pieces = TapestryPieces.FromSizes(new Dictionary<string, string>
        {
            ["ambush"] = "3x1",
            ["holy"] = "1x2",
            ["salvation"] = "2x2",
            ["notice-blackguard"] = "1x1",
        });

        Assert.Equal(
            ["ambush1", "ambush2", "ambush3", "holy1", "holy2", "salvation11", "salvation12", "salvation21", "salvation22", "notice-blackguard"],
            pieces);
    }

    [Fact]
    public void Vanilla_sizes_give_88_pieces()
    {
        // Copied from assets/survival/blocktypes/cloth/tapestry.json, game 1.22.7 (modding.md, Vanilla content we rely on: 31 pictures, 88 pieces).
        var sizes = new Dictionary<string, string>
        {
            ["ambush"] = "3x1", ["nightfall"] = "3x1", ["holy"] = "1x2", ["salvation"] = "2x2", ["rot"] = "3x1",
            ["themorning"] = "3x1", ["schematic-a"] = "2x2", ["schematic-b"] = "2x2", ["schematic-c"] = "1x2",
            ["schematic-c-bloody"] = "1x2", ["schematic-d"] = "2x2", ["rotbeast"] = "2x2", ["seraphim"] = "3x1",
            ["blackguard"] = "2x2", ["forlorn"] = "1x2", ["gate"] = "4x1", ["diet"] = "2x1", ["oldworld"] = "1x3",
            ["temporality"] = "1x2", ["aid-arm"] = "1x2", ["aid-head"] = "1x2", ["aid-leg"] = "1x2", ["contracts"] = "2x1",
            ["eidolon"] = "1x2", ["glider"] = "3x1", ["notice-blackguard"] = "1x1", ["return"] = "1x2", ["rickbat"] = "1x2",
            ["symptoms"] = "2x2", ["tempstorm"] = "2x2", ["translocator"] = "2x2",
        };
        Assert.Equal(31, sizes.Count);
        Assert.Equal(88, TapestryPieces.FromSizes(sizes).Count);
    }

    [Fact]
    public void Bad_size_is_reported()
    {
        Assert.Throws<FormatException>(() => TapestryPieces.FromSizes(new Dictionary<string, string> { ["x"] = "big" }));
    }
}
