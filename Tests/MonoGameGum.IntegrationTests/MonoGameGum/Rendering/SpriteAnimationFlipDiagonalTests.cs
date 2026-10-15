using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
/// An animation frame's FlipDiagonal transposes the drawn sprite whatever its source rectangle's
/// shape, flipping horizontally and vertically first and stretching to the sprite's footprint, as
/// Skia and raylib do (#6064).
/// </summary>
public class SpriteAnimationFlipDiagonalTests : BaseTestClass
{
    // One letter per texel, the first row of the texture first. R, G, B and W are the only
    // letters used, the rest are placeholders so a wrong transpose cannot match by accident.
    private static Color ColorFor(char letter) => letter switch
    {
        'R' => new Color(255, 0, 0, 255),
        'G' => new Color(0, 255, 0, 255),
        'B' => new Color(0, 0, 255, 255),
        'W' => new Color(255, 255, 255, 255),
        'C' => new Color(0, 255, 255, 255),
        'M' => new Color(255, 0, 255, 255),
        'Y' => new Color(255, 255, 0, 255),
        'K' => new Color(128, 128, 128, 255),
        _ => throw new System.ArgumentOutOfRangeException(nameof(letter)),
    };

    private static Texture2D CreateTexture(GraphicsDevice graphicsDevice, int width, int height, string texels)
    {
        Texture2D texture = new(graphicsDevice, width, height);
        Color[] data = texels.Select(ColorFor).ToArray();
        texture.SetData(data);
        return texture;
    }

    // A 10 pixel cell at column, row of a sprite whose top-left is at (x, y) and that is not rotated.
    private static (int X, int Y) CellCenter(int x, int y, int column, int row) => (x + 10 * column + 5, y + 10 * row + 5);

    private static Color[] Render(GraphicsDevice graphicsDevice, Texture2D texture, bool flipDiagonal,
        bool flipHorizontal, bool flipVertical, float width, float height, float rotation, float x, float y,
        params (int X, int Y)[] samples)
    {
        SystemManagers managers = SystemManagers.Default;

        AnimationChain chain = new() { Name = "c" };
        chain.Add(new AnimationFrame
        {
            FrameLength = 1f,
            Texture = texture,
            RightCoordinate = 1f,
            BottomCoordinate = 1f,
            FlipDiagonal = flipDiagonal,
            FlipHorizontal = flipHorizontal,
            FlipVertical = flipVertical,
        });
        AnimationChainList chains = new();
        chains.Add(chain);

        SpriteRuntime sprite = new();
        sprite.X = x;
        sprite.Y = y;
        sprite.WidthUnits = DimensionUnitType.Absolute;
        sprite.HeightUnits = DimensionUnitType.Absolute;
        sprite.Width = width;
        sprite.Height = height;
        sprite.Rotation = rotation;
        sprite.AnimationChains = chains;
        sprite.CurrentChainName = "c";

        ContainerRuntime root = new();
        root.AddChild(sprite);
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

    private static void ShouldBeLetter(Color actual, char expected)
    {
        Color expectedColor = ColorFor(expected);
        (actual.R, actual.G, actual.B).ShouldBe((expectedColor.R, expectedColor.G, expectedColor.B));
    }

    private static void ShouldBeLetters(Color[] actual, string expected)
    {
        actual.Length.ShouldBe(expected.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            ShouldBeLetter(actual[i], expected[i]);
        }
    }

    [Fact]
    public void Render_FlipDiagonalOnSquareFrame_SwapsTopRightAndBottomLeft()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateTexture(game.GraphicsDevice, 2, 2, "RGBW");

        // The sprite covers x 10..30, y 10..30. A transpose fixes the top-left and bottom-right
        // corners and swaps the top-right and bottom-left ones.
        Color[] pixels = Render(game.GraphicsDevice, texture, flipDiagonal: true, flipHorizontal: false, flipVertical: false,
            width: 20, height: 20, rotation: 0, x: 10, y: 10,
            CellCenter(10, 10, 0, 0), CellCenter(10, 10, 1, 0), CellCenter(10, 10, 0, 1), CellCenter(10, 10, 1, 1));

        ShouldBeLetters(pixels, "RBGW");
    }

    [Fact]
    public void Render_FlipDiagonalOnSquareFrameInNonSquareSprite_StaysInsideItsOwnBounds()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateTexture(game.GraphicsDevice, 2, 2, "RGBW");

