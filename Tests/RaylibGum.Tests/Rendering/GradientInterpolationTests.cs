using Gum.GueDeriving;
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

    private static Color[] Render(string shape, bool linear, int alpha1, int alpha2)
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
        gradient.Width = Size;
        gradient.Height = Size;
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
            gradient.SetProperty("GradientX1", 10f);
            gradient.SetProperty("GradientY1", 0f);
            gradient.SetProperty("GradientX2", 90f);
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
