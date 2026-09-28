using Gum.DataTypes;
using Gum.Plugins.BaseClasses;
using Gum.SelectionHistory;
using System.ComponentModel.Composition;

namespace Gum.Plugins.InternalPlugins.SelectionHistory;

/// <summary>
/// Feeds every real element/instance selection into <see cref="ISelectionHistory"/> so the
/// mouse Back/Forward buttons (wired in ElementTreeViewManager and MainWindow) have a stack
/// to navigate, and tells it what was deleted. The stack and navigation logic live in
/// SelectionHistoryService, which is independently unit tested.
/// </summary>
[Export(typeof(PluginBase))]
internal class MainSelectionHistoryPlugin : CorePriorityPlugin
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
        // A deleted element or instance is nowhere to go back to.
        this.ElementDelete += _selectionHistory.ForgetElement;
        this.InstanceDelete += (_, instance) => _selectionHistory.ForgetInstance(instance);
        this.InstancesDelete += (_, instances) =>
        {
            foreach (InstanceSave instance in instances)
            {
                _selectionHistory.ForgetInstance(instance);
            }
        };
    }

    private void HandleInstanceSelected(ElementSave? element, InstanceSave? instance)
    {
        _selectionHistory.RecordSelection(element, instance);
    }

    private void HandleElementSelected(ElementSave? element)
    {
        _selectionHistory.RecordSelection(element, null);
    }
}
