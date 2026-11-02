using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Issue #7307: a gradient is interpolated on the straight sRGB channels, like Skia and raylib do, not in
/// Apos.Shapes' default Oklab space (which brightens the mid-tones, and mixes premultiplied alpha). The
/// expected values are Skia's: for blue (0,0,255) to green (0,255,0) the channel at fraction t is
/// (0, 255 t, 255 (1 - t)). Real GL, Apos.Shapes.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class GradientInterpolationTests : BaseTestClass
{
    private const int Size = 100;
    private const int Tolerance = 8;
    private static readonly XnaColor Backdrop = new XnaColor((byte)70, (byte)70, (byte)90, (byte)255);

    [Theory]
    [InlineData("Rect", 0.25f)]
    [InlineData("Rect", 0.5f)]
    [InlineData("Rect", 0.75f)]
    [InlineData("Circle", 0.5f)]
    [InlineData("Arc", 0.25f)]
    [InlineData("Arc", 0.5f)]
    [InlineData("Arc", 0.75f)]
    public void LinearGradient_BlueToGreen_MidTonesAreStraightSrgb(string shape, float t)
    {
        XnaColor[] pixels = Render(shape, linear: true, alpha: 255);

        XnaColor actual = pixels[(50 * Size) + 10 + (int)(80 * t)];

        ((double)actual.R).ShouldBe((double)(0), Tolerance);
        ((double)actual.G).ShouldBe((double)((int)(255 * t)), Tolerance);
        ((double)actual.B).ShouldBe((double)((int)(255 * (1 - t))), Tolerance);
    }

    [Theory]
    [InlineData("Rect", 0.25f)]
    [InlineData("Rect", 0.5f)]
    [InlineData("Circle", 0.5f)]
    [InlineData("Arc", 0.5f)]
    [InlineData("Arc", 0.75f)]
    public void RadialGradient_BlueToGreen_MidTonesAreStraightSrgb(string shape, float t)
    {
        XnaColor[] pixels = Render(shape, linear: false, alpha: 255);

        // Radius 40 around (50,50), sampled along the row.
        XnaColor actual = pixels[(50 * Size) + 50 + (int)(40 * t)];

        ((double)actual.G).ShouldBe((double)((int)(255 * t)), Tolerance);
        ((double)actual.B).ShouldBe((double)((int)(255 * (1 - t))), Tolerance);
    }

    [Theory]
    [InlineData("Rect", 0.5f)]
    [InlineData("Circle", 0.25f)]
    [InlineData("Arc", 0.5f)]
    [InlineData("Arc", 0.75f)]
    public void LinearGradient_BlueToGreenAtHalfAlpha_BlendsStraightColorOverTheBackdrop(string shape, float t)
    {
        XnaColor[] pixels = Render(shape, linear: true, alpha: 128);

        XnaColor actual = pixels[(50 * Size) + 10 + (int)(80 * t)];

        // Both stops have alpha 128, so the straight and the premultiplied mix agree: (0, 255 t, 255 (1 - t))
        // at alpha 128/255 over the backdrop. A gradient whose stops differ in alpha still mixes premultiplied
        // in Apos.Shapes' shader (#7368).
        float alpha = 128 / 255f;
        float expectedR = (Backdrop.R * (1 - alpha));
        float expectedG = (255 * t * alpha) + (Backdrop.G * (1 - alpha));
        float expectedB = (255 * (1 - t) * alpha) + (Backdrop.B * (1 - alpha));
        ((double)actual.R).ShouldBe((double)((int)expectedR), Tolerance);
        ((double)actual.G).ShouldBe((double)((int)expectedG), Tolerance);
        ((double)actual.B).ShouldBe((double)((int)expectedB), Tolerance);
    }

    [Theory]
    [InlineData("Rect")]
    [InlineData("Circle")]
    public void LinearGradient_WithTheSameStartAndEnd_FillsWithTheEndColor(string shape)
    {
        // Skia clamps a zero length gradient to its last stop.
        XnaColor[] pixels = Render(shape, linear: true, alpha: 255, zeroLength: true);

        XnaColor actual = pixels[(50 * Size) + 40];

        ((double)actual.G).ShouldBe(255, Tolerance);
        ((double)actual.B).ShouldBe(0, Tolerance);
    }

    private static XnaColor[] Render(string shape, bool linear, int alpha, bool zeroLength = false)
    {
        using ShapeGameLease game = new ShapeGameLease();
        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;

        ContainerRuntime root = new();
        root.Width = Size;
        root.Height = Size;
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest solid fill.
        root.AddChild(new ColoredRectangleRuntime { Width = Size, Height = Size, Color = Backdrop });
#pragma warning restore CS0618

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
            arc.Thickness = 50;
            arc.Color = new XnaColor((byte)0, (byte)0, (byte)255, (byte)255);
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
        gradient.SetProperty("Alpha1", alpha);
        gradient.SetProperty("Red2", 0);
        gradient.SetProperty("Green2", 255);
        gradient.SetProperty("Blue2", 0);
        gradient.SetProperty("Alpha2", alpha);
        if (linear)
        {
            gradient.SetProperty("GradientX1", 10f);
            gradient.SetProperty("GradientY1", 0f);
            gradient.SetProperty("GradientX2", zeroLength ? 10f : 90f);
            gradient.SetProperty("GradientY2", 0f);
        }
        else
        {
            gradient.SetProperty("GradientX1", 50f);
            gradient.SetProperty("GradientY1", 50f);
            gradient.SetProperty("GradientInnerRadius", 0f);
            gradient.SetProperty("GradientOuterRadius", 40f);
        }
        root.AddChild(gradient);

        root.AddToManagers(managers, null);
        root.UpdateLayout();

        using RenderTarget2D capture = new(gd, Size, Size, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(XnaColor.Black);
            managers.Renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        XnaColor[] pixels = new XnaColor[Size * Size];
        capture.GetData(pixels);
        root.RemoveFromManagers();
        return pixels;
    }
}
