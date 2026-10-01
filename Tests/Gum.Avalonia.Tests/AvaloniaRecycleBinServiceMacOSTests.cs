using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using Gum.Avalonia.Services;
using Gum.Avalonia.Services.MacOS;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Trashes real files on macOS. The packaged app has no Automation consent, so trashing must not
/// go through Apple Events to Finder (issue #5556).
/// </summary>
public class AvaloniaRecycleBinServiceMacOSTests : IDisposable
{
    private const string SkipReason = "trashes through NSFileManager, macOS only";
    private readonly string _folder;

    public AvaloniaRecycleBinServiceMacOSTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "GumTrashTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, recursive: true);
    }

    [SkippableFact]
    public void MoveToRecycleBin_OnMacOS_RemovesTheFileFromItsFolder()
    {
        Skip.IfNot(OperatingSystem.IsMacOS(), SkipReason);
        string path = Path.Combine(_folder, "Trash Me.gucx");
        File.WriteAllText(path, "<ComponentSave />");

        new AvaloniaRecycleBinService().MoveToRecycleBin(new FilePath(path));

        File.Exists(path).ShouldBeFalse();
    }

    [SkippableFact]
    public void MoveToRecycleBin_OnMacOS_WithAMissingFile_TrashesTheRestAndThrowsIOException()
    {
        Skip.IfNot(OperatingSystem.IsMacOS(), SkipReason);
        string missing = Path.Combine(_folder, "Missing.gucx");
        string present = Path.Combine(_folder, "Present.gucx");
        File.WriteAllText(present, "<ComponentSave />");
        List<FilePath> paths = new List<FilePath> { new FilePath(missing), new FilePath(present) };

        IOException exception = Should.Throw<IOException>(() => new AvaloniaRecycleBinService().MoveToRecycleBin(paths));

        exception.Message.ShouldContain("Missing.gucx");
        File.Exists(present).ShouldBeFalse();
    }

    [SkippableFact]
    [SupportedOSPlatform("macos")]
    public void MoveToTrash_OnMacOS_PutsTheFileInTheTrash()
    {
        Skip.IfNot(OperatingSystem.IsMacOS(), SkipReason);
        string path = Path.Combine(_folder, "In The Trash.gucx");
        File.WriteAllText(path, "<ComponentSave />");

        string trashedPath = MacFileTrash.MoveToTrash(path);

        string trashFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".Trash");
        Path.GetDirectoryName(trashedPath).ShouldBe(trashFolder);
        File.Exists(trashedPath).ShouldBeTrue();
        DeleteIfAllowed(trashedPath);
    }

    // Removing an item from the Trash needs Full Disk Access, which a developer's terminal may not
    // have; leaving the small test file there is harmless.
    private static void DeleteIfAllowed(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (IOException)
        {
        }
    }
}
