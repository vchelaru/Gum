using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Gum.Renderables;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// An animation frame's RelativeX/RelativeY, keyframe color glides and flips reach the drawn raylib
/// nine-slice, as they do for a raylib sprite (#6035, #6058). The pixel tests render into an
/// <see cref="ContainerRuntime.IsRenderTarget"/> container so the baked texture can be sampled,
/// mirroring <see cref="SpriteAnimationFlipDiagonalTests"/>.
/// </summary>
public class NineSliceAnimationFrameTests : BaseTestClass
{
    // A 3x3 grid of distinct colors. With a 1 texel border drawn at BorderScale 10, each texel
    // fills one 10x10 cell of a 30x30 nine-slice.
    private static Color TexelColor(int x, int y) => new((byte)(40 + x * 80), (byte)(40 + y * 80), (byte)0, (byte)255);

    private static Texture2D CreateGridTexture()
    {
        Image image = GenImageColor(3, 3, TexelColor(0, 0));
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                ImageDrawPixel(ref image, x, y, TexelColor(x, y));
            }
        }
        Texture2D texture = LoadTextureFromImage(image);
        UnloadImage(image);
        return texture;
    }

    private static RenderTexture2D Render(AnimationFrame frame, float borderScale = 10, float rotation = 0, float x = 10, float y = 10)
    {
        AnimationChain chain = new() { Name = "c" };
        chain.Add(frame);

        ContainerRuntime container = new();
        container.Width = 80;
        container.Height = 80;
        container.IsRenderTarget = true;

        NineSliceRuntime nineSlice = new();
        nineSlice.X = x;
        nineSlice.Y = y;
        nineSlice.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        nineSlice.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        nineSlice.Width = 30;
        nineSlice.Height = 30;
        nineSlice.BorderScale = borderScale;
        nineSlice.Rotation = rotation;
        nineSlice.AnimationChains = new AnimationChainList { chain };
        nineSlice.CurrentChainName = "c";
        nineSlice.Animate = true;
        nineSlice.AnimateSelf(0.0);
        container.Children.Add(nineSlice);

        Gum.GumService.Default.Root.Children.Add(container);
        Gum.GumService.Default.Root.UpdateLayout();

        BeginDrawing();
        Gum.GumService.Default.Draw();
        EndDrawing();

        return Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value;
    }

    private static RenderTexture2D RenderGrid(Texture2D texture, bool flipHorizontal = false, bool flipVertical = false,
        bool flipDiagonal = false, float borderScale = 10, float rotation = 0, float x = 10, float y = 10)
    {
        return Render(new AnimationFrame
        {
            FrameLength = 1f,
            Texture = texture,
            RightCoordinate = 1f,
            BottomCoordinate = 1f,
            FlipHorizontal = flipHorizontal,
            FlipVertical = flipVertical,
            FlipDiagonal = flipDiagonal,
        }, borderScale, rotation, x, y);
    }

    // A render texture is stored bottom-up in GL, so rows are flipped relative to draw space.
    private static Color ReadPixel(RenderTexture2D renderTexture, int x, int y)
    {
        Image image = LoadImageFromTexture(renderTexture.Texture);
        try
        {
            return GetImageColor(image, x, renderTexture.Texture.Height - 1 - y);
        }
        finally
        {
            UnloadImage(image);
        }
    }

    // The color at the center of the cell in column, row of the unrotated nine-slice at (10, 10).
    private static Color CellColor(RenderTexture2D target, int column, int row) =>
        ReadPixel(target, 10 + column * 10 + 5, 10 + row * 10 + 5);

    private static void ShouldBe(Color actual, Color expected)
    {
        (actual.R, actual.G, actual.B).ShouldBe((expected.R, expected.G, expected.B));
    }

    private static void ClearRoot() => Gum.GumService.Default.Root.Children.Clear();

    [Fact]
    public void Render_NoFlip_DrawsFrameUnflipped()
    {
        Texture2D texture = CreateGridTexture();

        RenderTexture2D target = RenderGrid(texture);

        ShouldBe(CellColor(target, 0, 0), TexelColor(0, 0));
        ShouldBe(CellColor(target, 2, 0), TexelColor(2, 0));
        ShouldBe(CellColor(target, 0, 2), TexelColor(0, 2));
        ShouldBe(CellColor(target, 1, 1), TexelColor(1, 1));

        ClearRoot();
        UnloadTexture(texture);
    }

    [Fact]
    public void Render_FlipHorizontal_MirrorsTheNineSliceInsideItsOwnBounds()
    {
        Texture2D texture = CreateGridTexture();

        RenderTexture2D target = RenderGrid(texture, flipHorizontal: true);

        ShouldBe(CellColor(target, 0, 0), TexelColor(2, 0));
        ShouldBe(CellColor(target, 2, 0), TexelColor(0, 0));
        ShouldBe(CellColor(target, 0, 2), TexelColor(2, 2));
        ShouldBe(CellColor(target, 1, 1), TexelColor(1, 1));
        ReadPixel(target, 45, 25).A.ShouldBe((byte)0);

        ClearRoot();
        UnloadTexture(texture);
    }

    [Fact]
    public void Render_FlipVertical_MirrorsTheNineSliceInsideItsOwnBounds()
    {
        Texture2D texture = CreateGridTexture();

        RenderTexture2D target = RenderGrid(texture, flipVertical: true);

        ShouldBe(CellColor(target, 0, 0), TexelColor(0, 2));
        ShouldBe(CellColor(target, 2, 0), TexelColor(2, 2));
        ShouldBe(CellColor(target, 0, 2), TexelColor(0, 0));
        ReadPixel(target, 25, 45).A.ShouldBe((byte)0);

        ClearRoot();
        UnloadTexture(texture);
    }

    [Fact]
    public void Render_FlipHorizontalAtDefaultBorderScale_MirrorsTheNineSlice()
    {
        Texture2D texture = CreateGridTexture();

        // At BorderScale 1 the nine-slice normally takes raylib's built-in nine-patch draw, which
        // cannot mirror.
        RenderTexture2D target = RenderGrid(texture, flipHorizontal: true, borderScale: 1);

        ShouldBe(ReadPixel(target, 10, 10), TexelColor(2, 0));
        ShouldBe(ReadPixel(target, 39, 10), TexelColor(0, 0));
        ShouldBe(ReadPixel(target, 10, 39), TexelColor(2, 2));

        ClearRoot();
        UnloadTexture(texture);
    }

    [Fact]
    public void Render_FlipDiagonal_TransposesTheNineSlice()
    {
        Texture2D texture = CreateGridTexture();

        RenderTexture2D target = RenderGrid(texture, flipDiagonal: true);

        // A transpose fixes the top-left and bottom-right and swaps the other corners.
        ShouldBe(CellColor(target, 0, 0), TexelColor(0, 0));
        ShouldBe(CellColor(target, 2, 0), TexelColor(0, 2));
        ShouldBe(CellColor(target, 0, 2), TexelColor(2, 0));
        ShouldBe(CellColor(target, 2, 2), TexelColor(2, 2));
        ShouldBe(CellColor(target, 1, 0), TexelColor(0, 1));

        ClearRoot();
        UnloadTexture(texture);
    }

    [Fact]
    public void Render_FlipDiagonalWithFlipHorizontal_FlipsBeforeTransposing()
    {
        Texture2D texture = CreateGridTexture();

        RenderTexture2D target = RenderGrid(texture, flipDiagonal: true, flipHorizontal: true);

        // Screen cell (column, row) shows the flipped nine-slice's cell (row, column), which is
        // texel (2 - row, column).
        ShouldBe(CellColor(target, 0, 0), TexelColor(2, 0));
        ShouldBe(CellColor(target, 1, 0), TexelColor(2, 1));
        ShouldBe(CellColor(target, 0, 1), TexelColor(1, 0));
        ShouldBe(CellColor(target, 2, 2), TexelColor(0, 2));

        ClearRoot();
        UnloadTexture(texture);
    }

    [Fact]
    public void Render_FlipDiagonalOnRotatedNineSlice_TransposesInItsOwnAxes()
    {
        Texture2D texture = CreateGridTexture();

        RenderTexture2D target = RenderGrid(texture, flipDiagonal: true, rotation: 90, x: 30, y: 50);

        // Rotated 90 degrees counter-clockwise about its top-left it covers x 30..60, y 20..50: its
        // own +x points up and its own +y points right, so own cell (column, row) is centered at
        // screen (35 + 10 * row, 45 - 10 * column). Transposed, it shows texel (row, column).
        ShouldBe(ReadPixel(target, 35, 45), TexelColor(0, 0));
        ShouldBe(ReadPixel(target, 35, 25), TexelColor(0, 2));
        ShouldBe(ReadPixel(target, 55, 45), TexelColor(2, 0));

        ClearRoot();
        UnloadTexture(texture);
    }

    [Fact]
    public void Render_FrameOffset_ShiftsNineSliceRightAndUp()
    {
        Texture2D texture = CreateGridTexture();

        RenderTexture2D target = Render(new AnimationFrame
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
        ShouldBe(ReadPixel(target, 25, 10), TexelColor(0, 0));
        ReadPixel(target, 15, 15).A.ShouldBe((byte)0);
        ReadPixel(target, 45, 38).A.ShouldBe((byte)0);
        ShouldBe(ReadPixel(target, 45, 30), TexelColor(2, 2));

        ClearRoot();
        UnloadTexture(texture);
    }

    [Fact]
    public void Render_FrameOffsetAtDefaultBorderScale_ShiftsNineSliceRightAndUp()
    {
        Texture2D texture = CreateGridTexture();

        // At BorderScale 1 the nine-slice takes raylib's built-in nine-patch draw.
        RenderTexture2D target = Render(new AnimationFrame
        {
            FrameLength = 1f,
            Texture = texture,
            RightCoordinate = 1f,
            BottomCoordinate = 1f,
            RelativeX = 10,
            RelativeY = 5,
        }, borderScale: 1);

        ShouldBe(ReadPixel(target, 20, 5), TexelColor(0, 0));
        ReadPixel(target, 15, 15).A.ShouldBe((byte)0);
        ReadPixel(target, 45, 38).A.ShouldBe((byte)0);

        ClearRoot();
        UnloadTexture(texture);
    }

    [Fact]
    public void Render_FrameOffsetOnRotatedNineSlice_ShiftsAlongItsOwnAxes()
    {
        Texture2D texture = CreateGridTexture();

        RenderTexture2D target = Render(new AnimationFrame
        {
            FrameLength = 1f,
            Texture = texture,
            RightCoordinate = 1f,
            BottomCoordinate = 1f,
            RelativeX = 10,
        }, rotation: 90, x: 30, y: 50);

        // Un-offset, a 90 degree nine-slice covers x 30..60, y 20..50; its own +x points up, so
        // the offset moves it to y 10..40.
        ShouldBe(ReadPixel(target, 35, 35), TexelColor(0, 0));
        ReadPixel(target, 35, 45).A.ShouldBe((byte)0);

        ClearRoot();
        UnloadTexture(texture);
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
        NineSlice sut = new();
        sut.AnimationLogic.AnimationChains = CreateFadeChains();
        sut.AnimationLogic.CurrentChainName = "Fade";
        sut.AnimationLogic.Animate = true;

        sut.AnimateSelf(0.5);

        sut.Alpha.ShouldBe(100);
    }

    [Fact]
    public void Clone_AlphaGlide_ChangesCloneAlphaMidFrame()
    {
        NineSlice original = new();
        original.AnimationLogic.AnimationChains = CreateFadeChains();
        original.AnimationLogic.CurrentChainName = "Fade";
        NineSlice clone = original.Clone();
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

        NineSlice sut = new();
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
            ColorOperation = Gum.Content.AnimationChain.AnimationFrameColorOperation.Multiply,
            ColorInterpolation = "Linear",
        });
        chain.Add(new AnimationFrame { FrameLength = 1, Red = 200 });
        AnimationChainList chains = new();
        chains.Add(chain);

        NineSlice sut = new();
        sut.AnimationLogic.AnimationChains = chains;
        sut.AnimationLogic.CurrentChainName = "c";
        sut.AnimationLogic.Animate = true;

        sut.AnimateSelf(0.5);

        sut.Red.ShouldBe(100);
    }
}
