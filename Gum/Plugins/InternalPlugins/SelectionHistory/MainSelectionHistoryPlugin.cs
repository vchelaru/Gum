using Gum.DataTypes;
using Gum.Plugins.BaseClasses;
using Gum.SelectionHistory;
using System.ComponentModel.Composition;

namespace Gum.Plugins.InternalPlugins.SelectionHistory;

/// <summary>
/// Feeds every real element/instance selection into <see cref="ISelectionHistory"/> so the
/// mouse Back/Forward buttons (wired in ElementTreeViewManager and MainWindow) have a stack
/// to navigate. Recording is the plugin's only job - the stack and navigation logic live in
/// SelectionHistoryService, which is independently unit tested.
/// </summary>
[Export(typeof(PluginBase))]
internal class MainSelectionHistoryPlugin : PriorityPlugin
{
    private readonly ISelectionHistory _selectionHistory;

    [ImportingConstructor]
    public MainSelectionHistoryPlugin(ISelectionHistory selectionHistory)
    {
        _selectionHistory = selectionHistory;
    }

    public override void StartUp()
    {
        this.InstanceSelected += HandleInstanceSelected;
        this.ElementSelected += HandleElementSelected;
    }

    private void HandleInstanceSelected(ElementSave element, InstanceSave instance)
    {
        _selectionHistory.RecordSelection(element, instance);
    }

    private void HandleElementSelected(ElementSave? element)
    {
        _selectionHistory.RecordSelection(element, null);
    }
}
