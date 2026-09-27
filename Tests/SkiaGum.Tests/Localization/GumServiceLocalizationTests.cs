using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Localization;
using Shouldly;
using SkiaGum;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;

namespace SkiaGum.Tests.Localization;

/// <summary>
/// #5223: the Skia GumService loads the project's LocalizationFiles during Initialize and
/// re-translates live text on a language switch, the same as the MonoGame/raylib GumService.
/// </summary>
public class GumServiceLocalizationTests : IDisposable
{
    private readonly string _tempDirectory =
        Path.Combine(Path.GetTempPath(), "SkiaGumServiceLocalization_" + Guid.NewGuid().ToString("N"));
    private readonly string _previousRelativeDirectory = ToolsUtilities.FileManager.RelativeDirectory;

    public GumServiceLocalizationTests()
    {
        Directory.CreateDirectory(_tempDirectory);
        CustomSetPropertyOnRenderable.LocalizationService = null;
    }

    public void Dispose()
    {
        CustomSetPropertyOnRenderable.LocalizationService = null;
        ToolsUtilities.FileManager.RelativeDirectory = _previousRelativeDirectory;
        try { Directory.Delete(_tempDirectory, recursive: true); } catch { /* best-effort */ }
    }

    [Fact]
    public void Initialize_WithProjectLocalizationFile_ShouldTranslateAndRefreshText()
    {
        File.WriteAllText(Path.Combine(_tempDirectory, "Strings.csv"), "String ID,English,Spanish\nT_OK,OK,Aceptar\n");
        string gumxPath = SaveProject(new List<string> { "Strings.csv" });
        using SKSurface surface = SKSurface.Create(new SKImageInfo(100, 100));

        GumService.Default.Initialize(surface.Canvas, 100, 100, gumxPath);
        ILocalizationService service = CustomSetPropertyOnRenderable.LocalizationService;
        service.ShouldNotBeNull();
        service.CurrentLanguage = 1;
        TextRuntime text = new();
        GumService.Default.Root.AddChild(text);
        text.Text = "T_OK";
        service.CurrentLanguage = 2;

        ((Text)text.RenderableComponent!).RawText!.ShouldBe("Aceptar");
    }

    [Fact]
    public void Initialize_WithMixedCsvAndResxFiles_ShouldReportAWarning()
    {
        string gumxPath = SaveProject(new List<string> { "Strings.resx", "Extra.csv" });
        using SKSurface surface = SKSurface.Create(new SKImageInfo(100, 100));

        GumService.Default.Initialize(surface.Canvas, 100, 100, gumxPath);

        GumService.Default.LastLoadResult.ShouldNotBeNull();
        GumService.Default.LastLoadResult!.Warnings.ShouldContain(w => w.Contains("Mixed CSV/RESX"));
    }

    private string SaveProject(List<string> localizationFiles)
    {
        string gumxPath = Path.Combine(_tempDirectory, "Project.gumx");
        GumProjectSave project = new() { LocalizationFiles = localizationFiles };
        project.Save(gumxPath, saveElements: false);
        return gumxPath;
    }
}
