using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Regression for a latent bug found while implementing #4792 Gap 2's additive-overlay pass, which
/// reuses the ColorTextureAlpha technique. <c>SpriteRenderer.Draw</c> computed the BasicEffect fog
/// color's red/green channels as <c>color.R/255</c>/<c>color.G/255</c> — integer division of a
/// byte by an int, which truncates to 0 for every value except the boundaries 0 and 255 (where the
/// division happens to be exact). Blue used the correct <c>color.B/255f</c>. A pure red/green/blue
/// or white/black tint (0 or 255 per channel, e.g. the existing SpriteColorOperationLeakTests case)
/// never exposed this — only a partial-intensity tint does.
/// </summary>
public class ColorTextureAlphaFogColorScalingTests : BaseTestClass
{
    [Fact]
    public void Draw_ColorTextureAlphaWithPartialIntensityTint_ScalesRedAndGreenChannelsCorrectly()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        // Opaque white texture: ColorTextureAlpha discards the texture's own RGB and uses only its
        // alpha as a mask, so an opaque white pixel isolates the tint color in the output.
        Texture2D white = new(gd, 1, 1);
        white.SetData(new[] { new Color((byte)255, (byte)255, (byte)255, (byte)255) });

        SpriteRuntime sprite = new();
        sprite.WidthUnits = DimensionUnitType.Absolute;
        sprite.HeightUnits = DimensionUnitType.Absolute;
        sprite.Width = 32;
        sprite.Height = 32;
        sprite.Texture = white;
        // Partial-intensity, non-boundary tint - the case the truncated integer division broke.
        sprite.Color = new Color((byte)128, (byte)64, (byte)200, (byte)255);
        ((Sprite)sprite.RenderableComponent).ColorOperation = ColorOperation.ColorTextureAlpha;

        ContainerRuntime root = new();
        root.AddChild(sprite);
        root.AddToManagers(managers, null);
        root.UpdateLayout();

        try
        {
            Color pixel = RenderAndSample(gd, renderer, managers, sampleX: 16, sampleY: 16);

            // Integer division would have truncated R and G to 0; the fix scales them to ~128/~64.
            pixel.R.ShouldBeInRange((byte)115, (byte)140);
            pixel.G.ShouldBeInRange((byte)50, (byte)75);
            pixel.B.ShouldBeInRange((byte)185, (byte)210);
        }
        finally
        {
            white.Dispose();
        }
    }

    // Renders the managers tree into an off-screen capture target (twice, so first-frame one-time
    // setup doesn't skew the pixels) and samples one pixel. Mirrors SpriteColorOperationLeakTests.
    private static Color RenderAndSample(GraphicsDevice gd, Renderer renderer, SystemManagers managers,
        int sampleX, int sampleY)
    {
        const int w = 128;
        const int h = 128;
        using RenderTarget2D capture = new(gd, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0,
            RenderTargetUsage.PreserveContents);

        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(Color.Black);
            renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        Color[] data = new Color[w * h];
        capture.GetData(data);
        return data[(sampleY * w) + sampleX];
    }

    private class MinimalGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        public GumService GumService { get; }

        public MinimalGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
            GumService = new GumService();
        }

        protected override void Initialize()
        {
            base.Initialize();
            GumService.Initialize(this, Gum.Forms.DefaultVisualsVersion.V3);
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(Color.CornflowerBlue);

        protected override void Dispose(bool disposing)
        {
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
            LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }
}
