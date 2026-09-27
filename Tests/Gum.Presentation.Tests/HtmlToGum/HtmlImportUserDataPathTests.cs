using System.IO;
using HtmlToGumPlugin;
using Shouldly;
using ToolsUtilities;

namespace Gum.Presentation.Tests.HtmlToGum;

/// <summary>
/// HTML import's per-user files follow the app-data override, so <c>--user-data</c> runs and tests
/// never read or write the user's own import prefs.
/// </summary>
public class HtmlImportUserDataPathTests : BaseTestClass
{
    [Fact]
    public void PrefsAndTimingLog_ResolveUnderTheUserApplicationDataOverride()
    {
        string folder = Path.Combine(Path.GetTempPath(), "GumHtmlImportUserData");
        FileManager.UserApplicationDataFolderOverride = folder;

        ImportPrefs.PrefsPath.ShouldBe(Path.Combine(folder, "HtmlToGumPlugin", "import-prefs.json"));
        HtmlImportTimingLog.LogPath.ShouldBe(Path.Combine(folder, "HtmlToGumPlugin", "import-timings.log"));
    }
}
