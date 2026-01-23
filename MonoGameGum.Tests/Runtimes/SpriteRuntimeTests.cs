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
    [Fact]
    public void Clone_ShouldCreateClonedSprite()
    {
        Sprite sut = new((Texture2D?)null);

        var clone = sut.Clone() as Sprite;
        clone.ShouldNotBeNull();
    }

    // Not an InteractiveGue:
    //[Fact]
    //public void HasEvents_ShouldDefaultToFalse()
    //{
    //    SpriteRuntime sut = new();
    //    sut.HasEvents.ShouldBeFalse();
    //}

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

    [Fact]
    public void Height_ShouldIgnoreTextureHeight_IfUsingEntireTexture()
    {
        SpriteRuntime sut = new();

        sut.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        sut.TextureAddress = Gum.Managers.TextureAddress.EntireTexture;
        sut.TextureHeight = 150;

        sut.GetAbsoluteHeight().ShouldBe(64);
    }

}
