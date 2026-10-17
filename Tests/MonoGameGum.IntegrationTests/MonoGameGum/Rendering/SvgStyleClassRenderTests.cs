using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.DataTypes;
using Gum.Forms;
using Gum.GueDeriving;
using MonoGameAndGum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Issue #5168: Illustrator exports color paths through a <c>&lt;style&gt;</c> block and
/// <c>class</c> attributes. Apos.Shapes before 0.8.6 dropped the block, so every such path
/// drew black.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class SvgStyleClassRenderTests : BaseTestClass
{
    private const string ClassStyledSvg =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
          <defs><style>.st0 { fill: #ff0000; } .st1 { fill: #0000ff; }</style></defs>
          <rect class="st0" x="0" y="0" width="50" height="100" />
          <rect class="st1" x="50" y="0" width="50" height="100" />
        </svg>
        """;

    [Fact]
    public void Render_FillsFromStyleBlockClasses_AreApplied()
    {
        XnaColor[] pixels = RenderSvg();

        // Box is 100x100 at (50,50): red left half at x 50-100, blue right half at x 100-150.
        XnaColor left = pixels[(100 * CaptureWidth) + 75];
        left.R.ShouldBeGreaterThan((byte)200);
        left.B.ShouldBeLessThan((byte)60);

        XnaColor right = pixels[(100 * CaptureWidth) + 125];
        right.B.ShouldBeGreaterThan((byte)200);
        right.R.ShouldBeLessThan((byte)60);
    }

    private static XnaColor[] RenderSvg()
    {
        string svgPath = Path.Combine(Path.GetTempPath(), $"gum-svg-5168-{System.Guid.NewGuid():N}.svg");
        File.WriteAllText(svgPath, ClassStyledSvg);

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
            svg.Width = 100;
            svg.Height = 100;
            svg.X = 50;
            svg.Y = 50;
            svg.AddToManagers(managers, null);
            svg.UpdateLayout();

            return CapturePixels(gd, renderer, managers);
        }
        finally
        {
            File.Delete(svgPath);
        }
    }

    private const int CaptureWidth = 300;
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
