using Gum.GueDeriving;
using Gum.Renderables;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Issue #7307: raylib, like Skia, interpolates a gradient on the straight sRGB channels. For blue
/// (0,0,255) to green (0,255,0) the channel at fraction t is (0, 255 t, 255 (1 - t)). The endpoints sit
/// inside the shape (10 to 90 of 100), so the color at the endpoints is the stop itself, not a clamp
/// blended across the whole shape.
/// </summary>
public class GradientInterpolationTests : BaseTestClass
{
    private const int Size = 100;
    private const int Tolerance = 8;
    private static readonly Color Backdrop = new Color((byte)70, (byte)70, (byte)90, (byte)255);

    [Theory]
    [InlineData("Rect", 0f)]
    [InlineData("Rect", 0.25f)]
    [InlineData("Rect", 0.5f)]
    [InlineData("Rect", 0.75f)]
    [InlineData("Rect", 1f)]
    [InlineData("Circle", 0.25f)]
    [InlineData("Circle", 0.5f)]
    [InlineData("Arc", 0.5f)]
    [InlineData("Arc", 0.75f)]
    public void LinearGradient_BlueToGreen_IsStraightSrgb(string shape, float t)
    {
        Color[] pixels = Render(shape, linear: true, alpha1: 255, alpha2: 255);

        Color actual = pixels[(50 * Size) + 10 + (int)(80 * t) - (t >= 1f ? 1 : 0)];

        ((double)actual.R).ShouldBe(0, Tolerance);
        ((double)actual.G).ShouldBe(255 * t, Tolerance);
        ((double)actual.B).ShouldBe(255 * (1 - t), Tolerance);
    }

    [Theory]
    [InlineData("Rect", 0.25f)]
    [InlineData("Rect", 0.5f)]
    [InlineData("Circle", 0.5f)]
    [InlineData("Arc", 0.5f)]
    [InlineData("Arc", 0.75f)]
    public void RadialGradient_BlueToGreen_IsStraightSrgb(string shape, float t)
    {
        Color[] pixels = Render(shape, linear: false, alpha1: 255, alpha2: 255);

        Color actual = pixels[(50 * Size) + 50 + (int)(40 * t)];

        ((double)actual.G).ShouldBe(255 * t, Tolerance);
        ((double)actual.B).ShouldBe(255 * (1 - t), Tolerance);
    }

    [Theory]
    [InlineData("Rect", 0.5f)]
    [InlineData("Circle", 0.25f)]
    [InlineData("Arc", 0.5f)]
    public void LinearGradient_FadingToTransparentGreen_InterpolatesStraightAlpha(string shape, float t)
    {
        Color[] pixels = Render(shape, linear: true, alpha1: 255, alpha2: 0);

        Color actual = pixels[(50 * Size) + 10 + (int)(80 * t)];

        // Straight interpolation: (0, 255 t, 255 (1 - t)) at alpha (1 - t), over the backdrop.
        float alpha = 1 - t;
        ((double)actual.R).ShouldBe(Backdrop.R * (1 - alpha), Tolerance);
        ((double)actual.G).ShouldBe((255 * t * alpha) + (Backdrop.G * (1 - alpha)), Tolerance);
        ((double)actual.B).ShouldBe((255 * (1 - t) * alpha) + (Backdrop.B * (1 - alpha)), Tolerance);
    }

    [Theory]
    [InlineData("Rect")]
    [InlineData("Circle")]
    public void LinearGradient_WithTheSameStartAndEnd_FillsWithTheEndColor(string shape)
    {
        // Skia clamps a zero length gradient to its last stop.
        Color[] pixels = Render(shape, linear: true, alpha1: 255, alpha2: 255, gradientX1: 10, gradientX2: 10);

        Color actual = pixels[(50 * Size) + 40];

        ((double)actual.G).ShouldBe(255, Tolerance);
        ((double)actual.B).ShouldBe(0, Tolerance);
    }

    [Fact]
    public void LinearGradient_OnA2000SquareRectangle_EmitsAtMost36Vertices()
    {
        RectangleRuntime rectangle = new();
        rectangle.Width = 2000;
        rectangle.Height = 2000;
        rectangle.SetProperty("IsFilled", true);
        rectangle.SetProperty("StrokeWidth", 0f);
        rectangle.SetProperty("UseGradient", true);
        rectangle.SetProperty("GradientType", GradientType.Linear);
        rectangle.SetProperty("Green2", 255);
        rectangle.SetProperty("GradientX1", 500f);
        rectangle.SetProperty("GradientY1", 300f);
        rectangle.SetProperty("GradientX2", 1500f);
        rectangle.SetProperty("GradientY2", 900f);
        Gum.GumService.Default.Root.Children.Add(rectangle);
        Gum.GumService.Default.Root.UpdateLayout();

        BeginDrawing();
        Gum.GumService.Default.Draw();
        EndDrawing();

        LineRectangle renderable = (LineRectangle)rectangle.RenderableComponent!;
        renderable.LastLinearGradientVertexCount.ShouldBeInRange(3, 36);
        Gum.GumService.Default.Root.Children.Clear();
    }

