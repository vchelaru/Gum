using GumTestSupport;
using Gum.ProjectServices.FontGeneration;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using ToolsUtilities;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// A .fnt name with dots in it (from a font file such as Pixel12x10-v1.1.0.ttf) still gets its own
/// atlas PNG named after the full .fnt name, so variants of the font never share a PNG.
/// </summary>
public class KernSmithFileGeneratorDottedNameTests : IDisposable
{
    private readonly string _outputDirectory;

    public KernSmithFileGeneratorDottedNameTests()
    {
        _outputDirectory = Path.Combine(Path.GetTempPath(), "GumKernSmithDotted_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_outputDirectory);
    }

    public void Dispose()
    {
        Directory.Delete(_outputDirectory, recursive: true);
    }

    [Fact]
    public async Task GenerateFont_WithDottedFntName_WritesEachVariantItsOwnAtlas()
    {
        KernSmithFileGenerator generator = new KernSmithFileGenerator();
        string plainFnt = Path.Combine(_outputDirectory, "Font16Pixel12x10-v1.1.0_ttf_noSmooth.fnt");
        string outlineFnt = Path.Combine(_outputDirectory, "Font16Pixel12x10-v1.1.0_ttf_o1_noSmooth.fnt");

        GeneralResponse plainResponse = await generator.GenerateFont(
            new BmfcSave { FontName = "Pixel", FontFile = RepoPaths.TestFontFile, FontSize = 16, Ranges = "65-70" },
            plainFnt, createTask: false);
        GeneralResponse outlineResponse = await generator.GenerateFont(
            new BmfcSave { FontName = "Pixel", FontFile = RepoPaths.TestFontFile, FontSize = 16, Ranges = "65-70", OutlineThickness = 1 },
            outlineFnt, createTask: false);

        plainResponse.Succeeded.ShouldBeTrue(plainResponse.Message);
        outlineResponse.Succeeded.ShouldBeTrue(outlineResponse.Message);
        File.ReadAllText(plainFnt).ShouldContain("file=\"Font16Pixel12x10-v1.1.0_ttf_noSmooth_0.png\"");
        File.ReadAllText(outlineFnt).ShouldContain("file=\"Font16Pixel12x10-v1.1.0_ttf_o1_noSmooth_0.png\"");
        File.Exists(Path.Combine(_outputDirectory, "Font16Pixel12x10-v1.1.0_ttf_noSmooth_0.png")).ShouldBeTrue();
        File.Exists(Path.Combine(_outputDirectory, "Font16Pixel12x10-v1.1.0_ttf_o1_noSmooth_0.png")).ShouldBeTrue();
        Directory.GetFiles(_outputDirectory, "*.png").Length.ShouldBe(2);
    }

    [Fact]
    public async Task GenerateFont_WithDottedFntNameAndDropshadow_ShadowFntReferencesThePrimaryAtlas()
    {
        KernSmithFileGenerator generator = new KernSmithFileGenerator();
        string fnt = Path.Combine(_outputDirectory, "Font16Pixel-v1.1.0_ttf.fnt");
        BmfcSave bmfcSave = new BmfcSave
        {
            FontName = "Pixel",
            FontFile = RepoPaths.TestFontFile,
            FontSize = 16,
            Ranges = "65-70",
            HasDropshadow = true,
            DropshadowBlur = 2,
        };

        GeneralResponse response = await generator.GenerateFont(bmfcSave, fnt, createTask: false);

        response.Succeeded.ShouldBeTrue(response.Message);
        File.ReadAllText(fnt).ShouldContain("file=\"Font16Pixel-v1.1.0_ttf_0.png\"");
        string shadowFnt = Path.Combine(_outputDirectory, "Font16Pixel-v1.1.0_ttf-shadow.fnt");
        File.ReadAllText(shadowFnt).ShouldContain("file=\"Font16Pixel-v1.1.0_ttf_0.png\"");
        File.Exists(Path.Combine(_outputDirectory, "Font16Pixel-v1.1.0_ttf_0.png")).ShouldBeTrue();
    }
}
