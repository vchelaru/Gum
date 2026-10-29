using System;

#nullable enable

namespace Gum.Graphics.Animation;

/// <summary>A playback instant mapped onto the chain's forward timeline.</summary>
/// <param name="ForwardTime">Seconds into the forward timeline (0 = start of the first frame, T = end of the last).</param>
/// <param name="IsBackward">Whether the timeline is being played mirrored at this instant.</param>
public readonly record struct TimelinePoint(double ForwardTime, bool IsBackward);

/// <summary>
/// Reverse and PingPong as mirrors of the chain's forward timeline (length T, the sum of the frame
/// lengths). Mirrors FlatRedBall2's <c>PlaybackTimeline.Map</c> so both runtimes agree.
/// <list type="bullet">
/// <item>Reverse: cycle time t shows forward time T - t.</item>
/// <item>PingPong: forward, then mirrored, for a cycle of 2T.</item>
/// <item>PingPong and Reverse: the mirrored half first.</item>
/// </list>
/// </summary>
public static class PlaybackTimeline
{
    /// <summary>Length of one cycle: T, or 2T for a ping-pong.</summary>
    public static double CycleLength(double forwardLength, bool pingPong) =>
        pingPong ? 2 * forwardLength : forwardLength;

    /// <summary>
    /// The forward time shown <paramref name="cycleTime"/> seconds into a cycle (clamped to the
    /// cycle), and whether the timeline is mirrored there. A ping-pong's turnaround instant already
    /// counts as mirrored.
    /// </summary>
    public static TimelinePoint Map(double cycleTime, double forwardLength, bool pingPong, bool reverse)
    {
        double t = forwardLength;
        double u = System.Math.Clamp(cycleTime, 0, CycleLength(t, pingPong));
        if (!pingPong)
        {
            return reverse ? new TimelinePoint(t - u, true) : new TimelinePoint(u, false);
        }
        bool firstHalf = u < t;
        if (!reverse)
        {
            return firstHalf ? new TimelinePoint(u, false) : new TimelinePoint(2 * t - u, true);
        }
        return firstHalf ? new TimelinePoint(t - u, true) : new TimelinePoint(u - t, false);
    }

    /// <summary>The first cycle time that shows <paramref name="forwardTime"/>.</summary>
    public static double ToCycleTime(double forwardTime, double forwardLength, bool reverse) =>
        reverse ? forwardLength - forwardTime : forwardTime;

    /// <summary>
    /// Whether keyframe glides wrap from the last keyframe back to the first. Only a looping chain
    /// that is not a ping-pong does; a ping-pong turns around instead.
    /// </summary>
    public static bool GlidesAcrossLoop(bool loop, bool pingPong) => loop && !pingPong;
}
