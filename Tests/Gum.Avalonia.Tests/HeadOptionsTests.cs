using System.IO;
using Gum.CommandLine;
using Gum.Startup.SingleInstance;
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
                .ShouldBe("The file does not exist: " + path);
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

    [Fact]
    public void StartupFileError_ReadsTheValuesOfKnownFlagsAsValues()
    {
        string[] commandLine =
        {
            "--exit-after", "60", "--screenshot", "out.png", "--select", "Screens/Main#Label", "--theme", "light",
            "--user-data", "run data", "--zoom-to-fit",
        };

        HeadOptions.Parse(commandLine).StartupFileError.ShouldBeNull();
    }

    [Fact]
    public void StartupFileError_IgnoresUnknownFlagsAndTheirValues()
    {
        HeadOptions.Parse(new[] { "--exit-after", "5", "-v", "--parent-pid", "5", "--content-root", "folder", "--focus", "Foo" })
            .StartupFileError.ShouldBeNull();
    }

    [Fact]
    public void TryResolveStartupFile_WithAnElementFileInAMissingFolder_FindsNoProject()
    {
        string path = Path.Combine(Path.GetTempPath(), "no-such-folder-7374", "Components", "Foo.gucx");

        CommandLineManager.TryResolveStartupFile(path, out string? project, out string? elementName).ShouldBeTrue();

        project.ShouldBeNull();
        elementName.ShouldBe("Foo");
    }

    private static string MakeProjectFolder(out string project, out string menuScreen)
    {
        string folder = Path.Combine(Path.GetTempPath(), "HeadOptions-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(folder, "Screens", "Menus"));
        project = Path.Combine(folder, "Ui.gumx");
        menuScreen = Path.Combine(folder, "Screens", "Menus", "Main.gusx");
        File.WriteAllText(project, "");
        File.WriteAllText(menuScreen, "");
        return folder;
    }

    [Fact]
    public void HandoffRequest_ProjectAndScreen_NamesTheProjectAndTheScreenBelowItsFolder()
    {
        string folder = MakeProjectFolder(out string project, out string screen);
        try
        {
            HeadOptions.Parse(new[] { project, screen }).HandoffRequest.ShouldBe(new OpenRequest(project, "Menus/Main"));
            HeadOptions.Parse(new[] { screen, project }).HandoffRequest.ShouldBe(new OpenRequest(project, "Menus/Main"));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void HandoffRequest_ScreenOnly_FindsTheProjectBesideItsFolder()
    {
        string folder = MakeProjectFolder(out string project, out string screen);
        try
        {
            HeadOptions.Parse(new[] { screen }).HandoffRequest.ShouldBe(new OpenRequest(project, "Menus/Main"));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void HandoffRequest_ProjectOnly_HasNoElement()
    {
        string folder = MakeProjectFolder(out string project, out _);
        try
        {
            HeadOptions.Parse(new[] { project }).HandoffRequest.ShouldBe(new OpenRequest(project, null));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void HandoffRequest_IsNullWithoutAProject_OrWhenTheFileIsBad()
    {
        HeadOptions.Parse(new[] { "--exit-after", "5" }).HandoffRequest.ShouldBeNull();
        HeadOptions.Parse(new[] { Path.Combine(Path.GetTempPath(), "no-such-folder-8076", "Missing.gumx") }).HandoffRequest.ShouldBeNull();
        HeadOptions.Parse(new[] { "run.ps1" }).HandoffRequest.ShouldBeNull();
    }

    [Fact]
    public void Parse_ReadsNoHandoffAndAwaitOpenRequests()
    {
        HeadOptions options = HeadOptions.Parse(new[] { "--no-handoff", "--await-open-requests", "2" });

        options.NoHandoff.ShouldBeTrue();
        options.AwaitOpenRequests.ShouldBe(2);
        HeadOptions plain = HeadOptions.Parse(new[] { "project.gumx" });
        plain.NoHandoff.ShouldBeFalse();
        plain.AwaitOpenRequests.ShouldBeNull();
    }

    [Fact]
    public void UsesSingleInstance_OnlyForAttendedLaunchesWithAProject()
    {
        string folder = MakeProjectFolder(out string project, out _);
        try
        {
            HeadOptions.Parse(new[] { project }).UsesSingleInstance.ShouldBeTrue();
            HeadOptions.Parse(new[] { project, "--no-handoff" }).UsesSingleInstance.ShouldBeFalse();
            HeadOptions.Parse(new[] { project, "--exit-after", "5" }).UsesSingleInstance.ShouldBeFalse();
            HeadOptions.Parse(new[] { project, "--exit-after", "5", "--await-open-requests", "1" }).UsesSingleInstance.ShouldBeTrue();
            HeadOptions.Parse(new[] { "--exit-after", "5" }).UsesSingleInstance.ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
