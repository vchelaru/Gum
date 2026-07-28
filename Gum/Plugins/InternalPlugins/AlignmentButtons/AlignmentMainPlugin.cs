using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using System.ComponentModel.Composition;

namespace Gum.Plugins.AlignmentButtons
{
    [Export(typeof(PluginBase))]
    public class AlignmentMainPlugin : PriorityPlugin
    {
        private readonly ISelectedState _selectedState;

        private AlignmentTabVisibilityCoordinator _coordinator;

        [ImportingConstructor]
        public AlignmentMainPlugin(ISelectedState selectedState)
        {
            _selectedState = selectedState;
        }

        public override void StartUp()
        {
            AssignEvents();
            var tab = _tabManager.AddControl(new Gum.Plugins.AlignmentButtons.AlignmentPluginControl(), "Alignment");
            _coordinator = new AlignmentTabVisibilityCoordinator(_selectedState, tab);
            _coordinator.Refresh();
        }

        private void AssignEvents()
        {
            this.TreeNodeSelected += HandleTreeNodeSelected;
            this.StateWindowTreeNodeSelected += HandleStateWindowTreeNodeSelected;
            this.InstanceSelected += HandleInstanceSelected;
        }

        private void HandleStateWindowTreeNodeSelected(ITreeNode obj)
        {
            _coordinator.Refresh();
        }

        private void HandleTreeNodeSelected(ITreeNode? treeNode)
        {
            _coordinator.Refresh();
        }

        private void HandleInstanceSelected(ElementSave elementSave, InstanceSave instance)
        {
            // Auto-selecting a new instance (e.g. right-click Add Object on an already-selected
            // Screen) only raises InstanceSelected, not TreeNodeSelected - see issue #4067.
            _coordinator.Refresh();
        }
    }
}
