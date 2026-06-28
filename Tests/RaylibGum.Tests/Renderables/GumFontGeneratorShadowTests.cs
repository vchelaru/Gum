using KernSmith;
using KernSmith.Atlas;
using KernSmith.Gum;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// Spike tests for #2724 — KernSmith baked drop shadow plumbed through <see cref="GumFontGenerator"/>.
/// Validates option mapping, color decomposition, and that generation produces a visibly larger glyph footprint.
/// </summary>
public class GumFontGeneratorShadowTests
{
    [Fact]
    public void BuildOptions_WhenHasDropshadow_MapsShadowFieldsAndDecomposesAlphaToOpacity()
    {
        BmfcSave bmfcSave = BaseBmfcSave();
        bmfcSave.HasDropshadow = true;
        bmfcSave.DropshadowOffsetX = 2f;
        bmfcSave.DropshadowOffsetY = 3f;
        bmfcSave.DropshadowBlur = 4f;
        bmfcSave.DropshadowRed = 10;
        bmfcSave.DropshadowGreen = 20;
        bmfcSave.DropshadowBlue = 30;
        bmfcSave.DropshadowAlpha = 128;

        FontGeneratorOptions options = GumFontGenerator.BuildOptions(bmfcSave);

        options.ShadowOffsetX.ShouldBe(2);
        options.ShadowOffsetY.ShouldBe(3);
        options.ShadowBlur.ShouldBe(4);
        ((int)options.ShadowR).ShouldBe(10);
        ((int)options.ShadowG).ShouldBe(20);
        ((int)options.ShadowB).ShouldBe(30);
        options.ShadowOpacity.ShouldBe(128 / 255f, 0.001f);
    }

    [Fact]
    public void BuildOptions_WhenHasDropshadowIsFalse_LeavesShadowAtDefaults()
    {
        BmfcSave bmfcSave = BaseBmfcSave();
        bmfcSave.HasDropshadow = false;
        bmfcSave.DropshadowOffsetX = 5f;
        bmfcSave.DropshadowBlur = 9f;

        FontGeneratorOptions plain = GumFontGenerator.BuildOptions(bmfcSave);
        FontGeneratorOptions reference = GumFontGenerator.BuildOptions(BaseBmfcSave());

        plain.ShadowOffsetX.ShouldBe(reference.ShadowOffsetX);
        plain.ShadowOffsetY.ShouldBe(reference.ShadowOffsetY);
        plain.ShadowBlur.ShouldBe(reference.ShadowBlur);
        plain.ShadowOpacity.ShouldBe(reference.ShadowOpacity);
    }

    [Fact]
    public void Generate_WithDropshadow_ProducesLargerGlyphFootprintThanPlainFont()
    {
        BmfcSave plain = BaseBmfcSave();
        plain.Ranges = "65";

        BmfcSave shadowed = BaseBmfcSave();
        shadowed.Ranges = "65";
        shadowed.HasDropshadow = true;
        shadowed.DropshadowOffsetX = 2f;
        shadowed.DropshadowOffsetY = 2f;
        shadowed.DropshadowBlur = 2f;
        shadowed.DropshadowRed = 0;
        shadowed.DropshadowGreen = 0;
        shadowed.DropshadowBlue = 0;
        shadowed.DropshadowAlpha = 255;

        int plainAlpha = CountNonZeroAlphaPixels(GumFontGenerator.Generate(plain));
        int shadowAlpha = CountNonZeroAlphaPixels(GumFontGenerator.Generate(shadowed));

        shadowAlpha.ShouldBeGreaterThan(plainAlpha);
    }

    [Fact]
    public void FontCacheFileName_WhenHasDropshadow_IncludesShadowSignature()
    {
        BmfcSave bmfcSave = BaseBmfcSave();
        string plainName = bmfcSave.FontCacheFileName;

        bmfcSave.HasDropshadow = true;
        bmfcSave.DropshadowOffsetX = 1f;
        bmfcSave.DropshadowOffsetY = 2f;
        bmfcSave.DropshadowBlur = 3f;
        bmfcSave.DropshadowRed = 4;
        bmfcSave.DropshadowGreen = 5;
        bmfcSave.DropshadowBlue = 6;
        bmfcSave.DropshadowAlpha = 7;

        bmfcSave.FontCacheFileName.ShouldNotBe(plainName);
        bmfcSave.FontCacheFileName.ShouldContain("_ds");
    }

