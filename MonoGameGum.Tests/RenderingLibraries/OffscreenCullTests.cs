using System.Drawing;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Tests for the off-screen render cull (#2998). This file starts with the pure geometry
/// predicate (<see cref="CameraScissorExtensions.IsFullyOutside"/>); orderer- and renderer-level
/// cull behavior is added alongside as the feature is wired into the walk.
/// </summary>
public class OffscreenCullTests : BaseTestClass
{
    // clip spans [100,300] x [100,300]; with a 15px margin the keep-region is [85,315] x [85,315].
    private static readonly Rectangle Clip = new Rectangle(100, 100, 200, 200);
    private const int Margin = 15;

    [Fact]
    public void IsFullyOutside_ShouldBeFalse_WhenBoundsOverlapClip()
    {
        Rectangle bounds = new Rectangle(150, 150, 50, 50);

        CameraScissorExtensions.IsFullyOutside(bounds, Clip, Margin).ShouldBeFalse();
    }

    [Fact]
    public void IsFullyOutside_ShouldBeTrue_WhenBoundsFarToTheLeft()
    {
        Rectangle bounds = new Rectangle(0, 150, 50, 50);   // right edge = 50, well left of 85

        CameraScissorExtensions.IsFullyOutside(bounds, Clip, Margin).ShouldBeTrue();
    }

    [Fact]
    public void IsFullyOutside_ShouldBeTrue_WhenBoundsFarToTheRight()
    {
        Rectangle bounds = new Rectangle(400, 150, 50, 50); // left edge = 400, well right of 315

        CameraScissorExtensions.IsFullyOutside(bounds, Clip, Margin).ShouldBeTrue();
    }

    [Fact]
    public void IsFullyOutside_ShouldBeTrue_WhenBoundsFarAbove()
    {
        Rectangle bounds = new Rectangle(150, 0, 50, 50);   // bottom edge = 50, well above 85

        CameraScissorExtensions.IsFullyOutside(bounds, Clip, Margin).ShouldBeTrue();
    }

    [Fact]
    public void IsFullyOutside_ShouldBeTrue_WhenBoundsFarBelow()
    {
        Rectangle bounds = new Rectangle(150, 400, 50, 50); // top edge = 400, well below 315

        CameraScissorExtensions.IsFullyOutside(bounds, Clip, Margin).ShouldBeTrue();
    }

    [Fact]
    public void IsFullyOutside_ShouldBeFalse_WhenBoundsAreOutsideButWithinMargin()
    {
        // Right edge = 90, which is past the clip's left (100) but inside the 15px margin (>= 85).
        Rectangle bounds = new Rectangle(40, 150, 50, 50);

        CameraScissorExtensions.IsFullyOutside(bounds, Clip, Margin).ShouldBeFalse();
    }

    [Fact]
    public void IsFullyOutside_ShouldBeTrue_WhenBoundsAreJustBeyondMargin()
    {
        // Right edge = 84, one pixel past the 15px margin boundary (85).
        Rectangle bounds = new Rectangle(40, 150, 44, 50);

        CameraScissorExtensions.IsFullyOutside(bounds, Clip, Margin).ShouldBeTrue();
    }
}
