using System.IO;
using Shouldly;
using ToolsUtilities;

namespace GumTestSupport;

/// <summary>
/// Compiled into every tool test project, so each one proves its own process never uses the
/// machine-wide per-user folder that every test run shares.
/// </summary>
public class TestAppDataFolderTests
{
    [Fact]
    public void UserApplicationDataForThisApplication_IsThisProcessTempFolder()
    {
        string tempRoot = Path.GetFullPath(Path.GetTempPath());

        string resolved = FileManager.UserApplicationDataForThisApplication;

        resolved.ShouldBe(TestAppDataFolder.Path + Path.DirectorySeparatorChar);
        Path.GetFullPath(resolved).ShouldStartWith(tempRoot);
        Directory.Exists(resolved).ShouldBeTrue();
    }
}
