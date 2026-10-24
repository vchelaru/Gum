using Gum.ProjectServices.FontGeneration;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System;
using System.IO;
using System.Reflection;

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
    public void IsEmbedded_ShouldBeFalse_WhenAssemblyLacksBmFontResource()
    {
        // The test assembly never embeds bmfont.exe, like a ProjectServices published for macOS/Linux (#5457).
        BmFontExeFileGenerator.IsEmbedded(typeof(BmFontExeFileGeneratorTests).Assembly).ShouldBeFalse();
    }

    [Fact]
    public void IsEmbedded_ShouldBeTrue_WhenAssemblyHasBmFontResource()
    {
        Assembly projectServices = typeof(BmFontExeFileGenerator).Assembly;

        BmFontExeFileGenerator.IsEmbedded(projectServices).ShouldBeTrue();
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

    // The .bmfc sits in the project's FontCache, so the machine's absolute path never goes into it (#6722).
    [Fact]
    public void WriteBmfc_ShouldWriteFontFileRelativeToTheBmfc()
    {
        string templatePath = Path.Combine(_toolsDirectory, "template.bmfc");
        Directory.CreateDirectory(_toolsDirectory);
        File.WriteAllText(templatePath, "fontFile=FontFileVariable");
        string fontFile = Path.Combine(_toolsDirectory, "Project", "Fonts", "Body.ttf");
        string bmfcPath = Path.Combine(_toolsDirectory, "Project", "FontCache", "Font15Body.bmfc");
        Directory.CreateDirectory(Path.GetDirectoryName(bmfcPath)!);
        BmfcSave bmfcSave = new BmfcSave { FontName = "Body", FontSize = 15, FontFile = fontFile };

        BmFontExeFileGenerator.WriteBmfc(bmfcSave, bmfcPath, templatePath);

        File.ReadAllText(bmfcPath).ShouldBe("fontFile=" + Path.Combine("..", "Fonts", "Body.ttf"));
        bmfcSave.FontFile.ShouldBe(fontFile);
    }

    // A font on another drive has no path relative to the .bmfc, so it stays absolute; bmfont still finds it.
    [SkippableFact]
    public void WriteBmfc_ShouldKeepFontFileAbsolute_WhenOnAnotherDrive()
    {
        Skip.IfNot(OperatingSystem.IsWindows(), "drive letters are Windows-only");
        string templatePath = Path.Combine(_toolsDirectory, "template.bmfc");
        Directory.CreateDirectory(_toolsDirectory);
        File.WriteAllText(templatePath, "fontFile=FontFileVariable");
        string bmfcPath = Path.Combine(_toolsDirectory, "FontCache", "Font15Body.bmfc");
        Directory.CreateDirectory(Path.GetDirectoryName(bmfcPath)!);
        char otherDrive = char.ToUpperInvariant(Path.GetPathRoot(bmfcPath)![0]) == 'Z' ? 'Y' : 'Z';
        string fontFile = otherDrive + @":\Fonts\Body.ttf";
        BmfcSave bmfcSave = new BmfcSave { FontName = "Body", FontSize = 15, FontFile = fontFile };

        BmFontExeFileGenerator.WriteBmfc(bmfcSave, bmfcPath, templatePath);

        File.ReadAllText(bmfcPath).ShouldBe("fontFile=" + fontFile);
    }
}
