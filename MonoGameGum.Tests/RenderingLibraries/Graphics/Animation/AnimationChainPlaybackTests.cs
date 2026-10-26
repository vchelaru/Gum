using System.Collections.Generic;
using Gum.Graphics.Animation;
using RenderingLibrary.Graphics.Animation;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Graphics.Animation;

// Pins PingPong/Reverse playback against FlatRedBall1's AnimationChainPlaybackTests so every runtime
// agrees. All frame lengths are binary-exact so times on a frame boundary are exact.
public class AnimationChainPlaybackTests
{
    private static AnimationChainLogic Play(bool pingPong, bool reverse, params float[] frameLengths)
    {
        AnimationChain chain = new() { Name = "c", PingPong = pingPong, IsReversed = reverse };
        foreach (float length in frameLengths)
        {
            chain.Add(new AnimationFrame { FrameLength = length });
        }
        AnimationChainLogic logic = new() { AnimationChains = new AnimationChainList { chain } };
        logic.CurrentChainName = "c";
        logic.Animate = true;
        return logic;
    }

    private static int FrameAt(AnimationChainLogic logic, double cycleTime)
    {
        logic.TimeIntoAnimation = cycleTime;
        return logic.CurrentFrameIndex;
    }

    // Frames of 1s, 2s, 1s: T = 4.
    [Theory]
    [InlineData(false, false, 0.0, 0)]
    [InlineData(false, false, 0.5, 0)]
    [InlineData(false, false, 1.0, 1)]
    [InlineData(false, false, 2.9, 1)]
    [InlineData(false, false, 3.0, 2)]
    [InlineData(false, false, 3.9, 2)]
    [InlineData(false, true, 0.0, 2)]
    [InlineData(false, true, 0.9, 2)]
    [InlineData(false, true, 1.0, 1)]
    [InlineData(false, true, 2.9, 1)]
    [InlineData(false, true, 3.0, 0)]
    [InlineData(false, true, 3.9, 0)]
    [InlineData(true, false, 0.0, 0)]
    [InlineData(true, false, 3.9, 2)]
    [InlineData(true, false, 4.0, 2)] // the end frame shows double across the turnaround
    [InlineData(true, false, 4.9, 2)]
    [InlineData(true, false, 5.0, 1)]
    [InlineData(true, false, 6.9, 1)]
    [InlineData(true, false, 7.0, 0)]
    [InlineData(true, false, 7.9, 0)]
    [InlineData(true, false, 8.0, 0)] // wraps to the start of the next cycle
    [InlineData(true, true, 0.0, 2)]
    [InlineData(true, true, 1.0, 1)]
    [InlineData(true, true, 3.9, 0)]
    [InlineData(true, true, 4.0, 0)] // the first frame shows double across the turnaround
    [InlineData(true, true, 4.9, 0)]
    [InlineData(true, true, 5.0, 1)]
    [InlineData(true, true, 7.0, 2)]
    [InlineData(true, true, 7.9, 2)]
    public void TimeIntoAnimation_ShouldMapCycleTimeToFrame(bool pingPong, bool reverse, double cycleTime, int expectedFrame)
    {
        AnimationChainLogic logic = Play(pingPong, reverse, 1f, 2f, 1f);

        FrameAt(logic, cycleTime).ShouldBe(expectedFrame);
    }

    [Fact]
    public void TimeIntoAnimation_ExactBoundary_ShouldEnterTheNextFrameInPlaybackOrder()
    {
        AnimationChainLogic pingPong = Play(pingPong: true, reverse: false, 0.5f, 0.5f);

        FrameAt(pingPong, 0.5).ShouldBe(1); // forward: A -> B
        FrameAt(pingPong, 1.0).ShouldBe(1); // turnaround: B stays, now mirrored
        FrameAt(pingPong, 1.5).ShouldBe(0); // backward: B -> A

        AnimationChainLogic reverse = Play(pingPong: false, reverse: true, 0.5f, 0.5f);

        FrameAt(reverse, 0.0).ShouldBe(1); // reverse starts on the last frame
        FrameAt(reverse, 0.5).ShouldBe(0); // backward: B -> A
    }

    // Two 0.5s frames stepped at 0.25s for 4s: every step boundary is exact. The turnaround and the
    // ping-pong wrap keep the same frame on screen, so they apply nothing.
    [Theory]
    [InlineData(false, false, new[] { 1, 0, 1, 0, 1, 0, 1, 0 })]
    [InlineData(false, true, new[] { 0, 1, 0, 1, 0, 1, 0, 1 })]
    [InlineData(true, false, new[] { 1, 0, 1, 0 })]
    [InlineData(true, true, new[] { 0, 1, 0, 1 })]
    public void AnimateSelf_ShouldApplyFrameOnlyWhenTheShownFrameChanges(bool pingPong, bool reverse, int[] expectedApplied)
    {
        AnimationChainLogic logic = Play(pingPong, reverse, 0.5f, 0.5f);
        // Selecting a chain keeps frame index 0, which a Reverse chain does not show at time 0;
        // seeking the time first makes the shown frame and the index agree.
        logic.TimeIntoAnimation = 0;
        AnimationChain chain = logic.CurrentChain!;
        List<int> applied = new();
        logic.ApplyFrame = frame => applied.Add(chain.IndexOf(frame));

        for (int i = 0; i < 16; i++)
        {
            logic.AnimateSelf(0.25);
        }

        applied.ShouldBe(expectedApplied);
    }

    [Theory]
    [InlineData(false, 50f)] // a plain loop glides from the last keyframe back to the first
    [InlineData(true, 100f)] // a ping-pong turns around instead, so the last keyframe holds
    public void CurrentOffset_LastFrame_ShouldWrapGlideOnlyWhenNotPingPong(bool pingPong, float expectedX)
    {
        AnimationChain chain = new() { Name = "c", PingPong = pingPong };
        chain.Add(new AnimationFrame { FrameLength = 1f, RelativeX = 0, OffsetInterpolation = "Linear" });
        chain.Add(new AnimationFrame { FrameLength = 1f, RelativeX = 100, OffsetInterpolation = "Linear" });
        AnimationChainLogic logic = new() { AnimationChains = new AnimationChainList { chain } };
        logic.CurrentChainName = "c";

        logic.TimeIntoAnimation = 1.5;

        logic.CurrentFrameIndex.ShouldBe(1);
        logic.CurrentOffset.X.ShouldBe(expectedX, 0.001f);
    }
}
