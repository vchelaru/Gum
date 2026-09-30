using System.Collections.Generic;
using Avalonia;
using Gum.Avalonia.Services;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Windows reports a precision touchpad's scroll as wheel messages locked to one axis for the first
/// second or so of a diagonal swipe, so the canvas pans from the fingers' own contact reports
/// instead, using the wheel events only to time and route the pan (#5477).
/// </summary>
public class TouchpadPanTrackerTests
{
    [Fact]
    public void TakePan_DiagonalSwipe_PansBothAxesWhileWindowsReportsOnlyOne()
    {
        TouchpadPanTracker tracker = new TouchpadPanTracker();
        tracker.OnFrame([new TouchpadContact(1, 10, 10), new TouchpadContact(2, 30, 10)]);
        tracker.OnFrame([new TouchpadContact(1, 13, 14), new TouchpadContact(2, 33, 14)]);

        Vector? pan = tracker.TakePan(new Vector(0, 0.1));

        pan.ShouldNotBeNull();
        pan.Value.X.ShouldBe(3 * TouchpadPanTracker.PixelsPerMillimeter, tolerance: 0.001);
        pan.Value.Y.ShouldBe(4 * TouchpadPanTracker.PixelsPerMillimeter, tolerance: 0.001);
        tracker.TakePan(new Vector(0, 0.1)).ShouldBe(new Vector(0, 0));
    }

    [Fact]
    public void TakePan_WheelOpposesFingers_FollowsTheSystemScrollDirection()
    {
        // With "down motion scrolls down", Windows sends the opposite sign to the fingers' motion.
        TouchpadPanTracker tracker = new TouchpadPanTracker();
        tracker.OnFrame([new TouchpadContact(1, 10, 10), new TouchpadContact(2, 30, 10)]);
        tracker.OnFrame([new TouchpadContact(1, 12, 12), new TouchpadContact(2, 32, 12)]);

        Vector? pan = tracker.TakePan(new Vector(0, -0.1));

        pan.ShouldNotBeNull();
        pan.Value.X.ShouldBe(-2 * TouchpadPanTracker.PixelsPerMillimeter, tolerance: 0.001);
        pan.Value.Y.ShouldBe(-2 * TouchpadPanTracker.PixelsPerMillimeter, tolerance: 0.001);
    }

    [Fact]
    public void TakePan_NotExactlyTwoFingersDown_ReturnsNullSoTheWheelDeltaIsUsed()
    {
        TouchpadPanTracker tracker = new TouchpadPanTracker();
        tracker.TakePan(new Vector(0, 0.1)).ShouldBeNull();

        tracker.OnFrame([new TouchpadContact(1, 10, 10), new TouchpadContact(2, 30, 10)]);
        tracker.OnFrame([new TouchpadContact(1, 10, 10), new TouchpadContact(2, 30, 10), new TouchpadContact(3, 50, 10)]);

        tracker.TakePan(new Vector(0, 0.1)).ShouldBeNull();
    }

    [Fact]
    public void TakePan_FingerSetChanged_DropsMotionFromBeforeTheChange()
    {
        TouchpadPanTracker tracker = new TouchpadPanTracker();
        tracker.OnFrame([new TouchpadContact(1, 10, 10), new TouchpadContact(2, 30, 10)]);
        tracker.OnFrame([new TouchpadContact(1, 10, 20), new TouchpadContact(2, 30, 20)]);
        tracker.OnFrame([]);
        tracker.OnFrame([new TouchpadContact(3, 50, 50), new TouchpadContact(4, 70, 50)]);

        tracker.TakePan(new Vector(0, 0.1)).ShouldBe(new Vector(0, 0));
    }

    [Fact]
    public void Discard_DropsTravelSoAPinchDoesNotPanAfterward()
    {
        TouchpadPanTracker tracker = new TouchpadPanTracker();
        tracker.OnFrame([new TouchpadContact(1, 10, 10), new TouchpadContact(2, 30, 10)]);
        tracker.OnFrame([new TouchpadContact(1, 5, 10), new TouchpadContact(2, 45, 12)]);

        tracker.Discard();

        tracker.TakePan(new Vector(0, 0.1)).ShouldBe(new Vector(0, 0));
    }

    [Fact]
    public void AddReport_ParallelMode_CompletesAFrameFromTheReportedSlotsOnly()
    {
        TouchpadFrameAssembler assembler = new TouchpadFrameAssembler();

        IReadOnlyList<TouchpadContact>? frame = assembler.AddReport(2,
        [
            new TouchpadSlot(1, IsTouching: true, 10, 10),
            new TouchpadSlot(2, IsTouching: true, 30, 10),
            new TouchpadSlot(0, IsTouching: false, 0, 0),
        ]);

        frame.ShouldNotBeNull();
        frame.Count.ShouldBe(2);
        frame[1].ShouldBe(new TouchpadContact(2, 30, 10));
    }

    [Fact]
    public void AddReport_HybridMode_WaitsForTheRestOfTheFrameAndDropsLiftedFingers()
    {
        // Hybrid mode sends one slot per report; only the frame's first report carries the count.
        TouchpadFrameAssembler assembler = new TouchpadFrameAssembler();

        assembler.AddReport(3, [new TouchpadSlot(1, IsTouching: true, 10, 10)]).ShouldBeNull();
        assembler.AddReport(0, [new TouchpadSlot(2, IsTouching: false, 30, 10)]).ShouldBeNull();
        IReadOnlyList<TouchpadContact>? frame = assembler.AddReport(0, [new TouchpadSlot(3, IsTouching: true, 50, 10)]);

        frame.ShouldNotBeNull();
        frame.ShouldBe([new TouchpadContact(1, 10, 10), new TouchpadContact(3, 50, 10)]);
    }
}
