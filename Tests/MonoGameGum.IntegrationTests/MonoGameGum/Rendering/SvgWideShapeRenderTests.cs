using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Issue #6026 (follow-up to #5168): Apos.Shapes before 0.9.4 dropped any single shape wider than about 2.4 times
/// the viewBox height, so a wide logo whose background is one rectangle (Nintendo's 3:1 logo)
/// drew nothing, and it streaked a single shape with more curves across a row than a band holds
/// (the same logo's outlined lettering).
/// </summary>
[Collection(SharedGame.CollectionName)]
public class SvgWideShapeRenderTests : BaseTestClass
{
    [Theory]
    [InlineData(300)]
    [InlineData(600)]
    [InlineData(1200)]
    [InlineData(3000)]
    [InlineData(12000)]
    public void Render_SingleShapeManyTimesWiderThanTall_FillsItsWholeBox(int documentWidth)
    {
        string markup =
            $"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {documentWidth} 100"><path d="M0,0H{documentWidth}V100H0Z" fill="#ff0000" /></svg>""";

        XnaColor[] pixels = RenderSvg(markup, boxWidth: 300, boxHeight: 100);

        // The box is 300 wide and 100 tall at (0,100).
        AssertRed(pixels[(150 * CaptureWidth) + 150], "center");
        AssertRed(pixels[(110 * CaptureWidth) + 5], "left edge");
        AssertRed(pixels[(190 * CaptureWidth) + 294], "right edge");

        // Nothing spills past the box.
        pixels[(50 * CaptureWidth) + 150].ShouldBe(BackgroundColor);
        pixels[(250 * CaptureWidth) + 150].ShouldBe(BackgroundColor);
    }

    [Fact]
    public void Render_SingleShapeWithManyCurvesAcrossEachRow_DrawsEveryHole()
    {
        // One bar with 39 holes cut out of it: about 80 curves cross every row, far more than a
        // band holds, so a whole-shape bake would lose the holes it can't list.
        const int HoleCount = 39;
        const int Pitch = 15;
        const int HoleWidth = 6;
        StringBuilder path = new StringBuilder("M0,0H600V100H0Z");
        for (int i = 0; i < HoleCount; i++)
        {
            int x = 12 + (i * Pitch);
            path.Append($"M{x},20V80H{x + HoleWidth}V20Z");
        }
        string markup =
            $"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 600 100"><path d="{path}" fill="#ff0000" /></svg>""";

        XnaColor[] pixels = RenderSvg(markup, boxWidth: 600, boxHeight: 100);

        // The box is 600 wide and 100 tall at (0,100): one pixel per document unit.
        for (int i = 0; i < HoleCount; i++)
        {
            int holeCenterX = 12 + (i * Pitch) + (HoleWidth / 2);
            int solidCenterX = holeCenterX + (Pitch / 2);
            pixels[(150 * CaptureWidth) + holeCenterX].ShouldBe(BackgroundColor, $"hole {i}");
            AssertRed(pixels[(150 * CaptureWidth) + solidCenterX], $"solid after hole {i}");
        }

        // Above and below the holes the bar is solid all the way across.
        for (int x = 4; x < 596; x += 7)
        {
            AssertRed(pixels[(110 * CaptureWidth) + x], $"top strip at {x}");
            AssertRed(pixels[(190 * CaptureWidth) + x], $"bottom strip at {x}");
        }
    }

    [Fact]
    public void Render_DrawingSquashedFarNarrowerThanItsAspectRatio_StaysInsideItsBox()
    {
        // A 20:1 drawing squeezed into a 100x100 box at (0,100): one axis is stretched far more
        // than the other, and Apos.Shapes before 0.9.4 measured both em axes with one pixel size,
        // so the soft fringe past the edge ran far outside the box.
        const string markup =
            """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 2000 100"><path d="M0,0H2000V100H0Z" fill="#ff0000" /></svg>""";

        XnaColor[] pixels = RenderSvg(markup, boxWidth: 100, boxHeight: 100);

        AssertRed(pixels[(150 * CaptureWidth) + 50], "center");
        AssertRed(pixels[(190 * CaptureWidth) + 95], "inside the right edge");

        // Just past each edge of the box is still the background, not a fringe.
        for (int offset = 3; offset <= 12; offset += 3)
        {
            pixels[(150 * CaptureWidth) + 100 + offset].ShouldBe(BackgroundColor, $"{offset}px right of the box");
            pixels[((100 - offset) * CaptureWidth) + 50].ShouldBe(BackgroundColor, $"{offset}px above the box");
            pixels[((200 + offset) * CaptureWidth) + 50].ShouldBe(BackgroundColor, $"{offset}px below the box");
        }
    }

    private static void AssertRed(XnaColor pixel, string where)
    {
        pixel.R.ShouldBeGreaterThan((byte)200, where);
        pixel.G.ShouldBeLessThan((byte)60, where);
        pixel.B.ShouldBeLessThan((byte)60, where);
    }

    /// <summary>
    /// Draws the markup scaled into a box at (0,100) and returns the captured frame.
    /// </summary>
    private static XnaColor[] RenderSvg(string markup, int boxWidth, int boxHeight)
    {
        string svgPath = Path.Combine(Path.GetTempPath(), $"gum-svg-wide-{System.Guid.NewGuid():N}.svg");
        File.WriteAllText(svgPath, markup);

        try
        {
            using ShapeGameLease game = new ShapeGameLease();

            GraphicsDevice gd = game.GraphicsDevice;
            SystemManagers managers = SystemManagers.Default;
            Renderer renderer = managers.Renderer;

            SvgRuntime svg = new SvgRuntime();
            svg.SourceFile = svgPath;
            svg.WidthUnits = DimensionUnitType.Absolute;
            svg.HeightUnits = DimensionUnitType.Absolute;
            svg.Width = boxWidth;
            svg.Height = boxHeight;
            svg.X = 0;
            svg.Y = 100;
            svg.AddToManagers(managers, null);
            svg.UpdateLayout();

            return CapturePixels(gd, renderer, managers);
        }
        finally
        {
            File.Delete(svgPath);
        }
    }

    private const int CaptureWidth = 600;
    private const int CaptureHeight = 300;
    private static readonly XnaColor BackgroundColor = XnaColor.Black;

    private static XnaColor[] CapturePixels(GraphicsDevice gd, Renderer renderer, SystemManagers managers)
    {
        using RenderTarget2D capture = new(gd, CaptureWidth, CaptureHeight, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(BackgroundColor);
            renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        XnaColor[] pixels = new XnaColor[CaptureWidth * CaptureHeight];
        capture.GetData(pixels);
        return pixels;
    }
}
