using Gum.GueDeriving;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// A 360 degree <see cref="ArcRuntime"/> and a stroked <see cref="CircleRuntime"/> with the same box
/// and stroke width are the same shape, so they must draw the same pixels (#6254). Both renderables
/// tessellate with their own segment counts, so a handful of edge pixels may differ; a real
/// divergence (an inset or bleed) changes hundreds.
/// </summary>
public class ArcMatchesCircleTests : BaseTestClass
{
    private const int CanvasSize = 100;
    private const float BoxSize = 60;
    private const float StrokeWidth = 8;
    private const int EdgeNoiseTolerance = 8;

    [Theory]
    [InlineData(20f)]
    [InlineData(20.5f)]
    public void FullSweepArc_DrawsSamePixelsAsStrokedCircle(float position)
    {
        Color[] circle = Render(position, isArc: false);
        Color[] arc = Render(position, isArc: true);

        CountDifferingPixels(circle, arc).ShouldBeLessThanOrEqualTo(EdgeNoiseTolerance);
    }

    [Theory]
    [InlineData(20f)]
    [InlineData(20.5f)]
    public void FullSweepArc_ChangesNoMorePixelsOutsideItsBoxThanCircle(float position)
    {
        Color[] circle = Render(position, isArc: false);
        Color[] arc = Render(position, isArc: true);

        CountChangedOutsideBox(arc, position).ShouldBeLessThanOrEqualTo(
            CountChangedOutsideBox(circle, position) + EdgeNoiseTolerance);
    }

    private static int CountDifferingPixels(Color[] a, Color[] b)
    {
        int count = 0;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i].R != b[i].R || a[i].G != b[i].G || a[i].B != b[i].B || a[i].A != b[i].A)
            {
                count++;
            }
        }
        return count;
    }

    private static readonly Color Backdrop = new Color((byte)70, (byte)70, (byte)90, (byte)255);

    private static int CountChangedOutsideBox(Color[] pixels, float position)
    {
        int count = 0;
        for (int y = 0; y < CanvasSize; y++)
        {
            for (int x = 0; x < CanvasSize; x++)
            {
                bool inside = x >= position && x < position + BoxSize && y >= position && y < position + BoxSize;
                Color c = pixels[y * CanvasSize + x];
                if (!inside && (c.R != Backdrop.R || c.G != Backdrop.G || c.B != Backdrop.B || c.A != Backdrop.A))
                {
                    count++;
                }
            }
        }
        return count;
    }

    private static Color[] Render(float position, bool isArc)
    {
        ContainerRuntime container = new();
        container.Width = CanvasSize;
        container.Height = CanvasSize;
        container.IsRenderTarget = true;

        RectangleRuntime backdrop = new();
        backdrop.Width = CanvasSize;
        backdrop.Height = CanvasSize;
        backdrop.IsFilled = true;
        backdrop.StrokeWidth = 0;
        backdrop.FillColor = Backdrop;
        container.Children.Add(backdrop);

        GraphicalUiElement shape;
        if (isArc)
        {
            ArcRuntime arc = new();
            arc.StartAngle = 0;
            arc.SweepAngle = 360;
            arc.Thickness = StrokeWidth;
            arc.StrokeColor = new Color((byte)255, (byte)0, (byte)0, (byte)255);
            shape = arc;
        }
        else
        {
            CircleRuntime circle = new();
            circle.IsFilled = false;
            circle.StrokeWidth = StrokeWidth;
            circle.StrokeColor = new Color((byte)255, (byte)0, (byte)0, (byte)255);
            shape = circle;
        }
        shape.X = position;
        shape.Y = position;
        shape.Width = BoxSize;
        shape.Height = BoxSize;
        container.Children.Add(shape);

        Gum.GumService.Default.Root.Children.Add(container);
        Gum.GumService.Default.Root.UpdateLayout();

        BeginDrawing();
        Gum.GumService.Default.Draw();
        EndDrawing();

        RenderTexture2D rt = Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value;
        Image image = LoadImageFromTexture(rt.Texture);
        Color[] result = new Color[CanvasSize * CanvasSize];
        for (int y = 0; y < CanvasSize; y++)
        {
            for (int x = 0; x < CanvasSize; x++)
            {
                result[y * CanvasSize + x] = GetImageColor(image, x, rt.Texture.Height - 1 - y);
            }
        }
        UnloadImage(image);
        Gum.GumService.Default.Root.Children.Clear();
        return result;
    }
}
