using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Renderable-level guards for the Skia <see cref="RoundedRectangle"/> primitive's per-corner
/// custom-radius path. These cover geometry the runtime layer (<c>RectangleRuntime</c>) cannot
/// enforce on its own -- anything that depends on how <see cref="RoundedRectangle.BuildCustomCornerPath"/>
/// constructs its path.
/// </summary>
public class RoundedRectangleTests
{
    // Issue #4030 follow-up -- visible as a small square notch poking past a rounded corner on
    // the stroke slot (whose bounding rect is shrunk by half the stroke width, while the corner
    // radius pushed onto it stays the fill's un-shrunk value). Unlike SKCanvas.DrawRoundRect
    // (which clamps internally), the manual ArcTo path did not, so an oversized radius produced
    // an arc bigger than the corner it was cut into.
    [Fact]
    public void BuildCustomCornerPath_RadiusLargerThanHalfRectSize_PathStaysWithinBounds()
    {
        RoundedRectangle sut = new()
        {
            CustomRadiusTopLeft = 40f, // larger than half of either dimension below
        };
        SKRect boundingRect = new SKRect(0, 0, 50, 30);

        using SKPath path = sut.BuildCustomCornerPath(boundingRect);

        SKRect pathBounds = path.Bounds;
        pathBounds.Left.ShouldBeGreaterThanOrEqualTo(boundingRect.Left);
        pathBounds.Top.ShouldBeGreaterThanOrEqualTo(boundingRect.Top);
        pathBounds.Right.ShouldBeLessThanOrEqualTo(boundingRect.Right);
        pathBounds.Bottom.ShouldBeLessThanOrEqualTo(boundingRect.Bottom);
    }

    [Fact]
    public void BuildCustomCornerPath_RadiusSmallerThanHalfRectSize_UsesRequestedRadius()
    {
        // Sanity check the clamp doesn't kick in when it shouldn't -- a radius that already fits
        // must render at its requested size, not get shrunk further.
        RoundedRectangle sut = new()
        {
            CustomRadiusTopLeft = 10f,
        };
        SKRect boundingRect = new SKRect(0, 0, 100, 100);

        using SKPath path = sut.BuildCustomCornerPath(boundingRect);

        // The arc's bounding circle for a 10px, unclamped top-left radius touches x=10 along the
        // top edge (path.LineTo(boundingRect.Left + topLeft, boundingRect.Top) is the first point
        // on the path after the MoveTo), so the leftmost path point should sit at x=0 (the arc's
        // own left edge) -- not further right, which would indicate an unexpectedly smaller radius.
        path.Bounds.Left.ShouldBe(0f, tolerance: 0.01f);
    }

    // Issue #4030 follow-up -- unlike CircleRuntime's Skia path (Circle.FillRadiusInset,
    // #2834), RoundedRectangle had no fill-inset mechanism at all: the fill always drew at the
    // full bounding rect, the same outer edge as the stroke's outer edge. A solid, opaque stroke
    // hides the overlap everywhere except its own antialiased feather at the true outer boundary,
    // where the "underneath" color was the fill instead of the real background -- visible as a
    // darker/tinted fringe around the stroke's outer edge instead of a clean blend to the
    // background (confirmed by testing against a white background: the fringe read as the blue
    // fill bleeding through, not antialiasing against white).

    [Fact]
    public void GetFillInsetRect_IsFilledWithPositiveInset_ShrinksRectOnEachSide()
    {
        RoundedRectangle sut = new()
        {
            IsFilled = true,
            FillInset = 4f,
        };
        SKRect boundingRect = new SKRect(0, 0, 100, 60);

        SKRect result = sut.GetFillInsetRect(boundingRect);

        result.Left.ShouldBe(4f);
        result.Top.ShouldBe(4f);
        result.Right.ShouldBe(96f);
        result.Bottom.ShouldBe(56f);
    }

    [Fact]
    public void GetFillInsetRect_NotFilled_ReturnsRectUnchanged()
    {
        // Only the fill instance honors the inset -- the stroke instance must never shrink its
        // own bounding rect via FillInset (it has its own, separate stroke-width inset already).
        RoundedRectangle sut = new()
        {
            IsFilled = false,
            FillInset = 4f,
        };
        SKRect boundingRect = new SKRect(0, 0, 100, 60);

        SKRect result = sut.GetFillInsetRect(boundingRect);

        result.ShouldBe(boundingRect);
    }

    [Fact]
    public void GetFillInsetRect_InsetLargerThanRect_ClampsToNonNegativeSize()
    {
        RoundedRectangle sut = new()
        {
            IsFilled = true,
            FillInset = 100f,
        };
        SKRect boundingRect = new SKRect(0, 0, 20, 10);

        SKRect result = sut.GetFillInsetRect(boundingRect);

        result.Width.ShouldBeGreaterThanOrEqualTo(0f);
        result.Height.ShouldBeGreaterThanOrEqualTo(0f);
    }

    [Fact]
    public void BuildCustomCornerPath_FillInsetAndGetFillInsetRect_Composed_ShrinksPathBoundsBySameAmount()
    {
        // Mirrors exactly how DrawBound composes the two pieces: shift the rect via
        // GetFillInsetRect, then reduce the radius by the same inset in BuildCustomCornerPath.
        // The path's overall bounds must shrink by the inset amount on every side.
        RoundedRectangle sut = new()
        {
            IsFilled = true,
            CustomRadiusTopLeft = 20f,
            FillInset = 8f,
        };
        SKRect boundingRect = new SKRect(0, 0, 100, 100);

        SKRect insetRect = sut.GetFillInsetRect(boundingRect);
        using SKPath path = sut.BuildCustomCornerPath(insetRect, sut.FillInset);

        path.Bounds.Left.ShouldBe(8f, tolerance: 0.01f);
        path.Bounds.Top.ShouldBe(8f, tolerance: 0.01f);
        path.Bounds.Right.ShouldBe(92f, tolerance: 0.01f);
        path.Bounds.Bottom.ShouldBe(92f, tolerance: 0.01f);
    }
}
