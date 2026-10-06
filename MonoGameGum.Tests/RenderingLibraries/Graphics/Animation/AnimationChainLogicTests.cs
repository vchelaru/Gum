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
    public void CurrentChainName_Set_ShouldClampFrameIndex_WhenNewChainIsShorter()
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

        sut.CurrentFrameIndex.ShouldBe(1);
        sut.TimeIntoAnimation.ShouldBe(1);
        applied.ShouldBeSameAs(shortChain[1]);
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
}
