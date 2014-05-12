using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonFormsAndControls;
using Gum.DataTypes.Variables;
using Gum.ToolStates;
using Gum.ToolCommands;
using System.Windows.Forms;
using Gum.Commands;
using ToolsUtilities;

namespace Gum.Managers
{
    public partial class StateTreeViewManager
    {


        internal void AddStateClick()
        {
            GumCommands.Self.Edit.AddState();
        }

        internal void AddStateCategoryClick()
        {
            if (SelectedState.Self.SelectedElement == null)
            {
                MessageBox.Show("You must first select an element to add a state category");
            }
            else
            {
                TextInputWindow tiw = new TextInputWindow();
                tiw.Message = "Enter new category name:";

                if (tiw.ShowDialog() == DialogResult.OK)
                {
                    string name = tiw.Result;

                    StateSaveCategory category = ElementCommands.Self.AddCategory(
                        SelectedState.Self.SelectedElement, name);

                    RefreshUI(SelectedState.Self.SelectedElement);

                    SelectedState.Self.SelectedStateCategorySave = category;

                    GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();

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

            tsmi = new ToolStripMenuItem();
            tsmi.Text = "Add Category";
            tsmi.Click += ((obj, arg) =>
            {

                GumCommands.Self.Edit.AddCategory();
            });
            mMenuStrip.Items.Add(tsmi);

            if (SelectedState.Self.SelectedStateSave != null)
            {
                AddRenameStateMenuItem();

                AddDuplicateStateMenuItem();

                if (SelectedState.Self.SelectedStateSave != SelectedState.Self.SelectedElement.DefaultState)
                {
                    AddMakeDefaultStateMenuItem();
                }
            }

        }

        private void AddMakeDefaultStateMenuItem()
        {
            ToolStripMenuItem tsmi = new ToolStripMenuItem();

            tsmi.Text = "Make Default";

            tsmi.Click += delegate
            {
                StateSave state = SelectedState.Self.SelectedStateSave;

                var element = SelectedState.Self.SelectedElement;

                if (!element.States.Contains(state))
                {
                    // It's categorized
                    MessageBox.Show("Categorized states cannot be made default");
                }
                else
                {
                    element.States.Remove(state);
                    element.States.Insert(0, state);

                    StateTreeViewManager.Self.RefreshUI(SelectedState.Self.SelectedElement);

                    SelectedState.Self.SelectedStateSave = state;

                    GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
                }

            };
            mMenuStrip.Items.Add(tsmi);
        }

        private void AddDuplicateStateMenuItem()
        {
            ToolStripMenuItem tsmi = new ToolStripMenuItem();
            tsmi.Text = "Duplicate State";

            tsmi.Click += delegate
            {
                StateSave newState = SelectedState.Self.SelectedStateSave.Clone();


                newState.ParentContainer = SelectedState.Self.SelectedElement;

                if (SelectedState.Self.SelectedStateCategorySave == null)
                {
                    int index = SelectedState.Self.SelectedElement.States.IndexOf(SelectedState.Self.SelectedStateSave);
                    SelectedState.Self.SelectedElement.States.Insert(index+1, newState);
                }
                else
                {
                    int index = SelectedState.Self.SelectedStateCategorySave.States.IndexOf(SelectedState.Self.SelectedStateSave);
                    SelectedState.Self.SelectedStateCategorySave.States.Insert(index+1, newState);
                }


                while (SelectedState.Self.SelectedElement.AllStates.Any(item => item != newState && item.Name == newState.Name))
                {
                    newState.Name = StringFunctions.IncrementNumberAtEnd(newState.Name);
                }

                StateTreeViewManager.Self.RefreshUI(SelectedState.Self.SelectedElement);

                SelectedState.Self.SelectedStateSave = newState;

                GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();

            };
            mMenuStrip.Items.Add(tsmi);
        }

        private ToolStripMenuItem AddRenameStateMenuItem()
        {
            ToolStripMenuItem tsmi = new ToolStripMenuItem();
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
                    // I don't think we need to save the project when renaming a state:
                    //GumCommands.Self.FileCommands.TryAutoSaveProject();

                    GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
                }
            });
            mMenuStrip.Items.Add(tsmi);
            return tsmi;
        }

    }
}