    [Fact]
    public void BuildOptions_WhenHasDropshadow_LeavesChannelsUnsetSoAtlasPreservesShadowRgb()
    {
        BmfcSave bmfcSave = BaseBmfcSave();
        bmfcSave.HasDropshadow = true;

        FontGeneratorOptions options = GumFontGenerator.BuildOptions(bmfcSave);

        options.Channels.ShouldBeNull();
    }

    [Fact]
    public void BuildOptions_WhenHasDropshadowWithOutline_LeavesChannelsUnsetSoAtlasPreservesShadowRgb()
    {
        BmfcSave bmfcSave = BaseBmfcSave();
        bmfcSave.OutlineThickness = 2;
        bmfcSave.HasDropshadow = true;

        FontGeneratorOptions options = GumFontGenerator.BuildOptions(bmfcSave);

        options.Channels.ShouldBeNull();
    }

    [Fact]
    public void Generate_WithBlackShadow_BakesDarkAndBrightRgbPixels()
    {
        BmfcSave plain = BaseBmfcSave();
        plain.Ranges = "65";

        BmfcSave shadowed = BaseBmfcSave();
        shadowed.Ranges = "65";
        shadowed.HasDropshadow = true;
        shadowed.DropshadowOffsetX = 2f;
        shadowed.DropshadowOffsetY = 2f;
        shadowed.DropshadowBlur = 2f;
        shadowed.DropshadowRed = 0;
        shadowed.DropshadowGreen = 0;
        shadowed.DropshadowBlue = 0;
        shadowed.DropshadowAlpha = 255;

        (int plainMinR, int plainMaxR, int plainDark, int plainBright) =
            SummarizeRedChannel(GumFontGenerator.Generate(plain));
        (int shadowMinR, int shadowMaxR, int shadowDark, int shadowBright) =
            SummarizeRedChannel(GumFontGenerator.Generate(shadowed));

        plainDark.ShouldBe(0);
        plainBright.ShouldBeGreaterThan(0);
        plainMinR.ShouldBeGreaterThan(200);

        shadowDark.ShouldBeGreaterThan(0);
        shadowBright.ShouldBeGreaterThan(0);
        shadowMinR.ShouldBeLessThan(32);
        shadowMaxR.ShouldBeGreaterThan(200);
    }

    [Fact]
    public void FontCacheFileName_WhenHasDropshadowIsFalse_MatchesPlainKey()
    {
        BmfcSave bmfcSave = BaseBmfcSave();
        string plainName = bmfcSave.FontCacheFileName;

        bmfcSave.DropshadowOffsetX = 99f;
        bmfcSave.DropshadowBlur = 99f;

        bmfcSave.FontCacheFileName.ShouldBe(plainName);
    }

    private static BmfcSave BaseBmfcSave() => new BmfcSave
    {
        FontName = "Arial",
        FontSize = 24,
        UseSmoothing = true,
        Ranges = "65",
    };

    private static (int minR, int maxR, int darkPixelCount, int brightPixelCount) SummarizeRedChannel(KernSmith.Output.BmFontResult result)
    {
        int minR = 255;
        int maxR = 0;
        int darkPixelCount = 0;
        int brightPixelCount = 0;

        foreach (AtlasPage page in result.Pages)
        {
            byte[] pixels = page.PixelData;
            for (int i = 0; i + 3 < pixels.Length; i += 4)
            {
                if (pixels[i + 3] == 0)
                {
                    continue;
                }

                byte r = pixels[i];
                minR = Math.Min(minR, r);
                maxR = Math.Max(maxR, r);

                if (r < 32)
                {
                    darkPixelCount++;
                }

                if (r > 200)
                {
                    brightPixelCount++;
                }
            }
        }

        return (minR, maxR, darkPixelCount, brightPixelCount);
    }

    private static int CountNonZeroAlphaPixels(KernSmith.Output.BmFontResult result)
    {
        int count = 0;
        foreach (AtlasPage page in result.Pages)
        {
            byte[] pixels = page.PixelData;
            for (int i = 3; i < pixels.Length; i += 4)
            {
                if (pixels[i] > 0)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
