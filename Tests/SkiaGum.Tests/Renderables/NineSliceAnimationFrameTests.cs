using Gum;
using Gum.Content.AnimationChain;
using Gum.GueDeriving;
using Gum.Graphics.Animation;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// An animation frame's RelativeX/RelativeY, keyframe color glides and flips reach the drawn Skia
/// nine-slice, as they do for a Skia sprite (#6035, #6058).
/// </summary>
public class NineSliceAnimationFrameTests
{
    public NineSliceAnimationFrameTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    // A 3x3 grid of distinct colors. With a 1 texel border drawn at BorderScale 10, each texel
    // fills one 10x10 cell of a 30x30 nine-slice.
    private static SKColor TexelColor(int x, int y) => new((byte)(40 + x * 80), (byte)(40 + y * 80), 0);

    private static SKBitmap CreateGridTexture()
    {
        SKBitmap texture = new(3, 3);
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                texture.SetPixel(x, y, TexelColor(x, y));
            }
        }
        return texture;
    }

    private static NineSliceRuntime CreateNineSlice(AnimationFrame frame)
    {
        AnimationChain chain = new() { Name = "c" };
        chain.Add(frame);

        NineSliceRuntime nineSlice = new()
        {
            X = 10,
            Y = 10,
            Width = 30,
            Height = 30,
            WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            BorderScale = 10,
        };
        nineSlice.AnimationChains = new AnimationChainList { chain };
        nineSlice.CurrentChainName = "c";
        nineSlice.Animate = true;
        nineSlice.AnimateSelf(0.0);
        return nineSlice;
    }

    private static SKBitmap Render(AnimationFrame frame, float rotation = 0, float x = 10, float y = 10)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(80, 80));
        GumService.Default.Initialize(surface.Canvas, 80, 80);

        NineSliceRuntime nineSlice = CreateNineSlice(frame);
        nineSlice.X = x;
        nineSlice.Y = y;
        nineSlice.Rotation = rotation;
        GumService.Default.Root.Children.Add(nineSlice);

        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    private static SKBitmap RenderGrid(bool flipHorizontal = false, bool flipVertical = false, bool flipDiagonal = false,
        float rotation = 0, float x = 10, float y = 10)
    {
        using SKBitmap texture = CreateGridTexture();
        return Render(new AnimationFrame
        {
            FrameLength = 1f,
            Texture = texture,
            RightCoordinate = 1f,
            BottomCoordinate = 1f,
            FlipHorizontal = flipHorizontal,
            FlipVertical = flipVertical,
            FlipDiagonal = flipDiagonal,
        }, rotation, x, y);
    }

    // The color at the center of the cell in column, row of the unrotated nine-slice at (10, 10).
    private static SKColor CellColor(SKBitmap pixels, int column, int row) =>
        pixels.GetPixel(10 + column * 10 + 5, 10 + row * 10 + 5);

    [Fact]
    public void Render_NoFlip_DrawsFrameUnflipped()
    {
        using SKBitmap pixels = RenderGrid();

        CellColor(pixels, 0, 0).ShouldBe(TexelColor(0, 0));
        CellColor(pixels, 2, 0).ShouldBe(TexelColor(2, 0));
        CellColor(pixels, 0, 2).ShouldBe(TexelColor(0, 2));
        CellColor(pixels, 1, 1).ShouldBe(TexelColor(1, 1));
    }

    [Fact]
    public void Render_FlipHorizontal_MirrorsTheNineSliceInsideItsOwnBounds()
    {
        using SKBitmap pixels = RenderGrid(flipHorizontal: true);

        CellColor(pixels, 0, 0).ShouldBe(TexelColor(2, 0));
        CellColor(pixels, 2, 0).ShouldBe(TexelColor(0, 0));
        CellColor(pixels, 0, 2).ShouldBe(TexelColor(2, 2));
        CellColor(pixels, 1, 1).ShouldBe(TexelColor(1, 1));
        pixels.GetPixel(45, 25).Alpha.ShouldBe((byte)0);
    }

    [Fact]
    public void Render_FlipVertical_MirrorsTheNineSliceInsideItsOwnBounds()
    {
        using SKBitmap pixels = RenderGrid(flipVertical: true);

        CellColor(pixels, 0, 0).ShouldBe(TexelColor(0, 2));
        CellColor(pixels, 2, 0).ShouldBe(TexelColor(2, 2));
        CellColor(pixels, 0, 2).ShouldBe(TexelColor(0, 0));
        pixels.GetPixel(25, 45).Alpha.ShouldBe((byte)0);
    }

    [Fact]
    public void Render_FlipDiagonal_TransposesTheNineSlice()
    {
        using SKBitmap pixels = RenderGrid(flipDiagonal: true);

        // A transpose fixes the top-left and bottom-right and swaps the other corners.
        CellColor(pixels, 0, 0).ShouldBe(TexelColor(0, 0));
        CellColor(pixels, 2, 0).ShouldBe(TexelColor(0, 2));
        CellColor(pixels, 0, 2).ShouldBe(TexelColor(2, 0));
        CellColor(pixels, 2, 2).ShouldBe(TexelColor(2, 2));
        CellColor(pixels, 1, 0).ShouldBe(TexelColor(0, 1));
    }

    [Fact]
    public void Render_FlipDiagonalWithFlipHorizontal_FlipsBeforeTransposing()
    {
        using SKBitmap pixels = RenderGrid(flipDiagonal: true, flipHorizontal: true);

        // Screen cell (column, row) shows the flipped nine-slice's cell (row, column), which is
        // texel (2 - row, column).
        CellColor(pixels, 0, 0).ShouldBe(TexelColor(2, 0));
        CellColor(pixels, 1, 0).ShouldBe(TexelColor(2, 1));
        CellColor(pixels, 0, 1).ShouldBe(TexelColor(1, 0));
        CellColor(pixels, 2, 2).ShouldBe(TexelColor(0, 2));
    }

    [Fact]
    public void Render_FlipDiagonalOnRotatedNineSlice_TransposesInItsOwnAxes()
    {
        using SKBitmap pixels = RenderGrid(flipDiagonal: true, rotation: 90, x: 30, y: 50);

        // Rotated 90 degrees counter-clockwise about its top-left it covers x 30..60, y 20..50: its
        // own +x points up and its own +y points right, so own cell (column, row) is centered at
        // screen (35 + 10 * row, 45 - 10 * column). Transposed, it shows texel (row, column).
        pixels.GetPixel(35, 45).ShouldBe(TexelColor(0, 0));
        pixels.GetPixel(35, 25).ShouldBe(TexelColor(0, 2));
        pixels.GetPixel(55, 45).ShouldBe(TexelColor(2, 0));
    }

    [Fact]
    public void Render_FrameOffset_ShiftsNineSliceRightAndUp()
    {
        using SKBitmap texture = CreateGridTexture();
        using SKBitmap pixels = Render(new AnimationFrame
        {
            FrameLength = 1f,
            Texture = texture,
            RightCoordinate = 1f,
            BottomCoordinate = 1f,
            RelativeX = 10,
            RelativeY = 5,
        });

        // Un-offset the nine-slice would cover x 10..40, y 10..40; the offset moves it to
        // x 20..50, y 5..35.
        pixels.GetPixel(25, 10).ShouldBe(TexelColor(0, 0));
        pixels.GetPixel(15, 15).Alpha.ShouldBe((byte)0);
        pixels.GetPixel(45, 38).Alpha.ShouldBe((byte)0);
        pixels.GetPixel(45, 30).ShouldBe(TexelColor(2, 2));
    }

    [Fact]
    public void Render_FrameOffsetOnRotatedNineSlice_ShiftsAlongItsOwnAxes()
    {
        using SKBitmap texture = CreateGridTexture();
        using SKBitmap pixels = Render(new AnimationFrame
        {
            FrameLength = 1f,
            Texture = texture,
            RightCoordinate = 1f,
            BottomCoordinate = 1f,
            RelativeX = 10,
        }, rotation: 90, x: 30, y: 50);

        // Un-offset, a 90 degree nine-slice covers x 30..60, y 20..50; its own +x points up, so
        // the offset moves it to y 10..40.
        pixels.GetPixel(35, 35).ShouldBe(TexelColor(0, 0));
        pixels.GetPixel(35, 45).Alpha.ShouldBe((byte)0);
    }

    private static AnimationChainList CreateFadeChains()
    {
        AnimationChain chain = new() { Name = "Fade" };
        chain.Add(new AnimationFrame { FrameLength = 1, Alpha = 0, ColorInterpolation = "Linear" });
        chain.Add(new AnimationFrame { FrameLength = 1, Alpha = 200 });
        AnimationChainList chains = new();
        chains.Add(chain);
        return chains;
    }

    [Fact]
    public void AnimateSelf_AlphaGlide_ChangesAlphaMidFrame()
    {
        SkiaGum.Renderables.NineSlice sut = new();
        sut.AnimationLogic.AnimationChains = CreateFadeChains();
        sut.AnimationLogic.CurrentChainName = "Fade";
        sut.AnimationLogic.Animate = true;

        sut.AnimateSelf(0.5);

        sut.Alpha.ShouldBe(100);
    }

    [Fact]
    public void Clone_AlphaGlide_ChangesCloneAlphaMidFrame()
    {
        SkiaGum.Renderables.NineSlice original = new();
        original.AnimationLogic.AnimationChains = CreateFadeChains();
        original.AnimationLogic.CurrentChainName = "Fade";
        SkiaGum.Renderables.NineSlice clone = (SkiaGum.Renderables.NineSlice)original.Clone();
        clone.AnimationLogic.Animate = true;

        clone.AnimateSelf(0.5);

        clone.Alpha.ShouldBe(100);
    }

    [Fact]
    public void AnimateSelf_SkippedColorFrame_DoesNotApplyItsOwnColor()
    {
        AnimationChain chain = new() { Name = "c" };
        chain.Add(new AnimationFrame { FrameLength = 1, Alpha = 0, ColorInterpolation = "Linear" });
        chain.Add(new AnimationFrame { FrameLength = 1, Alpha = 255, SkipColorKeyframe = true });
        chain.Add(new AnimationFrame { FrameLength = 1, Alpha = 200 });
        AnimationChainList chains = new();
        chains.Add(chain);

        SkiaGum.Renderables.NineSlice sut = new();
        sut.AnimationLogic.AnimationChains = chains;
        sut.AnimationLogic.CurrentChainName = "c";
        sut.AnimationLogic.Animate = true;

        // Halfway through the glide that spans the skipped frame; its own alpha of 255 is ignored.
        sut.AnimateSelf(1.0);

        sut.Alpha.ShouldBe(100);
    }

    [Fact]
    public void AnimateSelf_RedGlideWithMultiplyOperation_AppliesInterpolatedRed()
    {
        AnimationChain chain = new() { Name = "c" };
        chain.Add(new AnimationFrame
        {
            FrameLength = 1,
            Red = 0,
            ColorOperation = AnimationFrameColorOperation.Multiply,
            ColorInterpolation = "Linear",
        });
        chain.Add(new AnimationFrame { FrameLength = 1, Red = 200 });
        AnimationChainList chains = new();
        chains.Add(chain);

        SkiaGum.Renderables.NineSlice sut = new();
        sut.AnimationLogic.AnimationChains = chains;
        sut.AnimationLogic.CurrentChainName = "c";
        sut.AnimationLogic.Animate = true;

        sut.AnimateSelf(0.5);

        sut.Red.ShouldBe(100);
    }
}
