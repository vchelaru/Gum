using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gum.Bundle;
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
}
