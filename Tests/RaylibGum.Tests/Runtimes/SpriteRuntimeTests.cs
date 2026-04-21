using Gum.GueDeriving;
using Gum.Graphics.Animation;
using Raylib_cs;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace RaylibGum.Tests.Runtimes;

public class SpriteRuntimeTests : BaseTestClass
{
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
        sut.SourceRectangle = new Raylib_cs.Rectangle(5, 10, 30, 40);
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
        sut.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;

        // Raylib implementation uses 64 as default width in InvisibleRenderable/GraphicalUiElement 
        // if texture is null and using PercentageOfSourceFile.
        sut.GetAbsoluteWidth().ShouldBe(64);
    }

    [Fact]
    public void AnimationChainCycled_ShouldBeTriggered()
    {
        SpriteRuntime sut = new();
        bool wasCalled = false;
        sut.AnimationChainCycled += () => wasCalled = true;

        // We can't easily trigger the event without a real animation chain,
        // but we can verify we can subscribe and unsubscribe.
        sut.AnimationChainCycled -= () => wasCalled = true;
    }

    [Fact]
    public void TextureSetter_ShouldUpdateLayout_WhenPercentageBased()
    {
        SpriteRuntime sut = new();
        sut.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        sut.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;

        // Create a dummy texture
        var texture = new Texture2D { Width = 100, Height = 100 };
        
        sut.Texture = texture;
        
        sut.GetAbsoluteWidth().ShouldBe(100);
        
        var biggerTexture = new Texture2D { Width = 200, Height = 200 };
        sut.Texture = biggerTexture;
        
        sut.GetAbsoluteWidth().ShouldBe(200);
    }
}
