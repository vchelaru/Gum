using System.Linq;
using KernSmith;
using KernSmith.Gum;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// Issue #4001 — dropshadow renders as a two-pass draw at runtime (a ShadowSilhouette variant
/// drawn offset + tinted under the primary glyph), not baked into the primary atlas. The in-memory
/// <see cref="GumFontGenerator"/> must request that variant and leave the primary a plain glyph
/// atlas — baking the shadow in was what let the runtime text-color modulate re-tint the shadow.
/// </summary>
public class GumFontGeneratorShadowTests
{
    [Fact]
    public void BuildOptions_WhenHasDropshadow_RequestsShadowSilhouetteVariantWithBlur()
    {
        BmfcSave bmfcSave = BaseBmfcSave();
        bmfcSave.HasDropshadow = true;
        bmfcSave.DropshadowBlur = 4f;

        FontGeneratorOptions options = GumFontGenerator.BuildOptions(bmfcSave);

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

        FontGeneratorOptions options = GumFontGenerator.BuildOptions(bmfcSave);

        options.ShadowOffsetX.ShouldBe(0);
        options.ShadowOffsetY.ShouldBe(0);
        options.ShadowBlur.ShouldBe(0);
        // Variants require the default channel layout (a custom ChannelConfig throws in KernSmith).
        options.Channels.ShouldBeNull();
    }

    [Fact]
    public void BuildOptions_WhenHasDropshadowWithOutline_LeavesChannelsDefaultForVariant()
    {
        BmfcSave bmfcSave = BaseBmfcSave();
        bmfcSave.OutlineThickness = 2;
        bmfcSave.HasDropshadow = true;

        FontGeneratorOptions options = GumFontGenerator.BuildOptions(bmfcSave);

        options.Channels.ShouldBeNull();
    }

    [Fact]
    public void BuildOptions_WhenHasDropshadowIsFalse_LeavesShadowAndVariantsAtDefaults()
    {
        BmfcSave bmfcSave = BaseBmfcSave();
        bmfcSave.HasDropshadow = false;
        bmfcSave.DropshadowOffsetX = 5f;
        bmfcSave.DropshadowBlur = 9f;

        FontGeneratorOptions options = GumFontGenerator.BuildOptions(bmfcSave);

        options.ShadowOffsetX.ShouldBe(0);
        options.ShadowBlur.ShouldBe(0);
        (options.Variants is null || options.Variants.Count == 0).ShouldBeTrue();
    }

    [Fact]
    public void Generate_WithDropshadow_ProducesShadowVariantSharingThePrimaryAtlas()
    {
        BmfcSave shadowed = BaseBmfcSave();
        shadowed.Ranges = "65";
        shadowed.HasDropshadow = true;
        shadowed.DropshadowOffsetX = 2f;
        shadowed.DropshadowOffsetY = 2f;
        shadowed.DropshadowBlur = 2f;
        shadowed.DropshadowAlpha = 255;

        KernSmith.Output.BmFontResult result = GumFontGenerator.Generate(shadowed);

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
