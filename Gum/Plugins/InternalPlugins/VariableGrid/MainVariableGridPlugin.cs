using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Mvvm;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gum.Plugins.InternalPlugins.VariableGrid
{
    [Export(typeof(PluginBase))]
    public class MainVariableGridPlugin : InternalPlugin
    {
        public override void StartUp()
        {
            this.TreeNodeSelected += HandleTreeNodeSelected;
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
