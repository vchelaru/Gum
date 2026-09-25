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
        AvaloniaMouseMapping.ApplyWheelDelta(args, new Vector(0.2, -0.1), KeyModifiers.None, isTrackpadScroll: true, dpiScale: 2);

        args.IsPanScroll.ShouldBeTrue();
        args.PanX.ShouldBe(20f, tolerance: 0.001f);
        args.PanY.ShouldBe(-10f, tolerance: 0.001f);
        args.Delta.ShouldBe(0);
    }

    [Fact]
    public void ApplyWheelDelta_TrackpadScrollWithCmd_Zooms()
    {
        GumMouseEventArgs args = new GumMouseEventArgs();

        AvaloniaMouseMapping.ApplyWheelDelta(args, new Vector(0, 0.5), KeyModifiers.Meta, isTrackpadScroll: true, dpiScale: 2);

        args.IsPanScroll.ShouldBeFalse();
        args.Delta.ShouldBe(60);
    }

    [Fact]
    public void ApplyWheelDelta_MouseWheel_ZoomsAtOneHundredTwentyPerNotch()
    {
        GumMouseEventArgs args = new GumMouseEventArgs();

        AvaloniaMouseMapping.ApplyWheelDelta(args, new Vector(0, -1), KeyModifiers.None, isTrackpadScroll: false, dpiScale: 2);

        args.IsPanScroll.ShouldBeFalse();
        args.Delta.ShouldBe(-120);
    }

    [Fact]
    public void PinchToWheelDelta_ZoomsOneNotchPerFifteenPercentOfPinch()
    {
        AvaloniaMouseMapping.PinchToWheelDelta(0.15).ShouldBe(120);
        AvaloniaMouseMapping.PinchToWheelDelta(-0.15).ShouldBe(-120);
    }
}
