using Gum.GueDeriving;
using Gum.Managers;
using Gum.RenderingLibrary;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Pixel-readback test for raylib Sprite clamp-to-edge (issue #3459, part of the #3432 parity
/// umbrella). Renders into an <see cref="ContainerRuntime.IsRenderTarget"/> container so the baked
/// texture can be sampled deterministically, mirroring the pattern in <see cref="SpriteWrapTests"/>.
/// </summary>
public class SpriteClampTests : BaseTestClass
{
    private static void DrawOnce()
    {
        BeginDrawing();
        GumService.Default.Draw();
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

    private static Texture2D CreateRedBlueTexture()
    {
        // A 2x1 texture: left texel pure red, right texel pure blue. Explicit RGBA (not the named
        // Color.Red/Blue constants, which raylib-cs defines as muted brand colors) so the R/B
        // channel thresholds below are unambiguous.
        Color pureRed = new Color((byte)255, (byte)0, (byte)0, (byte)255);
        Color pureBlue = new Color((byte)0, (byte)0, (byte)255, (byte)255);
        Image image = GenImageColor(2, 1, pureRed);
        ImageDrawPixel(ref image, 1, 0, pureBlue);
        Texture2D texture = LoadTextureFromImage(image);
        UnloadImage(image);
        return texture;
    }

    [Fact]
    public void Render_WrapFalseFlipHorizontal_OversizedSourceRectangle_MirrorsClampedImage()
    {
        Texture2D texture = CreateRedBlueTexture();

        ContainerRuntime container = new();
        container.X = 0;
        container.Y = 0;
        container.Width = 40;
        container.Height = 10;
        container.IsRenderTarget = true;

        SpriteRuntime sprite = new();
        sprite.Texture = texture;
        sprite.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.TextureAddress = TextureAddress.Custom;
        sprite.TextureLeft = 0;
        sprite.TextureTop = 0;
        sprite.TextureWidth = 4; // 2x the texture width -> right half is out of bounds
        sprite.TextureHeight = 1;
        sprite.Wrap = false;
        sprite.FlipHorizontal = true;
        sprite.Width = 40;
        sprite.Height = 10;
        container.Children.Add(sprite);

        GumService.Default.Root.Children.Add(container);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        RenderTexture2D renderTexture = Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value;

        // Unflipped, the strip reads Red, Blue, Blue(clamp), Blue(clamp) left-to-right (see
        // Render_WrapFalse_OversizedSourceRectangle_ClampsToEdgeInsteadOfRepeating). Flipping the
        // whole rendered strip mirrors that order: Blue(clamp), Blue(clamp), Blue, Red.
        Color leftClamped = ReadRenderTargetPixel(renderTexture, 5, 5);
        Color midClamped = ReadRenderTargetPixel(renderTexture, 15, 5);
        Color inBoundsBlue = ReadRenderTargetPixel(renderTexture, 25, 5);
        Color inBoundsRed = ReadRenderTargetPixel(renderTexture, 35, 5);

        leftClamped.B.ShouldBeGreaterThan((byte)200);
        leftClamped.R.ShouldBeLessThan((byte)50);

        midClamped.B.ShouldBeGreaterThan((byte)200);
        midClamped.R.ShouldBeLessThan((byte)50);

        inBoundsBlue.B.ShouldBeGreaterThan((byte)200);
        inBoundsBlue.R.ShouldBeLessThan((byte)50);

        // This is the exact regression the revert in #3457 guarded against: under hardware
        // TextureWrap.Clamp, FlipHorizontal's negative-source-dimension trick sampled a single
        // clamped edge texel across the whole quad instead of the flipped image. This assertion
        // fails the same way if RenderClamped ever routes back through SetTextureWrap.
        inBoundsRed.R.ShouldBeGreaterThan((byte)200);
        inBoundsRed.B.ShouldBeLessThan((byte)50);

        GumService.Default.Root.Children.Clear();
        UnloadTexture(texture);
    }

    [Fact]
    public void Render_WrapFalse_OversizedSourceRectangle_ClampsToEdgeInsteadOfRepeating()
    {
        Texture2D texture = CreateRedBlueTexture();

        ContainerRuntime container = new();
        container.X = 0;
        container.Y = 0;
        container.Width = 40;
        container.Height = 10;
        container.IsRenderTarget = true;

        SpriteRuntime sprite = new();
        sprite.Texture = texture;
        sprite.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.TextureAddress = TextureAddress.Custom;
        sprite.TextureLeft = 0;
        sprite.TextureTop = 0;
        sprite.TextureWidth = 4; // 2x the texture width -> right half is out of bounds
        sprite.TextureHeight = 1;
        sprite.Wrap = false;
        sprite.Width = 40;
        sprite.Height = 10;
        container.Children.Add(sprite);

        GumService.Default.Root.Children.Add(container);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        RenderTexture2D renderTexture = Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value;

        Color inBoundsRed = ReadRenderTargetPixel(renderTexture, 5, 5);
        Color inBoundsBlue = ReadRenderTargetPixel(renderTexture, 15, 5);
        Color clampedNearEdge = ReadRenderTargetPixel(renderTexture, 25, 5);
        Color clampedFarEdge = ReadRenderTargetPixel(renderTexture, 35, 5);

        inBoundsRed.R.ShouldBeGreaterThan((byte)200);
        inBoundsRed.B.ShouldBeLessThan((byte)50);

        inBoundsBlue.B.ShouldBeGreaterThan((byte)200);
        inBoundsBlue.R.ShouldBeLessThan((byte)50);

        // The out-of-bounds half must stretch the last (blue) texel, not repeat back to red.
        clampedNearEdge.B.ShouldBeGreaterThan((byte)200);
        clampedNearEdge.R.ShouldBeLessThan((byte)50);

        clampedFarEdge.B.ShouldBeGreaterThan((byte)200);
        clampedFarEdge.R.ShouldBeLessThan((byte)50);

        GumService.Default.Root.Children.Clear();
        UnloadTexture(texture);
    }
}
