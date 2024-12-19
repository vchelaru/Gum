using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Mvvm;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using System;
using System.ComponentModel.Composition;
using System.Windows.Forms;

namespace Gum.Plugins.InternalPlugins.VariableGrid
{
    [Export(typeof(PluginBase))]
    public class MainVariableGridPlugin : InternalPlugin
    {
        public override void StartUp()
        {
            this.TreeNodeSelected += HandleTreeNodeSelected;
            this.ReactToStateSaveSelected += HandleStateSelected;
            this.ReactToStateSaveCategorySelected += MainVariableGridPlugin_ReactToStateSaveCategorySelected;
            this.StateMovedToCategory += HandleStateMovedToCategory;
        }

        private void MainVariableGridPlugin_ReactToStateSaveCategorySelected(StateSaveCategory obj)
        {
            PropertyGridManager.Self.RefreshUI(force: true);

        }

        private void HandleStateMovedToCategory(StateSave save, StateSaveCategory category1, StateSaveCategory category2)
        {
            PropertyGridManager.Self.RefreshUI(force: true);
        }

        private void HandleStateSelected(StateSave save)
        {
            PropertyGridManager.Self.RefreshUI(force: true);
        }

        private void HandleTreeNodeSelected(TreeNode node)
        {
            var shouldShowButton = (GumState.Self.SelectedState.SelectedBehavior != null ||
                GumState.Self.SelectedState.SelectedComponent != null ||
                GumState.Self.SelectedState.SelectedScreen != null);
            if(shouldShowButton)
            {
                shouldShowButton = GumState.Self.SelectedState.SelectedInstance == null;
            }
            PropertyGridManager.Self.VariableViewModel.AddVariableButtonVisibility =
                shouldShowButton.ToVisibility();
        }
    }
}
