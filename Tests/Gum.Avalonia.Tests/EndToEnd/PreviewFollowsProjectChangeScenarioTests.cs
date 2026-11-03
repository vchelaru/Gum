using System.Linq;
using Avalonia.Headless.XUnit;
using Gum.Avalonia.Plugins.EditorTab;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// A Preview left open while another project is created or opened must not keep showing the old
/// project (#7541). The real editor tab plugin, the real launcher and a stand-in preview process.
/// </summary>
[Trait("Category", "EndToEnd")]
public class PreviewFollowsProjectChangeScenarioTests
{
    private static PluginManager PluginManagerInstance => TestAppBuilder.Services.GetRequiredService<PluginManager>();

    [AvaloniaFact]
    public void CreatingANewProject_ClosesThePreviewOfTheOldOne()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave main = tree.Project.AddScreen("MainScreen");
        tree.SaveAll();
        AvaloniaEditorTabPlugin plugin = PluginManagerInstance.PluginContainers.Keys.OfType<AvaloniaEditorTabPlugin>().Single();
        StandInPreview preview = new StandInPreview();
        plugin.PreviewLauncher.ProcessStarter = preview;
        try
        {
            tree.Click(tree.NodeFor(main));
            plugin.PreviewLauncher.Launch();
            plugin.PreviewLauncher.IsRunning.ShouldBeTrue();

            IProjectManager projectManager = TestAppBuilder.Services.GetRequiredService<IProjectManager>();
            projectManager.CreateNewProject();
            plugin.CallProjectLoad(projectManager.GumProjectSave!);

            plugin.PreviewLauncher.IsRunning.ShouldBeFalse();
            preview.CloseCount.ShouldBe(1);
        }
        finally
        {
            plugin.PreviewLauncher.Close();
            preview.DeleteSelectionFile();
        }
    }
}
