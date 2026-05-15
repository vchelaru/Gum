using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.Forms.DefaultVisuals;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Regression coverage for issue #2781 — ScrollBar's thumb is sized in absolute pixels
/// from a snapshot of <see cref="GraphicalUiElement.GetAbsoluteHeight"/>/<see cref="GraphicalUiElement.GetAbsoluteWidth"/>
/// on Track. If Track resizes after construction (parent resize, sibling resize, layout reflow,
/// or direct Track manipulation) and nothing re-runs UpdateThumbSize, the thumb keeps the old
/// absolute size. The fix is to subscribe to Track.SizeChanged so any Track resize — from any
/// upstream cause — flows back into thumb size + position.
/// </summary>
public class ScrollBarThumbResizeTests : BaseTestClass
{
    private static GraphicalUiElement GetThumbVisual(ScrollBar scrollBar) =>
        (GraphicalUiElement)scrollBar.Visual.GetGraphicalUiElementByName("ThumbInstance")!;

    // Math reference for ViewportSize=40, Minimum=0, Maximum=100:
    //   thumbRatio = ViewportSize / ((Maximum - Minimum) + ViewportSize) = 40 / 140 ≈ 0.2857
    // Default vertical visual has Visual.Height=128 → Track height (parent - 48) = 80
    //   → initial thumb height ≈ 22.86
    // Picked so MinimumThumbSize (16) does not clamp at either size we test.

    [Fact]
    public void ThumbPosition_ShouldRecompute_AfterTrackResize_Vertical()
    {
        // Track-position math (MaxThumbPosition = trackSize - thumbSize) reads Track size every
        // call. If only UpdateThumbSize re-runs on Track resize but UpdateThumbPositionAccordingToValue
        // does not, Value=50 still maps to the OLD track range and the thumb sits in the wrong place.
        DefaultScrollBarRuntime visual = new DefaultScrollBarRuntime();
        visual.Width = 24;
        visual.Height = 128;
        ScrollBar scrollBar = visual.FormsControl;
        scrollBar.Orientation = Orientation.Vertical;
        scrollBar.Minimum = 0;
        scrollBar.Maximum = 100;
        scrollBar.ViewportSize = 40;
        scrollBar.Value = 50;
        visual.UpdateLayout();

        float trackHeightBefore = scrollBar.Track!.GetAbsoluteHeight();
        float thumbHeightBefore = GetThumbVisual(scrollBar).GetAbsoluteHeight();
        float rangeBefore = trackHeightBefore - thumbHeightBefore;
        GetThumbVisual(scrollBar).Y.ShouldBe(rangeBefore * 0.5f, 0.5f,
            "Sanity: thumb Y at Value=50 should sit at midpoint of (trackHeight - thumbHeight).");

        visual.Height = 256;
        visual.UpdateLayout();

        float trackHeightAfter = scrollBar.Track.GetAbsoluteHeight();
        float thumbHeightAfter = GetThumbVisual(scrollBar).GetAbsoluteHeight();
        float rangeAfter = trackHeightAfter - thumbHeightAfter;
        GetThumbVisual(scrollBar).Y.ShouldBe(rangeAfter * 0.5f, 0.5f,
            "After Track resize, thumb Y must reflect the new (trackHeight - thumbHeight), not the cached old range.");
    }

    [Fact]
    public void ThumbSize_ShouldRespectMinimumThumbSize_AfterTrackResize()
    {
        // Configured so the BEFORE thumb is well above the floor (40px) and the AFTER thumb
        // would compute below the floor and must be clamped (-> 16px). A stale (un-recomputed)
        // thumb would still read 40px and fail this assertion — making this a genuine regression
        // check for the Track.SizeChanged subscription, not just a positive sanity assertion.
        DefaultScrollBarRuntime visual = new DefaultScrollBarRuntime();
        visual.Width = 24;
        visual.Height = 128;
        ScrollBar scrollBar = visual.FormsControl;
        scrollBar.Orientation = Orientation.Vertical;
        scrollBar.MinimumThumbSize = 16;
        scrollBar.Minimum = 0;
        scrollBar.Maximum = 10;
        scrollBar.ViewportSize = 10;          // ratio 0.5, initial thumb = 80 * 0.5 = 40
        visual.UpdateLayout();
        GetThumbVisual(scrollBar).GetAbsoluteHeight().ShouldBe(40f, 0.5f,
            "Sanity precondition: initial thumb should be 40, well above the 16 floor.");

        // Shrink Track to a size where (trackSize * ratio) = 10, which is below MinimumThumbSize.
        scrollBar.Track!.HeightUnits = DimensionUnitType.Absolute;
        scrollBar.Track.Height = 20;
        visual.UpdateLayout();

        GetThumbVisual(scrollBar).GetAbsoluteHeight().ShouldBe(16f, 0.01f,
            "After Track shrink, thumb must be re-clamped to MinimumThumbSize (16), not left at the stale 40.");
    }