    [Theory]
    [InlineData(2f, 0f)]
    [InlineData(10f, 0f)]
    [InlineData(30f, 0.5f)]
    [InlineData(50f, 1f)]
    [InlineData(57f, 1f)]
    public void LinearGradient_OnARectangleTurned90Degrees_FollowsTheShape(float localX, float t)
    {
        // A 60x40 box turned 90 degrees counterclockwise about its top-left at (20,80): local (lx, ly) is world (20 + ly, 80 - lx).
        Color[] pixels = Render("Rect", linear: true, alpha1: 255, alpha2: 255, left: 20, top: 80, width: 60, height: 40,
            rotation: 90, gradientX1: 10, gradientX2: 50);

        Color actual = pixels[((80 - (int)localX - 1) * Size) + 20 + 20];

        ((double)actual.G).ShouldBe(255 * t, Tolerance);
        ((double)actual.B).ShouldBe(255 * (1 - t), Tolerance);
    }

    [Fact]
    public void LinearGradient_OnARectangleTurned30Degrees_MatchesTheGradientFormula()
    {
        Color[] pixels = Render("Rect", linear: true, alpha1: 255, alpha2: 255, left: 20, top: 70, width: 70, height: 30,
            rotation: 30, gradientX1: 15, gradientX2: 55);

        float cos = MathF.Cos(30 * MathF.PI / 180);
        float sin = MathF.Sin(30 * MathF.PI / 180);
        foreach (float localX in new[] { 5f, 20f, 35f, 50f, 65f })
        {
            float localY = 15;
            int worldX = (int)(20 + (localX * cos) + (localY * sin));
            int worldY = (int)(70 - (localX * sin) + (localY * cos));
            float t = Math.Clamp((localX - 15) / 40f, 0f, 1f);

            Color actual = pixels[(worldY * Size) + worldX];

            ((double)actual.G).ShouldBe(255 * t, Tolerance + 4);
            ((double)actual.B).ShouldBe(255 * (1 - t), Tolerance + 4);
        }
    }

    [Fact]
    public void LinearGradient_OnANonSquareRectangleWithAlpha_MatchesTheGradientFormula()
    {
        Color[] pixels = Render("Rect", linear: true, alpha1: 255, alpha2: 64, left: 5, top: 30, width: 90, height: 20,
            rotation: 0, gradientX1: 10, gradientX2: 80);

        foreach (float localX in new[] { 5f, 25f, 45f, 65f, 85f })
        {
            float t = Math.Clamp((localX - 10) / 70f, 0f, 1f);
            float alpha = (255 + ((64 - 255) * t)) / 255f;
            Color actual = pixels[(40 * Size) + 5 + (int)localX];

            ((double)actual.G).ShouldBe((255 * t * alpha) + (Backdrop.G * (1 - alpha)), Tolerance);
            ((double)actual.B).ShouldBe((255 * (1 - t) * alpha) + (Backdrop.B * (1 - alpha)), Tolerance);
        }
    }

    private static Color[] Render(string shape, bool linear, int alpha1, int alpha2, float left = 0, float top = 0,
        float width = Size, float height = Size, float rotation = 0, float gradientX1 = 10, float gradientX2 = 90)
    {
        ContainerRuntime container = new();
        container.Width = Size;
        container.Height = Size;
        container.IsRenderTarget = true;

        RectangleRuntime backdrop = new();
        backdrop.Width = Size;
        backdrop.Height = Size;
        backdrop.IsFilled = true;
        backdrop.StrokeWidth = 0;
        backdrop.FillColor = Backdrop;
        container.Children.Add(backdrop);

        GraphicalUiElement gradient;
        if (shape == "Rect")
        {
            gradient = new RectangleRuntime();
        }
        else if (shape == "Circle")
        {
            gradient = new CircleRuntime();
        }
        else
        {
            ArcRuntime arc = new();
            arc.StartAngle = 0;
            arc.SweepAngle = 360;
            arc.SetProperty("Thickness", 50f);
            gradient = arc;
        }
        gradient.X = left;
        gradient.Y = top;
        gradient.Width = width;
        gradient.Height = height;
        gradient.Rotation = rotation;
        if (shape != "Arc")
        {
            gradient.SetProperty("IsFilled", true);
            gradient.SetProperty("StrokeWidth", 0f);
        }
        gradient.SetProperty("UseGradient", true);
        gradient.SetProperty("GradientType", linear ? GradientType.Linear : GradientType.Radial);
        gradient.SetProperty("Red1", 0);
        gradient.SetProperty("Green1", 0);
        gradient.SetProperty("Blue1", 255);
        gradient.SetProperty("Alpha1", alpha1);
        gradient.SetProperty("Red2", 0);
        gradient.SetProperty("Green2", 255);
        gradient.SetProperty("Blue2", 0);
        gradient.SetProperty("Alpha2", alpha2);
        if (linear)
        {
            gradient.SetProperty("GradientX1", gradientX1);
            gradient.SetProperty("GradientY1", 0f);
            gradient.SetProperty("GradientX2", gradientX2);
            gradient.SetProperty("GradientY2", 0f);
        }
        else
        {
            gradient.SetProperty("GradientX1", 50f);
            gradient.SetProperty("GradientY1", 50f);
            gradient.SetProperty("GradientInnerRadius", 0f);
            gradient.SetProperty("GradientOuterRadius", 40f);
        }
        container.Children.Add(gradient);

        Gum.GumService.Default.Root.Children.Add(container);
        Gum.GumService.Default.Root.UpdateLayout();

        BeginDrawing();
        Gum.GumService.Default.Draw();
        EndDrawing();

        RenderTexture2D rt = Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value;
        Image image = LoadImageFromTexture(rt.Texture);
        Color[] result = new Color[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                result[(y * Size) + x] = GetImageColor(image, x, rt.Texture.Height - 1 - y);
            }
        }
        UnloadImage(image);
        Gum.GumService.Default.Root.Children.Clear();
        return result;
    }
}
