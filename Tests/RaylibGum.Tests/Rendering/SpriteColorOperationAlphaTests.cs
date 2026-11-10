using RaylibGum.Tests.Helpers;
using Gum.DataTypes;
using Gum.GueDeriving;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// #7837: an opaque white sprite tinted (255, 128, 0) at Alpha 128 over black draws at half
/// brightness for Modulate and ColorTextureAlpha, the numbers MonoGame's SpriteAlphaTests and Skia pin.
/// </summary>
public class SpriteColorOperationAlphaTests : BaseTestClass
{
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
}
