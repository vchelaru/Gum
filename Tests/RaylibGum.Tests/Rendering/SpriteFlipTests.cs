using Gum.GueDeriving;
using Gum.Managers;
using Gum.RenderingLibrary;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Pixel-readback tests for raylib Sprite FlipHorizontal/FlipVertical on a source rectangle that
/// does not start at the texture origin (an atlas cell). Issue #4854: the flip shifted the source
/// window by one cell, so a flipped atlas sprite sampled the neighbouring (often empty) cell.
/// Renders into an <see cref="ContainerRuntime.IsRenderTarget"/> container so the baked texture
/// can be sampled deterministically, mirroring <see cref="SpriteClampTests"/>.
/// </summary>
public class SpriteFlipTests : BaseTestClass
{
    private static readonly Color PureGreen = new((byte)0, (byte)255, (byte)0, (byte)255);
    private static readonly Color PureRed = new((byte)255, (byte)0, (byte)0, (byte)255);
    private static readonly Color PureBlue = new((byte)0, (byte)0, (byte)255, (byte)255);

    private static void DrawOnce()
    {
        BeginDrawing();
        Gum.GumService.Default.Draw();
        EndDrawing();
    }

    // Reads a pixel in top-left-origin draw space. A render texture is stored bottom-up in GL, so
    // LoadImageFromTexture yields an image whose rows are flipped relative to draw space.
    private static Color ReadRenderTargetPixel(RenderTexture2D renderTexture, int x, int y)
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

    // A 4-texel strip (horizontal or vertical): Green, Green, Red, Blue. The Red/Blue pair at
    // offset 2 is the "atlas cell" the sprite shows; Green is the neighbouring cell that a
    // mis-shifted source window would sample instead.
    private static Texture2D CreateStripTexture(bool horizontal)
    {
        Image image = horizontal ? GenImageColor(4, 1, PureGreen) : GenImageColor(1, 4, PureGreen);
        if (horizontal)
        {
            ImageDrawPixel(ref image, 2, 0, PureRed);
            ImageDrawPixel(ref image, 3, 0, PureBlue);
        }
        else
        {
            ImageDrawPixel(ref image, 0, 2, PureRed);
            ImageDrawPixel(ref image, 0, 3, PureBlue);
        }
        Texture2D texture = LoadTextureFromImage(image);
        UnloadImage(image);
        return texture;
    }

    private static RenderTexture2D RenderAtlasCellSprite(Texture2D texture, bool horizontal,
        bool flipHorizontal, bool flipVertical, bool wrap)
    {
        int width = horizontal ? 20 : 10;
        int height = horizontal ? 10 : 20;

        ContainerRuntime container = new();
        container.X = 0;
        container.Y = 0;
        container.Width = width;
        container.Height = height;
        container.IsRenderTarget = true;

        SpriteRuntime sprite = new();
        sprite.Texture = texture;
        sprite.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.TextureAddress = TextureAddress.Custom;
        sprite.TextureLeft = horizontal ? 2 : 0;
        sprite.TextureTop = horizontal ? 0 : 2;
        sprite.TextureWidth = horizontal ? 2 : 1;
        sprite.TextureHeight = horizontal ? 1 : 2;
        sprite.Wrap = wrap;
        sprite.FlipHorizontal = flipHorizontal;
        sprite.FlipVertical = flipVertical;
        sprite.Width = width;
        sprite.Height = height;
        container.Children.Add(sprite);

        Gum.GumService.Default.Root.Children.Add(container);
        Gum.GumService.Default.Root.UpdateLayout();

        DrawOnce();

        return Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value;
    }

    private static void ShouldBe(Color actual, Color expected)
    {
        (actual.R, actual.G, actual.B).ShouldBe((expected.R, expected.G, expected.B));
    }

    [Fact]
    public void Render_FlipHorizontal_OffsetSourceRectangle_MirrorsThatCellOnly()
    {
        Texture2D texture = CreateStripTexture(horizontal: true);

        RenderTexture2D renderTexture = RenderAtlasCellSprite(texture, horizontal: true,
            flipHorizontal: true, flipVertical: false, wrap: false);

        // Unflipped the cell reads Red, Blue left-to-right; flipped it reads Blue, Red.
        ShouldBe(ReadRenderTargetPixel(renderTexture, 5, 5), PureBlue);
        ShouldBe(ReadRenderTargetPixel(renderTexture, 15, 5), PureRed);

        Gum.GumService.Default.Root.Children.Clear();
        UnloadTexture(texture);
    }

    [Fact]
    public void Render_FlipVertical_OffsetSourceRectangle_MirrorsThatCellOnly()
    {
        Texture2D texture = CreateStripTexture(horizontal: false);

        RenderTexture2D renderTexture = RenderAtlasCellSprite(texture, horizontal: false,
            flipHorizontal: false, flipVertical: true, wrap: false);

        // Unflipped the cell reads Red, Blue top-to-bottom; flipped it reads Blue, Red.
        ShouldBe(ReadRenderTargetPixel(renderTexture, 5, 5), PureBlue);
        ShouldBe(ReadRenderTargetPixel(renderTexture, 5, 15), PureRed);

        Gum.GumService.Default.Root.Children.Clear();
        UnloadTexture(texture);
    }

    [Fact]
    public void Render_WrapTrueFlipHorizontal_OffsetSourceRectangle_MirrorsThatCellOnly()
    {
        Texture2D texture = CreateStripTexture(horizontal: true);

        RenderTexture2D renderTexture = RenderAtlasCellSprite(texture, horizontal: true,
            flipHorizontal: true, flipVertical: false, wrap: true);

        // The tiled path flips each tile's source rect the same way as the plain path.
        ShouldBe(ReadRenderTargetPixel(renderTexture, 5, 5), PureBlue);
        ShouldBe(ReadRenderTargetPixel(renderTexture, 15, 5), PureRed);

        Gum.GumService.Default.Root.Children.Clear();
        UnloadTexture(texture);
    }
}
