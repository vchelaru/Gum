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
    public void StartupFileError_NamesAProjectOrElementFileThatDoesNotExist()
    {
        string folder = Path.Combine(Path.GetTempPath(), "no-such-folder-7374");

        foreach (string name in new[] { "Missing.gumj", "Missing.gumx", Path.Combine("Screens", "Missing.gusx"), Path.Combine("Components", "Missing.gucj") })
        {
            string path = Path.Combine(folder, name);
            HeadOptions.Parse(new[] { "--exit-after", "60", path, "--select", "A/B" }).StartupFileError
                .ShouldBe("The project file does not exist: " + path);
        }
    }

    [Fact]
    public void StartupFileError_ChecksTheLastFile_LikeTheCommandLineManager()
    {
        string existing = Path.GetTempFileName();
        string project = Path.ChangeExtension(existing, ".gumx");
        File.Move(existing, project);
        try
        {
            HeadOptions.Parse(new[] { "Missing.gumx", project }).StartupFileError.ShouldBeNull();
            HeadOptions.Parse(new[] { project, "Missing.gumx" }).StartupFileError.ShouldNotBeNull();
        }
        finally
        {
            File.Delete(project);
        }
    }

    [Fact]
    public void StartupFileError_IsNullForNoFile()
    {
        HeadOptions.Parse(new[] { "--exit-after", "60", "--select", "A" }).StartupFileError.ShouldBeNull();
    }

    [Fact]
    public void StartupFileError_RejectsADirectoryAndAFileThatIsNotAProject()
    {
        HeadOptions.Parse(new[] { Path.GetTempPath() }).StartupFileError.ShouldBe(Path.GetTempPath() + " is not a Gum project file");
        HeadOptions.Parse(new[] { "run.ps1", "--exit-after", "5" }).StartupFileError.ShouldBe("run.ps1 is not a Gum project file");
    }
}
