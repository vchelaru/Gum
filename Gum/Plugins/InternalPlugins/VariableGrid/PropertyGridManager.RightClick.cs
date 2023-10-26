using System;
using System.Windows.Forms;
using Gum.DataTypes.Variables;
using Gum.ToolStates;

namespace Gum.Managers
{
    public partial class PropertyGridManager
	{
        
        private void InitializeRightClickMenu()
        {
        }

        void HandleFileFromProjectSelect(object sender, EventArgs args)
        {
            ToolStripMenuItem menuItem = sender as ToolStripMenuItem;

            string text = menuItem.Text;

            // We don't want to use a recursive variable finder because if the Instance
            // doesn't have this variable then the RVF will return the variable from the
            // base.  Instead, we want to set the value right on the instance if an instance
            // is selected.
            //VariableSave variable = SelectedState.Self.SelectedRecursiveVariableFinder.GetVariable("SourceFile");
            if (SelectedState.Self.SelectedInstance != null)
            {
                SelectedState.Self.SelectedStateSave.SetValue(
                    SelectedState.Self.SelectedInstance.Name + ".SourceFile", text);

            }
            else
            {
                SelectedState.Self.SelectedStateSave.SetValue(
                    "SourceFile", text);

            }

            Wireframe.WireframeObjectManager.Self.RefreshAll(true);
            RefreshUI();

            GumCommands.Self.FileCommands.TryAutoSaveProject();
        }
    }
}
