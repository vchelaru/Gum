using System.IO;
using Shouldly;

namespace Gum.Avalonia.Tests;

public class HeadOptionsTests
{
    [Fact]
    public void Parse_ReadsTheUserDataFolder_AsAFullPath()
    {
        HeadOptions options = HeadOptions.Parse(new[] { "project.gumx", "--user-data", "run-data", "--exit-after", "3" });

        options.UserDataFolder.ShouldBe(Path.GetFullPath("run-data"));
        options.ExitAfterSeconds.ShouldBe(3);
    }

    [Fact]
    public void Parse_WithoutTheFlag_LeavesTheUserDataFolderNull()
    {
        HeadOptions.Parse(new[] { "project.gumx" }).UserDataFolder.ShouldBeNull();
    }

    [Fact]
    public void Parse_ReadsZoomToFit_AsASwitch()
    {
        HeadOptions.Parse(new[] { "project.gumx", "--zoom-to-fit", "--exit-after", "3" }).ZoomToFit.ShouldBeTrue();
        HeadOptions.Parse(new[] { "project.gumx" }).ZoomToFit.ShouldBeFalse();
    }

    [Fact]
    public void MissingProjectFile_NamesAProjectThatDoesNotExist()
    {
        string missing = Path.Combine(Path.GetTempPath(), "no-such-folder-7374", "Missing.gumj");

        HeadOptions.Parse(new[] { "--exit-after", "60", missing, "--select", "A/B" }).MissingProjectFile.ShouldBe(Path.GetFullPath(missing));
    }

    [Fact]
    public void MissingProjectFile_IsNullForAnExistingProjectOrNone()
    {
        string existing = Path.GetTempFileName();
        string project = Path.ChangeExtension(existing, ".gumx");
        File.Move(existing, project);
        try
        {
            HeadOptions.Parse(new[] { project, "--exit-after", "60" }).MissingProjectFile.ShouldBeNull();
            HeadOptions.Parse(new[] { "--exit-after", "60" }).MissingProjectFile.ShouldBeNull();
        }
        finally
        {
            File.Delete(project);
        }
    }
}
