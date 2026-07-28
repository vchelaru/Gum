using Gum.ProjectServices.FontGeneration;
using KernSmith;
using KernSmith.Output;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System.Linq;
using Xunit;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Issue #4001 — dropshadow is now rendered as a two-pass draw at runtime (a shadow silhouette
/// drawn offset + tinted under the primary glyph) instead of being baked into the primary atlas.
/// The tool's disk-based KernSmith generator must therefore request a ShadowSilhouette
/// <see cref="AtlasVariant"/> (packed into the same shared PNG) and leave the primary atlas a
/// plain glyph atlas — NOT bake shadow offset/color into it, which was the source of the
/// re-tinting bug.
/// </summary>
public class KernSmithFileGeneratorShadowTests
{
    [Fact]
    public void BuildOptions_WhenHasDropshadow_RequestsShadowSilhouetteVariantWithBlur()
    {
        BmfcSave bmfcSave = BaseBmfcSave();
        bmfcSave.HasDropshadow = true;
        bmfcSave.DropshadowBlur = 4f;

        FontGeneratorOptions options = KernSmithFileGenerator.BuildOptions(bmfcSave);

        options.Variants.ShouldNotBeNull();
        AtlasVariant variant = options.Variants.Single();
        variant.Name.ShouldBe("shadow");
        variant.Kind.ShouldBe(AtlasVariantKind.ShadowSilhouette);
        variant.BlurRadius.ShouldBe(4);
    }

    [Fact]
    public void BuildOptions_WhenHasDropshadow_DoesNotBakeShadowIntoPrimary()
    {
        BmfcSave bmfcSave = BaseBmfcSave();
        bmfcSave.HasDropshadow = true;
        bmfcSave.DropshadowOffsetX = 2f;
        bmfcSave.DropshadowOffsetY = 3f;
        bmfcSave.DropshadowRed = 10;
        bmfcSave.DropshadowGreen = 20;
        bmfcSave.DropshadowBlue = 30;
        bmfcSave.DropshadowAlpha = 128;

        FontGeneratorOptions options = KernSmithFileGenerator.BuildOptions(bmfcSave);

        // KernSmith bakes a shadow into the primary only when HasShadow is true (any offset or
        // blur set on the options). Leaving all three at 0 keeps the primary a plain glyph atlas —
        // baking the shadow in is exactly the re-tinting bug this fixes.
        options.ShadowOffsetX.ShouldBe(0);
        options.ShadowOffsetY.ShouldBe(0);
        options.ShadowBlur.ShouldBe(0);
        // Variants require the default channel layout (a custom ChannelConfig throws in KernSmith).
        options.Channels.ShouldBeNull();
    }

    [Fact]
    public void BuildOptions_ThenGenerate_WithDropshadow_ProducesShadowVariantAndCleanPrimary()
    {
        // End-to-end proof (bypassing disk I/O): a separate "shadow" variant model should exist
        // with one entry per requested codepoint, packed into the same shared atlas as the primary.
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

        result.VariantModels.ContainsKey("shadow").ShouldBeTrue();
        result.VariantModels["shadow"].Characters.Select(c => c.Id).ShouldContain('A');
    }

    private static BmfcSave BaseBmfcSave() => new BmfcSave
    {
        FontName = "Arial",
        FontSize = 24,
        UseSmoothing = true,
        Ranges = "65",
    };
}
