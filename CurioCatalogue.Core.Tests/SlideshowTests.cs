using CurioCatalogue.Core;

namespace CurioCatalogue.Core.Tests;

/// <summary>
/// A missing member of several items cycles through their stacks once a second, held while the mouse is over it
/// (docs/plans/2026-11-04-album-view.md, A2, "The cell"; as the handbook's slideshow).
/// </summary>
public class SlideshowTests
{
    [Fact]
    public void The_next_stack_comes_after_a_full_second_and_the_rest_of_the_time_is_kept()
    {
        var show = Slideshow.Start.Tick(0.6, 3, false);
        Assert.Equal(0, show.Index);

        show = show.Tick(0.6, 3, false);
        Assert.Equal(1, show.Index);
        Assert.Equal(0.2, show.Shown, 6);
    }

    [Fact]
    public void After_the_last_stack_comes_the_first_and_a_long_frame_skips_as_many_as_it_lasted()
    {
        Assert.Equal(0, new Slideshow(2, 0.5).Tick(0.5, 3, false).Index);
        Assert.Equal(1, Slideshow.Start.Tick(4.2, 3, false).Index);
    }

    [Fact]
    public void Under_the_mouse_it_holds_and_a_single_stack_never_moves()
    {
        var show = new Slideshow(1, 0.9);

        Assert.Equal(show, show.Tick(5, 3, hovered: true));
        Assert.Equal(Slideshow.Start, Slideshow.Start.Tick(5, 1, false));
    }
}
