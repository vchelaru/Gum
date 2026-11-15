using Gum.GueDeriving;
using Gum.RenderingLibrary;
using Raylib_cs;
using RaylibGum.Tests.Helpers;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Pixel tests that <see cref="ArcRuntime.Blend"/> and <see cref="CircleRuntime.Blend"/> reach
/// raylib's blend state (#7523, #7528). Red shapes are drawn over a blue backdrop: Additive keeps
/// the blue and adds red (magenta); Normal replaces it with red.
/// </summary>
public class ShapeBlendRenderTests : BaseTestClass
{
    private const int CanvasSize = 100;
    private static readonly Color Blue = new Color((byte)0, (byte)0, (byte)255, (byte)255);
    private static readonly Color Red = new Color((byte)255, (byte)0, (byte)0, (byte)255);
    private static readonly Color Magenta = new Color((byte)255, (byte)0, (byte)255, (byte)255);

    // Mid-stroke of a full-sweep arc: top of a 60px box at (20,20) with an 8px stroke.
    private const int ArcProbeX = 50;
    private const int ArcProbeY = 24;

    private static PixelGrid RenderArc(Blend blend)
    {
        return RaylibPixelHarness.Render(CanvasSize, CanvasSize, Blue, container =>
        {
            ArcRuntime arc = new();
            arc.X = 20;
            arc.Y = 20;
            arc.Width = 60;
            arc.Height = 60;
            arc.StartAngle = 0;
            arc.SweepAngle = 360;
            arc.Thickness = 8;
            arc.StrokeColor = Red;
            arc.Blend = blend;
            container.Children.Add(arc);
        });
    }

    [Fact]
    public void Draw_AdditiveArc_AddsStrokeOntoBackdrop()
    {
        PixelGrid pixels = RenderArc(Blend.Additive);

        pixels.ShouldMatchPixel(ArcProbeX, ArcProbeY, Magenta);
    }

    [Fact]
    public void Draw_NormalArc_ReplacesBackdropWithStroke()
    {
        PixelGrid pixels = RenderArc(Blend.Normal);

        pixels.ShouldMatchPixel(ArcProbeX, ArcProbeY, Red);
    }

    [Fact]
    public void Draw_AdditiveCircle_AddsFillOntoBackdrop()
    {
        PixelGrid pixels = RaylibPixelHarness.Render(CanvasSize, CanvasSize, Blue, container =>
        {
            CircleRuntime circle = new();
            circle.X = 20;
            circle.Y = 20;
            circle.Width = 60;
            circle.Height = 60;
            circle.IsFilled = true;
            circle.FillColor = Red;
            circle.Blend = Blend.Additive;
            container.Children.Add(circle);
        });

        pixels.ShouldMatchRegion(45, 45, 10, 10, Magenta);
    }
}
