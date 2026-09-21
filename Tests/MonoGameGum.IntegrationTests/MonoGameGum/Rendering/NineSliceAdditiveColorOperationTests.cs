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
/// End-to-end pixel check for #4821 gap 4: NineSlice's <c>ApplyAnimationFrame</c> never got the
/// Sprite.cs Add-color treatment - an authored <see cref="AnimationFrameColorOperation.Add"/> frame
/// silently dropped its additive tint even on MonoGame/KNI/FNA. Mirrors
/// <see cref="SpriteAdditiveColorOperationTests"/>.
/// </summary>
public class NineSliceAdditiveColorOperationTests : BaseTestClass
{
    [Fact]
    public void Draw_NineSliceWithAddColorOperationFrame_SaturatesTextureTowardWhiteTint()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        // Fully-opaque blue texture pixel. Add + white(255) should saturate every drawn pixel to
        // white regardless of the underlying art, same as the Sprite case.
        Texture2D blue = new(gd, 1, 1);
        blue.SetData(new[] { new Color((byte)0, (byte)0, (byte)255, (byte)255) });

        NineSliceRuntime nineSlice = new();
        nineSlice.WidthUnits = DimensionUnitType.Absolute;
        nineSlice.HeightUnits = DimensionUnitType.Absolute;
        nineSlice.Width = 32;
        nineSlice.Height = 32;
        nineSlice.Texture = blue;

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
        nineSlice.AnimationChains = chainList;
        nineSlice.CurrentChainName = "AddChain";

        ContainerRuntime root = new();
        root.AddChild(nineSlice);
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
    public void AnimateSelf_ShouldSetColorAndAddOperation_WhenFrameColorOperationIsAdd()
    {
        // Logic-level pin alongside the pixel check above: NineSlice.ApplyAnimationFrame requires a
        // non-null frame texture (throws otherwise), so this needs a real GraphicsDevice - unlike
        // Sprite's equivalent test in MonoGameGum.Tests, which tolerates a null texture.
        using MinimalGame game = new();
        game.RunOneFrame();

        Texture2D texture = new(game.GraphicsDevice, 1, 1);
        texture.SetData(new[] { Color.White });

        NineSlice nineSlice = new() { Red = 11, Green = 22, Blue = 33 };

        AnimationChain chain = new() { Name = "TestChain" };
        chain.Add(new AnimationFrame { FrameLength = 1.0f, Texture = texture, Red = 255, Green = 0, Blue = 0, ColorOperation = AnimationFrameColorOperation.Add });
        AnimationChainList chainList = new();
        chainList.Add(chain);

        try
        {
            nineSlice.AnimationChains = chainList;
            nineSlice.CurrentChainName = "TestChain";

            // The Add frame drives the NineSlice's own Color plus ColorOperation.Add - Color is the
            // single tint source, read differently depending on the operation.
            nineSlice.ColorOperation.ShouldBe(global::RenderingLibrary.Graphics.ColorOperation.Add);
            nineSlice.Red.ShouldBe(255);
            nineSlice.Green.ShouldBe(0);
            nineSlice.Blue.ShouldBe(0);
        }
        finally
        {
            texture.Dispose();
        }
    }

    [Fact]
    public void AnimateSelf_ShouldRestoreModulate_WhenFrameHasNoColorOperation()
    {
        // A later frame with no authored color must not inherit ColorOperation.Add from
        // an earlier Add frame.
        using MinimalGame game = new();
        game.RunOneFrame();

        Texture2D texture = new(game.GraphicsDevice, 1, 1);
        texture.SetData(new[] { Color.White });

        NineSlice nineSlice = new();

        AnimationChain chain = new() { Name = "TestChain" };
        chain.Add(new AnimationFrame { FrameLength = 1.0f, Texture = texture, Red = 255, Green = 255, Blue = 255, ColorOperation = AnimationFrameColorOperation.Add });
        chain.Add(new AnimationFrame { FrameLength = 1.0f, Texture = texture });
        AnimationChainList chainList = new();
        chainList.Add(chain);

        try
        {
            nineSlice.AnimationChains = chainList;
            nineSlice.Animate = true;
            nineSlice.CurrentChainName = "TestChain";
            nineSlice.ColorOperation.ShouldBe(global::RenderingLibrary.Graphics.ColorOperation.Add);

            // 1.5s into chain crosses from frame 0 (ends at 1.0s) into frame 1.
            nineSlice.AnimateSelf(1.5);

            nineSlice.ColorOperation.ShouldBe(global::RenderingLibrary.Graphics.ColorOperation.Modulate);
        }
        finally
        {
            texture.Dispose();
        }
    }

    // Mirrors SpriteAdditiveColorOperationTests.RenderAndSample.
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
