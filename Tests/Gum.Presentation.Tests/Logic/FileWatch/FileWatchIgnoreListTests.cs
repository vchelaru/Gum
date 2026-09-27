using Gum.Logic.FileWatch;
using Shouldly;
using ToolsUtilities;

namespace Gum.Presentation.Tests.Logic.FileWatch;

/// <summary>
/// Pins the time-based ignore, the ignore list's only mechanism (#5281).
/// </summary>
public class FileWatchIgnoreListTests
{
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
}
