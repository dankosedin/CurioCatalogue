using CurioCatalogue.Core;

namespace CurioCatalogue.Core.Tests;

/// <summary>The cells' even spread over lines (L6, L13): 16 at 12 a line is 8 + 8, never 12 + 4.</summary>
public class EvenLinesTests
{
    [Theory]
    [InlineData(0, 10, 1)]
    [InlineData(1, 10, 1)]
    [InlineData(7, 10, 7)]
    [InlineData(10, 10, 10)]
    [InlineData(11, 10, 6)]
    [InlineData(16, 12, 8)]
    [InlineData(23, 10, 8)]
    [InlineData(25, 10, 9)]
    [InlineData(5, 0, 1)]
    public void Cells_spread_evenly_over_the_lines_the_width_needs(int count, int most, int expected) =>
        Assert.Equal(expected, EvenLines.PerLine(count, most));
}
