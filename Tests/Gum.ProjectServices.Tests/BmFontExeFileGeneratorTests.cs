using Gum.ProjectServices.FontGeneration;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System;
using System.IO;

namespace Gum.ProjectServices.Tests;

public class BmFontExeFileGeneratorTests : IDisposable
{
    private readonly string _toolsDirectory =
        Path.Combine(Path.GetTempPath(), "GumBmFontTools_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_toolsDirectory))
        {
            Directory.Delete(_toolsDirectory, recursive: true);
        }
    }

    [Fact]
    public void EnsureToolsExtracted_ShouldExtractIntoToolsDirectory_NotAppBaseDirectory()
    {
        BmFontExeFileGenerator generator = new BmFontExeFileGenerator(callbacks: null, toolsDirectory: _toolsDirectory);

        generator.EnsureToolsExtracted();

        File.Exists(Path.Combine(_toolsDirectory, "Libraries", "bmfont.exe")).ShouldBeTrue();
        File.Exists(Path.Combine(_toolsDirectory, "Content", "BmfcTemplate.bmfc")).ShouldBeTrue();
    }

    [Fact]
    public void Save_ShouldReadTemplateFromGivenPath()
    {
        string templatePath = Path.Combine(_toolsDirectory, "template.bmfc");
        Directory.CreateDirectory(_toolsDirectory);
        File.WriteAllText(templatePath, "face=FontNameVariable");
        string outputPath = Path.Combine(_toolsDirectory, "out.bmfc");
        BmfcSave bmfcSave = new BmfcSave { FontName = "Arial", FontSize = 24 };

        bmfcSave.Save(outputPath, templatePath);

        File.ReadAllText(outputPath).ShouldContain("face=Arial");
    }
}
