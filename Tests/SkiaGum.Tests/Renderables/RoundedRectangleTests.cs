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
}
