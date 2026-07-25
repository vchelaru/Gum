using Gum.ProjectServices.FontGeneration;
using KernSmith;
using KernSmith.Output;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Issue #4019 — Gum's size-estimation heuristic (built for bmfont.exe, which cannot autofit)
/// doesn't account for dropshadow inflation and can undersize the atlas it hands to KernSmith,
/// forcing a single font to spill across multiple atlas pages. KernSmith can size its own atlas
/// via AutofitTexture, so BuildOptions should use that instead of trusting the heuristic's guess.
/// </summary>
public class KernSmithFileGeneratorSizeTests
{
    [Fact]
    public void BuildOptions_ShouldEnableAutofitTexture_RegardlessOfBmfcOutputDimensions()
    {
        BmfcSave bmfcSave = new BmfcSave
        {
            FontName = "Arial",
            FontSize = 48,
            Ranges = "32-126",
            // Mirrors the issue's undersized heuristic guess for a 48px font with a large dropshadow.
            OutputWidth = 1024,
            OutputHeight = 256,
        };

        FontGeneratorOptions options = KernSmithFileGenerator.BuildOptions(bmfcSave);

        options.AutofitTexture.ShouldBeTrue();
        options.MaxTextureWidth.ShouldBe(8192);
        options.MaxTextureHeight.ShouldBe(8192);
    }

    [Fact]
    public void BuildOptions_ThenGenerate_WithLargeDropshadow_ProducesSinglePage()
    {
        // The issue's exact repro: Font48Arial_ds31_-13_6_0_0_0_255.bmfc.
        BmfcSave bmfcSave = new BmfcSave
        {
            FontName = "Arial",
            FontSize = 48,
            Ranges = "32-126,160-255",
            HasDropshadow = true,
            DropshadowOffsetX = 31f,
            DropshadowOffsetY = -13f,
            DropshadowBlur = 6f,
            DropshadowAlpha = 255,
            // The undersized heuristic guess that produced the multi-page split in the issue.
            OutputWidth = 1024,
            OutputHeight = 256,
        };

        FontGeneratorOptions options = KernSmithFileGenerator.BuildOptions(bmfcSave);
        BmFontResult result = BmFont.GenerateFromSystem(bmfcSave.FontName, options);

        result.Pages.Count.ShouldBe(1);
    }
}
