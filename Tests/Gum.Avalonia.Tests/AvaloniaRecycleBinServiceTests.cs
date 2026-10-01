using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Gum.Avalonia.Services;
using Shouldly;
using Xunit;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Covers the Linux trash command: a batch goes out as one <c>gio</c> process.
/// </summary>
public class AvaloniaRecycleBinServiceTests
{
    [Fact]
    public void CreateTrashCommand_OnLinux_PassesEveryFileToOneGioCall()
    {
        List<string> paths = new List<string> { "/game/Content/A.gucx", "/game/Content/My Screen.gusx" };

        ProcessStartInfo command = AvaloniaRecycleBinService.CreateTrashCommand(paths);

        command.FileName.ShouldBe("gio");
        command.ArgumentList.ToArray().ShouldBe(new[] { "trash", "/game/Content/A.gucx", "/game/Content/My Screen.gusx" });
    }
}
