using System;
using System.IO;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// Covers <see cref="PreviewSelectionFile"/>: the tool's writes must survive GumPreview reading the
/// same file at the same moment, and must never throw into the plugin event that triggered them.
/// </summary>
public class PreviewSelectionFileTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"PreviewSelectionFileTests_{Guid.NewGuid():N}.txt");

    public void Dispose()
    {
        File.Delete(_path);
    }

    [Fact]
    public void TryWrite_WhilePreviewHoldsTheFileOpenForReading_ReplacesTheContent()
    {
        File.WriteAllText(_path, "element=Old\n");
        using FileStream reader = PreviewSelectionFile.OpenForReading(_path);

        PreviewSelectionFile.TryWrite(_path, "element=New\n").ShouldBeTrue();

        reader.Dispose();
        File.ReadAllText(_path).ShouldBe("element=New\n");
    }

    [Fact]
    public void TryWrite_WhenAnotherProcessHoldsTheFileWithoutSharingWrites_DoesNotThrow()
    {
        File.WriteAllText(_path, "element=Old\n");
        using FileStream exclusive = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);

        bool written = PreviewSelectionFile.TryWrite(_path, "element=New\n");

        // Only Windows enforces sharing modes; elsewhere the write goes through. Either way the
        // caller must get a bool back, never the IOException that disabled the plugin.
        written.ShouldBe(!OperatingSystem.IsWindows());
    }

    [Fact]
    public void TryRead_ReturnsTheMessageWrittenByTryWrite()
    {
        PreviewSelectionFile.TryWrite(_path, new PreviewSelectionMessage("MainMenu") { StateName = "Open" }.Serialize());

        PreviewSelectionMessage? message = PreviewSelectionFile.TryRead(_path);

        message.ShouldNotBeNull();
        message.ElementName.ShouldBe("MainMenu");
        message.StateName.ShouldBe("Open");
    }
}
