using System;
using System.IO;
using CrossRuntimeTests;
using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary;
using Shouldly;
using SkiaGum;
using SkiaGum.Content;
using SkiaSharp;

namespace SkiaGum.Tests.ShadowDusk;

/// <summary>
/// The shipped Outline shader on a container holding a nine-slice with transparent corners (#6984).
/// Each case renders the nine-slice plain and through the shader and checks the second against the
/// first: the outline must follow the nine-slice's alpha with no seams between the slices, no
/// clipping at the container bounds (the container has exactly Thickness of transparent margin), at
/// whole and fractional sizes, tiled and stretched, at different zoom levels. The same cases run
/// against MonoGame (MonoGameGum.IntegrationTests) and raylib (RaylibGum.Tests).
/// </summary>
public class NineSliceOutlineShaderTests : IDisposable
{
    private const int SurfaceSize = 128;

    public NineSliceOutlineShaderTests()
    {
        Gum.Wireframe.GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        GumService.Default.UseShadowDusk();
    }

    public void Dispose()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = SkSlRuntimeEffectLoader.Load;
    }

    private static string ShippedShaderPath => Path.Combine(AppContext.BaseDirectory, "Outline.slang");

    private static SKBitmap CreateFrameTexture()
    {
        SKBitmap texture = new(NineSliceOutlineOracle.TextureSize, NineSliceOutlineOracle.TextureSize);
        for (int y = 0; y < NineSliceOutlineOracle.TextureSize; y++)
        {
            for (int x = 0; x < NineSliceOutlineOracle.TextureSize; x++)
            {
                (byte r, byte g, byte b, byte a) = NineSliceOutlineOracle.Texel(x, y);
                texture.SetPixel(x, y, new SKColor(r, g, b, a));
            }
        }
        return texture;
    }

    private static (byte R, byte G, byte B, byte A)[] Render(
        SKBitmap texture, bool withOutline, float width, float height, bool isTiling, float borderScale,
        float thickness, float zoom)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(SurfaceSize, SurfaceSize));
        GumService.Default.Initialize(surface.Canvas, SurfaceSize, SurfaceSize);
        SystemManagers.Default.Renderer.Camera.Zoom = zoom;
        try
        {
            float margin = thickness;
            ContainerRuntime container = new()
            {
                Width = width + (2 * margin),
                Height = height + (2 * margin),
                IsRenderTarget = true,
            };

            NineSliceRuntime nineSlice = new();
            nineSlice.Texture = texture;
            nineSlice.IsTilingMiddleSections = isTiling;
            nineSlice.BorderScale = borderScale;
            nineSlice.WidthUnits = DimensionUnitType.Absolute;
            nineSlice.HeightUnits = DimensionUnitType.Absolute;
            nineSlice.X = margin;
            nineSlice.Y = margin;
            nineSlice.Width = width;
            nineSlice.Height = height;
            container.Children.Add(nineSlice);
            GumService.Default.Root.Children.Add(container);

            if (withOutline)
            {
                container.SetShaderParameter("Thickness", thickness);
                container.SetShaderParameter("OutlineGreen", 128f);
                container.SetShaderParameter("OutlineBlue", 255f);
                container.SourceShaderFile = ShippedShaderPath;
            }

            GumService.Default.Draw();

            using SKImage image = surface.Snapshot();
            using SKBitmap bitmap = SKBitmap.FromImage(image);
            (byte R, byte G, byte B, byte A)[] pixels = new (byte, byte, byte, byte)[SurfaceSize * SurfaceSize];
            for (int y = 0; y < SurfaceSize; y++)
            {
                for (int x = 0; x < SurfaceSize; x++)
                {
                    SKColor color = bitmap.GetPixel(x, y);
                    pixels[(y * SurfaceSize) + x] = (color.Red, color.Green, color.Blue, color.Alpha);
                }
            }
            return pixels;
        }
        finally
        {
            SystemManagers.Default.Renderer.Camera.Zoom = 1f;
        }
    }

    [Theory]
    [InlineData(24f, 24f, false, 1f, 1f, 1f)]
    [InlineData(40f, 28f, false, 1f, 1f, 1f)]
    [InlineData(40f, 28f, false, 1f, 2f, 1f)]
    [InlineData(40f, 28f, true, 1f, 1f, 1f)]
    [InlineData(40f, 28f, true, 1f, 2f, 1f)]
    [InlineData(41f, 29f, true, 1f, 1f, 1f)]
    [InlineData(40f, 28f, false, 2f, 1f, 1f)]
    [InlineData(40f, 28f, true, 2f, 2f, 1f)]
    [InlineData(40.5f, 27.5f, false, 1f, 1f, 1f)]
    [InlineData(40.5f, 27.5f, true, 1f, 2f, 1f)]
    [InlineData(40f, 28f, false, 1f, 1f, 2f)]
    [InlineData(40f, 28f, true, 1f, 2f, 2f)]
    [InlineData(40.5f, 27.5f, true, 1f, 1f, 2f)]
    public void Outline_OfANineSliceWithTransparentCorners_FollowsItsAlpha(
        float width, float height, bool isTiling, float borderScale, float thickness, float zoom)
    {
        using SKBitmap texture = CreateFrameTexture();

        var plain = Render(texture, false, width, height, isTiling, borderScale, thickness, zoom);
        var outlined = Render(texture, true, width, height, isTiling, borderScale, thickness, zoom);

        // The plain picture is a real nine-slice: a whole rectangle minus the transparent corner
        // texels (3 per corner, each borderScale * zoom pixels square).
        if (width % 1 == 0 && height % 1 == 0)
        {
            float cutTexel = borderScale * zoom;
            int expectedContent = (int)(width * zoom * height * zoom) - (int)(12 * cutTexel * cutTexel);
            NineSliceOutlineOracle.CountContent(plain).ShouldBe(expectedContent);
        }

        int reach = (int)(thickness * zoom);
        NineSliceOutlineOracle.Compare(plain, outlined, SurfaceSize, SurfaceSize, reach, (0, 128, 255)).ShouldBeEmpty();
    }
}
