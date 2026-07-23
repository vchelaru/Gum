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
    public void Color_ShouldRoundTrip_ThroughContainedRenderable()
    {
        // Pins the System.Drawing <-> XNA conversion in the Color property (ToUserColor/
        // ToContainerColor on XNALIKE). Distinct channels catch any R/B swap or dropped alpha.
        NineSliceRuntime sut = new();

        sut.Color = new Microsoft.Xna.Framework.Color(10, 20, 30, 40);

        sut.Color.R.ShouldBe((byte)10);
        sut.Color.G.ShouldBe((byte)20);
        sut.Color.B.ShouldBe((byte)30);
        sut.Color.A.ShouldBe((byte)40);
    }

    [Fact]
    public void Constructor_ShouldDefaultSizeTo100()
    {
        // Bucket 1 (#2908): all backends should default a NineSliceRuntime to 100x100.
        // Previously only the non-XNALIKE backends did so; the XNALIKE ctor left
        // Width/Height at 0, diverging from Raylib/Skia.
        NineSliceRuntime sut = new();
        sut.Width.ShouldBe(100);
        sut.Height.ShouldBe(100);
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

    [Fact]
    public void PropertyChanged_ShouldRaise_WhenAnimateChanges()
    {
        // #3110: these animation/frame setters previously raised NotifyPropertyChanged
        // only under #if SOKOL. They must notify on all backends so binding consumers
        // on MonoGame/Raylib/Skia receive the change.
        NineSliceRuntime sut = new();
        List<string> changed = new();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        sut.Animate = true;

        changed.ShouldContain(nameof(NineSliceRuntime.Animate));
    }

    [Fact]
    public void PropertyChanged_ShouldRaise_WhenAnimationChainsChanges()
    {
        NineSliceRuntime sut = new();
        List<string> changed = new();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        sut.AnimationChains = new AnimationChainList();

        changed.ShouldContain(nameof(NineSliceRuntime.AnimationChains));
    }

    [Fact]
    public void PropertyChanged_ShouldRaise_WhenAnimationSpeedChanges()
    {
        NineSliceRuntime sut = new();
        List<string> changed = new();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        sut.AnimationSpeed = 2.0f;

        changed.ShouldContain(nameof(NineSliceRuntime.AnimationSpeed));
    }

    [Fact]
    public void PropertyChanged_ShouldRaise_WhenCurrentChainNameChanges()
    {
        NineSliceRuntime sut = new();
        List<string> changed = new();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        sut.CurrentChainName = "SomeChain";

        changed.ShouldContain(nameof(NineSliceRuntime.CurrentChainName));
    }

    [Fact]
    public void PropertyChanged_ShouldRaise_WhenCustomFrameTextureCoordinateWidthChanges()
    {
        NineSliceRuntime sut = new();
        List<string> changed = new();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        sut.CustomFrameTextureCoordinateWidth = 8f;

        changed.ShouldContain(nameof(NineSliceRuntime.CustomFrameTextureCoordinateWidth));
    }
}
