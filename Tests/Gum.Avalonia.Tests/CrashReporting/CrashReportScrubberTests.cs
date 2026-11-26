using Gum.Avalonia.Diagnostics.CrashReporting;
using Gum.Diagnostics;
using Shouldly;

namespace Gum.Avalonia.Tests.CrashReporting;

public class CrashReportScrubberTests
{
    private static CrashReportScrubber Create(string home, string? project) =>
        new CrashReportScrubber(new PathScrubber("sam", home), () => project);

    [Fact]
    public void Scrub_ProjectInsideHome_ReplacesProjectBeforeHome()
    {
        CrashReportScrubber scrubber = Create(@"C:\Users\sam", @"C:\Users\sam\Games\MyGame\Content\");

        string? result = scrubber.Scrub(@"Could not load C:\Users\sam\Games\MyGame\Content\Screens\Main.gusx");

        result.ShouldBe(@"Could not load <project>\Screens\Main.gusx");
    }

    [Fact]
    public void Scrub_PathsWithOtherSeparatorOrCase_AreReplaced()
    {
        CrashReportScrubber scrubber = Create(@"C:\Users\Sam", @"D:\Work\MyGame");

        string? result = scrubber.Scrub(@"at c:/users/sam/source/File.cs in d:/work/mygame/a.png");

        result.ShouldBe(@"at %USER%/source/File.cs in <project>/a.png");
    }

    [Fact]
    public void Scrub_UnixPaths_AreReplaced()
    {
        CrashReportScrubber scrubber = Create("/home/sam", "/home/sam/work/MyGame");

        string? result = scrubber.Scrub("Missing /home/sam/work/MyGame/a.png and /home/sam/.config/x");

        result.ShouldBe("Missing <project>/a.png and %USER%/.config/x");
    }

    [Fact]
    public void Scrub_LongerFolderNameSharingTheProjectPrefix_IsUntouched()
    {
        CrashReportScrubber scrubber = Create(@"C:\Users\sam", @"D:\Game");

        scrubber.Scrub(@"D:\Games\file.txt").ShouldBe(@"D:\Games\file.txt");
    }

    [Fact]
    public void Scrub_UnrelatedTextAndNull_AreUntouched()
    {
        CrashReportScrubber scrubber = Create(@"C:\Users\sam", @"D:\Proj");

        scrubber.Scrub(@"Object reference not set at D:\Other\Thing.cs").ShouldBe(@"Object reference not set at D:\Other\Thing.cs");
        scrubber.Scrub(null).ShouldBeNull();
    }

    [Fact]
    public void Scrub_NoProjectOrRootProject_LeavesOtherPathsAlone()
    {
        CrashReportScrubber noProject = Create(@"C:\Users\sam", null);
        CrashReportScrubber rootProject = Create(@"C:\Users\sam", @"C:\");

        noProject.Scrub(@"C:\Windows\x.dll").ShouldBe(@"C:\Windows\x.dll");
        rootProject.Scrub(@"C:\Windows\x.dll").ShouldBe(@"C:\Windows\x.dll");
    }
}
