using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.StateInterpolation;
using Shouldly;
using StateAnimationPlugin.Timeline;
using StateAnimationPlugin.ViewModels;

namespace Gum.Presentation.Tests;

/// <summary>
/// The Animations tab's timeline math, shared by the WPF and Avalonia timelines: row grouping,
/// time-to-position mapping, ticks, and the sampled interpolation curve.
/// </summary>
public class TimelineLayoutTests
{
    [Fact]
    public void CenteredLeft_DoesNotShift_BeforeTheMarkerIsMeasured()
    {
        TimelineLayout.CenteredLeft(time: 1, length: 4, trackWidth: 400, itemWidth: 0).ShouldBe(100);
    }

    [Fact]
    public void CenteredLeft_ShiftsByHalfTheMarkerWidth()
    {
        TimelineLayout.CenteredLeft(time: 1, length: 4, trackWidth: 400, itemWidth: 10).ShouldBe(95);
    }

    [Fact]
    public void InterpolationSegments_AreEmpty_ForASingleState()
    {
        IReadOnlyList<InterpolationSegment> segments = InterpolationCurve.Segments(
            new[] { State("A", 0) }, animationLength: 1, width: 100, height: 20, clamp: true);

        segments.ShouldBeEmpty();
    }

    [Fact]
    public void InterpolationSegments_ClampAnOvershootingEasing_OnlyWhenAsked()
    {
        AnimatedKeyframeViewModel start = State("A", 0);
        start.InterpolationType = InterpolationType.Back;
        start.Easing = Easing.Out;
        AnimatedKeyframeViewModel[] keyframes = { start, State("B", 1) };

        IReadOnlyList<InterpolationSegment> clamped = InterpolationCurve.Segments(keyframes, 1, 100, 20, clamp: true);
        IReadOnlyList<InterpolationSegment> unclamped = InterpolationCurve.Segments(keyframes, 1, 100, 20, clamp: false);

        clamped[0].Points.ShouldAllBe(point => point.Y >= 0);
        unclamped[0].Points.ShouldContain(point => point.Y < 0);
    }

    [Fact]
    public void InterpolationSegments_ConnectConsecutiveStates_AndSkipEvents()
    {
        AnimatedKeyframeViewModel start = State("A", 0);
        start.InterpolationType = InterpolationType.Linear;
        AnimatedKeyframeViewModel[] keyframes = { start, Event("Fired", 0.5f), State("B", 1) };

        IReadOnlyList<InterpolationSegment> segments = InterpolationCurve.Segments(keyframes, animationLength: 1, width: 100, height: 20, clamp: true);

        segments.Count.ShouldBe(1);
        segments[0].StartX.ShouldBe(0);
        segments[0].EndX.ShouldBe(100);
        segments[0].Points.Count.ShouldBe(InterpolationCurve.SamplesPerSegment + 1);
        segments[0].Points[0].Y.ShouldBe(20);
        segments[0].Points[^1].Y.ShouldBe(0, tolerance: 0.0001);
    }

    [Fact]
    public void LengthToWidth_KeepsVeryShortSubAnimationsVisible()
    {
        TimelineLayout.LengthToWidth(keyframeLength: 0.001, length: 10, width: 100).ShouldBe(2);
    }

    [Fact]
    public void RowName_IsDefault_ForAnUncategorizedState()
    {
        TimelineLayout.RowName(State("Hover", 0)).ShouldBe(TimelineLayout.DefaultCategoryName);
    }

    [Fact]
    public void RowName_UsesTheStatesCategory()
    {
        TimelineLayout.RowName(State("Colors/Red", 0)).ShouldBe("Colors");
    }

    [Fact]
    public void StateAndEventRows_PutDefaultFirst_AndOrderEachRowByTime()
    {
        AnimatedKeyframeViewModel late = State("Colors/Blue", 2);
        AnimatedKeyframeViewModel early = State("Colors/Red", 1);
        AnimatedKeyframeViewModel uncategorized = State("Hover", 0);

        IReadOnlyList<TimelineRow> rows = TimelineLayout.StateAndEventRows(new[] { late, early, uncategorized, SubAnimation("Pulse", 0) });

        rows.Select(row => row.Name).ShouldBe(new[] { TimelineLayout.DefaultCategoryName, "Colors" });
        rows[1].Items.ShouldBe(new[] { early, late });
    }

    [Fact]
    public void SubAnimationRows_GiveEachSubAnimationItsOwnRow_InTimeOrder()
    {
        AnimatedKeyframeViewModel second = SubAnimation("Pulse", 2);
        AnimatedKeyframeViewModel first = SubAnimation("Fade", 1);

        IReadOnlyList<TimelineRow> rows = TimelineLayout.SubAnimationRows(new[] { second, State("A", 0), first });

        rows.Select(row => row.Name).ShouldBe(new[] { "Fade", "Pulse" });
    }

    [Fact]
    public void TickPositions_AreEmpty_WhenTheIntervalIsNotShorterThanTheAnimation()
    {
        TimelineLayout.TickPositions(length: 1, interval: 1, width: 100).ShouldBeEmpty();
    }

    [Fact]
    public void TickPositions_AreEvenlySpacedAcrossTheTrack()
    {
        TimelineLayout.TickPositions(length: 2, interval: 0.5, width: 100).ShouldBe(new double[] { 0, 25, 50, 75, 100 });
    }

    [Fact]
    public void TimeToX_IsZero_ForAnEmptyAnimation()
    {
        TimelineLayout.TimeToX(time: 1, length: 0, width: 100).ShouldBe(0);
    }

    private static AnimatedKeyframeViewModel State(string name, float time) => new AnimatedKeyframeViewModel { StateName = name, Time = time };

    private static AnimatedKeyframeViewModel Event(string name, float time) => new AnimatedKeyframeViewModel { EventName = name, Time = time };

    private static AnimatedKeyframeViewModel SubAnimation(string name, float time) => new AnimatedKeyframeViewModel { AnimationName = name, Time = time };
}
