using CurioCatalogue.Gui;

namespace CurioCatalogue.Tests;

/// <summary>Where the album dropdown's open list stands and which lines it shows (L2c of the album polish plan). Plain numbers:
/// a line is 30 high, as vanilla's (GuiElementListMenu.cs:26#unscaledLineHeight), at most 12 shown.</summary>
public class DropDownListTests
{
    private const double Line = 30;
    private const int Max = 12;

    [Fact]
    public void A_list_that_fits_under_the_box_opens_under_it_whole()
    {
        var placement = DropDownList.Place(roomAbove: 600, roomBelow: 100, Line, count: 3, Max);

        Assert.Equal(new DropDownList.Placement(Above: false, Lines: 3), placement);
    }

    [Fact]
    public void A_list_too_long_for_the_room_under_the_box_opens_over_the_page_scrolled()
    {
        // The row of controls at the bottom of a tall window: 17 tabs of crystals, room for 2 lines below and 20 above.
        var placement = DropDownList.Place(roomAbove: 600, roomBelow: 70, Line, count: 17, Max);

        Assert.Equal(new DropDownList.Placement(Above: true, Lines: Max), placement);
    }

    [Fact]
    public void A_list_opens_on_the_side_with_more_room_when_neither_holds_it()
    {
        Assert.Equal(new DropDownList.Placement(Above: true, Lines: 4), DropDownList.Place(roomAbove: 125, roomBelow: 95, Line, count: 9, Max));
        Assert.Equal(new DropDownList.Placement(Above: false, Lines: 4), DropDownList.Place(roomAbove: 95, roomBelow: 125, Line, count: 9, Max));
        Assert.Equal(new DropDownList.Placement(Above: false, Lines: 3), DropDownList.Place(roomAbove: 100, roomBelow: 95, Line, count: 9, Max)); // a tie: under
    }

    [Fact]
    public void A_list_with_no_room_on_either_side_still_shows_one_line()
    {
        Assert.Equal(1, DropDownList.Place(roomAbove: 10, roomBelow: 0, Line, count: 5, Max).Lines);
    }

    [Fact]
    public void The_lines_shown_follow_the_line_moved_to_by_as_little_as_they_can()
    {
        Assert.Equal(0, DropDownList.Show(first: 0, lines: 5, count: 17, index: 4)); // shown already
        Assert.Equal(1, DropDownList.Show(first: 0, lines: 5, count: 17, index: 5)); // one down
        Assert.Equal(3, DropDownList.Show(first: 6, lines: 5, count: 17, index: 3)); // up to it
        Assert.Equal(12, DropDownList.Show(first: 0, lines: 5, count: 17, index: 16)); // End
    }

    [Fact]
    public void Scrolling_stops_at_the_lists_ends()
    {
        Assert.Equal(0, DropDownList.Clamp(first: -1, lines: 5, count: 17));
        Assert.Equal(12, DropDownList.Clamp(first: 13, lines: 5, count: 17));
        Assert.Equal(0, DropDownList.Clamp(first: 2, lines: 5, count: 3)); // all shown: no scrolling
    }

    [Fact]
    public void The_line_under_the_mouse_counts_from_the_first_shown()
    {
        Assert.Equal(7, DropDownList.LineAt(offset: 65, Line, first: 5, lines: 5, count: 17));
        Assert.Null(DropDownList.LineAt(offset: -1, Line, first: 5, lines: 5, count: 17));
        Assert.Null(DropDownList.LineAt(offset: 150, Line, first: 5, lines: 5, count: 17)); // under the last line shown
        Assert.Null(DropDownList.LineAt(offset: 95, Line, first: 0, lines: 5, count: 3)); // past the list's end
    }
}