        // The sprite covers x 10..40, y 10..20 and its footprint does not change.
        Color[] pixels = Render(game.GraphicsDevice, texture, flipDiagonal: true, flipHorizontal: false, flipVertical: false,
            width: 30, height: 10, rotation: 0, x: 10, y: 10,
            (15, 12), (35, 12), (15, 18), (35, 18), (25, 25), (25, 5));

        ShouldBeLetter(pixels[0], 'R');
        ShouldBeLetter(pixels[1], 'B');
        ShouldBeLetter(pixels[2], 'G');
        ShouldBeLetter(pixels[3], 'W');
        pixels[4].ShouldBe(Color.Black);
        pixels[5].ShouldBe(Color.Black);
    }

    // The 4x2 texture reads RGBW over CMYK. Transposed, it is 2 wide and 4 tall with the first
    // column of the texture as the first row, so a 20x40 sprite shows a 10 pixel cell per texel.
    // Flipping horizontally or vertically happens before the transpose.
    [Theory]
    [InlineData(false, false, "RC" + "GM" + "BY" + "WK")]
    [InlineData(true, false, "WK" + "BY" + "GM" + "RC")]
    [InlineData(false, true, "CR" + "MG" + "YB" + "KW")]
    [InlineData(true, true, "KW" + "YB" + "MG" + "CR")]
    public void Render_FlipDiagonalOnNonSquareFrame_FlipsThenTransposesAndStretches(bool flipHorizontal, bool flipVertical, string expectedRows)
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateTexture(game.GraphicsDevice, 4, 2, "RGBW" + "CMYK");

        // The sprite covers x 10..30, y 10..50.
        (int X, int Y)[] samples = Enumerable.Range(0, 8)
            .Select(i => CellCenter(10, 10, column: i % 2, row: i / 2))
            .ToArray();

        Color[] pixels = Render(game.GraphicsDevice, texture, flipDiagonal: true, flipHorizontal, flipVertical,
            width: 20, height: 40, rotation: 0, x: 10, y: 10, samples);

        ShouldBeLetters(pixels, expectedRows);
    }

    [Fact]
    public void Render_FlipDiagonalOnNonSquareFrame_DrawsNothingOutsideTheSpritesFootprint()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateTexture(game.GraphicsDevice, 4, 2, "RGBW" + "CMYK");

        Color[] pixels = Render(game.GraphicsDevice, texture, flipDiagonal: true, flipHorizontal: false, flipVertical: false,
            width: 20, height: 40, rotation: 0, x: 10, y: 10,
            (5, 30), (35, 30), (20, 5), (20, 55));

        pixels.ShouldAllBe(pixel => pixel == Color.Black);
    }

    [Fact]
    public void Render_FlipDiagonalOnNonSquareFrameInRotatedSprite_TransposesInTheSpritesOwnAxes()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateTexture(game.GraphicsDevice, 4, 2, "RGBW" + "CMYK");

        // Rotated 90 degrees counter-clockwise about its top-left at (30, 60), the 20x40 sprite
        // covers x 30..70, y 40..60: its own right edge points up and its own bottom edge points
        // right, so a texel of the frame at (x, y) lands at screen (35 + 10 * x, 55 - 10 * y):
        // transposing then turning a quarter turn leaves the frame flipped vertically.
        (int X, int Y)[] samples = Enumerable.Range(0, 8)
            .Select(i => (35 + 10 * (i % 4), 55 - 10 * (i / 4)))
            .ToArray();

        Color[] pixels = Render(game.GraphicsDevice, texture, flipDiagonal: true, flipHorizontal: false, flipVertical: false,
            width: 20, height: 40, rotation: 90, x: 30, y: 60, samples);

        ShouldBeLetters(pixels, "RGBW" + "CMYK");
    }

    [Fact]
    public void Render_FlipDiagonalOffOnNonSquareFrame_DrawsFrameUnflipped()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateTexture(game.GraphicsDevice, 4, 2, "RGBW" + "CMYK");

        // The sprite covers x 10..50, y 10..30.
        (int X, int Y)[] samples = Enumerable.Range(0, 8)
            .Select(i => CellCenter(10, 10, column: i % 4, row: i / 4))
            .ToArray();

        Color[] pixels = Render(game.GraphicsDevice, texture, flipDiagonal: false, flipHorizontal: false, flipVertical: false,
            width: 40, height: 20, rotation: 0, x: 10, y: 10, samples);

        ShouldBeLetters(pixels, "RGBW" + "CMYK");
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
        public Gum.GumService GumService { get; }

        public MinimalGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
            GumService = new Gum.GumService();
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
