using System;
using System.IO;
using System.Linq;
using CrossRuntimeTests;
using Gum.DataTypes;
using Gum.GueDeriving;
using Raylib_cs;
using ShadowDusk.Compiler.Raylib;
using ShadowDusk.Core;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// The shipped Outline shader on a container holding a nine-slice with transparent corners (#6984).
/// Each case renders the nine-slice plain and through the shader and checks the second against the
/// first: the outline must follow the nine-slice's alpha with no seams between the slices, no
/// clipping at the container bounds (the container has exactly Thickness of transparent margin), at
/// whole and fractional sizes, tiled and stretched, at different zoom levels. The same cases run
/// against MonoGame (MonoGameGum.IntegrationTests) and Skia (SkiaGum.Tests).
/// </summary>
public class NineSliceOutlineShaderTests : BaseTestClass
{
    private static Texture2D CreateFrameTexture()
    {
        Image image = GenImageColor(NineSliceOutlineOracle.TextureSize, NineSliceOutlineOracle.TextureSize, Color.Blank);
        try
        {
            for (int y = 0; y < NineSliceOutlineOracle.TextureSize; y++)
            {
                for (int x = 0; x < NineSliceOutlineOracle.TextureSize; x++)
                {
                    (byte r, byte g, byte b, byte a) = NineSliceOutlineOracle.Texel(x, y);
                    ImageDrawPixel(ref image, x, y, new Color(r, g, b, a));
                }
            }
            return LoadTextureFromImage(image);
        }
        finally
        {
            UnloadImage(image);
        }
    }

    // Bakes the nine-slice, offset by thickness on both axes inside a container with that much
    // transparent margin, optionally through the outline shader, and returns the baked pixels and size.
    private static ((byte R, byte G, byte B, byte A)[] Pixels, int Width, int Height) Render(
        Texture2D texture, Shader? outline, float width, float height, bool isTiling, float borderScale,
        float thickness, float zoom)
    {
        float margin = thickness;
        ContainerRuntime outer = new()
        {
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            Width = width + (2 * margin),
            Height = height + (2 * margin),
            IsRenderTarget = true,
        };
        ContainerRuntime inner = new()
        {
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
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
        inner.Children.Add(nineSlice);
        outer.Children.Add(inner);
        Gum.GumService.Default.Root.Children.Add(outer);
        Gum.GumService.Default.Root.UpdateLayout();

        global::RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom = zoom;
        try
        {
            if (outline != null)
            {
                inner.RenderTargetEffect = outline.Value;
                inner.SetShaderParameter("Thickness", thickness);
                inner.SetShaderParameter("OutlineGreen", 128f);
                inner.SetShaderParameter("OutlineBlue", 255f);
            }

            BeginDrawing();
            Gum.GumService.Default.Draw();
            EndDrawing();

            RenderTexture2D baked = Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value;
            int bakedWidth = baked.Texture.Width;
            int bakedHeight = baked.Texture.Height;
            Image image = LoadImageFromTexture(baked.Texture);
            try
            {
                (byte, byte, byte, byte)[] pixels = new (byte, byte, byte, byte)[bakedWidth * bakedHeight];
                for (int y = 0; y < bakedHeight; y++)
                {
                    for (int x = 0; x < bakedWidth; x++)
                    {
                        // A render texture is stored bottom-up in GL, so flip the row.
                        Color color = GetImageColor(image, x, bakedHeight - 1 - y);
                        pixels[(y * bakedWidth) + x] = (color.R, color.G, color.B, color.A);
                    }
                }
                return (pixels, bakedWidth, bakedHeight);
            }
            finally
            {
                UnloadImage(image);
            }
        }
        finally
        {
            global::RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom = 1f;
            Gum.GumService.Default.Root.Children.Clear();
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
    public void Draw_OutlineOfANineSliceWithTransparentCorners_FollowsItsAlpha(
        float width, float height, bool isTiling, float borderScale, float thickness, float zoom)
    {
        Result<RaylibShader, ShaderError[]> conversion = RaylibConverter.ConvertSlang(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Outline.slang")), new RaylibConvertOptions(), TestContext.Current.CancellationToken);
        conversion.IsSuccess.ShouldBeTrue(
            conversion.IsFailure ? string.Join("\n", conversion.Error.Select(e => e.Message)) : "");

        Texture2D texture = CreateFrameTexture();
        Shader outline = LoadShaderFromMemory(null, conversion.Value.FragmentShader);
        try
        {
            var plain = Render(texture, null, width, height, isTiling, borderScale, thickness, zoom);
            var outlined = Render(texture, outline, width, height, isTiling, borderScale, thickness, zoom);

            outlined.Width.ShouldBe(plain.Width);
            outlined.Height.ShouldBe(plain.Height);

            // The plain picture is a real nine-slice: a whole rectangle minus the transparent corner
            // texels (3 per corner, each borderScale * zoom pixels square).
            if (width % 1 == 0 && height % 1 == 0)
            {
                float cutTexel = borderScale * zoom;
                int expectedContent = (int)(width * zoom * height * zoom) - (int)(12 * cutTexel * cutTexel);
                NineSliceOutlineOracle.CountContent(plain.Pixels).ShouldBe(expectedContent);
            }

            int reach = (int)(thickness * zoom);
            NineSliceOutlineOracle.Compare(plain.Pixels, outlined.Pixels, plain.Width, plain.Height, reach, (0, 128, 255)).ShouldBeEmpty();
        }
        finally
        {
            UnloadShader(outline);
            UnloadTexture(texture);
        }
    }
}
