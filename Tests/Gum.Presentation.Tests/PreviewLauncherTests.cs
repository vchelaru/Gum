using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// Covers the selection-file protocol's pure formatting logic. The rest of <see cref="PreviewLauncher"/>
/// only branches once a real preview process is running, which requires spawning an actual process to
/// reach (see <c>GumToolUnitTests.Plugins.InternalPlugins.EditorTab.PreviewLauncherTests</c> for the
/// validation-branch coverage that doesn't need one).
/// </summary>
public class PreviewLauncherTests
{
    [Fact]
    public void BuildSelectionFileContent_WhenActivate_AppendsTheActivateMarker()
    {
        PreviewLauncher.BuildSelectionFileContent("MainMenu", activate: true).ShouldBe("MainMenu\nactivate");
    }

    [Fact]
    public void BuildSelectionFileContent_WhenNotActivate_IsJustTheElementName()
    {
        PreviewLauncher.BuildSelectionFileContent("MainMenu", activate: false).ShouldBe("MainMenu");
    }
}
