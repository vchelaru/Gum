using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Services;
using Gum.ToolStates;
using System.ComponentModel.Composition;
using System.Windows.Forms;

namespace Gum.Plugins.AlignmentButtons
{
    [Export(typeof(PluginBase))]
    public class AlignmentMainPlugin : InternalPlugin
    {
        private readonly ISelectedState _selectedState;
        
        AlignmentPluginControl control;
        bool isAdded = false;

        public AlignmentMainPlugin()
        {
            _selectedState = Locator.GetRequiredService<ISelectedState>();
        }
        
        public override void StartUp()
        {
            AssignEvents();

        }

        private void AssignEvents()
        {
            this.TreeNodeSelected += HandleTreeNodeSelected;
            this.StateWindowTreeNodeSelected += HandleStateWindowTreeNodeSelected;
        }

        private void HandleStateWindowTreeNodeSelected(TreeNode obj)
        {
            RefreshTabVisibility();
        }

        private void HandleTreeNodeSelected(TreeNode treeNode)
        {
            RefreshTabVisibility();
        }

        private void RefreshTabVisibility()
        {
            bool shouldAdd = DetermineIfShouldShowTab();

            if (shouldAdd)
            {
                if (control == null)
                {
                    control = new Gum.Plugins.AlignmentButtons.AlignmentPluginControl();
                }

                if (!isAdded)
                {
                    _guiCommands.AddControl(control, "Alignment");
                    isAdded = true;
                }
            }
            else
            {
                if (control != null && isAdded)
                {
                    _guiCommands.RemoveControl(control);
                    isAdded = false;
                }
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

            if(shouldAdd && _selectedState.SelectedInstance != null)
            {
                var elementSave = ObjectFinder.Self.GetRootStandardElementSave(_selectedState.SelectedInstance);

                if(elementSave?.Name == "Circle")
                {
                    // circles currently can't anchor...
                    shouldAdd = false;
                }
            }

            return shouldAdd;
        }
    }
}
