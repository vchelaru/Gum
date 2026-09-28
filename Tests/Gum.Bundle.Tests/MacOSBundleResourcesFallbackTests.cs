using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gum.Bundle;
using Gum.DataTypes;
using Gum.Localization;
using Shouldly;
using ToolsUtilities;

namespace Gum.Bundle.Tests;

/// <summary>
/// A macOS .app bundle runs from Contents/MacOS/ but ships content in Contents/Resources/ (#5415).
/// Each test builds that layout in a temp folder and points FileManager at its MacOS folder, so the
/// tests run the same on every OS.
/// </summary>
public class MacOSBundleResourcesFallbackTests : IDisposable
{
    private readonly Func<string, Stream>? _previousHook;
    private readonly string _tempDir;
    private readonly string _macOsDirectory;
    private readonly string _resourcesDirectory;

    public MacOSBundleResourcesFallbackTests()
    {
        _previousHook = FileManager.CustomGetStreamFromFile;
        FileManager.CustomGetStreamFromFile = null;

        _tempDir = Path.Combine(Path.GetTempPath(), "MacOSBundleTests_" + Path.GetRandomFileName());
        string contents = Path.Combine(_tempDir, "Game.app", "Contents");
        _macOsDirectory = Path.Combine(contents, "MacOS") + Path.DirectorySeparatorChar;
        _resourcesDirectory = Path.Combine(contents, "Resources");
        Directory.CreateDirectory(_macOsDirectory);
        Directory.CreateDirectory(_resourcesDirectory);

        FileManager.MacOSBundleExecutableDirectoryOverride = _macOsDirectory;
    }

    public void Dispose()
    {
        FileManager.MacOSBundleExecutableDirectoryOverride = null;
        FileManager.CustomGetStreamFromFile = _previousHook;
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public void LooseFileGumFileProvider_ShouldEnumerateResources_WhenRootIsUnderMacOS()
    {
        string animationFile = Path.Combine(_resourcesDirectory, "Content", "GumProject", "Screens", "MainScreenAnimations.ganx");
        Directory.CreateDirectory(Path.GetDirectoryName(animationFile)!);
        File.WriteAllText(animationFile, "<ElementAnimationsSave />");

        LooseFileGumFileProvider provider = new LooseFileGumFileProvider(
            Path.Combine(_macOsDirectory, "Content", "GumProject"));

        provider.EnumerateFiles("*Animations.ganx").ToList()
            .ShouldBe(new List<string> { "Screens/MainScreenAnimations.ganx" });
        provider.Exists("Screens/MainScreenAnimations.ganx").ShouldBeTrue();
    }

    [Fact]
    public void Resolve_ShouldOpenGumpkgFromResources_WhenPathIsUnderMacOS()
    {
        string bundleInResources = Path.Combine(_resourcesDirectory, "Content", "Project.gumpkg");
        Directory.CreateDirectory(Path.GetDirectoryName(bundleInResources)!);
        using (FileStream stream = File.Create(bundleInResources))
        {
            GumBundleWriter.Write(stream, new (string, byte[])[]
            {
                ("Project.gumx", Encoding.UTF8.GetBytes("<GumProjectSave />")),
            });
        }

        ProjectResolution resolution = GumBundleLoader.Resolve(
            Path.Combine(_macOsDirectory, "Content", "Project.gumpkg"));

        resolution.UsedBundle.ShouldBeTrue();
        resolution.FileProvider.Exists("Project.gumx").ShouldBeTrue();
    }

    [Fact]
    public void Resolve_ShouldRootLooseProviderAtResources_WhenGumxSitsDirectlyInResources()
    {
        // Contents/MacOS/ exists, so the provider's own "root is missing" fallback never fires (#5451).
        File.WriteAllText(Path.Combine(_resourcesDirectory, "Project.gumx"), "<GumProjectSave />");
        string animationFile = Path.Combine(_resourcesDirectory, "Screens", "MainScreenAnimations.ganx");
        Directory.CreateDirectory(Path.GetDirectoryName(animationFile)!);
        File.WriteAllText(animationFile, "<ElementAnimationsSave />");

        ProjectResolution resolution = GumBundleLoader.Resolve(Path.Combine(_macOsDirectory, "Project.gumx"));

        resolution.FileProvider.EnumerateFiles("*Animations.ganx").ToList()
            .ShouldBe(new List<string> { "Screens/MainScreenAnimations.ganx" });
    }

    [Fact]
    public void GumProjectSaveLoad_ShouldReadGumxFromResources_WhenPathIsUnderMacOS()
    {
        string gumxInResources = Path.Combine(_resourcesDirectory, "Content", "Project.gumx");
        Directory.CreateDirectory(Path.GetDirectoryName(gumxInResources)!);
        File.WriteAllText(gumxInResources, "<GumProjectSave />");

        GumProjectSave? project = GumProjectSave.Load(
            Path.Combine(_macOsDirectory, "Content", "Project.gumx"), out GumLoadResult result);

        result.ErrorMessage.ShouldBeNullOrEmpty();
        project.ShouldNotBeNull();
    }

    [Fact]
    public void ProjectLocalizationLoader_ShouldLoadLooseResxAndSatellitesFromResources_WhenProjectIsUnderMacOS()
    {
        string localizationDirectory = Path.Combine(_resourcesDirectory, "Content", "Localization");
        Directory.CreateDirectory(localizationDirectory);
        File.WriteAllText(Path.Combine(localizationDirectory, "Strings.resx"), ResxWith("Hello"));
        File.WriteAllText(Path.Combine(localizationDirectory, "Strings.fr.resx"), ResxWith("Bonjour"));
        GumProjectSave project = new GumProjectSave();
        project.LocalizationFiles.Add("Localization/Strings.resx");
        LocalizationService service = new LocalizationService();
        List<string> skipped = new List<string>();

        ProjectLocalizationLoader.Load(project, Path.Combine(_macOsDirectory, "Content"), service,
            new ProjectLocalizationLoadOptions { OnSkipped = skipped.Add });

        skipped.ShouldBeEmpty();
        service.Keys.ShouldContain("T_Greeting");
        service.Languages.ShouldContain("fr");
    }

    [Fact]
    public void AddResxDatabase_ShouldReadResxFromResources_WhenPathIsUnderMacOS()
    {
        File.WriteAllText(Path.Combine(_resourcesDirectory, "Strings.resx"), ResxWith("Hello"));
        LocalizationService service = new LocalizationService();

        service.AddResxDatabase(new[] { Path.Combine(_macOsDirectory, "Strings.resx") });

        service.Keys.ShouldContain("T_Greeting");
    }

    private static string ResxWith(string greeting) =>
        $"<root><data name=\"T_Greeting\"><value>{greeting}</value></data></root>";
}
