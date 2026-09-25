using Avalonia;
using Avalonia.Input;
using Gum.Avalonia.Services;
using Gum.Input;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// A macOS trackpad's two-finger scroll pans the canvas; Cmd+scroll, a mouse wheel and every
/// other OS keep zooming (#4985).
/// </summary>
public class AvaloniaWheelMappingTests
{
    [Fact]
    public void ApplyWheelDelta_TrackpadScroll_PansByThePhysicalPixelsTheFingersMoved()
    {
        GumMouseEventArgs args = new GumMouseEventArgs();

        // Avalonia divides a precise scroll's points by 50; at 2x scaling one point is two pixels.
        AvaloniaMouseMapping.ApplyWheelDelta(args, new Vector(0.2, -0.1), KeyModifiers.None, WheelSource.MacTrackpad, dpiScale: 2);

        args.IsPanScroll.ShouldBeTrue();
        args.PanX.ShouldBe(20f, tolerance: 0.001f);
        args.PanY.ShouldBe(-10f, tolerance: 0.001f);
        args.Delta.ShouldBe(0);
    }

    [Fact]
    public void ApplyWheelDelta_TrackpadScrollWithCmd_Zooms()
    {
        GumMouseEventArgs args = new GumMouseEventArgs();

        AvaloniaMouseMapping.ApplyWheelDelta(args, new Vector(0, 0.5), KeyModifiers.Meta, WheelSource.MacTrackpad, dpiScale: 2);

        args.IsPanScroll.ShouldBeFalse();
        args.Delta.ShouldBe(60);
    }

    [Fact]
    public void ApplyWheelDelta_MouseWheel_ZoomsAtOneHundredTwentyPerNotch()
    {
        GumMouseEventArgs args = new GumMouseEventArgs();

        AvaloniaMouseMapping.ApplyWheelDelta(args, new Vector(0, -1), KeyModifiers.None, WheelSource.Wheel, dpiScale: 2);

        args.IsPanScroll.ShouldBeFalse();
        args.Delta.ShouldBe(-120);
    }

    [Fact]
    public void ApplyWheelDelta_MacMouseWheel_ZoomsOneNotchPerEventWhateverTheAcceleratedDelta()
    {
        // macOS reports one event per wheel click, scaled by scroll acceleration: about 0.02 for a
        // slow click and 3+ for a fast one (#5010).
        GumMouseEventArgs slowClick = new GumMouseEventArgs();
        GumMouseEventArgs fastClick = new GumMouseEventArgs();

        AvaloniaMouseMapping.ApplyWheelDelta(slowClick, new Vector(0, 0.02), KeyModifiers.None, WheelSource.MacMouseWheel, dpiScale: 2);
        AvaloniaMouseMapping.ApplyWheelDelta(fastClick, new Vector(0, -3.4), KeyModifiers.None, WheelSource.MacMouseWheel, dpiScale: 2);

        slowClick.Delta.ShouldBe(120);
        fastClick.Delta.ShouldBe(-120);
    }

    [Theory]
    [InlineData(true, true, WheelSource.MacTrackpad)]
    [InlineData(true, false, WheelSource.MacMouseWheel)]
    [InlineData(false, false, WheelSource.MacMouseWheel)]
    public void GetMacWheelSource_PanOnlyForPreciseEventsWithAGesturePhase(bool isPrecise, bool hasGesturePhase, WheelSource expected)
    {
        // Remote desktop injects precise pixel scrolls with no gesture phase for mouse-wheel clicks.
        AvaloniaMouseMapping.GetMacWheelSource(isPrecise, hasGesturePhase).ShouldBe(expected);
    }

    [Fact]
    public void PinchToWheelDelta_ZoomsOneNotchPerFifteenPercentOfPinch()
    {
        AvaloniaMouseMapping.PinchToWheelDelta(0.15).ShouldBe(120);
        AvaloniaMouseMapping.PinchToWheelDelta(-0.15).ShouldBe(-120);
    }
}
