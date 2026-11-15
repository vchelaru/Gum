using Gum.Startup.SingleInstance;
using Shouldly;

namespace Gum.Presentation.Tests.Startup.SingleInstance;

public class InstanceIdentityTests
{
    private static readonly string _root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "GumIdentity"));

    [Fact]
    public void KeyFor_CaseInsensitiveFileSystem_IgnoresCaseAndSeparatorsAndDotSegments()
    {
        string a = InstanceIdentity.KeyFor(Path.Combine(_root, "Ui", "Ui.gumx"), "vic", caseSensitive: false);
        string b = InstanceIdentity.KeyFor(Path.Combine(_root, "ui", "..", "UI", "UI.GUMX"), "vic", caseSensitive: false);

        b.ShouldBe(a);
    }

    [Fact]
    public void KeyFor_CaseSensitiveFileSystem_KeepsCase()
    {
        string a = InstanceIdentity.KeyFor(Path.Combine(_root, "Ui.gumx"), "vic", caseSensitive: true);
        string b = InstanceIdentity.KeyFor(Path.Combine(_root, "ui.gumx"), "vic", caseSensitive: true);

        b.ShouldNotBe(a);
    }

    [Fact]
    public void KeyFor_DifferentProjectsOrUsers_Differ()
    {
        string project = InstanceIdentity.KeyFor(Path.Combine(_root, "A", "Ui.gumx"), "vic", false);

        InstanceIdentity.KeyFor(Path.Combine(_root, "B", "Ui.gumx"), "vic", false).ShouldNotBe(project);
        InstanceIdentity.KeyFor(Path.Combine(_root, "A", "Ui.gumx"), "other", false).ShouldNotBe(project);
        InstanceIdentity.KeyFor(Path.Combine(_root, "A", "Ui.gumj"), "vic", false).ShouldNotBe(project);
    }

    [Fact]
    public void KeyFor_RelativePath_IsResolvedAgainstWorkingDirectory()
    {
        string relative = InstanceIdentity.KeyFor(Path.Combine("Samples", "Ui.gumx"), "vic", false);
        string absolute = InstanceIdentity.KeyFor(Path.GetFullPath(Path.Combine("Samples", "Ui.gumx")), "vic", false);

        relative.ShouldBe(absolute);
    }

    [Fact]
    public void KeyFor_IsShortAndSafeInAFileAndPipeName()
    {
        string key = InstanceIdentity.KeyFor(Path.Combine(_root, "Ui.gumx"), "vic ünï/\\:", false);

        key.Length.ShouldBe(16);
        key.ShouldAllBe(c => char.IsAsciiHexDigitLower(c));
        InstanceIdentity.PipeName(key).ShouldBe("gum-open-" + key);
    }

    [Fact]
    public void LockFilePath_LivesInTheInstanceDirectory()
    {
        InstanceIdentity.LockFilePath(_root, "abc").ShouldBe(Path.Combine(_root, "abc.lock"));
    }

    [Theory]
    [InlineData("Screens/Main.gusx", "Main")]
    [InlineData("Screens/Menus/Main.gusx", "Menus/Main")]
    [InlineData("Components/Controls/Button.gucx", "Controls/Button")]
    [InlineData("Components/Button.gucj", "Button")]
    [InlineData("Standards/Text.gutx", "Text")]
    [InlineData("SCREENS/Main.GUSX", "Main")]
    public void ElementNameFor_UsesPathBelowTheElementFolder(string relativeFile, string expected)
    {
        string project = Path.Combine(_root, "Ui", "Ui.gumx");

        InstanceIdentity.ElementNameFor(project, Path.Combine(_root, "Ui", relativeFile)).ShouldBe(expected);
    }

    [Fact]
    public void ElementNameFor_FileOutsideTheProjectFolders_FallsBackToTheFileName()
    {
        string project = Path.Combine(_root, "Ui", "Ui.gumx");

        InstanceIdentity.ElementNameFor(project, Path.Combine(_root, "Elsewhere", "Main.gusx")).ShouldBe("Main");
    }

    [Fact]
    public void ElementNameFor_NotAnElementFile_IsNull()
    {
        string project = Path.Combine(_root, "Ui", "Ui.gumx");

        InstanceIdentity.ElementNameFor(project, Path.Combine(_root, "Ui", "Screens", "notes.txt")).ShouldBeNull();
    }
}
