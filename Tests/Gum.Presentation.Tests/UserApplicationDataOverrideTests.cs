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
        try
        {
            FileManager.UserApplicationDataFolderOverride = folder;
            FileManager.UserApplicationDataForThisApplication.ShouldBe(folder + Path.DirectorySeparatorChar);

            FileManager.UserApplicationDataFolderOverride = folder + Path.DirectorySeparatorChar;
            FileManager.UserApplicationDataForThisApplication.ShouldBe(folder + Path.DirectorySeparatorChar);
        }
        finally
        {
            FileManager.UserApplicationDataFolderOverride = null;
        }
        FileManager.UserApplicationDataForThisApplication.ShouldBe(original);
    }
}
