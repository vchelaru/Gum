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
/// An animation frame's RelativeX/RelativeY and keyframe color glides reach the drawn MonoGame
/// nine-slice, as they do for a sprite (#6035).
/// </summary>
public class NineSliceAnimationFrameTests : BaseTestClass
{
    // A 3x3 grid of distinct colors. With a 1 texel border drawn at BorderScale 10, each texel
    // fills one 10x10 cell of a 30x30 nine-slice.
    private static Color TexelColor(int x, int y) => new((byte)(40 + x * 80), (byte)(40 + y * 80), (byte)0, (byte)255);

    private static Texture2D CreateGridTexture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, 3, 3);
        Color[] data = new Color[9];
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                data[y * 3 + x] = TexelColor(x, y);
            }
        }
        texture.SetData(data);
        return texture;
    }

    private static AnimationChainList ChainOf(params AnimationFrame[] frames)
    {
        AnimationChain chain = new() { Name = "c" };
        foreach (AnimationFrame frame in frames)
        {
            chain.Add(frame);
        }
        AnimationChainList chains = new();
        chains.Add(chain);
        return chains;
    }

    private static Color[] RenderFrameSamples(GraphicsDevice graphicsDevice, AnimationFrame frame, float rotation, float x, float y,
        params (int X, int Y)[] samples)
    {
        SystemManagers managers = SystemManagers.Default;

        NineSliceRuntime nineSlice = new();
        nineSlice.X = x;
        nineSlice.Y = y;
        nineSlice.WidthUnits = DimensionUnitType.Absolute;
        nineSlice.HeightUnits = DimensionUnitType.Absolute;
        nineSlice.Width = 30;
        nineSlice.Height = 30;
        nineSlice.BorderScale = 10;
        nineSlice.Rotation = rotation;
        nineSlice.AnimationChains = ChainOf(frame);
        nineSlice.CurrentChainName = "c";

        ContainerRuntime root = new();
        root.AddChild(nineSlice);
        root.AddToManagers(managers, null);
        root.UpdateLayout();

        try
        {
            return RenderAndSample(graphicsDevice, managers.Renderer, managers, samples);
        }
        finally
        {
            root.RemoveFromManagers();
        }
    }

    private static void ShouldBe(Color actual, Color expected)
    {
        (actual.R, actual.G, actual.B).ShouldBe((expected.R, expected.G, expected.B));
    }

    [Fact]
    public void Render_FrameOffset_ShiftsNineSliceRightAndUp()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateGridTexture(game.GraphicsDevice);

        // Un-offset the nine-slice would cover x 10..40, y 10..40; the offset moves it to
        // x 20..50, y 5..35.
        Color[] pixels = RenderFrameSamples(game.GraphicsDevice, new AnimationFrame
        {
            FrameLength = 1f,
            Texture = texture,
            RightCoordinate = 1f,
            BottomCoordinate = 1f,
            RelativeX = 10,
            RelativeY = 5,
        }, rotation: 0, x: 10, y: 10, (25, 10), (15, 15), (45, 38), (45, 30));

        ShouldBe(pixels[0], TexelColor(0, 0));
        ShouldBe(pixels[1], Color.Black);
        ShouldBe(pixels[2], Color.Black);
        ShouldBe(pixels[3], TexelColor(2, 2));
    }

    [Fact]
    public void Render_FrameOffsetOnRotatedNineSlice_ShiftsAlongItsOwnAxes()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateGridTexture(game.GraphicsDevice);

        // Un-offset, a 90 degree nine-slice covers x 30..60, y 20..50; its own +x points up, so
        // the offset moves it to y 10..40.
        Color[] pixels = RenderFrameSamples(game.GraphicsDevice, new AnimationFrame
        {
            FrameLength = 1f,
            Texture = texture,
            RightCoordinate = 1f,
            BottomCoordinate = 1f,
            RelativeX = 10,
        }, rotation: 90, x: 30, y: 50, (35, 35), (35, 45));

        ShouldBe(pixels[0], TexelColor(0, 0));
        ShouldBe(pixels[1], Color.Black);
    }

    [Fact]
    public void Render_NoOffset_DrawsNineSliceInPlace()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateGridTexture(game.GraphicsDevice);

        Color[] pixels = RenderFrameSamples(game.GraphicsDevice, new AnimationFrame
        {
            FrameLength = 1f,
            Texture = texture,
            RightCoordinate = 1f,
            BottomCoordinate = 1f,
        }, rotation: 0, x: 10, y: 10, (15, 15), (35, 35), (5, 5));

        ShouldBe(pixels[0], TexelColor(0, 0));
        ShouldBe(pixels[1], TexelColor(2, 2));
        ShouldBe(pixels[2], Color.Black);
    }

    private static NineSlice CreateAnimated(Texture2D texture, params AnimationFrame[] frames)
    {
        foreach (AnimationFrame frame in frames)
        {
            frame.Texture = texture;
            frame.RightCoordinate = 1f;
            frame.BottomCoordinate = 1f;
        }
        NineSlice nineSlice = new();
        nineSlice.AnimationChains = ChainOf(frames);
        nineSlice.CurrentChainName = "c";
        nineSlice.Animate = true;
        return nineSlice;
    }

    [Fact]
    public void AnimateSelf_AlphaGlide_ChangesAlphaMidFrame()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateGridTexture(game.GraphicsDevice);
        NineSlice sut = CreateAnimated(texture,
            new AnimationFrame { FrameLength = 1, Alpha = 0, ColorInterpolation = "Linear" },
            new AnimationFrame { FrameLength = 1, Alpha = 200 });

        sut.AnimateSelf(0.5);

        sut.Alpha.ShouldBe(100);
    }

    [Fact]
    public void Clone_AlphaGlide_ChangesCloneAlphaMidFrame()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateGridTexture(game.GraphicsDevice);
        NineSlice original = CreateAnimated(texture,
            new AnimationFrame { FrameLength = 1, Alpha = 0, ColorInterpolation = "Linear" },
            new AnimationFrame { FrameLength = 1, Alpha = 200 });
        NineSlice clone = original.Clone();
        clone.Animate = true;

        clone.AnimateSelf(0.5);

        clone.Alpha.ShouldBe(100);
    }

    [Fact]
    public void AnimateSelf_SkippedColorFrame_DoesNotApplyItsOwnColor()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateGridTexture(game.GraphicsDevice);
        NineSlice sut = CreateAnimated(texture,
            new AnimationFrame { FrameLength = 1, Alpha = 0, ColorInterpolation = "Linear" },
            new AnimationFrame { FrameLength = 1, Alpha = 255, SkipColorKeyframe = true },
            new AnimationFrame { FrameLength = 1, Alpha = 200 });

        // Halfway through the glide that spans the skipped frame; its own alpha of 255 is ignored.
        sut.AnimateSelf(1.0);

        sut.Alpha.ShouldBe(100);
    }

    [Fact]
    public void AnimateSelf_RedGlideWithMultiplyOperation_AppliesInterpolatedRed()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateGridTexture(game.GraphicsDevice);
        NineSlice sut = CreateAnimated(texture,
            new AnimationFrame
            {
                FrameLength = 1,
                Red = 0,
                ColorOperation = AnimationFrameColorOperation.Multiply,
                ColorInterpolation = "Linear",
            },
            new AnimationFrame { FrameLength = 1, Red = 200 });

        sut.AnimateSelf(0.5);

        sut.Red.ShouldBe(100);
    }

    private static Color[] RenderAndSample(GraphicsDevice gd, Renderer renderer, SystemManagers managers,
        (int X, int Y)[] samples)
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
        return samples.Select(sample => data[(sample.Y * w) + sample.X]).ToArray();
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
