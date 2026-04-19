using Gum.GueDeriving;
using Gum.Renderables;
using Shouldly;

namespace SokolGum.Tests.Runtimes;

public class ColoredRectangleRuntimeTests : BaseTestClass
{
    [Fact]
    public void ContainedRenderable_ShouldBeSolidRectangle()
    {
        var sut = new ColoredRectangleRuntime();
        sut.RenderableComponent.ShouldBeOfType<SolidRectangle>();
    }

    [Fact]
    public void Red_ShouldRoundTrip()
    {
        var sut = new ColoredRectangleRuntime { Red = 200 };
        sut.Red.ShouldBe(200);
    }

    [Fact]
    public void Green_ShouldRoundTrip()
    {
        var sut = new ColoredRectangleRuntime { Green = 150 };
        sut.Green.ShouldBe(150);
    }

    [Fact]
    public void Blue_ShouldRoundTrip()
    {
        var sut = new ColoredRectangleRuntime { Blue = 75 };
        sut.Blue.ShouldBe(75);
    }

    [Fact]
    public void Color_SetComponents_ShouldAggregateOnContainedRenderable()
    {
        var sut = new ColoredRectangleRuntime { Red = 10, Green = 20, Blue = 30 };
        var solid = (SolidRectangle)sut.RenderableComponent;
        solid.Color.R.ShouldBe((byte)10);
        solid.Color.G.ShouldBe((byte)20);
        solid.Color.B.ShouldBe((byte)30);
    }

    [Fact]
    public void ContainedSolidRectangle_Alpha_ShouldDefaultTo255()
    {
        // Alpha isn't exposed on ColoredRectangleRuntime (only Red/Green/Blue);
        // it's driven through the contained SolidRectangle. Document the
        // current surface — if ColoredRectangleRuntime.Alpha gets added later
        // to match RaylibGum, this test will catch the default-value change.
        var sut = new ColoredRectangleRuntime();
        var solid = (SolidRectangle)sut.RenderableComponent;
        solid.Alpha.ShouldBe(255);
    }
}
