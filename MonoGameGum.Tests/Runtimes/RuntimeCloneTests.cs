using Gum.GueDeriving;
using Gum.Renderables;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Numerics;
using Xunit;

// ColoredRectangleRuntime is obsolete but still ships, so its clone path stays covered.
#pragma warning disable CS0618

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Every XNA-family runtime must clone into an independent renderable without Apos.Shapes
/// (#5197). A renderable that is not <see cref="ICloneable"/> makes
/// <see cref="GraphicalUiElement.Clone"/> throw, and one that shares its <see cref="LinePrimitive"/>
/// with the source lets a clone's color or points leak back into the source.
/// </summary>
public class RuntimeCloneTests : BaseTestClass
{
    public static IEnumerable<object[]> RuntimeNames() => new[]
    {
        new object[] { nameof(CircleRuntime) },
        new object[] { nameof(ColoredRectangleRuntime) },
        new object[] { nameof(ContainerRuntime) },
        new object[] { nameof(NineSliceRuntime) },
        new object[] { nameof(PolygonRuntime) },
        new object[] { nameof(RectangleRuntime) },
        new object[] { nameof(SpriteRuntime) },
        new object[] { nameof(TextRuntime) },
    };

    private static GraphicalUiElement CreateRuntime(string name) => name switch
    {
        nameof(CircleRuntime) => new CircleRuntime(),
        nameof(ColoredRectangleRuntime) => new ColoredRectangleRuntime(),
        nameof(ContainerRuntime) => new ContainerRuntime(),
        nameof(NineSliceRuntime) => new NineSliceRuntime(),
        nameof(PolygonRuntime) => new PolygonRuntime(),
        nameof(RectangleRuntime) => new RectangleRuntime(),
        nameof(SpriteRuntime) => new SpriteRuntime(),
        nameof(TextRuntime) => new TextRuntime(),
        _ => throw new ArgumentException(name),
    };

    public static IEnumerable<object[]> LineRenderableNames() => new[]
    {
        new object[] { nameof(DefaultStrokedCircleRenderable) },
        new object[] { nameof(DefaultStrokedRectangleRenderable) },
        new object[] { nameof(FilledStrokedRectangle) },
        new object[] { nameof(Line) },
        new object[] { nameof(LineCircle) },
        new object[] { nameof(LineGrid) },
        new object[] { nameof(LinePolygon) },
        new object[] { nameof(LineRectangle) },
    };

    private static IRenderableIpso CreateLineRenderable(string name) => name switch
    {
        nameof(DefaultStrokedCircleRenderable) => new DefaultStrokedCircleRenderable(),
        nameof(DefaultStrokedRectangleRenderable) => new DefaultStrokedRectangleRenderable(),
        nameof(FilledStrokedRectangle) => new FilledStrokedRectangle(),
        nameof(Line) => new Line(),
        nameof(LineCircle) => new LineCircle(),
        nameof(LineGrid) => new LineGrid(null),
        nameof(LinePolygon) => new LinePolygon(),
        nameof(LineRectangle) => new LineRectangle(),
        _ => throw new ArgumentException(name),
    };

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void Clone_ShouldCreateIndependentRenderable(string runtimeName)
    {
        GraphicalUiElement original = CreateRuntime(runtimeName);
        original.Width = 50;

        GraphicalUiElement clone = original.Clone();
        clone.Width = 80;

        clone.RenderableComponent.ShouldNotBeSameAs(original.RenderableComponent);
        IRenderableIpso originalRenderable = (IRenderableIpso)original.RenderableComponent!;
        IRenderableIpso cloneRenderable = (IRenderableIpso)clone.RenderableComponent!;
        cloneRenderable.Children!.ShouldNotBeSameAs(originalRenderable.Children);
        original.Width.ShouldBe(50);
    }

