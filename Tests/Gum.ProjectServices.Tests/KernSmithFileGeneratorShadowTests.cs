using Gum.ProjectServices.FontGeneration;
using KernSmith;
using KernSmith.Atlas;
using KernSmith.Output;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System;
using Xunit;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Issue #4005 — the tool's own disk-based KernSmith generator (used by
/// HeadlessFontGenerationService/FontFileGeneratorSelector when a project's FontGenerator is
/// KernSmith) has its own private BuildOptions, duplicated from (and independent of)
/// KernSmith.Gum.GumFontGenerator.BuildOptions. That copy never mapped BmfcSave's dropshadow
/// fields onto FontGeneratorOptions at all, so toggling "Has Dropshadow" in the tool regenerated
/// a font file (the cache key changed) but the file never actually had a shadow baked into it.
/// </summary>
public class KernSmithFileGeneratorShadowTests
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

        FontGeneratorOptions options = KernSmithFileGenerator.BuildOptions(bmfcSave);

        options.ShadowOffsetX.ShouldBe(2);
        options.ShadowOffsetY.ShouldBe(3);
        options.ShadowBlur.ShouldBe(4);
        ((int)options.ShadowR).ShouldBe(10);
        ((int)options.ShadowG).ShouldBe(20);
        ((int)options.ShadowB).ShouldBe(30);
        options.ShadowOpacity.ShouldBe(128 / 255f, 0.001f);
    }

    [Fact]
    public void BuildOptions_WhenHasDropshadow_LeavesChannelsUnsetSoAtlasPreservesShadowRgb()
    {
        BmfcSave bmfcSave = BaseBmfcSave();
        bmfcSave.HasDropshadow = true;

        FontGeneratorOptions options = KernSmithFileGenerator.BuildOptions(bmfcSave);

        options.Channels.ShouldBeNull();
    }

    [Fact]
    public void BuildOptions_ThenGenerate_WithDropshadow_BakesDarkPixelsIntoTheAtlas()
    {
        // Shortcut end-to-end proof (bypassing disk I/O and the tool entirely): feed the tool's own
        // BuildOptions output straight into KernSmith's generator and confirm the in-memory atlas
        // actually contains dark (shadow) pixels rather than only white glyph pixels.
        BmfcSave bmfcSave = BaseBmfcSave();
        bmfcSave.Ranges = "65";
        bmfcSave.HasDropshadow = true;
        bmfcSave.DropshadowOffsetX = 2f;
        bmfcSave.DropshadowOffsetY = 2f;
        bmfcSave.DropshadowBlur = 2f;
        bmfcSave.DropshadowRed = 0;
        bmfcSave.DropshadowGreen = 0;
        bmfcSave.DropshadowBlue = 0;
        bmfcSave.DropshadowAlpha = 255;

        FontGeneratorOptions options = KernSmithFileGenerator.BuildOptions(bmfcSave);
        BmFontResult result = BmFont.GenerateFromSystem(bmfcSave.FontName, options);

        (int minR, int darkPixelCount, int brightPixelCount) = SummarizeRedChannel(result);

        darkPixelCount.ShouldBeGreaterThan(0, "the atlas should contain dark shadow pixels, not just white glyph pixels");
        brightPixelCount.ShouldBeGreaterThan(0, "the glyph itself should still be present");
        minR.ShouldBeLessThan(32);
    }

    private static BmfcSave BaseBmfcSave() => new BmfcSave
    {
        FontName = "Arial",
        FontSize = 24,
        UseSmoothing = true,
        Ranges = "65",
    };

    private static (int minR, int darkPixelCount, int brightPixelCount) SummarizeRedChannel(BmFontResult result)
    {
        int minR = 255;
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

        return (minR, darkPixelCount, brightPixelCount);
    }
}
