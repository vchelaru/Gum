using System.IO;
using Shouldly;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// An unattended run of a head redirects every per-user file (GeneralSettings.xml and the rest)
/// through this override, so it never writes its temp project into the user's own settings.
/// </summary>
public class UserApplicationDataOverrideTests
{
    [Fact]
    public void UserApplicationDataForThisApplication_UsesTheOverride_WithATrailingSeparator()
    {
        string folder = Path.Combine(Path.GetTempPath(), "GumUserDataOverride");
        string original = FileManager.UserApplicationDataForThisApplication;
        string? originalOverride = FileManager.UserApplicationDataFolderOverride;
        try
        {
            FileManager.UserApplicationDataFolderOverride = folder;
            FileManager.UserApplicationDataForThisApplication.ShouldBe(folder + Path.DirectorySeparatorChar);

            FileManager.UserApplicationDataFolderOverride = folder + Path.DirectorySeparatorChar;
            FileManager.UserApplicationDataForThisApplication.ShouldBe(folder + Path.DirectorySeparatorChar);
        }
        finally
        {
            // The test process has its own override (TestAppDataFolder); clearing it would send every
            // later test to the shared per-user folder.
            FileManager.UserApplicationDataFolderOverride = originalOverride;
        }
        FileManager.UserApplicationDataForThisApplication.ShouldBe(original);
    }
}
