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
using Gum.DataTypes;

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
                bool isDefault = SelectedState.Self.SelectedStateSave == SelectedState.Self.SelectedElement.DefaultState; 

                mMenuStrip.Items.Add("-");

                if(!isDefault)
                {
                    AddMenuItem("Rename State", RenameStateClick);
                }

                AddMenuItem("Duplicate State", DuplicateStateClick);

                if(!isDefault)
                {
                    AddMenuItem("Delete " + SelectedState.Self.SelectedStateSave.Name, DeleteStateClick);
                }



                if (SelectedState.Self.SelectedElement != null && SelectedState.Self.SelectedStateSave != SelectedState.Self.SelectedElement.DefaultState)
                {
                    mMenuStrip.Items.Add("-");

                    AddMoveToCategoryItems();

                    mMenuStrip.Items.Add("-");

                    if (GetIfCanMoveUp(SelectedState.Self.SelectedStateSave, SelectedState.Self.SelectedStateCategorySave))
                    {
                        AddMenuItem("^ Move Up", MoveUpClick, "Alt+Up");
                    }
                    AddMenuItem("v Move Down", MoveDownClick, "Alt+Down");
                }
            }
            // We used to show the category editing commands if a state was selected 
            // (if a state is selected, a category is implicitly selected too). Now we
            // check if a category is highlighted (not state)
            //if(SelectedState.Self.SelectedStateCategorySave != null)
            if(SelectedState.Self.SelectedStateCategorySave != null && SelectedState.Self.SelectedStateSave == null)
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

        public void MoveStateInDirection(int direction)
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
            GumCommands.Self.Edit.RemoveStateCategory(
                SelectedState.Self.SelectedStateCategorySave,
                SelectedState.Self.SelectedStateContainer as IStateCategoryListContainer);
        }

        private void DeleteStateClick()
        {
            GumCommands.Self.Edit.RemoveState(
                SelectedState.Self.SelectedStateSave,
                SelectedState.Self.SelectedStateContainer);
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

        private ToolStripMenuItem AddMenuItem(string text, Action clickAction, string shortcut = null)
        {
            var tsmi = new ToolStripMenuItem();
            tsmi.Text = text;
            tsmi.ShortcutKeyDisplayString = shortcut;
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


            while (SelectedState.Self.SelectedStateContainer.AllStates.Any(item => item != newState && item.Name == newState.Name))
            {
                newState.Name = StringFunctions.IncrementNumberAtEnd(newState.Name);
            }

            StateTreeViewManager.Self.RefreshUI(SelectedState.Self.SelectedElement);

            SelectedState.Self.SelectedStateSave = newState;

            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
        }

        private static void RenameStateClick()
        {
            GumCommands.Self.Edit.RenameState(SelectedState.Self.SelectedStateSave,
                SelectedState.Self.SelectedStateContainer);
        }

        private static void RenameCategoryClick()
        {
            GumCommands.Self.Edit.RenameStateCategory(
                SelectedState.Self.SelectedStateCategorySave, 
                SelectedState.Self.SelectedElement);
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
