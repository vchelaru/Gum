using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Covers the string-property-dispatch path (SetProperty -> CustomSetPropertyOnRenderable ->
// TrySetPropertyOnSprite) for Sprite, mirroring Tests/RaylibGum.Tests/Runtimes/SpriteSetPropertyTests.cs.
// These properties are set at runtime by the state/variable system (StateSave/VariableSave applied via
// GraphicalUiElement.SetProperty), not by direct C# property assignment, so this pins that the
// string-path dispatch produces the same result as direct C# usage (e.g. SpriteRuntime.Alpha = 128).
public class SpriteSetPropertyTests : BaseTestClass
{
    [Fact]
    public void SetProperty_AlphaRedGreenBlue_ShouldForwardToContainedSprite()
    {
        SpriteRuntime sut = new();

        sut.SetProperty(nameof(SpriteRuntime.Alpha), 128);
        sut.SetProperty(nameof(SpriteRuntime.Red), 10);
        sut.SetProperty(nameof(SpriteRuntime.Green), 20);
        sut.SetProperty(nameof(SpriteRuntime.Blue), 30);

        sut.Alpha.ShouldBe(128);
        sut.Red.ShouldBe(10);
        sut.Green.ShouldBe(20);
        sut.Blue.ShouldBe(30);
    }

    [Fact]
    public void SetProperty_Animate_ShouldForwardToContainedSprite()
    {
        SpriteRuntime sut = new();

        sut.SetProperty(nameof(SpriteRuntime.Animate), true);

        sut.Animate.ShouldBeTrue();
    }

    [Fact]
    public void SetProperty_Blend_ShouldForwardToContainedSprite()
    {
        SpriteRuntime sut = new();

        sut.SetProperty("Blend", Gum.RenderingLibrary.Blend.Additive);

        sut.Blend.ShouldBe(Gum.RenderingLibrary.Blend.Additive);
    }

    [Fact]
    public void SetProperty_Color_ShouldForwardToContainedSprite()
    {
        SpriteRuntime sut = new();
        var drawingColor = System.Drawing.Color.FromArgb(255, 10, 20, 30);

        sut.SetProperty(nameof(SpriteRuntime.Color), drawingColor);

        sut.Color.R.ShouldBe((byte)10);
        sut.Color.G.ShouldBe((byte)20);
        sut.Color.B.ShouldBe((byte)30);
    }

    [Fact]
    public void SetProperty_CurrentChainName_ShouldUpdateActiveChain()
    {
        SpriteRuntime sut = new();

        // AnimationChains defaults to chain index 0 ("FirstChain"), so switching to
        // "SecondChain" via SetProperty is what actually exercises the dispatch case
        // (rather than the AnimationChains setter's own default-frame application).
        var firstChain = new AnimationChain { Name = "FirstChain" };
        firstChain.Add(new AnimationFrame { FrameLength = 1f });
        var secondChain = new AnimationChain { Name = "SecondChain" };
        secondChain.Add(new AnimationFrame { FrameLength = 1f });
        var chainList = new AnimationChainList();
        chainList.Add(firstChain);
        chainList.Add(secondChain);
        sut.AnimationChains = chainList;
        sut.CurrentChainName.ShouldBe("FirstChain");

        sut.SetProperty(nameof(SpriteRuntime.CurrentChainName), "SecondChain");

        sut.CurrentChainName.ShouldBe("SecondChain");
    }
}
