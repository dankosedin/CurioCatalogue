using System;

namespace CurioCatalogue.Gui;

/// <summary>
/// Where an open dropdown's list stands and which of its lines show (<see cref="IconDropDown"/>; L2c): plain numbers, so the rules
/// are tested without the game. Lines are counted from the list's first; <c>first</c> is the first line shown when it scrolls.
/// </summary>
public static class DropDownList
{
    /// <summary>The list under the box or above it, and how many of its lines show.</summary>
    public readonly record struct Placement(bool Above, int Lines);

    /// <summary>
    /// Under the box when the whole list fits there (as vanilla's), else on the side with more room, scrolled. The row of controls
    /// stands at the bottom of the album window, so a long list (17 tabs of crystals) opens over the page.
    /// </summary>
    /// <param name="roomAbove">The room from the screen's top to the box, in the same units as <paramref name="line"/>.</param>
    /// <param name="roomBelow">The room from the box to the screen's bottom.</param>
    public static Placement Place(double roomAbove, double roomBelow, double line, int count, int maxLines)
    {
        int wanted = Math.Max(1, Math.Min(count, maxLines));
        int Fit(double room) => Math.Min(wanted, Math.Max(0, (int)Math.Floor(room / line)));
        int below = Fit(roomBelow), above = Fit(roomAbove);
        bool up = above > below; // a tie, or the whole list under the box: under it, as vanilla's
        return new Placement(up, Math.Max(1, up ? above : below));
    }

    /// <summary>The first line shown, kept within the list: no empty room under its last line.</summary>
    public static int Clamp(int first, int lines, int count) => Math.Max(0, Math.Min(first, count - lines));

    /// <summary>The first line shown so that line <paramref name="index"/> shows, moving as little as it can.</summary>
    public static int Show(int first, int lines, int count, int index)
    {
        if (index < first) first = index;
        else if (index >= first + lines) first = index - lines + 1;
        return Clamp(first, lines, count);
    }

    /// <summary>The line at <paramref name="offset"/> from the top of the lines shown, or null outside them.</summary>
    public static int? LineAt(double offset, double line, int first, int lines, int count)
    {
        if (offset < 0) return null;
        int row = (int)(offset / line);
        if (row >= lines) return null;
        int index = first + row;
        return index < count ? index : null;
    }
}
