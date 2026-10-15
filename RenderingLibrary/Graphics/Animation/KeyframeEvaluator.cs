using System;
using Gum.Content.AnimationChain;

#nullable enable

namespace Gum.Graphics.Animation
{
    /// <summary>
    /// The color channels at one instant of playback. A null channel means no keyframe so far set it.
    /// <see cref="Operation"/> is the color operation in effect, inherited through skipped frames.
    /// </summary>
    public readonly struct KeyframeColor
    {
        public int? Red { get; }
        public int? Green { get; }
        public int? Blue { get; }
        public int? Alpha { get; }
        public AnimationFrameColorOperation? Operation { get; }

        public KeyframeColor(int? red, int? green, int? blue, int? alpha, AnimationFrameColorOperation? operation)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
            Operation = operation;
        }
    }

    /// <summary>
    /// Evaluates per-frame keyframe interpolation (FlatRedBall2 #1478). Every frame is a keyframe
    /// unless it opts out per group; a keyframe's curve runs to the next keyframe of the same group,
    /// spanning the summed frame lengths in between. Groups (Offset: RelativeX/Y; Color: red, green,
    /// blue, alpha) are independent. The first frame is always a keyframe; a non-looping chain's last
    /// keyframe holds; a looping chain's last keyframe glides back to the first. With the default (snap)
    /// curve the result is exactly the frame's own value, so files without interpolation evaluate as
    /// they always did. Frame lengths and elapsed time are in the same unit (seconds at runtime, since
    /// loading converts millisecond chains).
    /// </summary>
    public static class KeyframeEvaluator
    {
        /// <summary>
        /// Whether any frame of <paramref name="chain"/> changes how the offset plays: a non-snap
        /// curve, or a skip flag on a frame other than the first. A chain without any evaluates as
        /// each frame's own <see cref="AnimationFrame.RelativeX"/>/<see cref="AnimationFrame.RelativeY"/>.
        /// </summary>
        public static bool HasOffsetKeyframes(AnimationChain chain)
        {
            for (int i = 0; i < chain.Count; i++)
            {
                AnimationFrame frame = chain[i];
                if (!frame.OffsetCurve.IsSnap || (i > 0 && frame.SkipOffsetKeyframe))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Whether any frame of <paramref name="chain"/> changes how the color plays: a non-snap
        /// curve, or a skip flag on a frame other than the first.
        /// </summary>
        public static bool HasColorKeyframes(AnimationChain chain)
        {
            for (int i = 0; i < chain.Count; i++)
            {
                AnimationFrame frame = chain[i];
                if (!frame.ColorCurve.IsSnap || (i > 0 && frame.SkipColorKeyframe))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The offset to display <paramref name="elapsedInFrame"/> into frame <paramref name="frameIndex"/>.
        /// An empty chain yields (0, 0); an out-of-range index clamps.
        /// </summary>
        public static (float X, float Y) EvaluateOffset(AnimationChain chain, bool loop, int frameIndex, double elapsedInFrame)
        {
            if (chain.Count == 0)
            {
                return (0f, 0f);
            }
            frameIndex = Math.Max(0, Math.Min(frameIndex, chain.Count - 1));

            FindSegment(chain, loop, frameIndex, elapsedInFrame, isColor: false, out int start, out int end, out float t);

            // Snap holds even at t == 1: the next keyframe takes over only when playback moves onto its frame.
            AnimationFrame from = chain[start];
            if (end < 0 || from.OffsetCurve.IsSnap)
            {
                return (from.RelativeX, from.RelativeY);
            }

            float eased = from.OffsetCurve.Evaluate(t);
            AnimationFrame to = chain[end];
            return (Lerp(from.RelativeX, to.RelativeX, eased), Lerp(from.RelativeY, to.RelativeY, eased));
        }

        /// <summary>
        /// The color channels to display, same contract as <see cref="EvaluateOffset"/>. A null channel
        /// inherits the most recent earlier color keyframe that set it. Each channel glides
        /// independently; a channel unset at one end counts as the operation's identity (255, or 0 for
        /// Add) and a channel unset at both stays null. Results round away from zero. The operation is
        /// never interpolated: it is the one in effect at <paramref name="frameIndex"/>.
        /// </summary>
        public static KeyframeColor EvaluateColor(AnimationChain chain, bool loop, int frameIndex, double elapsedInFrame)
        {
            if (chain.Count == 0)
            {
                return default;
            }
            frameIndex = Math.Max(0, Math.Min(frameIndex, chain.Count - 1));

            FindSegment(chain, loop, frameIndex, elapsedInFrame, isColor: true, out int start, out int end, out float t);

            KeyframeColor from = Resolve(chain, start);
            AnimationFrameColorOperation? operation = frameIndex == start ? from.Operation : ResolveOperation(chain, frameIndex);
            AnimationFrame startFrame = chain[start];
            if (end < 0 || startFrame.ColorCurve.IsSnap)
            {
                return new KeyframeColor(from.Red, from.Green, from.Blue, from.Alpha, operation);
            }

            float eased = startFrame.ColorCurve.Evaluate(t);
            KeyframeColor to = Resolve(chain, end);
            int fromIdentity = ChannelDefault(from.Operation);
            int toIdentity = ChannelDefault(to.Operation);
            return new KeyframeColor(
                LerpChannel(from.Red, to.Red, fromIdentity, toIdentity, eased),
                LerpChannel(from.Green, to.Green, fromIdentity, toIdentity, eased),
                LerpChannel(from.Blue, to.Blue, fromIdentity, toIdentity, eased),
                LerpChannel(from.Alpha, to.Alpha, 255, 255, eased),
                operation);
        }

        /// <summary>
        /// A frame's color after inheritance: each channel is the most recent value set at or before the
        /// frame in its chain, or null if none was. A color-skipped frame other than the first
        /// contributes no channels, but its operation still applies.
        /// </summary>
        public static KeyframeColor Resolve(AnimationChain chain, int frameIndex)
        {
            int? red = null;
            int? green = null;
            int? blue = null;
            int? alpha = null;
            AnimationFrameColorOperation? operation = null;

            for (int i = Math.Min(frameIndex, chain.Count - 1); i >= 0; i--)
            {
                AnimationFrame frame = chain[i];
                if (i == 0 || !frame.SkipColorKeyframe)
                {
                    red = red ?? frame.Red;
                    green = green ?? frame.Green;
                    blue = blue ?? frame.Blue;
                    alpha = alpha ?? frame.Alpha;
                }
                operation = operation ?? frame.ColorOperation;
            }

            return new KeyframeColor(red, green, blue, alpha, operation);
        }

        private static AnimationFrameColorOperation? ResolveOperation(AnimationChain chain, int frameIndex)
        {
            for (int i = Math.Min(frameIndex, chain.Count - 1); i >= 0; i--)
            {
                if (chain[i].ColorOperation.HasValue)
                {
                    return chain[i].ColorOperation;
                }
            }
            return null;
        }

        /// <summary>The R/G/B identity for an operation, i.e. what an unset channel contributes: 0 for Add, 255 otherwise.</summary>
        private static int ChannelDefault(AnimationFrameColorOperation? operation)
            => operation == AnimationFrameColorOperation.Add ? 0 : 255;

        // Locates the keyframe segment containing frameIndex: `start` is the controlling keyframe, `end`
        // the keyframe it glides to (-1 = nothing to glide to, hold `start`), `t` the linear progress
        // through it.
        private static void FindSegment(AnimationChain chain, bool loop, int frameIndex, double elapsedInFrame,
            bool isColor, out int start, out int end, out float t)
        {
            start = frameIndex;
            while (start > 0 && IsSkipped(chain[start], isColor))
            {
                start--;
            }

            end = frameIndex + 1;
            while (end < chain.Count && IsSkipped(chain[end], isColor))
            {
                end++;
            }
            if (end >= chain.Count)
            {
                end = loop ? 0 : -1;
            }

            t = 0f;
            // Nothing to glide to: a non-looping chain's last keyframe, or a looping chain with one keyframe.
            if (end < 0 || end == start)
            {
                end = -1;
                return;
            }

            int segmentEnd = end == 0 ? chain.Count : end;
            double duration = 0;
            double position = elapsedInFrame;
            for (int i = start; i < segmentEnd; i++)
            {
                double length = chain[i].FrameLength;
                duration += length;
                if (i < frameIndex)
                {
                    position += length;
                }
            }

            t = duration > 0 ? (float)Math.Max(0.0, Math.Min(position / duration, 1.0)) : 0f;
        }

        private static bool IsSkipped(AnimationFrame frame, bool isColor)
            => isColor ? frame.SkipColorKeyframe : frame.SkipOffsetKeyframe;

        private static float Lerp(float from, float to, float eased)
            => eased <= 0f ? from : eased >= 1f ? to : from + (to - from) * eased;

        private static int? LerpChannel(int? from, int? to, int fromIdentity, int toIdentity, float eased)
        {
            if (from == null && to == null)
            {
                return null;
            }
            if (eased <= 0f)
            {
                return from;
            }
            int a = from ?? fromIdentity;
            int b = to ?? toIdentity;
            if (eased >= 1f)
            {
                return b;
            }
            return (int)Math.Round(a + (b - a) * (double)eased, MidpointRounding.AwayFromZero);
        }
    }
}
