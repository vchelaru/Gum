using System;
using System.IO;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// Covers <see cref="PreviewExecutableLocator"/>. The Native AOT build is always preferred when
/// present (issue #4748: it now serves every project format, via PreviewLauncher converting a .gumx
/// project to JSON first), falling back to ReadyToRun, then a local dev build.
/// </summary>
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

        ResolvedPreviewExecutable? result = PreviewExecutableLocator.Resolve(headBaseDirectory);

        result.ShouldBeNull();
    }

    [Fact]
    public void Resolve_PrefersNativeAotBuild_OverReadyToRunBuild()
    {
        string headBaseDirectory = Path.Combine(_tempRoot, "Tool", "Gum.Avalonia", "bin", "Debug", "net10.0");
        string aotPreviewFolder = Path.Combine(headBaseDirectory, "Preview-Aot");
        string previewFolder = Path.Combine(headBaseDirectory, "Preview");
        Directory.CreateDirectory(aotPreviewFolder);
        Directory.CreateDirectory(previewFolder);
        string aotExePath = Path.Combine(aotPreviewFolder, ExeName);
        File.WriteAllText(aotExePath, "");
        File.WriteAllText(Path.Combine(previewFolder, ExeName), "");

        ResolvedPreviewExecutable? result = PreviewExecutableLocator.Resolve(headBaseDirectory);

        result.ShouldNotBeNull();
        result!.Value.ExecutablePath.ShouldBe(aotExePath);
        result.Value.IsNativeAot.ShouldBeTrue();
    }

    [Fact]
    public void Resolve_FallsBackToReadyToRunBuild_WhenAotBuildMissing()
    {
        // Not every published package (or platform) ships the Native AOT variant yet.
        string headBaseDirectory = Path.Combine(_tempRoot, "Tool", "Gum.Avalonia", "bin", "Debug", "net10.0");
        string previewFolder = Path.Combine(headBaseDirectory, "Preview");
        Directory.CreateDirectory(previewFolder);
        string exePath = Path.Combine(previewFolder, ExeName);
        File.WriteAllText(exePath, "");

        ResolvedPreviewExecutable? result = PreviewExecutableLocator.Resolve(headBaseDirectory);

        result.ShouldNotBeNull();
        result!.Value.ExecutablePath.ShouldBe(exePath);
        result.Value.IsNativeAot.ShouldBeFalse();
    }

    [Fact]
    public void Resolve_FallsBackToDevBuildOutput_WhenNeitherPublishedCopyExists()
    {
        string headBaseDirectory = Path.Combine(_tempRoot, "Tool", "Gum.Avalonia", "bin", "Debug", "net10.0");
        string devBinDirectory = Path.Combine(_tempRoot, "Tool", "GumPreview", "bin", "Debug", "net10.0");
        Directory.CreateDirectory(headBaseDirectory);
        Directory.CreateDirectory(devBinDirectory);
        string exePath = Path.Combine(devBinDirectory, ExeName);
        File.WriteAllText(exePath, "");

        ResolvedPreviewExecutable? result = PreviewExecutableLocator.Resolve(headBaseDirectory);

        result.ShouldNotBeNull();
        result!.Value.ExecutablePath.ShouldBe(exePath);
        result.Value.IsNativeAot.ShouldBeFalse();
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

        ResolvedPreviewExecutable? result = PreviewExecutableLocator.Resolve(headBaseDirectory);

        result.ShouldNotBeNull();
        result!.Value.ExecutablePath.ShouldBe(releaseExePath);
    }
}
