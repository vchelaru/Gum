using Microsoft.Xna.Framework.Graphics;
using Gum.GueDeriving;
using Gum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// The reference numbers for #6185: the same scene is rendered by SkiaGum in
/// <c>SkiaGum.Tests.Renderables.BlendTests</c> and must give the same pixels. A render-target
/// container holds a red fill over the left 55px, a 50% white square mask over the fill and a 50%
/// white circle mask straddling the fill edge, all over an opaque backdrop. Alpha-only blends apply
/// to a shape's whole bounds, so the circle's corner (40, 52) is erased as well.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class AlphaBlendSceneTests : BaseTestClass
{
    private const int Size = 100;

    [Fact]
    public void ReplaceAlpha_Scene_HasExpectedPixels()
    {
        XnaColor[] pixels = Render(Gum.RenderingLibrary.Blend.ReplaceAlpha);

        AssertPixel(pixels, 20, 20, 135, 50, 60);  // square over red: red at 50%
        AssertPixel(pixels, 50, 60, 135, 50, 60);  // circle over red
        AssertPixel(pixels, 70, 70, 35, 35, 45);   // circle over empty: black at 50%
        AssertPixel(pixels, 40, 52, 70, 70, 90);   // circle bounds, outside the circle: erased
        AssertPixel(pixels, 5, 5, 200, 30, 30);    // outside every mask
        AssertPixel(pixels, 90, 10, 70, 70, 90);   // empty, outside every mask
    }

    [Fact]
    public void MinAlpha_Scene_HasExpectedPixels()
    {
        XnaColor[] pixels = Render(Gum.RenderingLibrary.Blend.MinAlpha);

        AssertPixel(pixels, 20, 20, 135, 50, 60);
        AssertPixel(pixels, 50, 60, 135, 50, 60);
        AssertPixel(pixels, 70, 70, 70, 70, 90);   // circle over empty: stays empty
        AssertPixel(pixels, 40, 52, 70, 70, 90);
        AssertPixel(pixels, 5, 5, 200, 30, 30);
        AssertPixel(pixels, 90, 10, 70, 70, 90);
    }

    private static void AssertPixel(XnaColor[] pixels, int x, int y, int r, int g, int b)
    {
        XnaColor actual = pixels[(y * Size) + x];
        System.Math.Abs(actual.R - r).ShouldBeLessThanOrEqualTo(2, $"R at ({x}, {y}) was {actual.R}");
        System.Math.Abs(actual.G - g).ShouldBeLessThanOrEqualTo(2, $"G at ({x}, {y}) was {actual.G}");
        System.Math.Abs(actual.B - b).ShouldBeLessThanOrEqualTo(2, $"B at ({x}, {y}) was {actual.B}");
    }

    private static XnaColor[] Render(Gum.RenderingLibrary.Blend blend)
    {
        using ShapeGameLease game = new ShapeGameLease();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        ContainerRuntime root = new();
        root.Width = Size;
        root.Height = Size;
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest solid fill.
        root.AddChild(new ColoredRectangleRuntime { Width = Size, Height = Size, Color = new XnaColor((byte)70, (byte)70, (byte)90, (byte)255) });

        ContainerRuntime renderTarget = new();
        renderTarget.Width = Size;
        renderTarget.Height = Size;
        renderTarget.IsRenderTarget = true;
        renderTarget.AddChild(new ColoredRectangleRuntime { Width = 55, Height = Size, Color = new XnaColor((byte)200, (byte)30, (byte)30, (byte)255) });

        ColoredRectangleRuntime square = new() { X = 8, Y = 8, Width = 35, Height = 35, Color = XnaColor.White };
        square.Alpha = 128;
        square.Blend = blend;
        renderTarget.AddChild(square);
#pragma warning restore CS0618

        CircleRuntime circle = new();
        circle.X = 38;
        circle.Y = 50;
        circle.Width = 45;
        circle.Height = 45;
        circle.IsFilled = true;
        circle.StrokeWidth = 0;
        circle.FillColor = new XnaColor((byte)255, (byte)255, (byte)255, (byte)128);
        circle.Blend = blend;
        renderTarget.AddChild(circle);
        root.AddChild(renderTarget);

        root.AddToManagers(managers, null);
        root.UpdateLayout();

        using RenderTarget2D capture = new(gd, Size, Size, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(XnaColor.Black);
            renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        XnaColor[] pixels = new XnaColor[Size * Size];
        capture.GetData(pixels);

        root.RemoveFromManagers();
        return pixels;
    }
}
