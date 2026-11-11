using System;

namespace CurioCatalogue.Core;

/// <summary>
/// How many cells go on a line when a row of cells wraps: as many lines as the width needs, the cells spread evenly over them,
/// so 16 at 12 a line is 8 + 8, not 12 + 4 (L6 of the album polish plan, the author). The album's pages and the want list's
/// window (L13) lay cells out by it.
/// </summary>
public static class EvenLines
{
    /// <param name="count">How many cells there are.</param>
    /// <param name="most">The most that fit on a line (at least 1).</param>
    /// <returns>Cells on a full line, at least 1; the last line may have fewer.</returns>
    public static int PerLine(int count, int most)
    {
        most = Math.Max(1, most);
        int lineCount = Math.Max(1, (count + most - 1) / most);
        return Math.Max(1, (count + lineCount - 1) / lineCount);
    }
}
