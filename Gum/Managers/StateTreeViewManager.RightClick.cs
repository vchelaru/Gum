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
using Gum.Plugins;

namespace Gum.Managers
{

    public partial class StateTreeViewManager
    {
        const string mNoCategory = "<no category>";

        #region Add to menu

        internal void PopulateMenuStrip()
        {
            mMenuStrip.Items.Clear();

            AddMenuItem("Add State", GumCommands.Self.Edit.AddState);

            AddMenuItem("Add Category", GumCommands.Self.Edit.AddCategory);

            if (SelectedState.Self.SelectedStateSave != null)
            {
                mMenuStrip.Items.Add("-");

                AddMenuItem("Rename State", RenameStateClick);

                AddMenuItem("Duplicate State", DuplicateStateClick);

                AddMenuItem("Delete " + SelectedState.Self.SelectedStateSave.Name, DeleteStateClick);

                if (SelectedState.Self.SelectedStateSave != SelectedState.Self.SelectedElement.DefaultState)
                {
                    mMenuStrip.Items.Add("-");

                    AddMenuItem("Make Default", MakeDefaultClick);

                    AddMoveToCategoryItems();

                    mMenuStrip.Items.Add("-");

                    if (GetIfCanMoveUp(SelectedState.Self.SelectedStateSave, SelectedState.Self.SelectedStateCategorySave))
                    {
                        AddMenuItem("^ Move Up", MoveUpClick);
                    }
                    AddMenuItem("v Move Down", MoveDownClick);
                }
            }
            if(SelectedState.Self.SelectedStateCategorySave != null)
            {

                mMenuStrip.Items.Add("-");

                AddMenuItem("Rename Category", RenameCategoryClick);


                AddMenuItem("Delete " + SelectedState.Self.SelectedStateCategorySave.Name, DeleteCategoryClick);
            }
        }

        private void MoveUpClick()
        {
            MoveStateInDirection(-1);
        }

        private void MoveDownClick()
        {
            MoveStateInDirection(1);
        }

        private void MoveStateInDirection(int direction)
        {
            var state = SelectedState.Self.SelectedStateSave;
            var list = SelectedState.Self.SelectedElement.States;
            if(SelectedState.Self.SelectedStateCategorySave != null)
            {
                list = SelectedState.Self.SelectedStateCategorySave.States;
            }

            if(list != null && list.Contains(state))
            {
                int oldIndex = list.IndexOf(state);

                bool shouldSave = false;

                if(direction == -1 && GetIfCanMoveUp(state, SelectedState.Self.SelectedStateCategorySave))
                {
                    list.RemoveAt(oldIndex);
                    list.Insert(oldIndex - 1, state);
                    shouldSave = true;
                }
                else if(direction == 1 &&  oldIndex != list.Count-1)
                {
                    list.RemoveAt(oldIndex);
                    list.Insert(oldIndex + 1, state);
                    shouldSave = true;
                }

                if(shouldSave)
                {
                    GumCommands.Self.GuiCommands.RefreshStateTreeView();

                    GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
                }
            }
        }

        bool GetIfCanMoveUp(StateSave state, StateSaveCategory category)
        {
            var list = SelectedState.Self.SelectedElement.States;
            if (category != null)
            {
                list = category.States;
            }

            int stateIndex = list.IndexOf(state);

            int indexToBeGreaterThan = 0;
            if (category == null)
            {
                // Uncategorized, so it can't move up above the Default state
                indexToBeGreaterThan = 1;
            }

            return stateIndex > indexToBeGreaterThan;
        }


        private void DeleteCategoryClick()
        {
            GumCommands.Self.Edit.RemoveStateCategory(SelectedState.Self.SelectedStateCategorySave);
        }

        private void DeleteStateClick()
        {
            GumCommands.Self.Edit.RemoveState(SelectedState.Self.SelectedStateSave);
        }

        private void AddMoveToCategoryItems()
        {
            var categoryNames = SelectedState.Self.SelectedElement.Categories
                .Where(item=>item != SelectedState.Self.SelectedStateCategorySave)
                .Select(item => item.Name).ToList();

            if(SelectedState.Self.SelectedStateCategorySave != null)
            {
                categoryNames.Insert(0, mNoCategory);
            }

            if(categoryNames.Count != 0)
            {
                var rootItem = AddMenuItem("Move to category", null);

                foreach(var categoryName in categoryNames)
                {
                    var categorySpecificItem = new ToolStripMenuItem();
                    categorySpecificItem.Text = categoryName;
                    rootItem.DropDownItems.Add(categorySpecificItem);

                    // make a local var to prevent problems with delayed evaluation
                    string categoryNameEvaluated = categoryName;

                    categorySpecificItem.Click += delegate
                    {
                        MoveToCategory(categoryNameEvaluated);

                    };
                }
            }
        }

