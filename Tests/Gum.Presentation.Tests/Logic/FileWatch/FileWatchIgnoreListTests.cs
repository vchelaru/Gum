using Gum.Logic.FileWatch;
using Shouldly;
using System.IO;
using ToolsUtilities;

namespace Gum.Presentation.Tests.Logic.FileWatch;

/// <summary>
/// Pins the time-based ignore (#5281) and the content check that narrows it once the tool's write is
/// recorded (#6866).
/// </summary>
public class FileWatchIgnoreListTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "FileWatchIgnoreList_" + Path.GetRandomFileName());

    public FileWatchIgnoreListTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch { /* best-effort */ }
    }

    private FilePath WriteFile(string name, string content)
    {
        string path = Path.Combine(_directory, name);
        File.WriteAllText(path, content);
        return new FilePath(path);
    }

    [Fact]
    public void TryGetIgnoreFileChange_IgnoresEveryChangeUntilTheTime_AndNothingAfter()
    {
        FileWatchIgnoreList ignoreList = new FileWatchIgnoreList();
        FilePath saved = new FilePath("C:/Game/Components/Button.gucx");
        FilePath expired = new FilePath("C:/Game/Screens/Title.gusx");
        FilePath other = new FilePath("C:/Game/Components/Label.gucx");

        ignoreList.IgnoreNextChangeUntil(saved, DateTime.Now.AddMinutes(1));
        ignoreList.IgnoreNextChangeUntil(expired, DateTime.Now.AddMinutes(-1));

        // A save raises several watcher events; every one inside the window is ignored.
        ignoreList.TryGetIgnoreFileChange(saved).ShouldBeTrue();
        ignoreList.TryGetIgnoreFileChange(saved).ShouldBeTrue();
        ignoreList.TryGetIgnoreFileChange(expired).ShouldBeFalse();
        ignoreList.TryGetIgnoreFileChange(other).ShouldBeFalse();
    }

    [Fact]
    public void IgnoreNextChangeUntil_KeepsTheLaterTime()
    {
        FileWatchIgnoreList ignoreList = new FileWatchIgnoreList();
        FilePath file = new FilePath("C:/Game/Components/Button.gucx");
        DateTime later = DateTime.Now.AddMinutes(2);

        ignoreList.IgnoreNextChangeUntil(file, later);
        ignoreList.IgnoreNextChangeUntil(file, DateTime.Now.AddMinutes(1));

        ignoreList.TimedChangesToIgnore[file].ShouldBe(later);
    }

    [Fact]
    public void TryGetIgnoreFileChange_AfterTheWriteIsRecorded_IgnoresOnlyWhileTheFileHoldsThoseBytes()
    {
        FileWatchIgnoreList ignoreList = new FileWatchIgnoreList();
        FilePath file = WriteFile("Button.gucx", "tool bytes");
        ignoreList.IgnoreNextChangeUntil(file, DateTime.Now.AddMinutes(1));
        ignoreList.RecordWrittenContent(file);

        // A late event for the tool's own write.
        ignoreList.TryGetIgnoreFileChange(file).ShouldBeTrue();

        File.WriteAllText(file.FullPath, "outside bytes");
        ignoreList.TryGetIgnoreFileChange(file).ShouldBeFalse();

        // An outside edit that leaves the same bytes changes nothing to reload.
        File.WriteAllText(file.FullPath, "tool bytes");
        ignoreList.TryGetIgnoreFileChange(file).ShouldBeTrue();
    }

    [Fact]
    public void TryGetIgnoreFileChange_AfterTheWriteIsRecorded_DoesNotIgnoreADeletedFile()
    {
        FileWatchIgnoreList ignoreList = new FileWatchIgnoreList();
        FilePath file = WriteFile("Button.gucx", "tool bytes");
        ignoreList.IgnoreNextChangeUntil(file, DateTime.Now.AddMinutes(1));
        ignoreList.RecordWrittenContent(file);

        File.Delete(file.FullPath);

        ignoreList.TryGetIgnoreFileChange(file).ShouldBeFalse();
    }

    [Fact]
    public void IgnoreNextChangeUntil_ForASecondSave_IgnoresEveryEventUntilThatWriteIsRecorded()
    {
        FileWatchIgnoreList ignoreList = new FileWatchIgnoreList();
        FilePath file = WriteFile("Button.gucx", "first save");
        ignoreList.IgnoreNextChangeUntil(file, DateTime.Now.AddMinutes(1));
        ignoreList.RecordWrittenContent(file);

        // The second save has started: the bytes differ from the first record, and are still the tool's.
        ignoreList.IgnoreNextChangeUntil(file, DateTime.Now.AddMinutes(1));
        File.WriteAllText(file.FullPath, "second save");
        ignoreList.TryGetIgnoreFileChange(file).ShouldBeTrue();

        ignoreList.RecordWrittenContent(file);
        ignoreList.TryGetIgnoreFileChange(file).ShouldBeTrue();
    }

    [Fact]
    public void TryGetIgnoreFileChange_AfterTheWindow_IsFalseEvenWhenTheBytesMatch()
    {
        FileWatchIgnoreList ignoreList = new FileWatchIgnoreList();
        FilePath file = WriteFile("Button.gucx", "tool bytes");
        ignoreList.IgnoreNextChangeUntil(file, DateTime.Now.AddMinutes(-1));
        ignoreList.RecordWrittenContent(file);

        ignoreList.TryGetIgnoreFileChange(file).ShouldBeFalse();
    }
}
