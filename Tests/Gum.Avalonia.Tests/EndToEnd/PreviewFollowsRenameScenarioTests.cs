using System.Diagnostics;
using Avalonia.Headless.XUnit;
using Gum.Avalonia.Plugins.EditorTab;
using Gum.DataTypes;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// Preview follows a rename made in the tree: the rename keeps the element selected, so no
/// selection event follows, and the selection file would keep the old name (#7184). The real
/// editor tab plugin, the real launcher and a stand-in preview process.
/// </summary>
[Trait("Category", "EndToEnd")]
public class PreviewFollowsRenameScenarioTests
{
    private static PluginManager PluginManagerInstance => TestAppBuilder.Services.GetRequiredService<PluginManager>();

    [AvaloniaFact]
    public void RenamingTheShownScreen_PushesTheNewNameToPreview()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave main = tree.Project.AddScreen("MainMenu");
        tree.SaveAll();
        AvaloniaEditorTabPlugin plugin = PluginManagerInstance.PluginContainers.Keys.OfType<AvaloniaEditorTabPlugin>().Single();
        StandInPreview preview = new StandInPreview();
        plugin.PreviewLauncher.ProcessStarter = preview;
        try
        {
            tree.Click(tree.NodeFor(main));
            plugin.PreviewLauncher.Launch();

            // The editor tab plugin sits out of event dispatch without a canvas, so deliver the
            // event RenameLogic raises after a tree rename to it directly.
            main.Name = "MainMenu1234";
            plugin.CallElementRename(main, "MainMenu");

            preview.LastPushed().ElementName.ShouldBe("MainMenu1234");
        }
        finally
        {
            plugin.PreviewLauncher.Close();
            preview.DeleteSelectionFile();
        }
    }

    [AvaloniaFact]
    public void DeletingTheShownScreen_ThenSelectingAnother_PushesTheOther()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave main = tree.Project.AddScreen("MainMenu");
        ScreenSave other = tree.Project.AddScreen("Other");
        tree.SaveAll();
        AvaloniaEditorTabPlugin plugin = PluginManagerInstance.PluginContainers.Keys.OfType<AvaloniaEditorTabPlugin>().Single();
        StandInPreview preview = new StandInPreview();
        plugin.PreviewLauncher.ProcessStarter = preview;
        try
        {
            tree.Click(tree.NodeFor(main));
            plugin.PreviewLauncher.Launch();

            plugin.CallElementDelete(main);
            plugin.PreviewLauncher.PushSelection(other);

            preview.LastPushed().ElementName.ShouldBe("Other");
        }
        finally
        {
            plugin.PreviewLauncher.Close();
            preview.DeleteSelectionFile();
        }
    }

    [AvaloniaFact]
    public void DeletingTheShownScreen_WithNothingSelected_PushesNothing()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave main = tree.Project.AddScreen("MainMenu");
        tree.SaveAll();
        AvaloniaEditorTabPlugin plugin = PluginManagerInstance.PluginContainers.Keys.OfType<AvaloniaEditorTabPlugin>().Single();
        StandInPreview preview = new StandInPreview();
        plugin.PreviewLauncher.ProcessStarter = preview;
        try
        {
            tree.Click(tree.NodeFor(main));
            plugin.PreviewLauncher.Launch();

            plugin.CallElementDelete(main);
            plugin.PreviewLauncher.PushSelection(null);

            // Preview empties itself: its reload of the saved project finds the name gone.
            preview.LastPushed().ElementName.ShouldBe("MainMenu");
        }
        finally
        {
            plugin.PreviewLauncher.Close();
            preview.DeleteSelectionFile();
        }
    }

    private sealed class StandInPreview : IPreviewProcessStarter, IPreviewProcess
    {
        private ProcessStartInfo? _startInfo;

        public bool HasExited { get; private set; }

        public void Close() => HasExited = true;

        public ResolvedPreviewExecutable? Resolve() => new ResolvedPreviewExecutable("/Preview/GumPreview", IsNativeAot: false);

        public IPreviewProcess? Start(ProcessStartInfo startInfo)
        {
            _startInfo = startInfo;
            return this;
        }

        public PreviewSelectionMessage LastPushed() =>
            PreviewSelectionMessage.TryParse(File.ReadAllLines(SelectionFile)).ShouldNotBeNull();

        public void DeleteSelectionFile()
        {
            if (_startInfo != null)
            {
                File.Delete(SelectionFile);
            }
        }

        private string SelectionFile
        {
            get
            {
                List<string> arguments = _startInfo!.ArgumentList.ToList();
                return arguments[arguments.IndexOf("--selection-file") + 1];
            }
        }
    }
}
