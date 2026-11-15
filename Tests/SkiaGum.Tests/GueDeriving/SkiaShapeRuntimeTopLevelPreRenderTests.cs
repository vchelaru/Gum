using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;
using System;

namespace SkiaGum.Tests.GueDeriving;

/// <summary>
/// Issue #7975: a Skia shape runtime added top-level with <c>AddToManagers</c> registers only its
/// contained renderable on the Layer, so the render walk never reaches the runtime's own
/// <c>PreRender</c> override. Every value pushed there (corner radii, the circle fill inset, the
/// arc gradient start) must still reach the renderable, the same as
/// when the runtime is nested under a container.
/// </summary>
public class SkiaShapeRuntimeTopLevelPreRenderTests : IDisposable
{
    private readonly SKSurface _surface;

    public SkiaShapeRuntimeTopLevelPreRenderTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        _surface = SKSurface.Create(new SKImageInfo(120, 80));
        GumService.Default.Initialize(_surface.Canvas, 120, 80);
    }

    public void Dispose()
    {
        SystemManagers.Default.Renderer.Camera.Zoom = 1;
        _surface.Dispose();
    }

#pragma warning disable CS0618 // RoundedRectangleRuntime is obsolete but still ships, and #7975 is its bug.
    [Fact]
    public void Draw_RoundedRectangleNested_DrawsCornerRadius()
    {
        RoundedRectangleRuntime shape = new() { Width = 80, Height = 40, CornerRadius = 12, FillColor = SKColors.Red };

        SKColor corner = DrawAndReadPixel(shape, isTopLevel: false, x: 2, y: 2);

        corner.ShouldBe(SKColors.Black, "because a 12 px corner radius leaves the corner pixel unfilled");
    }

    [Fact]
    public void Draw_RoundedRectangleTopLevel_DrawsCornerRadius()
    {
        RoundedRectangleRuntime shape = new() { Width = 80, Height = 40, CornerRadius = 12, FillColor = SKColors.Red };

        SKColor corner = DrawAndReadPixel(shape, isTopLevel: true, x: 2, y: 2);

        corner.ShouldBe(SKColors.Black, "because a top-level runtime must draw its 12 px corner radius, not the default 5");
    }
#pragma warning restore CS0618

    [Fact]
    public void Draw_RectangleNested_DrawsCornerRadius()
    {
        RectangleRuntime shape = new() { Width = 80, Height = 40, CornerRadius = 12, IsFilled = true, FillColor = SKColors.Red, StrokeColor = SKColors.Transparent };

        SKColor corner = DrawAndReadPixel(shape, isTopLevel: false, x: 2, y: 2);

        corner.ShouldBe(SKColors.Black, "because a 12 px corner radius leaves the corner pixel unfilled");
    }

    [Fact]
    public void Draw_RectangleTopLevel_DrawsCornerRadius()
    {
        RectangleRuntime shape = new() { Width = 80, Height = 40, CornerRadius = 12, IsFilled = true, FillColor = SKColors.Red, StrokeColor = SKColors.Transparent };

        SKColor corner = DrawAndReadPixel(shape, isTopLevel: true, x: 2, y: 2);

        corner.ShouldBe(SKColors.Black, "because a top-level runtime must draw its 12 px corner radius, not the square default");
    }

    [Fact]
    public void Draw_RectangleCloneTopLevel_DrawsOwnCornerRadius()
    {
        RectangleRuntime source = new() { Width = 80, Height = 40, CornerRadius = 12, IsFilled = true, FillColor = SKColors.Red, StrokeColor = SKColors.Transparent };
        RectangleRuntime clone = (RectangleRuntime)source.Clone();
        source.CornerRadius = 0;

        SKColor corner = DrawAndReadPixel(clone, isTopLevel: true, x: 2, y: 2);

        corner.ShouldBe(SKColors.Black, "because the clone's renderable must run the clone's PreRender (radius 12), not the source's (radius 0)");
    }

    [Fact]
    public void Draw_CircleNested_InsetsFillInsideStroke()
    {
        CircleRuntime shape = CreateCircle();

        SKColor band = DrawAndReadPixel(shape, isTopLevel: false, x: 20, y: 2);

        band.Red.ShouldBe((byte)0, "because the fill is inset inside the half-transparent stroke, so no red shows through it");
    }

    [Fact]
    public void Draw_CircleTopLevel_InsetsFillInsideStroke()
    {
        CircleRuntime shape = CreateCircle();

        SKColor band = DrawAndReadPixel(shape, isTopLevel: true, x: 20, y: 2);

        band.Red.ShouldBe((byte)0, "because a top-level runtime must inset its fill inside the stroke too");
    }

    [Fact]
    public void Draw_ArcNested_GradientStartsAtColor()
    {
        ArcRuntime shape = CreateArc();

        SKColor start = DrawAndReadPixel(shape, isTopLevel: false, x: 8, y: 8);

        start.ShouldBe(SKColors.Red, "because the gradient start follows Color (red) and the end is red too");
    }

    [Fact]
    public void Draw_ArcTopLevel_GradientStartsAtColor()
    {
        ArcRuntime shape = CreateArc();

        SKColor start = DrawAndReadPixel(shape, isTopLevel: true, x: 8, y: 8);

        start.ShouldBe(SKColors.Red, "because a top-level runtime must sync the gradient start to Color too");
    }

    private static CircleRuntime CreateCircle() => new()
    {
        Width = 40,
        Height = 40,
        IsFilled = true,
        FillColor = SKColors.Red,
        StrokeColor = new SKColor(0, 255, 0, 128),
        StrokeWidth = 6,
        StrokeWidthUnits = DimensionUnitType.Absolute,
    };

    private static ArcRuntime CreateArc()
    {
        ArcRuntime arc = new()
        {
            Width = 40,
            Height = 40,
            StartAngle = 0,
            SweepAngle = 360,
            Thickness = 10,
            UseGradient = true,
            Color2 = SKColors.Red,
        };
#pragma warning disable CS0618 // Arc is single-slot; Color is its body color.
        arc.Color = SKColors.Red;
#pragma warning restore CS0618
        return arc;
    }

    private SKColor DrawAndReadPixel(GraphicalUiElement shape, bool isTopLevel, int x, int y)
    {
        _surface.Canvas.Clear(SKColors.Black);
        GraphicalUiElement added = shape;
        if (!isTopLevel)
        {
            ContainerRuntime container = new() { Width = 120, Height = 80 };
            container.Children.Add(shape);
            added = container;
        }

        added.AddToManagers(SystemManagers.Default, SystemManagers.Default.Renderer.MainLayer);
        try
        {
            GumService.Default.Draw();
        }
        finally
        {
            added.RemoveFromManagers();
        }

        using SKImage image = _surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.GetPixel(x, y);
    }
}
