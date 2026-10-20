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
/// Issue #6246: a Rectangle, RoundedRectangle or Circle with a stroke is a fill slot and a stroke slot, and
/// under ReplaceAlpha and MinAlpha the two give one mask: the fill where it is filled, the ring where it is
/// stroked, and the rest of the bounds erased. The same scene is rendered by SkiaGum in
/// <c>SkiaGum.Tests.Renderables.BlendTests</c> and must give the same pixels. Each 36x36 shape has a 6px
/// stroke at full alpha and a fill at 50% alpha, over an opaque red render target and an opaque backdrop.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class StrokedShapeAlphaMaskSceneTests : BaseTestClass
{
    private const int Width = 120;
    private const int Height = 80;
    private const int ShapeSize = 36;
    private const int Pitch = 40;
    private const int Origin = 2;

    private static readonly XnaColor Backdrop = new XnaColor((byte)70, (byte)70, (byte)90, (byte)255);
    private static readonly XnaColor Red = new XnaColor((byte)200, (byte)30, (byte)30, (byte)255);

    // Red at 50% over the backdrop.
    private static readonly XnaColor HalfRed = new XnaColor((byte)135, (byte)50, (byte)60, (byte)255);

    // The filled shapes can have a gradient fill instead: opaque at the left edge and 50% alpha from one pixel on,
    // so the interior has the same pixels as the solid 50% fill. That goes through its own branch when the stroke
    // slot draws the fill.
    [Theory]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha, false)]
    [InlineData(Gum.RenderingLibrary.Blend.MinAlpha, false)]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha, true)]
    [InlineData(Gum.RenderingLibrary.Blend.MinAlpha, true)]
    public void StrokedShapes_MaskAsOneShape(Gum.RenderingLibrary.Blend blend, bool useGradientFill)
    {
        XnaColor[] pixels = Render(blend, useGradientFill);

        // Column 0 is a Rectangle, 1 a RoundedRectangle (radius 10), 2 a Circle. Row 0 has a stroke only, row 1 a fill too.
        for (int column = 0; column < 3; column++)
        {
            for (int row = 0; row < 2; row++)
            {
                int left = Origin + (column * Pitch);
                int top = Origin + (row * Pitch);
                string name = $"column {column}, row {row}";

                AssertPixel(pixels, left + 3, top + (ShapeSize / 2), Red, $"{name}: ring");
                AssertPixel(pixels, left + (ShapeSize / 2), top + (ShapeSize / 2), row == 0 ? Backdrop : HalfRed, $"{name}: interior");

                // The bounds corner is ring on a Rectangle and outside the body of the other two, so it is erased.
                AssertPixel(pixels, left + 1, top + 1, column == 0 ? Red : Backdrop, $"{name}: bounds corner");
                AssertPixel(pixels, left - 1, top + 1, Red, $"{name}: outside the bounds");
            }
        }
    }

    // A stroke slot with another blend than its fill slot is not part of the fill slot's mask: the fill slot masks
    // and draws its fill itself, and the ring draws normally over it (white, the stroke color).
    [Fact]
    public void StrokeSlotWithAnotherBlend_IsNotMergedIntoTheFillSlotsMask()
    {
        XnaColor[] pixels = Render(Gum.RenderingLibrary.Blend.ReplaceAlpha, useGradientFill: false,
            strokeSlotBlend: Gum.RenderingLibrary.Blend.Normal);

        int left = Origin + (2 * Pitch);
        int top = Origin + Pitch;
        AssertPixel(pixels, left + (ShapeSize / 2), top + (ShapeSize / 2), HalfRed, "interior");
        AssertPixel(pixels, left + 3, top + (ShapeSize / 2), XnaColor.White, "ring");
    }

    // A transparent stroke draws nothing, so the fill is not inset and covers the ring zone itself.
    [Theory]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha)]
    [InlineData(Gum.RenderingLibrary.Blend.MinAlpha)]
    public void TransparentStroke_LeavesTheFillCoveringTheRingZone(Gum.RenderingLibrary.Blend blend)
    {
        XnaColor[] pixels = Render(blend, useGradientFill: false, strokeAlpha: 0);

        int left = Origin;
        int top = Origin + Pitch;
        AssertPixel(pixels, left + 3, top + (ShapeSize / 2), HalfRed, "ring zone");
        AssertPixel(pixels, left + (ShapeSize / 2), top + (ShapeSize / 2), HalfRed, "interior");
    }

    private static void AssertPixel(XnaColor[] pixels, int x, int y, XnaColor expected, string name)
    {
        XnaColor actual = pixels[(y * Width) + x];
        System.Math.Abs(actual.R - expected.R).ShouldBeLessThanOrEqualTo(3, $"{name}: R at ({x}, {y}) was {actual.R}");
        System.Math.Abs(actual.G - expected.G).ShouldBeLessThanOrEqualTo(3, $"{name}: G at ({x}, {y}) was {actual.G}");
        System.Math.Abs(actual.B - expected.B).ShouldBeLessThanOrEqualTo(3, $"{name}: B at ({x}, {y}) was {actual.B}");
    }

    private static XnaColor[] Render(Gum.RenderingLibrary.Blend blend, bool useGradientFill,
        Gum.RenderingLibrary.Blend? strokeSlotBlend = null, byte strokeAlpha = 255)
    {
        using ShapeGameLease game = new ShapeGameLease();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        ContainerRuntime root = new();
        root.Width = Width;
        root.Height = Height;
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest solid fill.
        root.AddChild(new ColoredRectangleRuntime { Width = Width, Height = Height, Color = Backdrop });

        ContainerRuntime renderTarget = new();
        renderTarget.Width = Width;
        renderTarget.Height = Height;
        renderTarget.IsRenderTarget = true;
        renderTarget.AddChild(new ColoredRectangleRuntime { Width = Width, Height = Height, Color = Red });
#pragma warning restore CS0618

        XnaColor stroke = new XnaColor((byte)255, (byte)255, (byte)255, strokeAlpha);
        XnaColor fill = new XnaColor((byte)255, (byte)255, (byte)255, (byte)128);
        for (int column = 0; column < 3; column++)
        {
            for (int row = 0; row < 2; row++)
            {
                float x = Origin + (column * Pitch);
                float y = Origin + (row * Pitch);
                bool isFilled = row == 1;
                if (column == 2)
                {
                    CircleRuntime circle = new();
                    circle.X = x;
                    circle.Y = y;
                    circle.Width = ShapeSize;
                    circle.Height = ShapeSize;
                    circle.IsFilled = isFilled;
                    circle.FillColor = fill;
                    circle.StrokeWidth = 6;
                    circle.StrokeColor = stroke;
                    circle.Blend = blend;
                    if (strokeSlotBlend != null)
                    {
                        ((RenderableShapeBase)((IRenderableIpso)circle).Children[0]).Blend = strokeSlotBlend.Value;
                    }
                    if (useGradientFill && isFilled)
                    {
                        circle.FillColor = new XnaColor((byte)255, (byte)255, (byte)255, (byte)255);
                        circle.UseGradient = true;
                        circle.GradientX1 = 0;
                        circle.GradientY1 = 0;
                        circle.GradientX2 = 1;
                        circle.GradientY2 = 0;
                        circle.Red2 = 255;
                        circle.Green2 = 255;
                        circle.Blue2 = 255;
                        circle.Alpha2 = 128;
                    }
                    renderTarget.AddChild(circle);
                }
                else
                {
                    RectangleRuntime rectangle = new();
                    rectangle.X = x;
                    rectangle.Y = y;
                    rectangle.Width = ShapeSize;
                    rectangle.Height = ShapeSize;
                    rectangle.CornerRadius = column == 1 ? 10 : 0;
                    rectangle.IsFilled = isFilled;
                    rectangle.FillColor = fill;
                    rectangle.StrokeWidth = 6;
                    rectangle.StrokeColor = stroke;
                    rectangle.Blend = blend;
                    if (useGradientFill && isFilled)
                    {
                        rectangle.FillColor = new XnaColor((byte)255, (byte)255, (byte)255, (byte)255);
                        rectangle.UseGradient = true;
                        rectangle.GradientX1 = 0;
                        rectangle.GradientY1 = 0;
                        rectangle.GradientX2 = 1;
                        rectangle.GradientY2 = 0;
                        rectangle.Red2 = 255;
                        rectangle.Green2 = 255;
                        rectangle.Blue2 = 255;
                        rectangle.Alpha2 = 128;
                    }
                    renderTarget.AddChild(rectangle);
                }
            }
        }
        root.AddChild(renderTarget);

        root.AddToManagers(managers, null);
        root.UpdateLayout();

        using RenderTarget2D capture = new(gd, Width, Height, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(XnaColor.Black);
            renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        XnaColor[] pixels = new XnaColor[Width * Height];
        capture.GetData(pixels);

        root.RemoveFromManagers();
        return pixels;
    }
}
