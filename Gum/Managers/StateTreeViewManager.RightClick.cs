using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonFormsAndControls;
using Gum.DataTypes.Variables;
using Gum.ToolStates;
using Gum.ToolCommands;
using System.Windows.Forms;

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

        internal void PopulateMenuStrip()
        {
            mMenuStrip.Items.Clear();

            var tsmi = new ToolStripMenuItem();
            tsmi.Text = "Add State";
            tsmi.Click += ( (obj, arg) =>
                {
                    
                    GumCommands.Self.Edit.AddState();
                });
            mMenuStrip.Items.Add(tsmi);

            if (SelectedState.Self.SelectedStateSave != null)
            {
                tsmi = new ToolStripMenuItem();
                tsmi.Text = "Rename State";
                tsmi.Click += ((obj, arg) =>
                {
                    TextInputWindow tiw = new TextInputWindow();
                    tiw.Message = "Enter new name";
                    tiw.Result = SelectedState.Self.SelectedStateSave.Name;
                    var result = tiw.ShowDialog();

                    if (result == DialogResult.OK)
                    {
                        SelectedState.Self.SelectedStateSave.Name = tiw.Result;
                        GumCommands.Self.GuiCommands.RefreshStateTreeView();
                        GumCommands.Self.FileCommands.SaveProject();
                        GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
                    }
                });
                mMenuStrip.Items.Add(tsmi);


            }

        }

    }
}