        private ToolStripMenuItem AddMenuItem(string text, Action clickAction)
        {
            var tsmi = new ToolStripMenuItem();
            tsmi.Text = text;
            if(clickAction != null)
            {
                tsmi.Click += delegate
                {
                    clickAction();
                };
            }
            mMenuStrip.Items.Add(tsmi);

            return tsmi;
        }

        #endregion

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

        private static void MakeDefaultClick()
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
        }

        private static void DuplicateStateClick()
        {
            StateSave newState = SelectedState.Self.SelectedStateSave.Clone();


            newState.ParentContainer = SelectedState.Self.SelectedElement;

            if (SelectedState.Self.SelectedStateCategorySave == null)
            {
                int index = SelectedState.Self.SelectedElement.States.IndexOf(SelectedState.Self.SelectedStateSave);
                SelectedState.Self.SelectedElement.States.Insert(index + 1, newState);
            }
            else
            {
                int index = SelectedState.Self.SelectedStateCategorySave.States.IndexOf(SelectedState.Self.SelectedStateSave);
                SelectedState.Self.SelectedStateCategorySave.States.Insert(index + 1, newState);
            }


            while (SelectedState.Self.SelectedElement.AllStates.Any(item => item != newState && item.Name == newState.Name))
            {
                newState.Name = StringFunctions.IncrementNumberAtEnd(newState.Name);
            }

            StateTreeViewManager.Self.RefreshUI(SelectedState.Self.SelectedElement);

            SelectedState.Self.SelectedStateSave = newState;

            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
        }

        private static void RenameStateClick()
        {
            TextInputWindow tiw = new TextInputWindow();
            tiw.Message = "Enter new state name";
            tiw.Result = SelectedState.Self.SelectedStateSave.Name;
            var result = tiw.ShowDialog();

            if (result == DialogResult.OK)
            {
                string oldName = SelectedState.Self.SelectedStateSave.Name;

                SelectedState.Self.SelectedStateSave.Name = tiw.Result;
                GumCommands.Self.GuiCommands.RefreshStateTreeView();
                // I don't think we need to save the project when renaming a state:
                //GumCommands.Self.FileCommands.TryAutoSaveProject();

                PluginManager.Self.StateRename(SelectedState.Self.SelectedStateSave, oldName);

                GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
            }
        }

        private static void RenameCategoryClick()
        {
            TextInputWindow tiw = new TextInputWindow();
            tiw.Message = "Enter new category name";
            tiw.Result = SelectedState.Self.SelectedStateCategorySave.Name;
            var result = tiw.ShowDialog();

            if (result == DialogResult.OK)
            {
                string oldName = SelectedState.Self.SelectedStateCategorySave.Name;

                SelectedState.Self.SelectedStateCategorySave.Name = tiw.Result;
                GumCommands.Self.GuiCommands.RefreshStateTreeView();
                // I don't think we need to save the project when renaming a state:
                //GumCommands.Self.FileCommands.TryAutoSaveProject();

                PluginManager.Self.CategoryRename(SelectedState.Self.SelectedStateCategorySave, oldName);

                GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
            }
        }

        private static void MoveToCategory(string categoryNameToMoveTo)
        {
            var stateToMove = SelectedState.Self.SelectedStateSave;

            ////////////////////Early Out //////////////////////
            if(stateToMove == null)
            {
                return;
            }
            //////////////////End Early Out /////////////////////

            var categoryToMoveFrom = SelectedState.Self.SelectedElement.Categories
                .FirstOrDefault(item=>item.States.Contains(stateToMove));

            var categoryToMoveTo = SelectedState.Self.SelectedElement.Categories
                .FirstOrDefault(item=>item.Name == categoryNameToMoveTo);

            if(categoryNameToMoveTo == mNoCategory)
            {
                categoryToMoveFrom.States.Remove(stateToMove);
                SelectedState.Self.SelectedElement.States.Add(stateToMove);
            }
            else
            {
                if(categoryToMoveFrom != null)
                {
                    categoryToMoveFrom.States.Remove(stateToMove);
                }
                else
                {
                    SelectedState.Self.SelectedElement.States.Remove(stateToMove);
                }

                categoryToMoveTo.States.Add(stateToMove);
            }

            GumCommands.Self.GuiCommands.RefreshStateTreeView();
            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
        }
    }
}
