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

        string? result = PreviewExecutableLocator.Resolve(headBaseDirectory);

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

        string? result = PreviewExecutableLocator.Resolve(headBaseDirectory);

        result.ShouldBe(exePath);
    }

    [Fact]
    public void Resolve_FallsBackToDevSampleBuildOutput_WhenPublishedCopyMissing()
    {
        string headBaseDirectory = Path.Combine(_tempRoot, "Tool", "Gum.Avalonia", "bin", "Debug", "net10.0");
        string sampleBinDirectory = Path.Combine(_tempRoot, "Samples", "GumPreview", "GumPreview", "bin", "Debug", "net8.0");
        Directory.CreateDirectory(headBaseDirectory);
        Directory.CreateDirectory(sampleBinDirectory);
        string exePath = Path.Combine(sampleBinDirectory, ExeName);
        File.WriteAllText(exePath, "");

        string? result = PreviewExecutableLocator.Resolve(headBaseDirectory);

        result.ShouldBe(exePath);
    }

    [Fact]
    public void Resolve_PrefersPublishedExecutable_OverDevSampleBuildOutput()
    {
        string headBaseDirectory = Path.Combine(_tempRoot, "Tool", "Gum.Avalonia", "bin", "Debug", "net10.0");
        string previewFolder = Path.Combine(headBaseDirectory, "Preview");
        string sampleBinDirectory = Path.Combine(_tempRoot, "Samples", "GumPreview", "GumPreview", "bin", "Debug", "net8.0");
        Directory.CreateDirectory(previewFolder);
        Directory.CreateDirectory(sampleBinDirectory);
        string publishedExePath = Path.Combine(previewFolder, ExeName);
        File.WriteAllText(publishedExePath, "");
        File.WriteAllText(Path.Combine(sampleBinDirectory, ExeName), "");

        string? result = PreviewExecutableLocator.Resolve(headBaseDirectory);

        result.ShouldBe(publishedExePath);
    }
}
