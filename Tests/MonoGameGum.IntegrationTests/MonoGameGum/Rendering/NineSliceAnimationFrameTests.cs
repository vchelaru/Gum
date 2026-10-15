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
        params (int X, int Y)[] samples) =>
        RenderFrameSamples(graphicsDevice, frame, rotation, x, y, 30, 30, borderScale: 10, isTiling: false, samples);

    private static Color[] RenderFrameSamples(GraphicsDevice graphicsDevice, AnimationFrame frame, float rotation, float x, float y,
        float width, float height, float borderScale, bool isTiling, params (int X, int Y)[] samples)
    {
        SystemManagers managers = SystemManagers.Default;

        NineSliceRuntime nineSlice = new();
        nineSlice.X = x;
        nineSlice.Y = y;
        nineSlice.WidthUnits = DimensionUnitType.Absolute;
        nineSlice.HeightUnits = DimensionUnitType.Absolute;
        nineSlice.Width = width;
        nineSlice.Height = height;
        nineSlice.BorderScale = borderScale;
        nineSlice.IsTilingMiddleSections = isTiling;
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

    private static AnimationFrame FlippedFrame(Texture2D texture, bool flipHorizontal = false, bool flipVertical = false,
        bool flipDiagonal = false) => new()
    {
        FrameLength = 1f,
        Texture = texture,
        RightCoordinate = 1f,
        BottomCoordinate = 1f,
        FlipHorizontal = flipHorizontal,
        FlipVertical = flipVertical,
        FlipDiagonal = flipDiagonal,
    };

    // The center of the cell in column, row of the unrotated 30x30 nine-slice at (10, 10).
    private static (int X, int Y) Cell(int column, int row) => (10 + column * 10 + 5, 10 + row * 10 + 5);

    [Fact]
    public void Render_FlipHorizontal_MirrorsTheNineSliceInsideItsOwnBounds()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateGridTexture(game.GraphicsDevice);

        Color[] pixels = RenderFrameSamples(game.GraphicsDevice, FlippedFrame(texture, flipHorizontal: true),
            rotation: 0, x: 10, y: 10, Cell(0, 0), Cell(2, 0), Cell(0, 2), Cell(1, 1), (45, 25));

        ShouldBe(pixels[0], TexelColor(2, 0));
        ShouldBe(pixels[1], TexelColor(0, 0));
        ShouldBe(pixels[2], TexelColor(2, 2));
        ShouldBe(pixels[3], TexelColor(1, 1));
        ShouldBe(pixels[4], Color.Black);
    }

    [Fact]
    public void Render_FlipVertical_MirrorsTheNineSliceInsideItsOwnBounds()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateGridTexture(game.GraphicsDevice);

        Color[] pixels = RenderFrameSamples(game.GraphicsDevice, FlippedFrame(texture, flipVertical: true),
            rotation: 0, x: 10, y: 10, Cell(0, 0), Cell(2, 0), Cell(0, 2), (25, 45));

        ShouldBe(pixels[0], TexelColor(0, 2));
        ShouldBe(pixels[1], TexelColor(2, 2));
        ShouldBe(pixels[2], TexelColor(0, 0));
        ShouldBe(pixels[3], Color.Black);
    }

    [Fact]
    public void Render_FlipDiagonal_TransposesTheNineSlice()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateGridTexture(game.GraphicsDevice);

        Color[] pixels = RenderFrameSamples(game.GraphicsDevice, FlippedFrame(texture, flipDiagonal: true),
            rotation: 0, x: 10, y: 10, Cell(0, 0), Cell(2, 0), Cell(0, 2), Cell(2, 2), Cell(1, 0));

        // A transpose fixes the top-left and bottom-right and swaps the other corners.
        ShouldBe(pixels[0], TexelColor(0, 0));
        ShouldBe(pixels[1], TexelColor(0, 2));
        ShouldBe(pixels[2], TexelColor(2, 0));
        ShouldBe(pixels[3], TexelColor(2, 2));
        ShouldBe(pixels[4], TexelColor(0, 1));
    }

    [Fact]
    public void Render_FlipDiagonalWithFlipHorizontal_FlipsBeforeTransposing()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateGridTexture(game.GraphicsDevice);

        Color[] pixels = RenderFrameSamples(game.GraphicsDevice, FlippedFrame(texture, flipHorizontal: true, flipDiagonal: true),
            rotation: 0, x: 10, y: 10, Cell(0, 0), Cell(1, 0), Cell(0, 1), Cell(2, 2));

        // Screen cell (column, row) shows the flipped nine-slice's cell (row, column), which is
        // texel (2 - row, column).
        ShouldBe(pixels[0], TexelColor(2, 0));
        ShouldBe(pixels[1], TexelColor(2, 1));
        ShouldBe(pixels[2], TexelColor(1, 0));
        ShouldBe(pixels[3], TexelColor(0, 2));
    }

    [Fact]
    public void Render_FlipDiagonalWithFlipVertical_FlipsBeforeTransposing()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateGridTexture(game.GraphicsDevice);

        Color[] pixels = RenderFrameSamples(game.GraphicsDevice, FlippedFrame(texture, flipVertical: true, flipDiagonal: true),
            rotation: 0, x: 10, y: 10, Cell(0, 0), Cell(1, 0), Cell(0, 1), Cell(2, 2));

        // Screen cell (column, row) shows the flipped nine-slice's cell (row, column), which is
        // texel (row, 2 - column).
        ShouldBe(pixels[0], TexelColor(0, 2));
        ShouldBe(pixels[1], TexelColor(0, 1));
        ShouldBe(pixels[2], TexelColor(1, 2));
        ShouldBe(pixels[3], TexelColor(2, 0));
    }

    [Fact]
    public void Render_FlipDiagonalOnRotatedNineSlice_TransposesInItsOwnAxes()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateGridTexture(game.GraphicsDevice);

        // Rotated 90 degrees counter-clockwise about its top-left it covers x 30..60, y 20..50: its
        // own +x points up and its own +y points right, so own cell (column, row) is centered at
        // screen (35 + 10 * row, 45 - 10 * column). Transposed, it shows texel (row, column).
        Color[] pixels = RenderFrameSamples(game.GraphicsDevice, FlippedFrame(texture, flipDiagonal: true),
            rotation: 90, x: 30, y: 50, (35, 45), (35, 25), (55, 45));

        ShouldBe(pixels[0], TexelColor(0, 0));
        ShouldBe(pixels[1], TexelColor(0, 2));
        ShouldBe(pixels[2], TexelColor(2, 0));
    }

    [Fact]
    public void Render_FlipHorizontalOnRotatedNineSlice_MirrorsInItsOwnAxes()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateGridTexture(game.GraphicsDevice);

        // Own cell (column, row) is centered at screen (35 + 10 * row, 45 - 10 * column), so
        // mirroring swaps own columns 0 and 2.
        Color[] pixels = RenderFrameSamples(game.GraphicsDevice, FlippedFrame(texture, flipHorizontal: true),
            rotation: 90, x: 30, y: 50, (35, 45), (35, 25));

        ShouldBe(pixels[0], TexelColor(2, 0));
        ShouldBe(pixels[1], TexelColor(0, 0));
    }

    [Fact]
    public void Render_FlipDiagonalOnNonSquareNineSlice_StretchesTheTransposedImageToTheFootprint()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateGridTexture(game.GraphicsDevice);

        // A 60x30 nine-slice has columns 10, 40 and 10 wide and three 10 tall rows. Transposed
        // into the same 60x30 footprint, its columns become three bands 5, 20 and 5 tall and its
        // rows become three bands 20 wide.
        Color[] pixels = RenderFrameSamples(game.GraphicsDevice, FlippedFrame(texture, flipDiagonal: true),
            rotation: 0, x: 10, y: 10, width: 60, height: 30, borderScale: 10, isTiling: false,
            (20, 12), (60, 12), (20, 36), (40, 25), (65, 45));

        ShouldBe(pixels[0], TexelColor(0, 0));
        ShouldBe(pixels[1], TexelColor(0, 2));
        ShouldBe(pixels[2], TexelColor(2, 0));
        ShouldBe(pixels[3], TexelColor(1, 1));
        ShouldBe(pixels[4], Color.Black);
    }

    // A 9x3 texture whose columns each have their own red, so a 3 texel wide middle band reads
    // as three distinct stripes.
    private static Texture2D CreateStripTexture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, 9, 3);
        Color[] data = new Color[27];
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                data[y * 9 + x] = new Color((byte)(20 + x * 25), (byte)(20 + y * 80), (byte)0, (byte)255);
            }
        }
        texture.SetData(data);
        return texture;
    }

    [Fact]
    public void Render_FlipHorizontalWithTiledMiddleSections_MirrorsTheTiling()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateStripTexture(game.GraphicsDevice);

        // At BorderScale 10 the 9x3 texture has 30 wide borders and a 3 texel (30 px) wide middle
        // tile, so the 100 wide nine-slice's 40 wide top band is one full tile (texels 3, 4, 5)
        // followed by a 10 wide partial one (texel 3). Mirrored, the partial tile comes first.
        Color[] unflipped = RenderFrameSamples(game.GraphicsDevice, FlippedFrame(texture),
            rotation: 0, x: 10, y: 10, width: 100, height: 30, borderScale: 10, isTiling: true,
            (45, 15), (55, 15), (65, 15), (75, 15));
        Color[] flipped = RenderFrameSamples(game.GraphicsDevice, FlippedFrame(texture, flipHorizontal: true),
            rotation: 0, x: 10, y: 10, width: 100, height: 30, borderScale: 10, isTiling: true,
            (45, 15), (55, 15), (65, 15), (75, 15));

        Color Stripe(int texelX) => new((byte)(20 + texelX * 25), (byte)20, (byte)0, (byte)255);

        ShouldBe(unflipped[0], Stripe(3));
        ShouldBe(unflipped[1], Stripe(4));
        ShouldBe(unflipped[2], Stripe(5));
        ShouldBe(unflipped[3], Stripe(3));
        ShouldBe(flipped[0], Stripe(3));
        ShouldBe(flipped[1], Stripe(5));
        ShouldBe(flipped[2], Stripe(4));
        ShouldBe(flipped[3], Stripe(3));
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