    [Theory]
    [MemberData(nameof(LineRenderableNames))]
    public void Clone_LineRenderable_ShouldNotShareColorOrChildren(string renderableName)
    {
        IRenderableIpso original = CreateLineRenderable(renderableName);
        System.Drawing.Color originalColor = System.Drawing.Color.FromArgb(255, 10, 20, 30);
        System.Drawing.Color cloneColor = System.Drawing.Color.FromArgb(255, 200, 100, 50);
        SetColor(original, originalColor);

        IRenderableIpso clone = (IRenderableIpso)((ICloneable)original).Clone();
        SetColor(clone, cloneColor);

        clone.ShouldBeOfType(original.GetType());
        GetColor(original).ShouldBe(originalColor);
        GetColor(clone).ShouldBe(cloneColor);
        clone.Children.ShouldNotBeSameAs(original.Children);
    }

    [Fact]
    public void Clone_LineRenderable_ShouldNotKeepTheSourcesParent()
    {
        LineRectangle parent = new LineRectangle();
        LineCircle original = new LineCircle();
        original.Parent = parent;

        LineCircle clone = (LineCircle)((ICloneable)original).Clone();

        clone.Parent.ShouldBeNull();
        parent.Children.ShouldNotContain(clone);
    }

    [Fact]
    public void Clone_PolygonRuntime_ShouldNotSharePointsOrColor()
    {
        PolygonRuntime original = new PolygonRuntime();
        original.SetPoints(new[] { new Vector2(0, 0), new Vector2(10, 0), new Vector2(0, 10) });
        original.Color = new Microsoft.Xna.Framework.Color(10, 20, 30, 255);

        PolygonRuntime clone = (PolygonRuntime)original.Clone();
        clone.SetPoints(new[] { new Vector2(5, 5) });
        clone.Color = new Microsoft.Xna.Framework.Color(200, 100, 50, 255);

        LinePolygon originalPolygon = (LinePolygon)original.RenderableComponent!;
        originalPolygon.PointCount.ShouldBe(3);
        originalPolygon.PointAt(1).ShouldBe(new Vector2(10, 0));
        original.Color.ShouldBe(new Microsoft.Xna.Framework.Color(10, 20, 30, 255));
        ((LinePolygon)clone.RenderableComponent!).PointCount.ShouldBe(1);
    }

    [Fact]
    public void Clone_CircleRuntime_ShouldNotShareStrokeColorOrRadius()
    {
        CircleRuntime original = new CircleRuntime();
        original.StrokeColor = new Microsoft.Xna.Framework.Color(10, 20, 30, 255);
        original.Width = 40;
        original.Height = 40;

        CircleRuntime clone = (CircleRuntime)original.Clone();
        clone.StrokeColor = new Microsoft.Xna.Framework.Color(200, 100, 50, 255);
        clone.Width = 90;
        clone.Height = 90;

        original.StrokeColor.ShouldBe(new Microsoft.Xna.Framework.Color(10, 20, 30, 255));
        ((LineCircle)original.RenderableComponent!).Radius.ShouldBe(20);
        clone.StrokeColor.ShouldBe(new Microsoft.Xna.Framework.Color(200, 100, 50, 255));
    }

    [Fact]
    public void Clone_CircleRuntime_ShouldKeepVisibility()
    {
        CircleRuntime original = new CircleRuntime();
        original.Visible = false;

        CircleRuntime clone = (CircleRuntime)original.Clone();

        clone.Visible.ShouldBeFalse();
    }

    private static void SetColor(IRenderableIpso renderable, System.Drawing.Color color)
    {
        switch (renderable)
        {
            case FilledStrokedRectangle filledStroked: filledStroked.StrokeColor = color; break;
            case Line line: line.Color = color; break;
            case LineCircle circle: circle.Color = color; break;
            case LineGrid grid: grid.Color = color; break;
            case LinePolygon polygon: polygon.Color = color; break;
            case LineRectangle rectangle: rectangle.Color = color; break;
            default: throw new ArgumentException(renderable.GetType().Name);
        }
    }

    private static System.Drawing.Color GetColor(IRenderableIpso renderable) => renderable switch
    {
        FilledStrokedRectangle filledStroked => filledStroked.StrokeColor,
        Line line => line.Color,
        LineCircle circle => circle.Color,
        LineGrid grid => grid.Color,
        LinePolygon polygon => polygon.Color,
        LineRectangle rectangle => rectangle.Color,
        _ => throw new ArgumentException(renderable.GetType().Name),
    };
}
