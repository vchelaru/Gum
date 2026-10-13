using GumTestSupport;
using Gum.ProjectServices.FontGeneration;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using ToolsUtilities;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// A font file generates the same bitmap font whichever container it arrives in: the tool accepts
/// .woff and .ttc paths exactly as it does a .ttf. The containers are built from the repository's
/// test font so nothing binary is committed for them.
/// </summary>
public class KernSmithFileGeneratorFontFormatTests : IDisposable
{
    private readonly string _outputDirectory;

    public KernSmithFileGeneratorFontFormatTests()
    {
        _outputDirectory = Path.Combine(Path.GetTempPath(), "GumKernSmithFormats_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_outputDirectory);
    }

    public void Dispose()
    {
        Directory.Delete(_outputDirectory, recursive: true);
    }

    [Theory]
    [InlineData(".ttf")]
    [InlineData(".woff")]
    [InlineData(".ttc")]
    public async Task GenerateFont_FromAFontFileOfAnyContainer_RasterizesTheRequestedGlyphs(string extension)
    {
        byte[] ttf = File.ReadAllBytes(RepoPaths.TestFontFile);
        byte[] contents = extension switch
        {
            ".woff" => FontContainerTestWriter.ToWoff(ttf),
            ".ttc" => FontContainerTestWriter.ToTtc(ttf),
            _ => ttf,
        };
        string fontFile = Path.Combine(_outputDirectory, "Container" + extension);
        File.WriteAllBytes(fontFile, contents);
        KernSmithFileGenerator generator = new KernSmithFileGenerator();
        BmfcSave bmfcSave = new BmfcSave { FontName = "Container", FontFile = fontFile, FontSize = 18, Ranges = "65-70" };
        string outputFnt = Path.Combine(_outputDirectory, "Font18Container" + extension.TrimStart('.') + ".fnt");

        GeneralResponse response = await generator.GenerateFont(bmfcSave, outputFnt, createTask: false);

        response.Succeeded.ShouldBeTrue(response.Message);
        string fnt = File.ReadAllText(outputFnt);
        // 'A'..'F' (65-70) are the requested range; every one must have been rasterized.
        foreach (int id in new[] { 65, 66, 67, 68, 69, 70 })
        {
            fnt.ShouldContain($"char id={id} ");
        }
    }
}
