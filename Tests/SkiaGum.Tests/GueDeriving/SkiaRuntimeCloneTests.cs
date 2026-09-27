using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Numerics;
using RenderableShapeBase = SkiaGum.Renderables.RenderableShapeBase;

namespace SkiaGum.Tests.GueDeriving;

/// <summary>
/// Every Skia runtime must clone into an independent renderable (#5111). A renderable that is
/// not <see cref="ICloneable"/> makes <see cref="GraphicalUiElement.Clone"/> throw.
/// </summary>
public class SkiaRuntimeCloneTests
{
    public static IEnumerable<object[]> RuntimeNames() => new[]
    {
        new object[] { nameof(ArcRuntime) },
        new object[] { nameof(CircleRuntime) },
        new object[] { nameof(ColoredCircleRuntime) },
        new object[] { nameof(ColoredRectangleRuntime) },
        new object[] { nameof(LineGridRuntime) },
        new object[] { nameof(LineRuntime) },
        new object[] { nameof(LottieAnimationRuntime) },
        new object[] { nameof(NineSliceRuntime) },
        new object[] { nameof(PolygonRuntime) },
        new object[] { nameof(RectangleRuntime) },
        new object[] { nameof(RoundedRectangleRuntime) },
        new object[] { nameof(SolidRectangleRuntime) },
        new object[] { nameof(SpriteRuntime) },
        new object[] { nameof(SvgRuntime) },
    };

    private static GraphicalUiElement Create(string name) => name switch
    {
        nameof(ArcRuntime) => new ArcRuntime(),
        nameof(CircleRuntime) => new CircleRuntime(),
        nameof(ColoredCircleRuntime) => new ColoredCircleRuntime(),
        nameof(ColoredRectangleRuntime) => new ColoredRectangleRuntime(),
        nameof(LineGridRuntime) => new LineGridRuntime(),
        nameof(LineRuntime) => new LineRuntime(),
        nameof(LottieAnimationRuntime) => new LottieAnimationRuntime(),
        nameof(NineSliceRuntime) => new NineSliceRuntime(),
        nameof(PolygonRuntime) => new PolygonRuntime(),
        nameof(RectangleRuntime) => new RectangleRuntime(),
        nameof(RoundedRectangleRuntime) => new RoundedRectangleRuntime(),
        nameof(SolidRectangleRuntime) => new SolidRectangleRuntime(),
        nameof(SpriteRuntime) => new SpriteRuntime(),
        nameof(SvgRuntime) => new SvgRuntime(),
        _ => throw new ArgumentException(name),
    };

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void Clone_ShouldCreateIndependentRenderable(string runtimeName)
    {
        GraphicalUiElement original = Create(runtimeName);

        GraphicalUiElement clone = original.Clone();

        clone.RenderableComponent.ShouldNotBeSameAs(original.RenderableComponent);
        IRenderableIpso originalRenderable = (IRenderableIpso)original.RenderableComponent;
        IRenderableIpso cloneRenderable = (IRenderableIpso)clone.RenderableComponent;
        cloneRenderable.Children.ShouldNotBeSameAs(originalRenderable.Children);
    }

    [Fact]
    public void Clone_LineGridRuntime_ShouldCopyCellSizeIndependently()
    {
        LineGridRuntime original = new LineGridRuntime();
        original.CellWidth = 20;

        LineGridRuntime clone = (LineGridRuntime)original.Clone();
        clone.CellWidth = 40;

        original.CellWidth.ShouldBe((ushort)20);
        clone.CellWidth.ShouldBe((ushort)40);
    }

    [Fact]
    public void Clone_PolygonRuntime_ShouldNotSharePoints()
    {
        PolygonRuntime original = new PolygonRuntime();
        original.SetPoints(new[] { new Vector2(0, 0), new Vector2(10, 0), new Vector2(0, 10) });

        PolygonRuntime clone = (PolygonRuntime)original.Clone();
        clone.SetPoints(new[] { new Vector2(5, 5) });

        original.Points.Count.ShouldBe(3);
    }

    [Fact]
    public void Clone_ShouldNotDisposeTheSourcesCachedPaint()
    {
        PaintProbeCircle original = new PaintProbeCircle();
        SKRect bounds = new SKRect(0, 0, 10, 10);
        SKPaint originalPaint = original.GetPaintForTest(bounds);

        ((ICloneable)original).Clone();

        originalPaint.Handle.ShouldNotBe(IntPtr.Zero);
        original.GetPaintForTest(bounds).ShouldBeSameAs(originalPaint);
    }

    [Fact]
    public void Clone_ClonesPaint_ShouldBeIndependentOfSource()
    {
        PaintProbeCircle original = new PaintProbeCircle();
        SKRect bounds = new SKRect(0, 0, 10, 10);
        SKPaint originalPaint = original.GetPaintForTest(bounds);

        PaintProbeCircle clone = (PaintProbeCircle)((ICloneable)original).Clone();

        clone.GetPaintForTest(bounds).ShouldNotBeSameAs(originalPaint);
    }

    [Fact]
    public void Clone_ShouldNotKeepTheSourcesPreRenderHook()
    {
        Circle original = new Circle();
        original.OnPreRender = () => { };

        Circle clone = (Circle)((ICloneable)original).Clone();

        clone.OnPreRender.ShouldBeNull();
    }

    private sealed class PaintProbeCircle : Circle
    {
        public SKPaint GetPaintForTest(SKRect bounds) => GetCachedPaint(bounds, 0);
    }
}
