using Gum.GueDeriving;
using Gum.Managers;
using Gum.RenderingLibrary;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Pixel-readback test for raylib Sprite tiling (issue #3456, part of the #3432 parity umbrella).
/// Renders into an <see cref="ContainerRuntime.IsRenderTarget"/> container so the baked texture can
/// be sampled deterministically, mirroring the pattern in <see cref="RenderTargetTests"/>.
/// </summary>
public class SpriteWrapTests : BaseTestClass
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

    [Fact]
    public void Render_WrapTrue_TilesSourceRectangleAcrossTexture()
    {
        // A 2x1 texture: left texel pure red, right texel pure blue. Tiling this across a source
        // rectangle twice as wide as the texture must repeat the red/blue pair rather than clamp or
        // stretch. Explicit RGBA (not the named Color.Red/Blue constants, which raylib-cs defines as
        // muted brand colors, e.g. Color.Red = 230,41,55) so the R/B channel thresholds below are
        // unambiguous.
        Color pureRed = new Color((byte)255, (byte)0, (byte)0, (byte)255);
        Color pureBlue = new Color((byte)0, (byte)0, (byte)255, (byte)255);
        Image image = GenImageColor(2, 1, pureRed);
        ImageDrawPixel(ref image, 1, 0, pureBlue);
        Texture2D texture = LoadTextureFromImage(image);
        UnloadImage(image);

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
        sprite.TextureWidth = 4; // 2x the texture width -> tiles twice
        sprite.TextureHeight = 1;
        sprite.Wrap = true;
        sprite.Width = 40;
        sprite.Height = 10;
        container.Children.Add(sprite);

        GumService.Default.Root.Children.Add(container);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        RenderTexture2D renderTexture = Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value;

        Color firstRed = ReadRenderTargetPixel(renderTexture, 5, 5);
        Color firstBlue = ReadRenderTargetPixel(renderTexture, 15, 5);
        Color secondRed = ReadRenderTargetPixel(renderTexture, 25, 5);
        Color secondBlue = ReadRenderTargetPixel(renderTexture, 35, 5);

        firstRed.R.ShouldBeGreaterThan((byte)200);
        firstRed.B.ShouldBeLessThan((byte)50);

        firstBlue.B.ShouldBeGreaterThan((byte)200);
        firstBlue.R.ShouldBeLessThan((byte)50);

        // The second tile proves wrapping: without it, the area beyond the texture bounds would
        // clamp to the last (blue) texel or stretch, never repeating back to red.
        secondRed.R.ShouldBeGreaterThan((byte)200);
        secondRed.B.ShouldBeLessThan((byte)50);

        secondBlue.B.ShouldBeGreaterThan((byte)200);
        secondBlue.R.ShouldBeLessThan((byte)50);

        GumService.Default.Root.Children.Clear();
        UnloadTexture(texture);
    }

    [Fact]
    public void Render_WrapFalse_ClampsSourceRectangleInsteadOfRepeating()
    {
        // Same 2x1 red/blue texture and oversized source rectangle as the Wrap=true test, but with
        // Wrap=false. raylib's default GL texture wrap mode is Repeat (TextureWrap.Repeat = 0), so
        // without an explicit Clamp, drawing an oversized source rectangle silently repeats instead
        // of clamping to the edge pixel — the opposite of Wrap=false's intended "clamp" semantics.
        Color pureRed = new Color((byte)255, (byte)0, (byte)0, (byte)255);
        Color pureBlue = new Color((byte)0, (byte)0, (byte)255, (byte)255);
        Image image = GenImageColor(2, 1, pureRed);
        ImageDrawPixel(ref image, 1, 0, pureBlue);
        Texture2D texture = LoadTextureFromImage(image);
        UnloadImage(image);

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
        sprite.TextureWidth = 4; // 2x the texture width -> would tile twice if wrapped
        sprite.TextureHeight = 1;
        sprite.Wrap = false;
        sprite.Width = 40;
        sprite.Height = 10;
        container.Children.Add(sprite);

        GumService.Default.Root.Children.Add(container);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        RenderTexture2D renderTexture = Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value;

        // The area beyond the texture's right edge (destination x >= 20, matching source x >= 2 on
        // the 2px texture) must clamp to the last (blue) texel, not wrap back to red.
        Color pastEdge = ReadRenderTargetPixel(renderTexture, 25, 5);
        Color pastEdgeFurther = ReadRenderTargetPixel(renderTexture, 35, 5);

        pastEdge.B.ShouldBeGreaterThan((byte)200);
        pastEdge.R.ShouldBeLessThan((byte)50);

        pastEdgeFurther.B.ShouldBeGreaterThan((byte)200);
        pastEdgeFurther.R.ShouldBeLessThan((byte)50);

        GumService.Default.Root.Children.Clear();
        UnloadTexture(texture);
    }
}
