using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonFormsAndControls;
using Gum.DataTypes.Variables;
using Gum.ToolStates;
using Gum.ToolCommands;

namespace Gum.Managers
{
    public partial class StateTreeViewManager
    {


        internal void AddStateClick()
        {
            TextInputWindow tiw = new TextInputWindow();
            tiw.Message = "Enter new State name:";

            if (tiw.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string name = tiw.Result;

                StateSave stateSave = ElementCommands.Self.AddState(
                    SelectedState.Self.SelectedElement, name);

                RefreshUI(SelectedState.Self.SelectedElement);

                SelectedState.Self.SelectedStateSave = stateSave;
                if (ProjectManager.Self.GeneralSettingsFile.AutoSave)
                {
                    ProjectManager.Self.SaveElement(SelectedState.Self.SelectedElement);
                }
            }
        }



    }
}
