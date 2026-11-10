using RaylibGum.Tests.Helpers;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// A sprite's tint alpha fades every color operation linearly over an opaque backdrop. The texture is
/// opaque gray (40,40,40) and the tint is (255,128,0); expected values are the straight-alpha blend
/// <c>backdrop * (1 - a) + source * a</c>, where source is tex * tint (Modulate), tint
/// (ColorTextureAlpha) or clamp(tex + tint) (Add). #7837: the Alpha property fades Modulate and
/// ColorTextureAlpha the same way, matching MonoGame's SpriteAlphaTests and Skia.
/// </summary>
public class SpriteColorOperationAlphaTests : BaseTestClass
{
    private static Color DrawOverBackdrop(ColorOperation colorOperation, byte alpha, Color backdrop)
    {
        Image image = GenImageColor(4, 4, new Color((byte)40, (byte)40, (byte)40, (byte)255));
        Texture2D texture = LoadTextureFromImage(image);
        UnloadImage(image);

        try
        {
            PixelGrid pixels = RaylibPixelHarness.Render(32, 32, backdrop, container =>
            {
                SpriteRuntime sprite = new();
                sprite.WidthUnits = DimensionUnitType.Absolute;
                sprite.HeightUnits = DimensionUnitType.Absolute;
                sprite.Width = 32;
                sprite.Height = 32;
                sprite.Texture = texture;
                sprite.Color = new Color((byte)255, (byte)128, (byte)0, alpha);
                sprite.ColorOperation = colorOperation;
                container.Children.Add(sprite);
            });
            return pixels[16, 16];
        }
        finally
        {
            UnloadTexture(texture);
        }
    }

    [Theory]
    [InlineData(255, 0, 0, 0, 255, 168, 40)]
    [InlineData(128, 0, 0, 0, 128, 84, 20)]
    [InlineData(64, 0, 0, 0, 64, 42, 10)]
    [InlineData(255, 70, 70, 90, 255, 168, 40)]
    [InlineData(128, 70, 70, 90, 163, 119, 65)]
    [InlineData(64, 70, 70, 90, 116, 95, 77)]
    public void Draw_Add_FadesByTintAlpha(byte alpha, byte backR, byte backG, byte backB,
        byte expectedR, byte expectedG, byte expectedB)
    {
        Color center = DrawOverBackdrop(ColorOperation.Add, alpha, new Color(backR, backG, backB, (byte)255));

        AssertColor(center, expectedR, expectedG, expectedB);
    }

    [Theory]
    [InlineData(128, 0, 0, 0, 20, 10, 0)]
    [InlineData(128, 70, 70, 90, 55, 45, 45)]
    public void Draw_Modulate_FadesByTintAlpha(byte alpha, byte backR, byte backG, byte backB,
        byte expectedR, byte expectedG, byte expectedB)
    {
        Color center = DrawOverBackdrop(ColorOperation.Modulate, alpha, new Color(backR, backG, backB, (byte)255));

        AssertColor(center, expectedR, expectedG, expectedB);
    }

    [Theory]
    [InlineData(128, 0, 0, 0, 128, 64, 0)]
    [InlineData(128, 70, 70, 90, 163, 99, 45)]
    public void Draw_ColorTextureAlpha_FadesByTintAlpha(byte alpha, byte backR, byte backG, byte backB,
        byte expectedR, byte expectedG, byte expectedB)
    {
        Color center = DrawOverBackdrop(ColorOperation.ColorTextureAlpha, alpha, new Color(backR, backG, backB, (byte)255));

        AssertColor(center, expectedR, expectedG, expectedB);
    }

