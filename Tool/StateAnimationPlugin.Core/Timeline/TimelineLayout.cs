using FlatRedBall.Glue.StateInterpolation;
using StateAnimationPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StateAnimationPlugin.Timeline;

/// <summary>One row of the Animations tab's timeline: a category of state/event keyframes, or one sub-animation.</summary>
public sealed class TimelineRow
{
    /// <summary>Creates a row.</summary>
    public TimelineRow(string name, IReadOnlyList<AnimatedKeyframeViewModel> items)
    {
        Name = name;
        Items = items;
    }

    /// <summary>The row label: a state category, "Default", or a sub-animation's name.</summary>
    public string Name { get; }

    /// <summary>The row's keyframes, in time order.</summary>
    public IReadOnlyList<AnimatedKeyframeViewModel> Items { get; }
}

/// <summary>
/// What the timeline shows and where, independent of any UI framework: how keyframes group into
/// rows, and how times map to horizontal positions. Both heads' timeline views draw from this.
/// </summary>
public static class TimelineLayout
{
    /// <summary>The row name for uncategorized states and events.</summary>
    public const string DefaultCategoryName = "Default";

    /// <summary>One row per sub-animation keyframe, in time order.</summary>
    public static IReadOnlyList<TimelineRow> SubAnimationRows(IEnumerable<AnimatedKeyframeViewModel>? keyframes) =>
        (keyframes ?? Enumerable.Empty<AnimatedKeyframeViewModel>())
            .Where(keyframe => !string.IsNullOrEmpty(keyframe.AnimationName))
            .OrderBy(keyframe => keyframe.Time)
            .Select(keyframe => new TimelineRow(keyframe.AnimationName!, new[] { keyframe }))
            .ToList();

    /// <summary>State and event keyframes grouped by category, "Default" first, each row in time order.</summary>
    public static IReadOnlyList<TimelineRow> StateAndEventRows(IEnumerable<AnimatedKeyframeViewModel>? keyframes) =>
        (keyframes ?? Enumerable.Empty<AnimatedKeyframeViewModel>())
            .Where(keyframe => !string.IsNullOrEmpty(keyframe.StateName) || !string.IsNullOrEmpty(keyframe.EventName))
            .GroupBy(RowName)
            .OrderBy(group => group.Key == DefaultCategoryName ? 0 : 1)
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new TimelineRow(group.Key, group.OrderBy(keyframe => keyframe.Time).ToList()))
            .ToList();

    /// <summary>The row a keyframe belongs to: its sub-animation, its state's category, or "Default".</summary>
    public static string RowName(AnimatedKeyframeViewModel keyframe)
    {
        if (keyframe is { AnimationName.Length: > 0 })
        {
            return keyframe.AnimationName;
        }
        return keyframe.DisplayName.IndexOf('/') is var slash and > 0
            ? keyframe.DisplayName.Substring(0, slash)
            : DefaultCategoryName;
    }

    /// <summary>The x of <paramref name="time"/> on a track <paramref name="width"/> wide showing <paramref name="length"/> seconds.</summary>
    public static double TimeToX(double time, double length, double width) =>
        length <= 0 ? 0 : Math.Max(0, time / length * Math.Max(0, width));

    /// <summary>The width of a sub-animation lasting <paramref name="keyframeLength"/>; never under 2 so short ones stay visible.</summary>
    public static double LengthToWidth(double keyframeLength, double length, double width) =>
        length <= 0 ? 0 : Math.Max(2, keyframeLength / length * Math.Max(0, width));

    /// <summary>The left edge that centers a marker <paramref name="itemWidth"/> wide on <paramref name="time"/>.</summary>
    public static double CenteredLeft(double time, double length, double trackWidth, double itemWidth)
    {
        if (length <= 0)
        {
            return 0;
        }
        double x = time / length * Math.Max(0, trackWidth);
        // The marker's width may still be 0 on its first measure; then it is not shifted.
        return itemWidth > 0 ? x - itemWidth / 2 : x;
    }

    /// <summary>
    /// The x of each tick every <paramref name="interval"/> seconds from 0 to <paramref name="length"/>.
    /// None when the interval is not positive or is not shorter than the animation.
    /// </summary>
    public static IEnumerable<double> TickPositions(double length, double interval, double width)
    {
        if (interval <= 0 || length <= 0 || interval >= length)
        {
            yield break;
        }
        // Step with an integer counter so floating-point error does not accumulate.
        int count = (int)Math.Floor(length / interval);
        for (int i = 0; i <= count; i++)
        {
            yield return i * interval * width / length;
        }
    }
}

/// <summary>One interpolation segment of the timeline's curve: from a state keyframe to the next.</summary>
public sealed class InterpolationSegment
{
    /// <summary>Creates a segment over its sampled points.</summary>
    public InterpolationSegment(IReadOnlyList<(double X, double Y)> points, double startX, double endX)
    {
        Points = points;
        StartX = startX;
        EndX = endX;
    }

    /// <summary>The curve, left to right; y grows downward from 0 (value 1) to the track height (value 0).</summary>
    public IReadOnlyList<(double X, double Y)> Points { get; }

    /// <summary>The x of the segment's first keyframe.</summary>
    public double StartX { get; }

    /// <summary>The x of the next keyframe.</summary>
    public double EndX { get; }
}

/// <summary>
/// Samples the easing curve between consecutive state keyframes so each head can draw it as a filled
/// shape under a line. Pure: the same keyframes give the same points.
/// </summary>
public static class InterpolationCurve
{
    /// <summary>Points sampled per segment.</summary>
    public const int SamplesPerSegment = 32;

    /// <summary>
    /// The segments between consecutive state keyframes (events and sub-animations are skipped) on a
    /// track <paramref name="width"/> by <paramref name="height"/>. With <paramref name="clamp"/>,
    /// overshooting easings (back, elastic) are drawn clamped to the track.
    /// </summary>
    public static IReadOnlyList<InterpolationSegment> Segments(
        IEnumerable<AnimatedKeyframeViewModel>? keyframes, double animationLength, double width, double height, bool clamp)
    {
        List<InterpolationSegment> segments = new List<InterpolationSegment>();
        if (keyframes == null || animationLength <= 0 || width <= 0 || height <= 0)
        {
            return segments;
        }

        List<AnimatedKeyframeViewModel> states = keyframes
            .Where(keyframe => !string.IsNullOrEmpty(keyframe.StateName))
            .OrderBy(keyframe => keyframe.Time)
            .ToList();

        for (int i = 0; i < states.Count - 1; i++)
        {
            AnimatedKeyframeViewModel current = states[i];
            double startX = current.Time / animationLength * width;
            double endX = states[i + 1].Time / animationLength * width;
            if (endX <= startX)
            {
                continue;
            }

            TweeningFunction tween = Tweener.GetInterpolationFunction(current.InterpolationType, current.Easing);
            List<(double X, double Y)> points = new List<(double X, double Y)>(SamplesPerSegment + 1);
            for (int sample = 0; sample <= SamplesPerSegment; sample++)
            {
                float t = (float)sample / SamplesPerSegment;
                double value = tween(t, 0f, 1f, 1f);
                if (clamp)
                {
                    value = Math.Clamp(value, 0, 1);
                }
                points.Add((startX + (endX - startX) * t, (1 - value) * height));
            }
            segments.Add(new InterpolationSegment(points, startX, endX));
        }
        return segments;
    }
}
