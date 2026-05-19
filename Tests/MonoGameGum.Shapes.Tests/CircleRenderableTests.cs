using MonoGameAndGum.Renderables;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// Issue #2852: when Width != Height the Apos.Shapes Circle previously sized its radius from
// Width alone, which produced two divergent results vs Skia (the tool/viewport renderer):
// the circle could overflow the box on the short axis, and a wide-but-short circle's center
// landed below the box because center.Y was also derived from Width. These tests pin the
// canonical behavior to "fit inside the bounding box, centered" — matching Skia.
public class CircleRenderableTests
{
    [Fact]
    public void Radius_WhenWidthGreaterThanHeight_UsesSmallerDimension()
    {
        Circle sut = new() { Width = 200, Height = 50 };

        sut.Radius.ShouldBe(25f);
    }

    [Fact]
    public void Radius_WhenHeightGreaterThanWidth_UsesSmallerDimension()
    {
        Circle sut = new() { Width = 50, Height = 200 };

        sut.Radius.ShouldBe(25f);
    }

    [Fact]
    public void Radius_WhenSquare_IsHalfDimension()
    {
        Circle sut = new() { Width = 80, Height = 80 };

        sut.Radius.ShouldBe(40f);
    }

    [Fact]
    public void Radius_Setter_KeepsWidthAndHeightSquare()
    {
        Circle sut = new();

        sut.Radius = 30;

        sut.Width.ShouldBe(60f);
        sut.Height.ShouldBe(60f);
    }
}
