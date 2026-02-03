using Gum.DataTypes.Variables;
using Gum.ToolStates;
using Gum.Wireframe;
using System;
using System.Windows.Forms;

namespace Gum.Managers
{
    public partial class PropertyGridManager
	{
        
        private void InitializeRightClickMenu()
        {
        }

        void HandleFileFromProjectSelect(object? sender, EventArgs args)
        {
            ToolStripMenuItem menuItem = sender as ToolStripMenuItem;

            string text = menuItem.Text;

            // We don't want to use a recursive variable finder because if the Instance
            // doesn't have this variable then the RVF will return the variable from the
            // base.  Instead, we want to set the value right on the instance if an instance
            // is selected.
            //VariableSave variable = _selectedState.SelectedRecursiveVariableFinder.GetVariable("SourceFile");
            if (_selectedState.SelectedInstance != null)
            {
                _selectedState.SelectedStateSave.SetValue(
                    _selectedState.SelectedInstance.Name + ".SourceFile", text, "string");

            }
            else
            {
                _selectedState.SelectedStateSave.SetValue(
                    "SourceFile", text, "string");

            }

            _wireframeObjectManager.RefreshAll(true);
            _guiCommands.RefreshVariables();

            _fileCommands.TryAutoSaveProject();
        }
    }
}