    [Fact]
    public void ThumbSize_ShouldUpdate_WhenScrollBarVisualResizes_Horizontal()
    {
        // Visual-level resize path, horizontal orientation. Track is RelativeToParent so resizing
        // Visual propagates into Track and Track.SizeChanged fires. Guards against the fix
        // accidentally regressing the path that the previous Visual.SizeChanged hook covered.
        DefaultScrollBarRuntime visual = new DefaultScrollBarRuntime();
        visual.Width = 128;
        visual.Height = 24;
        ScrollBar scrollBar = visual.FormsControl;
        scrollBar.Orientation = Orientation.Horizontal;
        scrollBar.Minimum = 0;
        scrollBar.Maximum = 100;
        scrollBar.ViewportSize = 40;
        visual.UpdateLayout();

        float thumbWidthBefore = GetThumbVisual(scrollBar).GetAbsoluteWidth();

        visual.Width = 256;
        visual.UpdateLayout();

        float thumbWidthAfter = GetThumbVisual(scrollBar).GetAbsoluteWidth();
        thumbWidthAfter.ShouldBeGreaterThan(thumbWidthBefore,
            "Thumb width must grow when the ScrollBar Visual (and therefore Track) widens.");

        float trackWidthAfter = scrollBar.Track!.GetAbsoluteWidth();
        double expectedRatio = 40.0 / (100.0 + 40.0);
        thumbWidthAfter.ShouldBe((float)(trackWidthAfter * expectedRatio), 0.5f,
            "Thumb width should equal trackWidth * (ViewportSize / valueRange) after Visual resize.");
    }

    [Fact]
    public void ThumbSize_ShouldUpdate_WhenScrollBarVisualResizes_Vertical()
    {
        // Visual-level resize path, vertical orientation. See horizontal sibling for rationale.
        DefaultScrollBarRuntime visual = new DefaultScrollBarRuntime();
        visual.Width = 24;
        visual.Height = 128;
        ScrollBar scrollBar = visual.FormsControl;
        scrollBar.Orientation = Orientation.Vertical;
        scrollBar.Minimum = 0;
        scrollBar.Maximum = 100;
        scrollBar.ViewportSize = 40;
        visual.UpdateLayout();

        float thumbHeightBefore = GetThumbVisual(scrollBar).GetAbsoluteHeight();

        visual.Height = 256;
        visual.UpdateLayout();

        float thumbHeightAfter = GetThumbVisual(scrollBar).GetAbsoluteHeight();
        thumbHeightAfter.ShouldBeGreaterThan(thumbHeightBefore,
            "Thumb height must grow when the ScrollBar Visual (and therefore Track) grows.");

        float trackHeightAfter = scrollBar.Track!.GetAbsoluteHeight();
        double expectedRatio = 40.0 / (100.0 + 40.0);
        thumbHeightAfter.ShouldBe((float)(trackHeightAfter * expectedRatio), 0.5f,
            "Thumb height should equal trackHeight * (ViewportSize / valueRange) after Visual resize.");
    }

    [Fact]
    public void ThumbSize_ShouldUpdate_WhenTrackResizesDirectly_Horizontal()
    {
        // Canonical bug, horizontal. Same shape as vertical sibling.
        DefaultScrollBarRuntime visual = new DefaultScrollBarRuntime();
        visual.Width = 128;
        visual.Height = 24;
        ScrollBar scrollBar = visual.FormsControl;
        scrollBar.Orientation = Orientation.Horizontal;
        scrollBar.Minimum = 0;
        scrollBar.Maximum = 100;
        scrollBar.ViewportSize = 40;
        visual.UpdateLayout();

        float thumbWidthBefore = GetThumbVisual(scrollBar).GetAbsoluteWidth();

        scrollBar.Track!.WidthUnits = DimensionUnitType.Absolute;
        scrollBar.Track.Width = 200;
        visual.UpdateLayout();

        float thumbWidthAfter = GetThumbVisual(scrollBar).GetAbsoluteWidth();
        thumbWidthAfter.ShouldNotBe(thumbWidthBefore,
            "Thumb width must change when Track is resized directly.");

        double expectedRatio = 40.0 / (100.0 + 40.0);
        thumbWidthAfter.ShouldBe((float)(200 * expectedRatio), 0.5f,
            "Thumb width should equal new trackWidth * (ViewportSize / valueRange).");
    }

    [Fact]
    public void ThumbSize_ShouldUpdate_WhenTrackResizesDirectly_Vertical()
    {
        // CANONICAL FAILING TEST FOR THE BUG.
        // Resizes Track without touching Visual. The pre-fix Visual.SizeChanged hook does not
        // fire here, so nothing re-runs UpdateThumbSize and the thumb keeps its old absolute
        // height. Post-fix this is covered by the Track.SizeChanged subscription.
        DefaultScrollBarRuntime visual = new DefaultScrollBarRuntime();
        visual.Width = 24;
        visual.Height = 128;
        ScrollBar scrollBar = visual.FormsControl;
        scrollBar.Orientation = Orientation.Vertical;
        scrollBar.Minimum = 0;
        scrollBar.Maximum = 100;
        scrollBar.ViewportSize = 40;
        visual.UpdateLayout();

        float thumbHeightBefore = GetThumbVisual(scrollBar).GetAbsoluteHeight();

        // Override Track sizing to be absolute so Visual size doesn't dictate it. Then resize
        // Track. This is the path the original Visual.SizeChanged hook fails to cover.
        scrollBar.Track!.HeightUnits = DimensionUnitType.Absolute;
        scrollBar.Track.Height = 200;
        visual.UpdateLayout();

        float thumbHeightAfter = GetThumbVisual(scrollBar).GetAbsoluteHeight();
        thumbHeightAfter.ShouldNotBe(thumbHeightBefore,
            "Thumb height must change when Track is resized directly (independent of Visual size).");

        double expectedRatio = 40.0 / (100.0 + 40.0);
        thumbHeightAfter.ShouldBe((float)(200 * expectedRatio), 0.5f,
            "Thumb height should equal new trackHeight * (ViewportSize / valueRange).");
    }
}
