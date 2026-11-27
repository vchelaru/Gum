using RenderingLibrary;
using Shouldly;
using SkiaGum.Content.Fonts;
using SkiaSharp;
using System.IO;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A font with no bold or italic file draws bold and italic synthetically, as the KernSmith-baked atlases do on
/// MonoGame and raylib (#8480); a family with real bold and italic files draws those files unchanged.
/// </summary>
public class TextSyntheticStyleTests
{
    private const string SingleFileFamily = "SyntheticStyleTests_Liberation";
    private const string RealStylesFamily = "SyntheticStyleTests_Nunito";
    private const string NunitoBoldAsRegularFamily = "SyntheticStyleTests_NunitoBoldAsRegular";
    private const string NunitoItalicAsRegularFamily = "SyntheticStyleTests_NunitoItalicAsRegular";
    private const string DisplayedText = "Dragon Armor";

    static TextSyntheticStyleTests()
    {
        GumFontMapper.RegisterFont(SingleFileFamily, ReadFont("LiberationSans-Regular.ttf"));
        GumFontMapper.RegisterFont(RealStylesFamily, ReadFont("Nunito-Regular.ttf"));
        GumFontMapper.RegisterFont(RealStylesFamily, ReadFont("Nunito-Bold.ttf"), "Bold");
        GumFontMapper.RegisterFont(RealStylesFamily, ReadFont("Nunito-Italic.ttf"), "Italic");
        GumFontMapper.RegisterFont(NunitoBoldAsRegularFamily, ReadFont("Nunito-Bold.ttf"));
        GumFontMapper.RegisterFont(NunitoItalicAsRegularFamily, ReadFont("Nunito-Italic.ttf"));
    }

    private static byte[] ReadFont(string fileName) =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", fileName));

    private static Text CreateText(string family, bool isBold, bool isItalic)
    {
        Text text = new Text();
        text.FontName = family;
        text.FontSize = 14;
        text.BoldWeight = isBold ? 1.5f : 1;
        text.IsItalic = isItalic;
        text.RawText = DisplayedText;
        text.Width = 200;
        text.Height = 40;
        return text;
    }

    private static SKBitmap Render(Text text)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(220, 40));
        SystemManagers managers = new SystemManagers { Canvas = surface.Canvas };
        surface.Canvas.Clear(SKColors.Transparent);
        text.Render(managers);
        using SKImage image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    private static long Ink(SKBitmap bitmap)
    {
        long total = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                total += bitmap.GetPixel(x, y).Alpha;
            }
        }
        return total;
    }

    private static int RightmostInkColumn(SKBitmap bitmap)
    {
        for (int x = bitmap.Width - 1; x >= 0; x--)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                if (bitmap.GetPixel(x, y).Alpha > 0)
                {
                    return x;
                }
            }
        }
        return -1;
    }

    [Fact]
    public void Render_BoldOnSingleFileFont_DrawsMoreInkThanRegular()
    {
        using SKBitmap regular = Render(CreateText(SingleFileFamily, isBold: false, isItalic: false));
        using SKBitmap bold = Render(CreateText(SingleFileFamily, isBold: true, isItalic: false));

        Ink(bold).ShouldBeGreaterThan(Ink(regular) * 11 / 10);
    }

    [Fact]
    public void Render_ItalicOnSingleFileFont_SlantsTheGlyphs()
    {
        using SKBitmap regular = Render(CreateText(SingleFileFamily, isBold: false, isItalic: false));
        using SKBitmap italic = Render(CreateText(SingleFileFamily, isBold: false, isItalic: true));

        // A slant to the right moves the tops of the last glyph past where the upright text ends.
        RightmostInkColumn(italic).ShouldBeGreaterThan(RightmostInkColumn(regular));
    }

    [Fact]
    public void Render_BoldOnSingleFileFont_KeepsLineBreaksAndInkInsideTheMeasuredWidth()
    {
        Text regular = CreateText(SingleFileFamily, isBold: false, isItalic: false);
        Text bold = CreateText(SingleFileFamily, isBold: true, isItalic: false);

        using SKBitmap boldBitmap = Render(bold);

        bold.GetCachedTextBlock().MeasuredWidth.ShouldBe(regular.GetCachedTextBlock().MeasuredWidth);
        // Synthetic bold grows each glyph by well under a pixel at 14px, so the ink still ends at the measured edge.
        RightmostInkColumn(boldBitmap).ShouldBeLessThanOrEqualTo((int)Math.Ceiling(bold.GetCachedTextBlock().MeasuredWidth) + 1);
    }

    [Fact]
    public void Render_BoldOnFamilyWithBoldFile_DrawsTheBoldFileWithoutExtraWeight()
    {
        using SKBitmap bold = Render(CreateText(RealStylesFamily, isBold: true, isItalic: false));
        using SKBitmap boldFileAtRegularWeight = Render(CreateText(NunitoBoldAsRegularFamily, isBold: false, isItalic: false));

        bold.Bytes.SequenceEqual(boldFileAtRegularWeight.Bytes).ShouldBeTrue();
    }

    [Fact]
    public void Render_ItalicOnFamilyWithItalicFile_DrawsTheItalicFileWithoutExtraSlant()
    {
        using SKBitmap italic = Render(CreateText(RealStylesFamily, isBold: false, isItalic: true));
        using SKBitmap italicFileUpright = Render(CreateText(NunitoItalicAsRegularFamily, isBold: false, isItalic: false));

        italic.Bytes.SequenceEqual(italicFileUpright.Bytes).ShouldBeTrue();
    }
}
