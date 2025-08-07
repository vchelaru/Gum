using MonoGameGum.GueDeriving;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;
public class SpriteRuntimeTests
{
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
}
