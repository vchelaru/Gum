using Gum.GueDeriving;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Runtimes;

/// <summary>
/// #7313 - a gradient Arc is an ellipse inscribed in its box, turned about the box's top-left corner,
/// with the gradient following the shape; an unset or negative radial outer radius is 100 pixels.
/// Samples come from the same scene rendered on Skia: opaque, over white, blue (0,0,255) to green (0,255,0).
/// </summary>
public class ArcGradientPlacementTests : BaseTestClass
{
    private const int Size = 200;
    private const float Tolerance = 20f;

    // Renders a 30 pixel thick full-sweep arc whose box center is (100, 100) after rotating.
    private static Color[,] Render(float width, float height, float rotation, bool radial, float outerRadius = 50f)
    {
        ContainerRuntime container = new();
        container.Width = Size;
        container.Height = Size;
        container.IsRenderTarget = true;

        RectangleRuntime background = new();
        background.Width = Size;
        background.Height = Size;
        background.IsFilled = true;
        background.FillColor = new Color((byte)255, (byte)255, (byte)255, (byte)255);
        container.Children.Add(background);

        double radians = rotation * System.Math.PI / 180;
        double halfW = width / 2;
        double halfH = height / 2;
        ArcRuntime arc = new() { StartAngle = 0, SweepAngle = 360, Thickness = 30 };
        arc.X = (float)(100 - (halfW * System.Math.Cos(radians) + halfH * System.Math.Sin(radians)));
        arc.Y = (float)(100 - (-halfW * System.Math.Sin(radians) + halfH * System.Math.Cos(radians)));
        arc.Width = width;
        arc.Height = height;
        arc.Rotation = rotation;
        arc.UseGradient = true;
        arc.Red1 = 0; arc.Green1 = 0; arc.Blue1 = 255; arc.Alpha1 = 255;
        arc.Red2 = 0; arc.Green2 = 255; arc.Blue2 = 0; arc.Alpha2 = 255;
        if (radial)
        {
            arc.GradientType = GradientType.Radial;
            arc.GradientX1 = width / 2;
            arc.GradientY1 = height / 2;
            arc.GradientOuterRadius = outerRadius;
        }
        else
        {
            arc.GradientType = GradientType.Linear;
            arc.GradientX2 = width;
            arc.GradientY2 = height;
        }
        container.Children.Add(arc);

        Gum.GumService.Default.Root.Children.Add(container);
        Gum.GumService.Default.Root.UpdateLayout();
        BeginDrawing();
        Gum.GumService.Default.Draw();
        EndDrawing();

        Image image = LoadImageFromTexture(Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value.Texture);
        ImageFlipVertical(ref image);
        Color[,] pixels = new Color[Size, Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                pixels[x, y] = GetImageColor(image, x, y);
            }
        }
        UnloadImage(image);
        Gum.GumService.Default.Root.Children.Clear();
        return pixels;
    }

    private static void ShouldBeNear(Color actual, int r, int g, int b)
    {
        ((float)actual.R).ShouldBe(r, Tolerance);
        ((float)actual.G).ShouldBe(g, Tolerance);
        ((float)actual.B).ShouldBe(b, Tolerance);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(30f)]
    [InlineData(90f)]
    public void RotatedArc_StaysCenteredOnTheRotatedBox(float rotation)
    {
        Color[,] pixels = Render(100, 100, rotation, radial: false);

        // The ring is 20 to 50 pixels from the center (100,100), whatever the rotation.
        pixels[100, 65].R.ShouldBeLessThan((byte)200);
        pixels[135, 100].R.ShouldBeLessThan((byte)200);
        pixels[100, 100].R.ShouldBe((byte)255);
        pixels[100, 40].R.ShouldBe((byte)255);
    }

    [Fact]
    public void LinearGradient_Rotated90_RunsFromBottomLeftToTopRight()
    {
        Color[,] pixels = Render(100, 100, 90f, radial: false);

        // Skia: the box diagonal, turned 90 degrees counterclockwise with the shape.
        ((int)pixels[70, 130].B).ShouldBeGreaterThan(pixels[70, 130].G + 80);
        ((int)pixels[130, 70].G).ShouldBeGreaterThan(pixels[130, 70].B + 80);
    }

    [Fact]
    public void LinearGradient_Rotated30_MatchesSkiaSamples()
    {
        Color[,] pixels = Render(100, 100, 30f, radial: false);

        ShouldBeNear(pixels[56, 100], 0, 56, 199);
        ShouldBeNear(pixels[144, 100], 0, 199, 56);
    }

    [Fact]
    public void ArcInNonSquareBox_FillsTheBox()
    {
        Color[,] pixels = Render(140, 80, 0f, radial: false);

        // Box x 30..170, y 60..140; the ring's outer edge is on the box.
        pixels[34, 100].R.ShouldBeLessThan((byte)200);
        pixels[165, 100].R.ShouldBeLessThan((byte)200);
        pixels[100, 64].R.ShouldBeLessThan((byte)200);
        pixels[100, 136].R.ShouldBeLessThan((byte)200);
        pixels[100, 100].R.ShouldBe((byte)255);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-10f)]
    public void RadialGradient_OuterRadiusZeroOrLess_UsesOneHundredPixels(float outerRadius)
    {
        Color[,] pixels = Render(100, 100, 0f, radial: true, outerRadius);

        // 35 pixels from the center is 35% of the way to 100: (0, 89, 166) over blue-to-green.
        ShouldBeNear(pixels[135, 100], 0, 89, 166);
        ShouldBeNear(pixels[100, 65], 0, 89, 166);
    }
}
