using Gum.Diagnostics;
using Shouldly;

namespace Gum.Presentation.Tests;

public class PathScrubberTests
{
    [Fact]
    public void Scrub_WindowsHomeFolder_ReplacesPathAndKeepsRest()
    {
        PathScrubber scrubber = new PathScrubber("sam", @"C:\Users\sam");

        scrubber.Scrub(@"Loaded C:\Users\sam\Documents\Game\Game.gumx").ShouldBe(@"Loaded %USER%\Documents\Game\Game.gumx");
    }

    [Fact]
    public void Scrub_WindowsHomeFolderWithForwardSlashes_IsReplaced()
    {
        PathScrubber scrubber = new PathScrubber("sam", @"C:\Users\sam");

        scrubber.Scrub("file:///C:/Users/sam/Game/Game.gumx").ShouldBe("file:///%USER%/Game/Game.gumx");
    }

    [Fact]
    public void Scrub_WindowsHomeFolderWithSpace_IsReplaced()
    {
        PathScrubber scrubber = new PathScrubber("Vic Personal", @"C:\Users\Vic Personal");

        scrubber.Scrub(@"C:\Users\Vic Personal\AppData\Roaming\Gum").ShouldBe(@"%USER%\AppData\Roaming\Gum");
    }

    [Fact]
    public void Scrub_WindowsHomeFolder_IgnoresCase()
    {
        PathScrubber scrubber = new PathScrubber("sam", @"C:\Users\sam");

        scrubber.Scrub(@"c:\users\SAM\x.txt").ShouldBe(@"%USER%\x.txt");
    }

    [Fact]
    public void Scrub_MacHomeFolder_ReplacesPath()
    {
        PathScrubber scrubber = new PathScrubber("sam", "/Users/sam");

        scrubber.Scrub("Opened /Users/sam/Library/Application Support/Gum/settings.json")
            .ShouldBe("Opened %USER%/Library/Application Support/Gum/settings.json");
    }

    [Fact]
    public void Scrub_LinuxHomeFolder_ReplacesPath()
    {
        PathScrubber scrubber = new PathScrubber("sam", "/home/sam");

        scrubber.Scrub("/home/sam/.config/Gum/settings.json").ShouldBe("%USER%/.config/Gum/settings.json");
    }

    [Fact]
    public void Scrub_OtherUsersHomePath_IsReplacedToo()
    {
        PathScrubber scrubber = new PathScrubber("sam", "/home/sam");

        scrubber.Scrub(@"D:\Users\pat\Game and /Users/lee/Game and /home/kim/Game")
            .ShouldBe(@"%USER%\Game and %USER%/Game and %USER%/Game");
    }

    [Fact]
    public void Scrub_UserNameAsWholeWord_IsReplaced()
    {
        PathScrubber scrubber = new PathScrubber("sam", "/home/sam");

        scrubber.Scrub("Opened by sam at noon; Sam again.").ShouldBe("Opened by %USER% at noon; %USER% again.");
    }

    [Fact]
    public void Scrub_UserNameInsideOtherWords_IsKept()
    {
        PathScrubber scrubber = new PathScrubber("sam", "/home/sam");

        scrubber.Scrub("sample, Samsung, wisam, sam2").ShouldBe("sample, Samsung, wisam, sam2");
    }

    [Fact]
    public void Scrub_UserNameNextToUnderscore_IsReplaced()
    {
        PathScrubber scrubber = new PathScrubber("sam", "/home/sam");

        scrubber.Scrub("sam_x and x_sam and sam_").ShouldBe("%USER%_x and x_%USER% and %USER%_");
    }

    [Fact]
    public void Scrub_UserNameInsideLongerWords_IsKept()
    {
        PathScrubber scrubber = new PathScrubber("sam", "/home/sam");

        scrubber.Scrub("Samantha sample").ShouldBe("Samantha sample");
    }

    [Fact]
    public void Scrub_NoUserNameOrHome_LeavesTextAlone()
    {
        PathScrubber scrubber = new PathScrubber(null, "");

        scrubber.Scrub("nothing to remove here").ShouldBe("nothing to remove here");
    }

    [Fact]
    public void Scrub_UserNameWithRegexCharacters_IsTreatedAsText()
    {
        PathScrubber scrubber = new PathScrubber("a.b+c", @"C:\Users\a.b+c");

        scrubber.Scrub(@"C:\Users\a.b+c\x and aXb+c and a.b+c").ShouldBe(@"%USER%\x and aXb+c and %USER%");
    }

    [Fact]
    public void Scrub_PathsOutsideAnyHomeFolder_AreKept()
    {
        PathScrubber scrubber = new PathScrubber("sam", @"C:\Users\sam");

        scrubber.Scrub(@"D:\Games\Space\Space.gumx").ShouldBe(@"D:\Games\Space\Space.gumx");
    }
}
