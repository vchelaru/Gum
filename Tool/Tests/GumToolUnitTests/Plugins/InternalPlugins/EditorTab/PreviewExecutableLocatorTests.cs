using System;
using System.IO;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.EditorTab;

public class PreviewExecutableLocatorTests : IDisposable
{
    private static readonly string ExeName = OperatingSystem.IsWindows() ? "GumPreview.exe" : "GumPreview";

    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "GumPreviewLocatorTests_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    [Fact]
    public void Resolve_ReturnsNull_WhenNoExecutableExistsAnywhere()
    {
        string headBaseDirectory = Path.Combine(_tempRoot, "Tool", "Gum.Avalonia", "bin", "Debug", "net10.0");
        Directory.CreateDirectory(headBaseDirectory);

        string? result = PreviewExecutableLocator.Resolve(headBaseDirectory, isJsonFormat: false);

        result.ShouldBeNull();
    }

    [Fact]
    public void Resolve_FindsPublishedExecutable_NextToHead()
    {
        string headBaseDirectory = Path.Combine(_tempRoot, "Tool", "Gum.Avalonia", "bin", "Debug", "net10.0");
        string previewFolder = Path.Combine(headBaseDirectory, "Preview");
        Directory.CreateDirectory(previewFolder);
        string exePath = Path.Combine(previewFolder, ExeName);
        File.WriteAllText(exePath, "");

        string? result = PreviewExecutableLocator.Resolve(headBaseDirectory, isJsonFormat: false);

        result.ShouldBe(exePath);
    }

    [Fact]
    public void Resolve_FallsBackToDevBuildOutput_WhenPublishedCopyMissing()
    {
        string headBaseDirectory = Path.Combine(_tempRoot, "Tool", "Gum.Avalonia", "bin", "Debug", "net10.0");
        string devBinDirectory = Path.Combine(_tempRoot, "Tool", "GumPreview", "bin", "Debug", "net10.0");
        Directory.CreateDirectory(headBaseDirectory);
        Directory.CreateDirectory(devBinDirectory);
        string exePath = Path.Combine(devBinDirectory, ExeName);
        File.WriteAllText(exePath, "");

        string? result = PreviewExecutableLocator.Resolve(headBaseDirectory, isJsonFormat: false);

        result.ShouldBe(exePath);
    }

    [Fact]
    public void Resolve_PrefersPublishedExecutable_OverDevBuildOutput()
    {
        string headBaseDirectory = Path.Combine(_tempRoot, "Tool", "Gum.Avalonia", "bin", "Debug", "net10.0");
        string previewFolder = Path.Combine(headBaseDirectory, "Preview");
        string devBinDirectory = Path.Combine(_tempRoot, "Tool", "GumPreview", "bin", "Debug", "net10.0");
        Directory.CreateDirectory(previewFolder);
        Directory.CreateDirectory(devBinDirectory);
        string publishedExePath = Path.Combine(previewFolder, ExeName);
        File.WriteAllText(publishedExePath, "");
        File.WriteAllText(Path.Combine(devBinDirectory, ExeName), "");

        string? result = PreviewExecutableLocator.Resolve(headBaseDirectory, isJsonFormat: false);

        result.ShouldBe(publishedExePath);
    }

    [Fact]
    public void Resolve_PrefersDevReleaseBuild_OverDevDebugBuild()
    {
        // A local dev build should default to the faster Release output when both configurations
        // are present, so Preview reflects real (non-Debug-JIT) performance without extra setup.
        string headBaseDirectory = Path.Combine(_tempRoot, "Tool", "Gum.Avalonia", "bin", "Debug", "net10.0");
        string debugBinDirectory = Path.Combine(_tempRoot, "Tool", "GumPreview", "bin", "Debug", "net10.0");
        string releaseBinDirectory = Path.Combine(_tempRoot, "Tool", "GumPreview", "bin", "Release", "net10.0");
        Directory.CreateDirectory(headBaseDirectory);
        Directory.CreateDirectory(debugBinDirectory);
        Directory.CreateDirectory(releaseBinDirectory);
        File.WriteAllText(Path.Combine(debugBinDirectory, ExeName), "");
        string releaseExePath = Path.Combine(releaseBinDirectory, ExeName);
        File.WriteAllText(releaseExePath, "");

        string? result = PreviewExecutableLocator.Resolve(headBaseDirectory, isJsonFormat: false);

        result.ShouldBe(releaseExePath);
    }

    [Fact]
    public void Resolve_JsonFormat_PrefersAotBuild_OverReadyToRunBuild()
    {
        string headBaseDirectory = Path.Combine(_tempRoot, "Tool", "Gum.Avalonia", "bin", "Debug", "net10.0");
        string aotPreviewFolder = Path.Combine(headBaseDirectory, "Preview-Aot");
        string previewFolder = Path.Combine(headBaseDirectory, "Preview");
        Directory.CreateDirectory(aotPreviewFolder);
        Directory.CreateDirectory(previewFolder);
        string aotExePath = Path.Combine(aotPreviewFolder, ExeName);
        File.WriteAllText(aotExePath, "");
        File.WriteAllText(Path.Combine(previewFolder, ExeName), "");

        string? result = PreviewExecutableLocator.Resolve(headBaseDirectory, isJsonFormat: true);

        result.ShouldBe(aotExePath);
    }

    [Fact]
    public void Resolve_JsonFormat_FallsBackToReadyToRunBuild_WhenAotBuildMissing()
    {
        // Not every published package (or platform) ships the Native AOT variant yet - the
        // ReadyToRun build loads .gumj projects fine too, just without the faster AOT startup.
        string headBaseDirectory = Path.Combine(_tempRoot, "Tool", "Gum.Avalonia", "bin", "Debug", "net10.0");
        string previewFolder = Path.Combine(headBaseDirectory, "Preview");
        Directory.CreateDirectory(previewFolder);
        string exePath = Path.Combine(previewFolder, ExeName);
        File.WriteAllText(exePath, "");

        string? result = PreviewExecutableLocator.Resolve(headBaseDirectory, isJsonFormat: true);

        result.ShouldBe(exePath);
    }

    [Fact]
    public void Resolve_XmlFormat_NeverUsesAotBuild_EvenWhenPresent()
    {
        // .gumx loads through a plain XmlSerializer, which is not Native-AOT-safe - the AOT build
        // must never be selected for an XML project, even if one happens to be published.
        string headBaseDirectory = Path.Combine(_tempRoot, "Tool", "Gum.Avalonia", "bin", "Debug", "net10.0");
        string aotPreviewFolder = Path.Combine(headBaseDirectory, "Preview-Aot");
        string previewFolder = Path.Combine(headBaseDirectory, "Preview");
        Directory.CreateDirectory(aotPreviewFolder);
        Directory.CreateDirectory(previewFolder);
        File.WriteAllText(Path.Combine(aotPreviewFolder, ExeName), "");
        string readyToRunExePath = Path.Combine(previewFolder, ExeName);
        File.WriteAllText(readyToRunExePath, "");

        string? result = PreviewExecutableLocator.Resolve(headBaseDirectory, isJsonFormat: false);

        result.ShouldBe(readyToRunExePath);
    }
}
