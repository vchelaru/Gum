using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using MonoGameAndGum.Renderables;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// ColoredCircleRuntime and RoundedRectangleRuntime are obsolete but still ship.
#pragma warning disable CS0618
public class AposShapeRuntimeCloneTests
{
    [Fact]
    public void ColoredCircleRuntime_Clone_ShouldNotReplaceOrMutateSourceRenderable()
    {
        ColoredCircleRuntime source = new();
        source.Color = Color.White;
        Circle sourceCircle = (Circle)source.RenderableComponent!;

        ColoredCircleRuntime clone = (ColoredCircleRuntime)source.Clone();
        clone.Color = Color.Red;

        source.RenderableComponent.ShouldBeSameAs(sourceCircle);
        sourceCircle.Color.ShouldBe(Color.White);
        ((Circle)clone.RenderableComponent!).Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void ColoredCircleRuntime_Clone_ShouldPushOwnStrokeWidthOnPreRender()
    {
        ColoredCircleRuntime source = new();
        source.StrokeWidth = 1;

        ColoredCircleRuntime clone = (ColoredCircleRuntime)source.Clone();
        clone.StrokeWidth = 5;
        Circle cloneCircle = (Circle)clone.RenderableComponent!;
        cloneCircle.PreRender();

        cloneCircle.StrokeWidth.ShouldBe(5);
    }

    [Fact]
    public void RoundedRectangleRuntime_Clone_ShouldPushOwnStrokeWidthOnPreRender()
    {
        RoundedRectangleRuntime source = new();
        source.StrokeWidth = 1;

        RoundedRectangleRuntime clone = (RoundedRectangleRuntime)source.Clone();
        clone.StrokeWidth = 5;
        RoundedRectangle cloneRectangle = (RoundedRectangle)clone.RenderableComponent!;
        cloneRectangle.PreRender();

        cloneRectangle.StrokeWidth.ShouldBe(5);
    }
}
#pragma warning restore CS0618
