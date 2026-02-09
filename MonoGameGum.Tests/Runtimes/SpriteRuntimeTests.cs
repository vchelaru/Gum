using Gum.Graphics.Animation;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;
public class SpriteRuntimeTests : BaseTestClass
{
    #region Helpers

    private static Sprite CreateAnimatedSprite(params float[] frameLengths)
    {
        var sprite = new Sprite((Texture2D?)null);

        var chain = new AnimationChain { Name = "TestChain" };
        foreach (var length in frameLengths)
        {
            chain.Add(new AnimationFrame { FrameLength = length });
        }

        var chainList = new AnimationChainList();
        chainList.Add(chain);

        sprite.AnimationChains = chainList;
        // mCurrentChainIndex defaults to 0, which points to the first chain.
        // Avoid setting CurrentChainName as its setter calls UpdateToCurrentAnimationFrame
        // which requires non-null textures.
        sprite.Animate = true;

        return sprite;
    }

    #endregion

    [Fact]
    public void AnimateSelf_ShouldFireAnimationChainCycled_WhenLooping()
    {
        var sut = CreateAnimatedSprite(1.0f);
        var cycleCount = 0;
        sut.AnimationChainCycled += () => cycleCount++;

        sut.AnimateSelf(1.5);

        cycleCount.ShouldBe(1);
    }

    [Fact]
    public void AnimateSelf_ShouldFireAnimationChainCycled_WhenNotLooping()
    {
        var sut = CreateAnimatedSprite(1.0f);
        sut.IsAnimationChainLooping = false;
        var cycleCount = 0;
        sut.AnimationChainCycled += () => cycleCount++;

        sut.AnimateSelf(1.5);

        cycleCount.ShouldBe(1);
    }

    [Fact]
    public void AnimateSelf_ShouldStopAnimating_WhenNotLoopingAndAnimationEnds()
    {
        var sut = CreateAnimatedSprite(1.0f);
        sut.IsAnimationChainLooping = false;

        sut.AnimateSelf(1.5);

        sut.Animate.ShouldBeFalse();
        sut.TimeIntoAnimation.ShouldBe(1.0);
    }

    [Fact]
    public void AnimationChainFrameIndex_ShouldForwardToContainedSprite()
    {
        SpriteRuntime sut = new();

        sut.AnimationChainFrameIndex = 3;

        sut.AnimationChainFrameIndex.ShouldBe(3);
    }

    [Fact]
    public void AnimationChainSpeed_ShouldForwardToContainedSprite()
    {
        SpriteRuntime sut = new();

        sut.AnimationChainSpeed = 2.5f;

        sut.AnimationChainSpeed.ShouldBe(2.5f);
    }


    [Fact]
    public void AnimationChainTime_ShouldForwardToContainedSprite()
    {
        SpriteRuntime sut = new();

        sut.AnimationChainTime = 1.5;

        sut.AnimationChainTime.ShouldBe(1.5);
    }

    [Fact]
    public void Clone_ShouldCreateClonedSprite()
    {
        Sprite sut = new((Texture2D?)null);

        var clone = sut.Clone() as Sprite;
        clone.ShouldNotBeNull();
    }

    [Fact]
    public void CurrentFrameIndex_ShouldSyncTimeIntoAnimation()
    {
        // 3 frames: 0.5s, 1.0s, 0.75s
        var sut = CreateAnimatedSprite(0.5f, 1.0f, 0.75f);

        sut.CurrentFrameIndex = 2;

        // Time should be sum of frames 0 and 1: 0.5 + 1.0 = 1.5
        sut.TimeIntoAnimation.ShouldBe(1.5);
    }
    // Not an InteractiveGue:
    //[Fact]
    //public void HasEvents_ShouldDefaultToFalse()
    //{
    //    SpriteRuntime sut = new();
    //    sut.HasEvents.ShouldBeFalse();
    //}

    [Fact]
    public void Height_ShouldIgnoreTextureHeight_IfUsingEntireTexture()
    {
        SpriteRuntime sut = new();

        sut.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        sut.TextureAddress = Gum.Managers.TextureAddress.EntireTexture;
        sut.TextureHeight = 150;

        sut.GetAbsoluteHeight().ShouldBe(64);
    }

    [Fact]
    public void IsAnimationChainLooping_ShouldDefaultToTrue()
    {
        SpriteRuntime sut = new();

        sut.IsAnimationChainLooping.ShouldBeTrue();
    }

    [Fact]
    public void IsAnimationChainLooping_ShouldForwardToContainedSprite()
    {
        SpriteRuntime sut = new();

        sut.IsAnimationChainLooping = false;

        sut.IsAnimationChainLooping.ShouldBeFalse();
    }

    [Fact]
    public void SourceRectangle_AssignsTextureValues()
    {
        SpriteRuntime sut = new();
        sut.SourceRectangle = new Microsoft.Xna.Framework.Rectangle(5, 10, 30, 40);
        sut.TextureLeft.ShouldBe(5);
        sut.TextureTop.ShouldBe(10);
        sut.TextureWidth.ShouldBe(30);
        sut.TextureHeight.ShouldBe(40);
    }

    [Fact]
    public void TextureValues_AssignSourceRectangle()
    {
        SpriteRuntime sut = new();
        sut.TextureLeft = 5;
        sut.TextureTop = 10;
        sut.TextureWidth = 30;
        sut.TextureHeight = 40;
        var rect = sut.SourceRectangle;
        rect.X.ShouldBe(5);
        rect.Y.ShouldBe(10);
        rect.Width.ShouldBe(30);
        rect.Height.ShouldBe(40);
    }

    [Fact]
    public void Width_ShouldBeDefault_WithNullTexture()
    {
        SpriteRuntime sut = new();
        sut.Width.ShouldBe(100);
        sut.WidthUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile);

        sut.GetAbsoluteWidth().ShouldBe(64);
    }

    [Fact]
    public void Width_ShouldIgnoreTextureWidth_IfUsingEntireTexture()
    {
        SpriteRuntime sut = new();

        sut.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        sut.TextureAddress = Gum.Managers.TextureAddress.EntireTexture;
        sut.TextureWidth = 150;

        sut.GetAbsoluteWidth().ShouldBe(64);
    }

}
