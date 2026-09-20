using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.Content.AnimationChain;
using Gum.DataTypes;
using Gum.Graphics.Animation;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// End-to-end pixel checks that an authored <see cref="AnimationFrameColorOperation.Add"/> frame
/// renders as an additive pass (<c>ColorOperation.Add</c> +
/// <see cref="Renderer.DrawAdditiveColorOverlay"/>) instead of being silently dropped.
/// </summary>
public class SpriteAdditiveColorOperationTests : BaseTestClass
{
    [Fact]
    public void Draw_SpriteWithAddColorOperationFrame_SaturatesTextureTowardWhiteTint()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        // Fully-opaque blue texture pixel. Add + white(255) should saturate every drawn pixel to
        // white regardless of the underlying art - the MonsterProjectWeb "un-revealed silhouette"
        // use case from the issue.
        Texture2D blue = new(gd, 1, 1);
        blue.SetData(new[] { new Color((byte)0, (byte)0, (byte)255, (byte)255) });

        SpriteRuntime sprite = new();
        sprite.WidthUnits = DimensionUnitType.Absolute;
        sprite.HeightUnits = DimensionUnitType.Absolute;
        sprite.Width = 32;
        sprite.Height = 32;
        sprite.Texture = blue;

        AnimationChain chain = new() { Name = "AddChain" };
        chain.Add(new AnimationFrame
        {
            FrameLength = 1.0f,
            Texture = blue,
            Red = 255,
            Green = 255,
            Blue = 255,
            ColorOperation = AnimationFrameColorOperation.Add,
        });
        AnimationChainList chainList = new();
        chainList.Add(chain);
        sprite.AnimationChains = chainList;
        sprite.CurrentChainName = "AddChain";

        ContainerRuntime root = new();
        root.AddChild(sprite);
        root.AddToManagers(managers, null);
        root.UpdateLayout();

        try
        {
            Color pixel = RenderAndSample(gd, renderer, managers, sampleX: 16, sampleY: 16);

            pixel.R.ShouldBeGreaterThan((byte)240);
            pixel.G.ShouldBeGreaterThan((byte)240);
            pixel.B.ShouldBeGreaterThan((byte)240);
        }
        finally
        {
            blue.Dispose();
        }
    }

    [Fact]
    public void Draw_SpriteWithAddColorOperationAbove_DoesNotLeakStateToPlainSpriteBelow()
    {
        // Regression guard for the state-leak class SpriteColorOperationLeakTests pins for
        // ColorTextureAlpha/Fog: DrawAdditiveColorOverlay's mid-render BeginSpriteBatch calls must
        // restore the ambient BlendState/ColorOperation, or every renderable drawn afterward in the
        // walk paints with the overlay's additive blend / ColorTextureAlpha technique instead of its
        // own.
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        Texture2D white = new(gd, 1, 1);
        white.SetData(new[] { new Color((byte)255, (byte)255, (byte)255, (byte)255) });
        Texture2D green = new(gd, 1, 1);
        green.SetData(new[] { new Color((byte)0, (byte)255, (byte)0, (byte)255) });

        SpriteRuntime addSprite = new();
        addSprite.WidthUnits = DimensionUnitType.Absolute;
        addSprite.HeightUnits = DimensionUnitType.Absolute;
        addSprite.Width = 32;
        addSprite.Height = 32;
        addSprite.X = 0;
        addSprite.Y = 0;
        addSprite.Texture = white;

        AnimationChain chain = new() { Name = "AddChain" };
        chain.Add(new AnimationFrame
        {
            FrameLength = 1.0f,
            Texture = white,
            Red = 255,
            Green = 255,
            Blue = 255,
            ColorOperation = AnimationFrameColorOperation.Add,
        });
        AnimationChainList chainList = new();
        chainList.Add(chain);
        addSprite.AnimationChains = chainList;
        addSprite.CurrentChainName = "AddChain";

        // Plain Modulate sprite drawn immediately after the Add sprite in the same walk.
        SpriteRuntime plainSprite = new();
        plainSprite.WidthUnits = DimensionUnitType.Absolute;
        plainSprite.HeightUnits = DimensionUnitType.Absolute;
        plainSprite.Width = 32;
        plainSprite.Height = 32;
        plainSprite.X = 0;
        plainSprite.Y = 40;
        plainSprite.Texture = green;
        plainSprite.Color = Color.White;

        ContainerRuntime root = new();
        root.AddChild(addSprite);
        root.AddChild(plainSprite);
        root.AddToManagers(managers, null);
        root.UpdateLayout();

        try
        {
            Color plainPixel = RenderAndSample(gd, renderer, managers, sampleX: 16, sampleY: 56);

            plainPixel.G.ShouldBeGreaterThan((byte)200);
            plainPixel.R.ShouldBeLessThan((byte)80);
            plainPixel.B.ShouldBeLessThan((byte)80);
        }
        finally
        {
            white.Dispose();
            green.Dispose();
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
