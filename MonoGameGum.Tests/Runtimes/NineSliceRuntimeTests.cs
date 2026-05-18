using Gum.Graphics.Animation;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Animation;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;
public class NineSliceRuntimeTests
{
    #region Helpers

    private static NineSlice CreateAnimatedNineSlice(params float[] frameLengths)
    {
        NineSlice nineSlice = new();

        AnimationChain chain = new() { Name = "TestChain" };
        foreach (float length in frameLengths)
        {
            chain.Add(new AnimationFrame { FrameLength = length });
        }

        AnimationChainList chainList = new();
        chainList.Add(chain);

        nineSlice.AnimationChains = chainList;
        nineSlice.Animate = true;
        nineSlice.Visible = true;

        return nineSlice;
    }

    #endregion

    [Fact]
    public void AnimationLogic_ShouldExposeSharedPlaybackState()
    {
        // Acceptance for #2821: XNA NineSlice must compose AnimationChainLogic
        // so NineSliceRuntime can route accessors uniformly with SpriteRuntime.
        NineSlice nineSlice = new();
        nineSlice.AnimationLogic.ShouldNotBeNull();
    }

    [Fact]
    public void Animate_ShouldRouteThroughAnimationLogic()
    {
        NineSlice nineSlice = new();
        nineSlice.Animate = true;
        nineSlice.AnimationLogic.Animate.ShouldBeTrue();
    }

    [Fact]
    public void AnimateSelf_ShouldFireAnimationChainCycled_WhenLooping()
    {
        NineSlice sut = CreateAnimatedNineSlice(1.0f);
        int cycleCount = 0;
        sut.AnimationLogic.AnimationChainCycled += () => cycleCount++;

        sut.AnimateSelf(1.5);

        cycleCount.ShouldBe(1);
    }

    [Fact]
    public void AnimationChains_ShouldRouteThroughContainedNineSliceAnimationLogic()
    {
        NineSliceRuntime sut = new();
        AnimationChainList chains = new();
        sut.AnimationChains = chains;

        NineSlice contained = (NineSlice)sut.RenderableComponent;
        contained.AnimationLogic.AnimationChains.ShouldBe(chains);
    }

    [Fact]
    public void ExposeChildrenEvents_ShouldBeTrue()
    {
        NineSliceRuntime sut = new();
        sut.ExposeChildrenEvents.ShouldBeTrue();
    }

    [Fact]
    public void HasEvents_ShouldDefaultToFalse()
    {
        NineSliceRuntime sut = new();
        sut.HasEvents.ShouldBeFalse();
    }
}
