using Gum.Controls;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.PropertyGridHelpers;
using Gum.ToolStates;
using System;
using System.ComponentModel.Composition;
using System.Windows.Forms;

namespace Gum.Plugins.StatePlugin
{
    // This is new as of Oct 30, 2020
    // I'd like to move all state logic to this plugin over time.
    [Export(typeof(Gum.Plugins.BaseClasses.PluginBase))]
    public class MainStatePlugin : InternalPlugin
    {
        StateView stateView;

        PluginTab pluginTab;

        public override void StartUp()
        {
            this.StateWindowTreeNodeSelected += HandleStateSelected;
            this.TreeNodeSelected += HandleTreeNodeSelected;

            stateView = new StateView();
            pluginTab = GumCommands.Self.GuiCommands.AddControl(stateView, "States", TabLocation.CenterTop);

            ((SelectedState)SelectedState.Self).StateView = stateView;

            // State Tree ViewManager needs init before MenuStripManager
            StateTreeViewManager.Self.Initialize(this.stateView.TreeView, this.stateView.StateContextMenuStrip);
        }

        private void HandleTreeNodeSelected(TreeNode node)
        {
            var element = SelectedState.Self.SelectedElement;
            if(element != null)
            {
                pluginTab.Title = $"{element.Name} States";

            }
            else
            {
                pluginTab.Title = "States";
            }
        }

        private void HandleStateSelected(TreeNode stateTreeNode)
        {
            var currentCategory = SelectedState.Self.SelectedStateCategorySave;
            var currentState = SelectedState.Self.SelectedStateSave;

            if(currentCategory != null && currentState != null)
            {
                PropagateVariableForCategorizedState(currentState);
            }
            else if(currentCategory != null)
            {
                foreach(var state in currentCategory.States)
                {
                    PropagateVariableForCategorizedState(state);
                }
            }
        }

        private void PropagateVariableForCategorizedState(StateSave currentState)
        {
            foreach(var variable in currentState.Variables)
            {
                VariableInCategoryPropagationLogic.Self.PropagateVariablesInCategory(variable.Name);
            }
        }
    }
}
