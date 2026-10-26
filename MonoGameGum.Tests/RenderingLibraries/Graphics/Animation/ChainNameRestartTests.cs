using System.Collections.Generic;
using Gum.Graphics.Animation;
using RenderingLibrary.Graphics.Animation;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Graphics.Animation;

public class ChainNameRestartTests
{
    private static AnimationChain Chain(string name, bool reverse = false, bool pingPong = false, int frames = 3)
    {
        AnimationChain c = new() { Name = name, IsReversed = reverse, PingPong = pingPong };
        for (int i = 0; i < frames; i++) c.Add(new AnimationFrame { FrameLength = 1f });
        return c;
    }

    [Fact]
    public void Reverse_ByName_ShowsLastFrameImmediately_AndAppliesOnce()
    {
        AnimationChain rev = Chain("R", reverse: true);
        List<int> applied = new();
        AnimationChainLogic l = new() { AnimationChains = new AnimationChainList { Chain("P"), rev }, ApplyFrame = f => applied.Add(rev.IndexOf(f)) };
        l.CurrentChainName = "R";
        l.CurrentFrameIndex.ShouldBe(2);
        applied.ShouldBe(new[] { 2 });
    }

    [Fact]
    public void PingPongReverse_ByName_ShowsLastFrame()
    {
        AnimationChainLogic l = new() { AnimationChains = new AnimationChainList { Chain("P"), Chain("R", reverse: true, pingPong: true) } };
        l.CurrentChainName = "R";
        l.CurrentFrameIndex.ShouldBe(2);
    }

    [Fact]
    public void PlainChain_RestartsAtFrameOne_AfterOtherChainAdvanced()
    {
        AnimationChainLogic l = new() { AnimationChains = new AnimationChainList { Chain("A"), Chain("B") } };
        l.CurrentChainName = "A"; l.Animate = true; l.AnimateSelf(1.5);
        l.CurrentFrameIndex.ShouldBe(1);
        l.CurrentChainName = "B";
        l.CurrentFrameIndex.ShouldBe(0); l.TimeIntoAnimation.ShouldBe(0);
    }

    [Fact]
    public void SameNameEveryFrame_IsNoOp_WhileAnimating()
    {
        int applied = 0;
        AnimationChainLogic l = new() { AnimationChains = new AnimationChainList { Chain("A") }, ApplyFrame = _ => applied++ };
        l.CurrentChainName = "A"; l.Animate = true;
        for (int i = 0; i < 5; i++) { l.AnimateSelf(0.5); l.CurrentChainName = "A"; }
        l.TimeIntoAnimation.ShouldBe(2.5);
        l.CurrentFrameIndex.ShouldBe(2);
    }

    [Fact]
    public void MissingName_DoesNotThrow_AndHasNoChain()
    {
        AnimationChainLogic l = new() { AnimationChains = new AnimationChainList { Chain("A") } };
        l.CurrentChainName = "Nope";
        l.CurrentChain.ShouldBeNull();
        l.AnimateSelf(1);
    }

    [Fact]
    public void DefaultIndexZeroChain_NamedForFirstTime_KeepsPlaybackAndSeedsLooping()
    {
        AnimationChain a = Chain("A");
        a.Loop = false;
        AnimationChainLogic l = new() { AnimationChains = new AnimationChainList { a }, Animate = true };
        l.AnimateSelf(1.5);

        l.CurrentChainName = "A";

        l.CurrentFrameIndex.ShouldBe(1);
        l.TimeIntoAnimation.ShouldBe(1.5);
        l.IsAnimationChainLooping.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NameSetBeforeChains_ReverseChain_ShouldShowLastFrameWhenResolved(bool viaTick)
    {
        AnimationChain rev = Chain("R", reverse: true);
        List<int> applied = new();
        AnimationChainLogic l = new() { ApplyFrame = f => applied.Add(rev.IndexOf(f)) };
        l.CurrentChainName = "R";
        l.AnimationChains = new AnimationChainList { rev };
        l.RefreshCurrentChainToDesiredName();   // what the sprite property assignment does
        l.UpdateToCurrentAnimationFrame();
        if (viaTick) { l.Animate = true; l.AnimateSelf(0); }
        l.CurrentFrameIndex.ShouldBe(2);
        applied[^1].ShouldBe(2);
        applied.ShouldNotContain(0);
    }
}
