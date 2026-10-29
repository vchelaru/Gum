using Gum.Graphics.Animation;
using RenderingLibrary.Graphics.Animation;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Graphics.Animation;

// Pins issue #4708: AnimationChainLogic seeds IsAnimationChainLooping from the newly-selected
// chain's authored Loop value each time CurrentChainName switches chains, instead of always
// defaulting true, while still allowing a per-instance override afterward.
public class AnimationChainLogicTests
{
    private static AnimationChainList MakeChains(params (string name, bool loop)[] chains)
    {
        var list = new AnimationChainList();
        foreach ((string name, bool loop) in chains)
        {
            var chain = new AnimationChain { Name = name, Loop = loop };
            chain.Add(new AnimationFrame { FrameLength = 0.1f });
            list.Add(chain);
        }
        return list;
    }

    [Fact]
    public void CurrentChainName_Set_SeedsIsAnimationChainLoopingFromChainLoop()
    {
        var sut = new AnimationChainLogic { AnimationChains = MakeChains(("Attack", false)) };

        sut.CurrentChainName = "Attack";

        sut.IsAnimationChainLooping.ShouldBeFalse();
    }

    [Fact]
    public void CurrentChainName_Set_ReseedsIsAnimationChainLoopingOnEachChainSwitch()
    {
        var sut = new AnimationChainLogic { AnimationChains = MakeChains(("Attack", false), ("Walk", true)) };
        sut.CurrentChainName = "Attack";

        sut.CurrentChainName = "Walk";

        sut.IsAnimationChainLooping.ShouldBeTrue();
    }

    [Fact]
    public void IsAnimationChainLooping_CanStillBeOverridden_AfterCurrentChainNameSeedsIt()
    {
        var sut = new AnimationChainLogic { AnimationChains = MakeChains(("Attack", false)) };
        sut.CurrentChainName = "Attack";

        sut.IsAnimationChainLooping = true;

        sut.IsAnimationChainLooping.ShouldBeTrue();
    }

