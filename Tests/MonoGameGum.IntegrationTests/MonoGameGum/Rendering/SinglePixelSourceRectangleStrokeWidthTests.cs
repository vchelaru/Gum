using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// A stroked rectangle drawn through <see cref="Renderer.SinglePixelSourceRectangle"/> (solids sampled
/// from an atlas region) must be <see cref="RectangleRuntime.StrokeWidth"/> screen pixels thick at any
/// camera zoom, the same as when no source rectangle is set.
/// </summary>
public class SinglePixelSourceRectangleStrokeWidthTests : BaseTestClass
{
    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    public void StrokeWidth_ShouldBeScreenPixels_WhenSinglePixelSourceRectangleIsSet(float zoom)
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        // A 3x3 white atlas sampled through its center texel.
        using Texture2D atlas = new(gd, 3, 3, false, SurfaceFormat.Color);
        XnaColor[] white = new XnaColor[9];
        System.Array.Fill(white, XnaColor.White);
        atlas.SetData(white);
        renderer.SinglePixelTexture = atlas;
        renderer.SinglePixelSourceRectangle = new System.Drawing.Rectangle(1, 1, 1, 1);
        renderer.Camera.Zoom = zoom;

        RectangleRuntime rectangle = new();
        rectangle.X = 10;
        rectangle.Y = 10;
        rectangle.Width = 50;
        rectangle.Height = 50;
        rectangle.StrokeWidth = 8;
        rectangle.Color = XnaColor.White;
        rectangle.AddToManagers(managers, null);
        rectangle.UpdateLayout();

        int thickness = MeasureTopEdgeThickness(gd, renderer, managers, 200, 200, column: (int)(35 * zoom));

        rectangle.RemoveFromManagers();

        thickness.ShouldBe(8);
    }

    /// <summary>
    /// Renders the scene and counts the white pixels in <paramref name="column"/>, starting from the
    /// first white pixel found scanning down.
    /// </summary>
    private static int MeasureTopEdgeThickness(GraphicsDevice gd, Renderer renderer, SystemManagers managers,
        int w, int h, int column)
    {
        using RenderTarget2D capture = new(gd, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0,
            RenderTargetUsage.PreserveContents);

        gd.SetRenderTarget(capture);
        gd.Clear(XnaColor.Black);
        renderer.Draw(managers);
        gd.SetRenderTarget(null);

        XnaColor[] data = new XnaColor[w * h];
        capture.GetData(data);

        int y = 0;
        while (y < h && data[(y * w) + column].R < 128) y++;
        int thickness = 0;
        while (y + thickness < h && data[((y + thickness) * w) + column].R >= 128) thickness++;
        return thickness;
    }

    private class MinimalGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        public MinimalGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
        }

        protected override void Initialize()
        {
            base.Initialize();
            Gum.GumService.Default.Initialize(this, Gum.Forms.DefaultVisualsVersion.V3);
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(XnaColor.CornflowerBlue);

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
