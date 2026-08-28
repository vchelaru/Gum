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
/// Issue #4509: Apos.Shapes' <c>DrawSvg</c> takes a scalar em size measured off the viewBox's
/// height, so a Width that disagrees with the file's aspect ratio used to be ignored — a 2:1
/// drawing in a square box rendered at full 2:1 width and overran its siblings, where SkiaGum's
/// <c>VectorSprite</c> squashes it to fill the box. <c>Svg.Render</c> now re-opens the ShapeBatch
/// with a stretching view matrix when the mismatch is real.
///
/// The document is full-bleed (blue left half, red right half of a 2:1 viewBox) so the assertions
/// read the drawing's own halves rather than depending on where a partial drawing anchors.
/// </summary>
public class SvgNonUniformScaleRenderTests : BaseTestClass
{
    private const string TwoByOneHalvesSvg =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 100">
          <rect x="0" y="0" width="100" height="100" fill="#0000ff" />
          <rect x="100" y="0" width="100" height="100" fill="#ff0000" />
        </svg>
        """;

    [Fact]
    public void AbsoluteWidth_NarrowerThanTheFilesAspectRatio_SquashesTheDrawingIntoTheBox()
    {
        XnaColor[] pixels = RenderSquashedSvg(rotation: 0);

        // Squashed, the drawing's blue half occupies x 50-100 and its red half x 100-150, so the
        // far side of the box is red. Unsquashed the red half starts at x 150 and this is blue.
        XnaColor insideBoxFarSide = pixels[(100 * CaptureWidth) + 125];
        insideBoxFarSide.R.ShouldBeGreaterThan((byte)200);
        insideBoxFarSide.B.ShouldBeLessThan((byte)60);

        // And nothing spills past the Width layout stacks siblings against.
        pixels[(100 * CaptureWidth) + 160].ShouldBe(BackgroundColor);
    }

    [Fact]
    public void Rotated_SquashedDrawing_StretchesAlongItsOwnAxisRatherThanShearing()
    {
        // Rotation pivots on the top-left corner, so -90 lays the drawing's long axis down the
        // screen from y 50 and swings its short axis out to x 0-50.
        XnaColor[] pixels = RenderSquashedSvg(rotation: -90);

        // Squashed, the long axis is 100 and its red half runs y 100-150. Unsquashed the axis is
        // 200 long and y 125 is still inside the blue half.
        XnaColor alongAxisFarSide = pixels[(125 * CaptureWidth) + 25];
        alongAxisFarSide.R.ShouldBeGreaterThan((byte)200);
        alongAxisFarSide.B.ShouldBeLessThan((byte)60);

        // Past the squashed length nothing is drawn - the stretch followed the rotated axis
        // rather than shearing the drawing across the screen's own.
        pixels[(200 * CaptureWidth) + 25].ShouldBe(BackgroundColor);
    }

    [Fact]
    public void ZoomedCamera_SquashedDrawing_StillHonorsTheCameraView()
    {
        // The stretch is applied by re-opening the ShapeBatch, so it has to compose onto the view
        // the batch was opened with rather than replace it - otherwise the drawing drops out of the
        // camera while every sibling shape stays in it.
        XnaColor[] pixels = RenderSquashedSvg(rotation: 0, zoom: 2);

        // At 2x the 100x100 box at (50,50) covers 200x200 at (100,100), so its red half runs
        // x 200-300. Ignoring the camera would leave the drawing back at x 50-150.
        XnaColor insideZoomedBox = pixels[(150 * CaptureWidth) + 250];
        insideZoomedBox.R.ShouldBeGreaterThan((byte)200);
        insideZoomedBox.B.ShouldBeLessThan((byte)60);

        pixels[(150 * CaptureWidth) + 75].ShouldBe(BackgroundColor);
    }

    /// <summary>
    /// Draws the 2:1 document in a 100x100 box at (50,50) - a deliberate aspect mismatch - and
    /// returns the captured frame.
    /// </summary>
    private static XnaColor[] RenderSquashedSvg(float rotation, float zoom = 1)
    {
        string svgPath = Path.Combine(Path.GetTempPath(), $"gum-svg-4509-{System.Guid.NewGuid():N}.svg");
        File.WriteAllText(svgPath, TwoByOneHalvesSvg);

        try
        {
            using MinimalGame game = new();
            game.RunOneFrame();

            GraphicsDevice gd = game.GraphicsDevice;
            SystemManagers managers = SystemManagers.Default;
            Renderer renderer = managers.Renderer;
            renderer.Camera.Zoom = zoom;

            SvgRuntime svg = new SvgRuntime();
            svg.SourceFile = svgPath;
            svg.WidthUnits = DimensionUnitType.Absolute;
            svg.HeightUnits = DimensionUnitType.Absolute;
            svg.Width = 100;
            svg.Height = 100;
            svg.X = 50;
            svg.Y = 50;
            svg.Rotation = rotation;
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

    private class MinimalGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        public MinimalGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this)
            {
                // Apos.Shapes uses an SM4 effect the default Reach profile can't load - #4403.
                GraphicsProfile = GraphicsProfile.HiDef,
            };
        }

        protected override void Initialize()
        {
            base.Initialize();
            Gum.GumService.Default.Initialize(this, DefaultVisualsVersion.V3);
            ShapeRenderer.Self.Initialize(GraphicsDevice, Content);
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(BackgroundColor);

        protected override void Dispose(bool disposing)
        {
            if (Gum.GumService.Default.IsInitialized)
            {
                Gum.GumService.Default.Uninitialize();
            }
            LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }
}