    // One chain whose frames have the given lengths; frames are distinguishable by reference.
    private static AnimationChainList MakeChainWithFrameLengths(params float[] frameLengths)
    {
        AnimationChain chain = new() { Name = "Chain" };
        foreach (float frameLength in frameLengths)
        {
            chain.Add(new AnimationFrame { FrameLength = frameLength });
        }
        AnimationChainList list = new();
        list.Add(chain);
        return list;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void CurrentFrameIndex_Set_ShouldApplyThatFrame(int frameIndex)
    {
        AnimationChainList chains = MakeChainWithFrameLengths(0.5f, 1.0f, 0.75f);
        AnimationFrame? applied = null;
        AnimationChainLogic sut = new() { AnimationChains = chains, ApplyFrame = frame => applied = frame };

        sut.CurrentFrameIndex = frameIndex;

        applied.ShouldBeSameAs(chains[0][frameIndex]);
    }

    [Theory]
    [InlineData(5, 2, 2.0)] // past the end shows and reports the last frame
    [InlineData(3, 2, 2.0)]
    [InlineData(-1, 0, 0.0)] // negative shows and reports the first frame
    public void CurrentFrameIndex_Set_ShouldClampToChain_WhenOutOfRange(int frameIndex, int expectedFrameIndex, double expectedTime)
    {
        AnimationChainList chains = MakeChainWithFrameLengths(1, 1, 1);
        AnimationFrame? applied = null;
        AnimationChainLogic sut = new() { AnimationChains = chains, ApplyFrame = frame => applied = frame };
        sut.CurrentFrameIndex = 1;

        sut.CurrentFrameIndex = frameIndex;

        sut.CurrentFrameIndex.ShouldBe(expectedFrameIndex);
        sut.TimeIntoAnimation.ShouldBe(expectedTime);
        applied.ShouldBeSameAs(chains[0][expectedFrameIndex]);
    }

    [Fact]
    public void CurrentChainName_SetToDifferentChain_ShouldRestartAtFirstFrame()
    {
        AnimationChain longChain = new() { Name = "Long" };
        longChain.Add(new AnimationFrame { FrameLength = 1 });
        longChain.Add(new AnimationFrame { FrameLength = 1 });
        longChain.Add(new AnimationFrame { FrameLength = 1 });
        AnimationChain shortChain = new() { Name = "Short" };
        shortChain.Add(new AnimationFrame { FrameLength = 1 });
        shortChain.Add(new AnimationFrame { FrameLength = 1 });
        AnimationFrame? applied = null;
        AnimationChainLogic sut = new()
        {
            AnimationChains = new AnimationChainList { longChain, shortChain },
            ApplyFrame = frame => applied = frame
        };
        sut.CurrentChainName = "Long";
        sut.CurrentFrameIndex = 2;

        sut.CurrentChainName = "Short";

        sut.CurrentFrameIndex.ShouldBe(0);
        sut.TimeIntoAnimation.ShouldBe(0);
        applied.ShouldBeSameAs(shortChain[0]);
    }

    [Fact]
    public void CurrentChainName_SetToSameChain_ShouldKeepPlaybackAndNotReapply()
    {
        AnimationChainList chains = MakeChainWithFrameLengths(1, 1, 1);
        int applied = 0;
        AnimationChainLogic sut = new() { AnimationChains = chains, ApplyFrame = _ => applied++ };
        sut.CurrentChainName = "Chain";
        sut.TimeIntoAnimation = 1.5;
        applied = 0;

        sut.CurrentChainName = "Chain";

        sut.CurrentFrameIndex.ShouldBe(1);
        sut.TimeIntoAnimation.ShouldBe(1.5);
        applied.ShouldBe(0);
    }

    [Fact]
    public void UpdateToCurrentAnimationFrame_ShouldClampFrameIndex_WhenAnimationChainsSwappedToShorterChain()
    {
        AnimationChainList shorter = MakeChainWithFrameLengths(1, 1);
        AnimationFrame? applied = null;
        AnimationChainLogic sut = new()
        {
            AnimationChains = MakeChainWithFrameLengths(1, 1, 1, 1),
            ApplyFrame = frame => applied = frame
        };
        sut.CurrentFrameIndex = 3;

        sut.AnimationChains = shorter;
        sut.UpdateToCurrentAnimationFrame();

        sut.CurrentFrameIndex.ShouldBe(1);
        sut.TimeIntoAnimation.ShouldBe(1);
        applied.ShouldBeSameAs(shorter[0][1]);
    }

    [Fact]
    public void AnimateSelf_ShouldNotThrow_WhenAnimationChainsSwappedToFewerChains()
    {
        AnimationChainList twoChains = MakeChains(("A", true), ("B", true));
        AnimationChainLogic sut = new() { AnimationChains = twoChains, Animate = true };
        sut.CurrentChainName = "B";

        sut.AnimationChains = MakeChains(("A", true));

        Should.NotThrow(() => sut.AnimateSelf(0.1)).ShouldBeFalse();
    }

    [Fact]
    public void CurrentFrameIndex_Set_ShouldNotApply_WhenNoChainsAreSet()
    {
        bool wasApplied = false;
        AnimationChainLogic sut = new() { ApplyFrame = _ => wasApplied = true };

        sut.CurrentFrameIndex = 1;

        wasApplied.ShouldBeFalse();
        sut.CurrentFrameIndex.ShouldBe(1);
    }

    [Theory]
    [InlineData(1.2, true, 1)] // mid-frame
    [InlineData(3.0, true, 1)] // past the 2.25s end, looping wraps to 0.75s
    [InlineData(3.0, false, 2)] // past the end, not looping holds the last frame
    public void TimeIntoAnimation_Set_ShouldApplyFrameAtThatTime(double time, bool isLooping, int expectedFrameIndex)
    {
        AnimationChainList chains = MakeChainWithFrameLengths(0.5f, 1.0f, 0.75f);
        AnimationFrame? applied = null;
        AnimationChainLogic sut = new()
        {
            AnimationChains = chains,
            IsAnimationChainLooping = isLooping,
            ApplyFrame = frame => applied = frame
        };

        sut.TimeIntoAnimation = time;

        sut.CurrentFrameIndex.ShouldBe(expectedFrameIndex);
        applied.ShouldBeSameAs(chains[0][expectedFrameIndex]);
        sut.TimeIntoAnimation.ShouldBe(time);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TimeIntoAnimation_Set_ShouldClampToZero_WhenNegative(bool isLooping)
    {
        AnimationChainList chains = MakeChainWithFrameLengths(1, 1, 1);
        AnimationFrame? applied = null;
        AnimationChainLogic sut = new()
        {
            AnimationChains = chains,
            IsAnimationChainLooping = isLooping,
            ApplyFrame = frame => applied = frame
        };
        sut.CurrentFrameIndex = 1;

        sut.TimeIntoAnimation = -1;

        sut.TimeIntoAnimation.ShouldBe(0);
        sut.CurrentFrameIndex.ShouldBe(0);
        applied.ShouldBeSameAs(chains[0][0]);
        sut.Animate = true;
        Should.NotThrow(() => sut.AnimateSelf(0.5));
        sut.TimeIntoAnimation.ShouldBe(0.5);
    }

    [Fact]
    public void TimeIntoAnimation_Set_ShouldStoreRawNegative_WhenNoChainsAreSet()
    {
        AnimationChainLogic sut = new();

        sut.TimeIntoAnimation = -1;
        sut.TimeIntoAnimation.ShouldBe(-1);

        sut.AnimationChains = MakeChainWithFrameLengths(1, 1, 1);
        sut.UpdateToCurrentAnimationFrame();
        sut.TimeIntoAnimation.ShouldBe(0);
        sut.CurrentFrameIndex.ShouldBe(0);
    }

    [Fact]
    public void AnimateSelf_ShouldClampTimeToZero_WhenNegativeSpeedPassesStartOfNonLoopingChain()
    {
        AnimationChainLogic sut = new()
        {
            AnimationChains = MakeChainWithFrameLengths(1, 1, 1),
            IsAnimationChainLooping = false,
            AnimationSpeed = -1,
            Animate = true
        };
        sut.CurrentFrameIndex = 1;

        Should.NotThrow(() => sut.AnimateSelf(1.5));

        sut.TimeIntoAnimation.ShouldBe(0);
        sut.CurrentFrameIndex.ShouldBe(0);
    }

    [Fact]
    public void AnimateSelf_ShouldWrapFromTheEnd_WhenNegativeSpeedPassesMoreThanOneLoopOfLoopingChain()
    {
        AnimationChainList chains = MakeChainWithFrameLengths(1, 1, 1);
        AnimationFrame? applied = null;
        AnimationChainLogic sut = new()
        {
            AnimationChains = chains,
            AnimationSpeed = -1,
            Animate = true,
            ApplyFrame = frame => applied = frame
        };

        Should.NotThrow(() => sut.AnimateSelf(4.5));

        sut.TimeIntoAnimation.ShouldBe(1.5);
        sut.CurrentFrameIndex.ShouldBe(1);
        applied.ShouldBeSameAs(chains[0][1]);
    }

    [Fact]
    public void TimeIntoAnimation_Set_ShouldNotApply_WhenNoChainsAreSet()
    {
        bool wasApplied = false;
        AnimationChainLogic sut = new() { ApplyFrame = _ => wasApplied = true };

        sut.TimeIntoAnimation = 1.5;

        wasApplied.ShouldBeFalse();
    }

    [Theory]
    [InlineData(1f, 2)] // forward past the end holds the last frame
    [InlineData(-1f, 0)] // backward past the start holds the first frame
    public void AnimateSelf_ShouldRaiseFinishedOnceAndNotCycled_WhenNonLoopingChainReachesItsEnd(float speed, int expectedFrameIndex)
    {
        AnimationChainList chains = MakeChainWithFrameLengths(1, 1, 1);
        AnimationChainLogic sut = new()
        {
            AnimationChains = chains,
            IsAnimationChainLooping = false,
            AnimationSpeed = speed,
            Animate = true
        };
        sut.CurrentFrameIndex = 1;
        int cycledCount = 0;
        int finishedCount = 0;
        bool animateWhenFinished = true;
        sut.AnimationChainCycled += () => cycledCount++;
        sut.AnimationChainFinished += () =>
        {
            finishedCount++;
            animateWhenFinished = sut.Animate;
        };

        sut.AnimateSelf(0.5);
        finishedCount.ShouldBe(0);
        sut.AnimateSelf(2);
        sut.AnimateSelf(2);

        finishedCount.ShouldBe(1);
        animateWhenFinished.ShouldBeFalse();
        cycledCount.ShouldBe(0);
        sut.Animate.ShouldBeFalse();
        sut.CurrentFrameIndex.ShouldBe(expectedFrameIndex);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(-1f)]
    public void AnimateSelf_ShouldRaiseCycledAndNotFinished_WhenLoopingChainWraps(float speed)
    {
        AnimationChainLogic sut = new()
        {
            AnimationChains = MakeChainWithFrameLengths(1, 1, 1),
            IsAnimationChainLooping = true,
            AnimationSpeed = speed,
            Animate = true
        };
        sut.CurrentFrameIndex = 1;
        int cycledCount = 0;
        int finishedCount = 0;
        sut.AnimationChainCycled += () => cycledCount++;
        sut.AnimationChainFinished += () => finishedCount++;

        sut.AnimateSelf(2);

        cycledCount.ShouldBe(1);
        finishedCount.ShouldBe(0);
        sut.Animate.ShouldBeTrue();
    }

    [Fact]
    public void AnimateSelf_ShouldNotRaiseFinished_WhenTimeStoredNegativeBeforeChainAndPlayingForward()
    {
        AnimationChainLogic sut = new() { IsAnimationChainLooping = false, Animate = true };
        sut.TimeIntoAnimation = -5;
        sut.AnimationChains = MakeChainWithFrameLengths(1, 1, 1);
        int finishedCount = 0;
        sut.AnimationChainFinished += () => finishedCount++;

        sut.AnimateSelf(0.5);

        finishedCount.ShouldBe(0);
        sut.Animate.ShouldBeTrue();
    }

    [Fact]
    public void Clone_ShouldNotCarryOverEventSubscribers()
    {
        AnimationChainLogic original = new()
        {
            AnimationChains = MakeChainWithFrameLengths(1),
            IsAnimationChainLooping = false,
            Animate = true
        };
        int originalCycled = 0;
        int originalFinished = 0;
        original.AnimationChainCycled += () => originalCycled++;
        original.AnimationChainFinished += () => originalFinished++;

        AnimationChainLogic clone = original.Clone(applyFrame: null);
        clone.AnimateSelf(1.5);
        clone.IsAnimationChainLooping = true;
        clone.Animate = true;
        clone.AnimateSelf(1.5);

        clone.Animate.ShouldBeTrue();
        originalFinished.ShouldBe(0);
        originalCycled.ShouldBe(0);
    }

    // Three frames of 1s each. The frame under test is read back via CurrentFrameIndex.
    private static AnimationChainLogic MakeMirrored(bool pingPong, bool reverse, bool loop = true)
    {
        AnimationChainList chains = MakeChainWithFrameLengths(1, 1, 1);
        chains[0].PingPong = pingPong;
        chains[0].IsReversed = reverse;
        chains[0].Loop = loop;
        AnimationChainLogic sut = new() { AnimationChains = chains };
        sut.CurrentChainName = "Chain";
        sut.Animate = true;
        return sut;
    }

    [Theory]
    [InlineData(0.5, 2)]
    [InlineData(1.5, 1)]
    [InlineData(2.5, 0)]
    public void AnimateSelf_Reverse_ShouldShowFramesLastToFirst(double seconds, int expectedFrame)
    {
        AnimationChainLogic sut = MakeMirrored(pingPong: false, reverse: true);

        sut.AnimateSelf(seconds);

        sut.CurrentFrameIndex.ShouldBe(expectedFrame);
    }

    [Theory]
    [InlineData(0.5, 0)]
    [InlineData(2.5, 2)]
    [InlineData(3.5, 2)] // end frame shows across the turnaround
    [InlineData(4.5, 1)]
    [InlineData(5.5, 0)]
    public void AnimateSelf_PingPong_ShouldPlayForwardThenBack(double seconds, int expectedFrame)
    {
        AnimationChainLogic sut = MakeMirrored(pingPong: true, reverse: false);

        sut.AnimateSelf(seconds);

        sut.CurrentFrameIndex.ShouldBe(expectedFrame);
    }

    [Theory]
    [InlineData(0.5, 2)]
    [InlineData(2.5, 0)]
    [InlineData(3.5, 0)]
    [InlineData(5.5, 2)]
    public void AnimateSelf_PingPongAndReverse_ShouldStartWithMirroredHalf(double seconds, int expectedFrame)
    {
        AnimationChainLogic sut = MakeMirrored(pingPong: true, reverse: true);

        sut.AnimateSelf(seconds);

        sut.CurrentFrameIndex.ShouldBe(expectedFrame);
    }

    [Fact]
    public void AnimateSelf_PingPong_ShouldNotApplyFrameAtTurnaround()
    {
        AnimationChainLogic sut = MakeMirrored(pingPong: true, reverse: false);
        sut.AnimateSelf(2.9);
        int applied = 0;
        sut.ApplyFrame = _ => applied++;

        sut.AnimateSelf(0.2); // crosses T = 3

        applied.ShouldBe(0);
    }

    [Fact]
    public void AnimateSelf_PingPong_ShouldCycleOncePerDoubleLength()
    {
        AnimationChainLogic sut = MakeMirrored(pingPong: true, reverse: false);
        int cycled = 0;
        sut.AnimationChainCycled += () => cycled++;

        sut.AnimateSelf(3.5);
        cycled.ShouldBe(0);
        sut.AnimateSelf(3.0); // 6.5 crosses 2T = 6

        cycled.ShouldBe(1);
    }

    [Fact]
    public void AnimateSelf_PingPongNotLooping_ShouldStopAfterFullCycleOnFirstFrame()
    {
        AnimationChainLogic sut = MakeMirrored(pingPong: true, reverse: false, loop: false);
        int finished = 0;
        sut.AnimationChainFinished += () => finished++;

        sut.AnimateSelf(5.0);
        sut.Animate.ShouldBeTrue();
        sut.AnimateSelf(2.0);

        sut.Animate.ShouldBeFalse();
        finished.ShouldBe(1);
        sut.CurrentFrameIndex.ShouldBe(0);
    }

    [Fact]
    public void AnimateSelf_ReverseNotLooping_ShouldStopOnFirstFrame()
    {
        AnimationChainLogic sut = MakeMirrored(pingPong: false, reverse: true, loop: false);

        sut.AnimateSelf(5.0);

        sut.Animate.ShouldBeFalse();
        sut.CurrentFrameIndex.ShouldBe(0);
    }

    [Fact]
    public void CurrentFrameIndex_SetOnReverseChain_ShouldRoundTrip()
    {
        AnimationChainLogic sut = MakeMirrored(pingPong: false, reverse: true);

        sut.CurrentFrameIndex = 1;

        sut.CurrentFrameIndex.ShouldBe(1);
        sut.TimeIntoAnimation.ShouldBe(1.0);
    }}
