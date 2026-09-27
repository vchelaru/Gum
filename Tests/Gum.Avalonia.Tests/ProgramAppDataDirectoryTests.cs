using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The head's appsettings.json, crash logs and freeze diagnostics resolve through the same per-user
/// folder as the rest of the tool's settings.
/// </summary>
public class ProgramAppDataDirectoryTests : IDisposable
{
    private readonly string? _originalOverride;

    public ProgramAppDataDirectoryTests()
    {
        _originalOverride = FileManager.UserApplicationDataFolderOverride;
    }

    public void Dispose()
    {
        FileManager.UserApplicationDataFolderOverride = _originalOverride;
    }

    [Fact]
    public void GetAppDataDirectory_MatchesUserApplicationDataForThisApplication()
    {
        string folder = Path.Combine(Path.GetTempPath(), "GumHeadUserData");

        FileManager.UserApplicationDataFolderOverride = folder;
        Program.GetAppDataDirectory().ShouldBe(folder);

        // An empty --user-data value means no override for GeneralSettings.xml; appsettings.json must
        // not land in the working directory instead.
        FileManager.UserApplicationDataFolderOverride = "";
        Program.GetAppDataDirectory().ShouldBe(
            FileManager.UserApplicationDataForThisApplication.TrimEnd(Path.DirectorySeparatorChar));
    }
}
