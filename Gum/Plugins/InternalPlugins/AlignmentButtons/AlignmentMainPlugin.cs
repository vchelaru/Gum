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
        private AlignmentPluginControl _control;

        [ImportingConstructor]
        public AlignmentMainPlugin(ISelectedState selectedState)
        {
            _selectedState = selectedState;
        }

        public override void StartUp()
        {
            AssignEvents();
            _control = new Gum.Plugins.AlignmentButtons.AlignmentPluginControl();
            var tab = _tabManager.AddControl(_control, "Alignment");
            _coordinator = new AlignmentTabVisibilityCoordinator(_selectedState, tab);
            Refresh();
        }

        private void AssignEvents()
        {
            this.TreeNodeSelected += HandleTreeNodeSelected;
            this.StateWindowTreeNodeSelected += HandleStateWindowTreeNodeSelected;
            this.InstanceSelected += HandleInstanceSelected;
        }

        private void Refresh()
        {
            _coordinator.Refresh();
            _control.ViewModel.RefreshStateLabel();
        }

        private void HandleStateWindowTreeNodeSelected(ITreeNode obj)
        {
            Refresh();
        }

        private void HandleTreeNodeSelected(ITreeNode? treeNode)
        {
            Refresh();
        }

        private void HandleInstanceSelected(ElementSave elementSave, InstanceSave instance)
        {
            // Auto-selecting a new instance (e.g. right-click Add Object on an already-selected
            // Screen) only raises InstanceSelected, not TreeNodeSelected - see issue #4067.
            Refresh();
        }
    }
}