    [Fact]
    public void Draw_AddWithWrap_AddsTint()
    {
        // The tiled draw path takes the same Add math as the single-quad path.
        Image image = GenImageColor(4, 4, new Color((byte)40, (byte)40, (byte)40, (byte)255));
        Texture2D texture = LoadTextureFromImage(image);
        UnloadImage(image);

        try
        {
            PixelGrid pixels = RaylibPixelHarness.Render(32, 32, new Color((byte)70, (byte)70, (byte)90, (byte)255), container =>
            {
                SpriteRuntime sprite = new();
                sprite.WidthUnits = DimensionUnitType.Absolute;
                sprite.HeightUnits = DimensionUnitType.Absolute;
                sprite.Width = 32;
                sprite.Height = 32;
                sprite.Texture = texture;
                sprite.TextureAddress = TextureAddress.Custom;
                sprite.TextureLeft = 0;
                sprite.TextureTop = 0;
                sprite.TextureWidth = 8;
                sprite.TextureHeight = 8;
                sprite.SetProperty("Wrap", true);
                sprite.Color = new Color((byte)255, (byte)128, (byte)0, (byte)128);
                sprite.ColorOperation = ColorOperation.Add;
                container.Children.Add(sprite);
            });

            AssertColor(pixels[16, 16], 163, 119, 65);
        }
        finally
        {
            UnloadTexture(texture);
        }
    }

    [Theory]
    [InlineData(255, 255, 168, 40)]
    [InlineData(128, 163, 119, 65)]
    public void Draw_NineSliceAdd_FadesByTintAlpha(byte alpha, byte expectedR, byte expectedG, byte expectedB)
    {
        Image image = GenImageColor(12, 12, new Color((byte)40, (byte)40, (byte)40, (byte)255));
        Texture2D texture = LoadTextureFromImage(image);
        UnloadImage(image);

        try
        {
            PixelGrid pixels = RaylibPixelHarness.Render(32, 32, new Color((byte)70, (byte)70, (byte)90, (byte)255), container =>
            {
                NineSliceRuntime nineSlice = new();
                nineSlice.WidthUnits = DimensionUnitType.Absolute;
                nineSlice.HeightUnits = DimensionUnitType.Absolute;
                nineSlice.Width = 32;
                nineSlice.Height = 32;
                nineSlice.Texture = texture;
                nineSlice.Color = new Color((byte)255, (byte)128, (byte)0, alpha);
                nineSlice.ColorOperation = ColorOperation.Add;
                container.Children.Add(nineSlice);
            });

            AssertColor(pixels[16, 16], expectedR, expectedG, expectedB);
        }
        finally
        {
            UnloadTexture(texture);
        }
    }

    [Theory]
    [InlineData(ColorOperation.Modulate)]
    [InlineData(ColorOperation.ColorTextureAlpha)]
    public void HalfAlphaTintedSprite_OverBlack_DrawsTintAtHalfBrightness(ColorOperation colorOperation)
    {
        Image image = GenImageColor(4, 4, new Color((byte)255, (byte)255, (byte)255, (byte)255));
        Texture2D texture = LoadTextureFromImage(image);
        UnloadImage(image);

        try
        {
            PixelGrid pixels = RaylibPixelHarness.Render(32, 32, new Color((byte)0, (byte)0, (byte)0, (byte)255), container =>
            {
                SpriteRuntime sprite = new();
                sprite.WidthUnits = DimensionUnitType.Absolute;
                sprite.HeightUnits = DimensionUnitType.Absolute;
                sprite.Width = 32;
                sprite.Height = 32;
                sprite.Texture = texture;
                sprite.Color = new Color((byte)255, (byte)128, (byte)0, (byte)255);
                sprite.Alpha = 128;
                sprite.ColorOperation = colorOperation;
                container.Children.Add(sprite);
            });

            Color center = pixels[16, 16];
            ((int)center.R).ShouldBeInRange(126, 130, $"{colorOperation}: {center}");
            ((int)center.G).ShouldBeInRange(62, 66, $"{colorOperation}: {center}");
            ((int)center.B).ShouldBeInRange(0, 2, $"{colorOperation}: {center}");
        }
        finally
        {
            UnloadTexture(texture);
        }
    }

    private static void AssertColor(Color actual, byte r, byte g, byte b)
    {
        bool matches = Math.Abs(actual.R - r) <= 3 && Math.Abs(actual.G - g) <= 3 && Math.Abs(actual.B - b) <= 3;
        matches.ShouldBeTrue($"Expected ({r},{g},{b}) +-3 but drew ({actual.R},{actual.G},{actual.B})");
    }
}
