using System.Runtime.InteropServices;
using Gum.Services;
using Shouldly;

namespace Gum.Presentation.Tests.Services;

public class FileSystemRevealServiceTests
{
    [Fact]
    public void BuildRevealCommand_ShouldUseExplorerSelect_OnWindows()
    {
        string path = Path.Combine(Path.GetTempPath(), "Screens", "MainMenu.gusx");

        (string fileName, string arguments) = FileSystemRevealService.BuildRevealCommand(path, OSPlatform.Windows);

        fileName.ShouldBe("explorer.exe");
        arguments.ShouldStartWith("/select,\"");
        arguments.ShouldEndWith("MainMenu.gusx\"");
        arguments.ShouldNotContain("/select,\"/");
    }

    [Fact]
    public void BuildRevealCommand_ShouldUseOpenReveal_OnMacOS()
    {
        string path = Path.Combine(Path.GetTempPath(), "Screens", "MainMenu.gusx");

        (string fileName, string arguments) = FileSystemRevealService.BuildRevealCommand(path, OSPlatform.OSX);

        fileName.ShouldBe("open");
        arguments.ShouldStartWith("-R \"");
        arguments.ShouldEndWith("MainMenu.gusx\"");
    }

    [Fact]
    public void BuildRevealCommand_ShouldOpenContainingFolder_OnLinux()
    {
        string path = Path.Combine(Path.GetTempPath(), "Screens", "MainMenu.gusx");

        (string fileName, string arguments) = FileSystemRevealService.BuildRevealCommand(path, OSPlatform.Linux);

        fileName.ShouldBe("xdg-open");
        arguments.ShouldEndWith("Screens\"");
        arguments.ShouldNotContain("MainMenu.gusx");
    }
}

public class ShellCommandTests
{
    [Fact]
    public void Build_ShouldUseCmd_OnWindows()
    {
        (string fileName, string arguments) = ShellCommand.Build("npm install", OSPlatform.Windows);

        fileName.ShouldBe("cmd.exe");
        arguments.ShouldBe("/c npm install");
    }

    [Fact]
    public void Build_ShouldUseSh_OffWindows()
    {
        (string fileName, string arguments) = ShellCommand.Build("npm install", OSPlatform.Linux);

        fileName.ShouldBe("/bin/sh");
        arguments.ShouldBe("-c \"npm install\"");
    }

    [Fact]
    public void Build_ShouldEscapeQuotes_OffWindows()
    {
        (string _, string arguments) = ShellCommand.Build("echo \"hi\"", OSPlatform.OSX);

        arguments.ShouldBe("-c \"echo \\\"hi\\\"\"");
    }
}
