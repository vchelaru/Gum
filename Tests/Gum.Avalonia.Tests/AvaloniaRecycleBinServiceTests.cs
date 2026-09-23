using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Gum.Avalonia.Services;
using Shouldly;
using Xunit;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Covers the non-Windows trash commands: a batch must go out as one process so macOS plays the
/// trash sound once, not once per file (issue #4926).
/// </summary>
public class AvaloniaRecycleBinServiceTests
{
    [Fact]
    public void CreateTrashCommand_OnLinux_PassesEveryFileToOneGioCall()
    {
        List<string> paths = new List<string> { "/game/Content/A.gucx", "/game/Content/My Screen.gusx" };

        ProcessStartInfo command = AvaloniaRecycleBinService.CreateTrashCommand(paths, isMacOS: false);

        command.FileName.ShouldBe("gio");
        command.ArgumentList.ToArray().ShouldBe(new[] { "trash", "/game/Content/A.gucx", "/game/Content/My Screen.gusx" });
    }

    [Fact]
    public void CreateTrashCommand_OnMacOS_DeletesEveryFileInOneFinderCall_WithQuotesEscaped()
    {
        List<string> paths = new List<string> { "/game/A.gucx", "/game/Say \"hi\".gusx" };

        ProcessStartInfo command = AvaloniaRecycleBinService.CreateTrashCommand(paths, isMacOS: true);

        command.FileName.ShouldBe("osascript");
        command.ArgumentList.ToArray().ShouldBe(new[]
        {
            "-e",
            "tell application \"Finder\" to delete {POSIX file \"/game/A.gucx\", POSIX file \"/game/Say \\\"hi\\\".gusx\"}",
        });
    }
}
