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

        private IPluginTab _tab;

        [ImportingConstructor]
        public AlignmentMainPlugin(ISelectedState selectedState)
        {
            _selectedState = selectedState;
        }
        
        public override void StartUp()
        {
            AssignEvents();
            _tab = _tabManager.AddControl(new Gum.Plugins.AlignmentButtons.AlignmentPluginControl(), "Alignment");
            RefreshTabVisibility();
        }

        private void AssignEvents()
        {
            this.TreeNodeSelected += HandleTreeNodeSelected;
            this.StateWindowTreeNodeSelected += HandleStateWindowTreeNodeSelected;
        }

        private void HandleStateWindowTreeNodeSelected(ITreeNode obj)
        {
            RefreshTabVisibility();
        }

        private void HandleTreeNodeSelected(ITreeNode? treeNode)
        {
            RefreshTabVisibility();
        }

        private void RefreshTabVisibility()
        {
            if (DetermineIfShouldShowTab())
            {
                _tab.Show();
            }
            else
            {
                _tab.Hide();
            }
        }

        private bool DetermineIfShouldShowTab()
        {
            var shouldAdd = _selectedState.SelectedElement != null &&
                _selectedState.SelectedStateSave != null;

            if (shouldAdd)
            {
                if (_selectedState.SelectedScreen != null &&
                    _selectedState.SelectedInstance == null)
                {
                    // screens as a whole can't be aligned
                    shouldAdd = false;
                }

            }

            return shouldAdd;
        }
    }
}
