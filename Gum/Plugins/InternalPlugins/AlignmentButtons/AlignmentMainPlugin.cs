using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using System.ComponentModel.Composition;
using System.Windows.Forms;

namespace Gum.Plugins.AlignmentButtons
{
    [Export(typeof(PluginBase))]
    public class AlignmentMainPlugin : InternalPlugin
    {
        AlignmentPluginControl control;
        bool isAdded = false;

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
                    GumCommands.Self.GuiCommands.AddControl(control, "Alignment");
                    isAdded = true;
                }
            }
            else
            {
                if (control != null && isAdded)
                {
                    GumCommands.Self.GuiCommands.RemoveControl(control);
                    isAdded = false;
                }
            }
        }

        private static bool DetermineIfShouldShowTab()
        {
            var shouldAdd = SelectedState.Self.SelectedElement != null &&
                SelectedState.Self.SelectedStateSave != null;

            if (shouldAdd)
            {
                if (SelectedState.Self.SelectedScreen != null &&
                    SelectedState.Self.SelectedInstance == null)
                {
                    // screens as a whole can't be aligned
                    shouldAdd = false;
                }

            }

            if(shouldAdd && SelectedState.Self.SelectedInstance != null)
            {
                var elementSave = ObjectFinder.Self.GetRootStandardElementSave(SelectedState.Self.SelectedInstance);

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
