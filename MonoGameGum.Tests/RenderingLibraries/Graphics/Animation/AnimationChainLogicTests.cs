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

    [Fact(Skip = "Out-of-range frame index behavior needs a decision: #5813")]
    public void CurrentFrameIndex_Set_ShouldShowLastFrame_WhenPastTheEnd()
    {
        AnimationChainList chains = MakeChainWithFrameLengths(1, 1, 1);
        AnimationFrame? applied = null;
        AnimationChainLogic sut = new() { AnimationChains = chains, ApplyFrame = frame => applied = frame };

        sut.CurrentFrameIndex = 5;

        sut.CurrentFrameIndex.ShouldBe(2);
        applied.ShouldBeSameAs(chains[0][2]);
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

    [Fact]
    public void TimeIntoAnimation_Set_ShouldNotThrowOrApply_WhenNegative()
    {
        bool wasApplied = false;
        AnimationChainLogic sut = new()
        {
            AnimationChains = MakeChainWithFrameLengths(0.5f, 1.0f),
            ApplyFrame = _ => wasApplied = true
        };

        sut.TimeIntoAnimation = -1;

        wasApplied.ShouldBeFalse();
        sut.TimeIntoAnimation.ShouldBe(-1);
    }

    [Fact]
    public void TimeIntoAnimation_Set_ShouldNotApply_WhenNoChainsAreSet()
    {
        bool wasApplied = false;
        AnimationChainLogic sut = new() { ApplyFrame = _ => wasApplied = true };

        sut.TimeIntoAnimation = 1.5;

        wasApplied.ShouldBeFalse();
    }
}
