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
/// Regression for the ColorTextureAlpha state leak (surfaced alongside issue #3486). MonoGame's
/// ColorTextureAlpha technique is a Fog hack that mutates the shared BasicEffect. In Deferred
/// SpriteBatch mode the effect state is applied at the batch's End, so mutating fog for an incoming
/// ColorTextureAlpha sprite BEFORE the previous (Modulate) batch was flushed retroactively fogged
/// every sprite already queued — the whole frame drawn above the ColorTextureAlpha sprite came out
/// wrong. The fix flushes the pending batch before any shared-effect mutation
/// (<c>SpriteBatchStack.FlushIfBegan</c>, called at the top of <c>SpriteRenderer.BeginSpriteBatch</c>).
/// </summary>
public class SpriteColorOperationLeakTests : BaseTestClass
{
    [Fact]
    public void Draw_ModulateSpriteAboveColorTextureAlphaSprite_DoesNotFogTheModulateSprite()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        // Pure green (Color.Green is HTML green = 0,128,0 — use full-intensity so the assertion is
        // unambiguous about "the sprite kept its own color").
        Texture2D green = new(gd, 1, 1);
        green.SetData(new[] { new Color((byte)0, (byte)255, (byte)0, (byte)255) });

        // Modulate sprite (green texture, white tint => green) at the top; it is queued first.
        SpriteRuntime modulate = MakeSprite(green, x: 0, y: 0, RenderingLibrary.Graphics.ColorOperation.Modulate);

        // ColorTextureAlpha sprite below it. Drawing this one flips the shared BasicEffect's fog;
        // before the fix that flushed the still-pending Modulate batch fogged.
        SpriteRuntime colorTextureAlpha = MakeSprite(green, x: 0, y: 40,
            RenderingLibrary.Graphics.ColorOperation.ColorTextureAlpha);

        ContainerRuntime root = new();
        root.X = 0;
        root.Y = 0;
        root.AddChild(modulate);
        root.AddChild(colorTextureAlpha);

        root.AddToManagers(managers, null);
        root.UpdateLayout();

        try
        {
            Color modulatePixel = RenderAndSample(gd, renderer, managers, sampleX: 16, sampleY: 16);

            // The Modulate sprite must still read green — its own texture*tint result — not the
            // leaked fog color the ColorTextureAlpha sprite's Fog toggle produced.
            modulatePixel.G.ShouldBeGreaterThan((byte)200);
            modulatePixel.R.ShouldBeLessThan((byte)80);
            modulatePixel.B.ShouldBeLessThan((byte)80);
        }
        finally
        {
            green.Dispose();
        }
    }

    private static SpriteRuntime MakeSprite(Texture2D texture, int x, int y,
        RenderingLibrary.Graphics.ColorOperation colorOperation)
    {
        SpriteRuntime sprite = new();
        sprite.WidthUnits = DimensionUnitType.Absolute;
        sprite.HeightUnits = DimensionUnitType.Absolute;
        sprite.Width = 32;
        sprite.Height = 32;
        sprite.X = x;
        sprite.Y = y;
        sprite.Texture = texture;
        sprite.Color = Color.White;
        ((RenderingLibrary.Graphics.Sprite)sprite.RenderableComponent).ColorOperation = colorOperation;
        return sprite;
    }

    // Renders the managers tree into an off-screen capture target (twice, so first-frame one-time
    // setup doesn't skew the pixels) and samples one pixel. Mirrors RenderTargetEffectTests.
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
            GumService.Initialize(this, Gum.Forms.DefaultVisualsVersion.V2);
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
