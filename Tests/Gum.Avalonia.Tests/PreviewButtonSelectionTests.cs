using Avalonia.Headless.XUnit;
using Gum.Avalonia.Plugins.EditorTab;
using Gum.DataTypes;
using Gum.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The Preview button is clickable whenever an element or one of its instances is selected (#6990).
/// Selecting an instance straight from no selection raises only InstanceSelected, not ElementSelected.
/// </summary>
public class PreviewButtonSelectionTests
{
    [AvaloniaFact]
    public void PreviewCommand_IsEnabled_WhenAnInstanceIsSelectedFromNoSelection()
    {
        PluginManager pluginManager = TestAppBuilder.Services.GetRequiredService<PluginManager>();
        AvaloniaEditorTabPlugin plugin = pluginManager.PluginContainers.Keys.OfType<AvaloniaEditorTabPlugin>().Single();
        ScreenSave screen = new ScreenSave { Name = "Screen" };
        InstanceSave instance = new InstanceSave { Name = "Instance", BaseType = "Container", ParentContainer = screen };
        try
        {
            plugin.Editor.PreviewCommand.CanExecute(null).ShouldBeFalse();

            plugin.CallInstanceSelected(screen, instance);

            plugin.Editor.PreviewCommand.CanExecute(null).ShouldBeTrue();
        }
        finally
        {
            plugin.CallElementSelected(null);
        }
    }
}
