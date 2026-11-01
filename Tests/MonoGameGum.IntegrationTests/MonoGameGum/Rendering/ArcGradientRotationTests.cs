using Gum.GueDeriving;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// #7313 - an Arc's gradient turns with the shape, and a radial gradient's outer radius of 0 or less is
/// 100 pixels, as on Skia. Blue (0,0,255) to green (0,255,0), 100x100 box centered at (100,100). Real GL, Apos.Shapes.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class ArcGradientRotationTests : BaseTestClass
{
    private const int Size = 200;

    private static XnaColor[] Render(float rotation, bool radial, float outerRadius)
    {
        using ShapeGameLease game = new ShapeGameLease();
        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;

        ContainerRuntime root = new();
        root.Width = Size;
        root.Height = Size;
#pragma warning disable CS0618
        root.AddChild(new ColoredRectangleRuntime { Width = Size, Height = Size, Color = XnaColor.White });
#pragma warning restore CS0618

        double radians = rotation * Math.PI / 180;
        ArcRuntime arc = new() { StartAngle = 0, SweepAngle = 360, Thickness = 30 };
        arc.X = (float)(100 - (50 * Math.Cos(radians) + 50 * Math.Sin(radians)));
        arc.Y = (float)(100 - (-50 * Math.Sin(radians) + 50 * Math.Cos(radians)));
        arc.Width = 100;
        arc.Height = 100;
        arc.Rotation = rotation;
        arc.UseGradient = true;
        arc.Red1 = 0; arc.Green1 = 0; arc.Blue1 = 255; arc.Alpha1 = 255;
        arc.Red2 = 0; arc.Green2 = 255; arc.Blue2 = 0; arc.Alpha2 = 255;
        if (radial)
        {
            arc.GradientType = GradientType.Radial;
            arc.GradientX1 = 50;
            arc.GradientY1 = 50;
            arc.GradientOuterRadius = outerRadius;
        }
        else
        {
            arc.GradientType = GradientType.Linear;
            arc.GradientX2 = 100;
            arc.GradientY2 = 100;
        }
        root.AddChild(arc);
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

    [Fact]
    public void LinearGradient_Rotated90_RunsFromBottomLeftToTopRight()
    {
        XnaColor[] pixels = Render(90f, radial: false, 0f);

        XnaColor bottomLeft = pixels[(130 * Size) + 70];
        XnaColor topRight = pixels[(70 * Size) + 130];
        ((int)bottomLeft.B).ShouldBeGreaterThan(bottomLeft.G + 80);
        ((int)topRight.G).ShouldBeGreaterThan(topRight.B + 80);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-10f)]
    public void RadialGradient_OuterRadiusZeroOrLess_UsesOneHundredPixels(float outerRadius)
    {
        XnaColor[] pixels = Render(0f, radial: true, outerRadius);

        // 35 pixels from the center is 35% of the way to 100: about (0, 89, 166).
        XnaColor sample = pixels[(100 * Size) + 135];
        ((float)sample.G).ShouldBe(89f, 20f);
        ((float)sample.B).ShouldBe(166f, 20f);
    }
}
