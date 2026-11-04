using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;
using static MonoGameGum.IntegrationTests.MonoGameGum.Rendering.AlphaModeRenderHarness;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// #7585: a NineSlice whose frame has transparent corners and half transparent edges (a rounded, antialiased
/// frame) draws the same pixels on the straight-alpha pipeline and on the premultiplied one, at stretched,
/// whole and fractional sizes, plain, tinted and at half alpha. Expected values are worked out by hand.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class PremultipliedNineSliceRenderTests : BaseTestClass
{
    public enum Sizing { Stretched, Whole, Fractional }

    private static readonly XnaColor CornerTexel = new((byte)255, (byte)0, (byte)255, (byte)0);
    private static readonly XnaColor EdgeTexel = new((byte)40, (byte)200, (byte)40, (byte)128);
    private static readonly XnaColor CenterTexel = new((byte)200, (byte)100, (byte)50, (byte)255);

    // A 9x9 texture: a 3x3 grid of cells, three texels each.
    private static XnaColor[] BuildTexels()
    {
        XnaColor[] texels = new XnaColor[81];
        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                texels[(y * 9) + x] = CellTexel(x / 3, y / 3);
            }
        }
        return texels;
    }

    private static XnaColor CellTexel(int column, int row)
    {
        bool cornerColumn = column != 1;
        bool cornerRow = row != 1;
        if (cornerColumn && cornerRow)
        {
            return CornerTexel;
        }
        return cornerColumn || cornerRow ? EdgeTexel : CenterTexel;
    }

    private readonly record struct Layout(float X, float Y, float Width, float Height, float BorderScale)
    {
        public float Border => 3 * BorderScale;
    }

    private static Layout GetLayout(Sizing sizing) => sizing switch
    {
        Sizing.Stretched => new Layout(10, 6, 60, 36, 4),
        Sizing.Whole => new Layout(10, 6, 60, 36, 1),
        _ => new Layout(10.5f, 6.5f, 60.5f, 36.5f, 4),
    };

    public override void Dispose()
    {
        Renderer.UsePremultipliedAlpha = false;
        base.Dispose();
    }

    [Theory]
    [InlineData(false, Sizing.Stretched, Variant.Plain)]
    [InlineData(false, Sizing.Stretched, Variant.Tint)]
    [InlineData(false, Sizing.Stretched, Variant.HalfAlpha)]
    [InlineData(false, Sizing.Whole, Variant.Plain)]
    [InlineData(false, Sizing.Fractional, Variant.Plain)]
    [InlineData(true, Sizing.Stretched, Variant.Plain)]
    [InlineData(true, Sizing.Stretched, Variant.Tint)]
    [InlineData(true, Sizing.Stretched, Variant.HalfAlpha)]
    [InlineData(true, Sizing.Whole, Variant.Plain)]
    [InlineData(true, Sizing.Fractional, Variant.Plain)]
    [InlineData(true, Sizing.Fractional, Variant.HalfAlpha)]
    public void NineSlice_CellCenters_MatchHandComputedComposite(bool premultiplied, Sizing sizing, Variant variant)
    {
        XnaColor[] pixels = RenderNineSlice(premultiplied, sizing, variant);
        Layout layout = GetLayout(sizing);

        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                (int x, int y) = CellCenter(layout, column, row);
                XnaColor pixel = pixels[(y * Width) + x];
                MaxChannelDifference(pixel, Expected(CellTexel(column, row), variant)).ShouldBeLessThanOrEqualTo(3,
                    $"cell ({column},{row}) at ({x},{y}), premultiplied {premultiplied}, {sizing}, {variant}: got {pixel}");
            }
        }
    }

    [Theory]
    [InlineData(Sizing.Stretched, Variant.Plain)]
    [InlineData(Sizing.Stretched, Variant.Tint)]
    [InlineData(Sizing.Stretched, Variant.HalfAlpha)]
    [InlineData(Sizing.Whole, Variant.Plain)]
    [InlineData(Sizing.Fractional, Variant.Plain)]
    public void NineSlice_Premultiplied_MatchesStraight(Sizing sizing, Variant variant)
    {
        XnaColor[] straight = RenderNineSlice(false, sizing, variant);
        XnaColor[] premultiplied = RenderNineSlice(true, sizing, variant);

        MaxChannelDifference(straight, premultiplied).ShouldBeLessThanOrEqualTo(3);
    }

    [Theory]
    [InlineData(false, Sizing.Stretched)]
    [InlineData(false, Sizing.Fractional)]
    [InlineData(true, Sizing.Stretched)]
    [InlineData(true, Sizing.Whole)]
    [InlineData(true, Sizing.Fractional)]
    public void NineSlice_TransparentCorners_LeaveNoBrightFringeOrSeam(bool premultiplied, Sizing sizing)
    {
        XnaColor[] pixels = RenderNineSlice(premultiplied, sizing, Variant.Plain);
        Layout layout = GetLayout(sizing);

        // Nothing may be brighter than the opaque center (200, 100, 50) or the backdrop: the corners'
        // magenta (255, 0, 255) must not show, alone or blended into the neighboring edge texels.
        for (int y = (int)layout.Y + 1; y < (int)(layout.Y + layout.Height) - 1; y++)
        {
            for (int x = (int)layout.X + 1; x < (int)(layout.X + layout.Width) - 1; x++)
            {
                XnaColor p = pixels[(y * Width) + x];
                p.R.ShouldBeLessThanOrEqualTo((byte)203, $"({x},{y}) {p}");
                p.B.ShouldBeLessThanOrEqualTo((byte)93, $"({x},{y}) {p}");
            }
        }

        // A seam between slices would let the backdrop through the opaque center: a row across the
        // middle of the center cell must stay the center color all along.
        (int _, int centerY) = CellCenter(layout, 1, 1);
        for (int x = (int)(layout.X + layout.Border) + 2; x < (int)(layout.X + layout.Width - layout.Border) - 2; x++)
        {
            MaxChannelDifference(pixels[(centerY * Width) + x], Expected(CenterTexel, Variant.Plain))
                .ShouldBeLessThanOrEqualTo(3, $"center row at x {x}");
        }
    }

    private static (int X, int Y) CellCenter(Layout layout, int column, int row)
    {
        float left = column == 0 ? layout.X : column == 1 ? layout.X + layout.Border : layout.X + layout.Width - layout.Border;
        float right = column == 0 ? layout.X + layout.Border : column == 1 ? layout.X + layout.Width - layout.Border : layout.X + layout.Width;
        float top = row == 0 ? layout.Y : row == 1 ? layout.Y + layout.Border : layout.Y + layout.Height - layout.Border;
        float bottom = row == 0 ? layout.Y + layout.Border : row == 1 ? layout.Y + layout.Height - layout.Border : layout.Y + layout.Height;
        return ((int)((left + right) / 2), (int)((top + bottom) / 2));
    }

    private static XnaColor[] RenderNineSlice(bool premultiplied, Sizing sizing, Variant variant)
    {
        Layout layout = GetLayout(sizing);
        return Render(premultiplied, 9, 9, BuildTexels(), texture =>
        {
            NineSliceRuntime nineSlice = new();
            nineSlice.Texture = texture;
            nineSlice.WidthUnits = DimensionUnitType.Absolute;
            nineSlice.HeightUnits = DimensionUnitType.Absolute;
            nineSlice.Width = layout.Width;
            nineSlice.Height = layout.Height;
            nineSlice.X = layout.X;
            nineSlice.Y = layout.Y;
            nineSlice.BorderScale = layout.BorderScale;
            Apply(variant, c => nineSlice.Color = c, a => nineSlice.Alpha = a);
            return nineSlice;
        });
    }
}
