using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.AlignmentButtons.ViewModels;
using Gum.ToolStates;
using System.ComponentModel.Composition;

namespace Gum.Plugins.AlignmentButtons
{
    /// <summary>
    /// The Alignment tab, shared by both heads: shows or hides the tab as the selection changes and
    /// keeps the state banner current. Each head supplies the view for <see cref="AlignmentViewModel"/>
    /// (TabViewRegistry).
    /// </summary>
    [Export(typeof(PluginBase))]
    public class AlignmentMainPlugin : CorePriorityPlugin
    {
        private readonly ISelectedState _selectedState;
        private readonly AlignmentViewModel _viewModel;

        private AlignmentTabVisibilityCoordinator _coordinator = null!;

        [ImportingConstructor]
        public AlignmentMainPlugin(ISelectedState selectedState, AlignmentViewModel viewModel)
        {
            _selectedState = selectedState;
            _viewModel = viewModel;
        }

        public override void StartUp()
        {
            AssignEvents();
            // Each head resolves the view model to its own view (TabViewRegistry).
            IPluginTab tab = _tabManager.AddControl(_viewModel, "Alignment");
            _coordinator = new AlignmentTabVisibilityCoordinator(_selectedState, tab);
            Refresh();
        }

        private void AssignEvents()
        {
            this.TreeNodeSelected += HandleTreeNodeSelected;
            this.ReactToStateSaveSelected += HandleStateSaveSelected;
            this.ReactToStateSaveCategorySelected += HandleStateSaveCategorySelected;
            this.InstanceSelected += HandleInstanceSelected;
        }

        private void Refresh()
        {
            _coordinator.Refresh();
            _viewModel.RefreshStateLabel();
        }

        private void HandleStateSaveSelected(StateSave? state)
        {
            Refresh();
        }

        private void HandleStateSaveCategorySelected(StateSaveCategory? category)
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
