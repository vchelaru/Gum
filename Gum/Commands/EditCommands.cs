using CommonFormsAndControls;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ToolCommands;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Gum.Commands
{
    public class EditCommands
    {
        public void AddState()
        {
            if (SelectedState.Self.SelectedElement == null)
            {
                MessageBox.Show("You must first select an element to add a state");
            }
            else
            {
                TextInputWindow tiw = new TextInputWindow();
                tiw.Message = "Enter new state name:";

                if (tiw.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string name = tiw.Result;

                    StateSave stateSave = ElementCommands.Self.AddState(
                        SelectedState.Self.SelectedElement, SelectedState.Self.SelectedStateCategorySave, name);

                    StateTreeViewManager.Self.RefreshUI(SelectedState.Self.SelectedElement);

                    SelectedState.Self.SelectedStateSave = stateSave;

                    GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
                }
            }

        }

        public void RemoveState(StateSave stateSave)
        {
            var response = MessageBox.Show($"Are you sure you want to delete the state {stateSave.Name}?", "Delete state?", MessageBoxButtons.YesNo);

            if (response == DialogResult.Yes)
            {
                ObjectRemover.Self.Remove(stateSave);
            }
        }


        public void RemoveStateCategory( StateSaveCategory category)
        {
            var response = MessageBox.Show($"Are you sure you want to delete the category {category.Name}?", "Delete category?", MessageBoxButtons.YesNo);

            if (response == DialogResult.Yes)
            {
                ObjectRemover.Self.Remove(category);
            }
        }

        public void AddCategory()
        {
            StateTreeViewManager.Self.AddStateCategoryClick();
        }
    }
}
