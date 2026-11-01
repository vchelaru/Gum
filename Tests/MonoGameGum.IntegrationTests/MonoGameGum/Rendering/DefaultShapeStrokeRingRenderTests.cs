using Microsoft.Xna.Framework.Graphics;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// #7346 - a fresh Circle/Rectangle carries a 1 px white stroke on every runtime (the documented
/// default). Pins it by pixel: red fill over black, the left-edge pixel is the white ring.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class DefaultShapeStrokeRingRenderTests : BaseTestClass
{
    private static XnaColor EdgePixel(Func<GraphicalUiElement> create, bool gradient = false)
    {
        using ShapeGameLease game = new ShapeGameLease();
        GraphicalUiElement shape = create();
        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;

        ContainerRuntime root = new();
        root.Width = 100;
        root.Height = 100;
#pragma warning disable CS0618
        root.AddChild(new ColoredRectangleRuntime { Width = 100, Height = 100, Color = XnaColor.Black });
#pragma warning restore CS0618

        shape.SetProperty("IsFilled", true);
        if (gradient)
        {
            shape.SetProperty("UseGradient", true);
            shape.SetProperty("GradientType", GradientType.Radial);
            shape.SetProperty("Red1", 255);
            shape.SetProperty("Green1", 0);
            shape.SetProperty("Blue1", 0);
            shape.SetProperty("Alpha1", 255);
            shape.SetProperty("Red2", 255);
            shape.SetProperty("Green2", 0);
            shape.SetProperty("Blue2", 0);
            shape.SetProperty("Alpha2", 255);
            shape.SetProperty("GradientX1", 32f);
            shape.SetProperty("GradientY1", 32f);
            shape.SetProperty("GradientInnerRadius", 0f);
            shape.SetProperty("GradientOuterRadius", 32f);
        }
        else
        {
            shape.SetProperty("FillRed", 255);
            shape.SetProperty("FillGreen", 0);
            shape.SetProperty("FillBlue", 0);
        }
        shape.X = 20;
        shape.Y = 20;
        shape.Width = 64;
        shape.Height = 64;
        root.AddChild(shape);

        root.AddToManagers(managers, null);
        root.UpdateLayout();
        using RenderTarget2D capture = new(gd, 100, 100, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(XnaColor.Black);
            managers.Renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);
        XnaColor[] pixels = new XnaColor[100 * 100];
        capture.GetData(pixels);
        root.RemoveFromManagers();
        return pixels[(52 * 100) + 20];
    }

    [Fact]
    public void DefaultCircle_StrokeWidthIsOne()
    {
        using ShapeGameLease game = new ShapeGameLease();
        new CircleRuntime().StrokeWidth.ShouldBe(1f);
    }

    [Fact]
    public void DefaultCircle_DrawsWhiteRingAtEdge()
    {
        EdgePixel(() => new CircleRuntime()).G.ShouldBeGreaterThan((byte)200);
    }

    [Fact]
    public void DefaultCircle_Gradient_DrawsWhiteRingAtEdge()
    {
        EdgePixel(() => new CircleRuntime(), gradient: true).G.ShouldBeGreaterThan((byte)200);
    }

    [Fact]
    public void CircleWithStrokeWidthZero_DrawsNoRing()
    {
        EdgePixel(() => new CircleRuntime { StrokeWidth = 0 }).G.ShouldBeLessThan((byte)50);
    }

    [Fact]
    public void DefaultRectangle_DrawsWhiteRingAtEdge()
    {
        EdgePixel(() => new RectangleRuntime()).G.ShouldBeGreaterThan((byte)200);
    }

    [Fact]
    public void DefaultRectangle_Gradient_DrawsWhiteRingAtEdge()
    {
        EdgePixel(() => new RectangleRuntime(), gradient: true).G.ShouldBeGreaterThan((byte)200);
    }
}
