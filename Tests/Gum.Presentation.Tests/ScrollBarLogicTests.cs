using RenderingLibrary;
using Shouldly;
using TextureCoordinateSelectionPlugin.Logic;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins <see cref="ScrollBarLogic"/>'s min/max/viewport/value math after its extraction from
/// <c>ScrollBarLogicWpf</c> (ADR-0005) — the math previously lived mixed in with direct WPF
/// <c>ScrollBar</c> property writes.
/// </summary>
public class ScrollBarLogicTests
{
    [Fact]
    public void CalculateHorizontalRange_ReturnsRangeBasedOnCameraXAndContentWidth()
    {
        Camera camera = new Camera { ClientWidth = 800, Zoom = 2f };
        camera.X = 100f;
        ScrollBarLogic scrollBarLogic = new ScrollBarLogic();

        ScrollBarRange range = scrollBarLogic.CalculateHorizontalRange(camera, contentWidth: 1000);

        range.Minimum.ShouldBe(-200);
        range.Maximum.ShouldBe(800);
        range.ViewportSize.ShouldBe(400);
        range.Value.ShouldBe(100);
    }

    [Fact]
    public void CalculateVerticalRange_ReturnsRangeBasedOnCameraYAndContentHeight()
    {
        Camera camera = new Camera { ClientHeight = 600, Zoom = 2f };
        camera.Y = 50f;
        ScrollBarLogic scrollBarLogic = new ScrollBarLogic();

        ScrollBarRange range = scrollBarLogic.CalculateVerticalRange(camera, contentHeight: 500);

        range.Minimum.ShouldBe(-150);
        range.Maximum.ShouldBe(350);
        range.ViewportSize.ShouldBe(300);
        range.Value.ShouldBe(50);
    }

    [Fact]
    public void CalculateHorizontalRange_ClampsMaximumToMinimum_WhenContentIsSmallerThanViewableArea()
    {
        Camera camera = new Camera { ClientWidth = 800, Zoom = 2f };
        ScrollBarLogic scrollBarLogic = new ScrollBarLogic();

        // contentWidth is deliberately negative to force the raw (unclamped) maximum below the
        // minimum, exercising the Math.Max clamp in CalculateRange.
        ScrollBarRange range = scrollBarLogic.CalculateHorizontalRange(camera, contentWidth: -1000);

        range.Minimum.ShouldBe(-200);
        range.Maximum.ShouldBe(-200);
    }
}
