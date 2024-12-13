using System;
using System.Linq;
using Gum.DataTypes.Variables;
using Gum.ToolStates;
using System.Windows.Forms;
using ToolsUtilities;
using Gum.DataTypes;
using Gum.PropertyGridHelpers;

namespace Gum.Managers
{

    public class StateTreeViewRightClickService
    {
        const string mNoCategory = "<no category>";
        public ContextMenuStrip OldMenuStrip { get; internal set; }

        public System.Windows.Controls.ContextMenu NewMenuStrip { get; internal set; }

        public StateTreeViewRightClickService()
        {
        }

        #region Add to menu

        internal void PopulateMenuStrip()
        {
            ClearMenuStrip();

            if (SelectedState.Self.SelectedStateCategorySave != null)
            {
                // As of 5/24/2023, we no longer support uncategorized states
                AddMenuItem("Add State", GumCommands.Self.Edit.AddState);
            }

            AddMenuItem("Add Category", GumCommands.Self.Edit.AddCategory);

            if (SelectedState.Self.SelectedStateSave != null)
            {
                bool isDefault = SelectedState.Self.SelectedStateSave == SelectedState.Self.SelectedElement?.DefaultState;
                
                AddSplitter();

                if (!isDefault)
                {
                    AddMenuItem("Rename State", RenameStateClick);
                }

                if (SelectedState.Self.SelectedStateCategorySave != null)
                {
                    AddMenuItem("Duplicate State", DuplicateStateClick);
                }

                if (!isDefault)
                {
                    AddMenuItem("Delete " + SelectedState.Self.SelectedStateSave.Name, DeleteStateClick);
                }



                if (SelectedState.Self.SelectedElement != null && SelectedState.Self.SelectedStateSave != SelectedState.Self.SelectedElement.DefaultState)
                {
                    AddSplitter();

                    AddMoveToCategoryItems();

                    AddSplitter();

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
            if (SelectedState.Self.SelectedStateCategorySave != null && SelectedState.Self.SelectedStateSave == null)
            {

                AddSplitter();

                AddMenuItem("Rename Category", RenameCategoryClick);


                AddMenuItem("Delete " + SelectedState.Self.SelectedStateCategorySave.Name, DeleteCategoryClick);
            }
        }

        private void AddSplitter()
        {
            OldMenuStrip.Items.Add("-");
            NewMenuStrip.Items.Add(new System.Windows.Controls.Separator());
        }

        private void ClearMenuStrip()
        {
            OldMenuStrip.Items.Clear();
            NewMenuStrip.Items.Clear();
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


        public void DeleteCategoryClick()
        {
            GumCommands.Self.Edit.RemoveStateCategory(
                SelectedState.Self.SelectedStateCategorySave,
                SelectedState.Self.SelectedStateContainer as IStateCategoryListContainer);
        }

        public void DeleteStateClick()
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

            // As of before 2024 we no longer allow uncategorized non-default states
            //if(SelectedState.Self.SelectedStateCategorySave != null)
            //{
            //    categoryNames.Insert(0, mNoCategory);
            //}

            if(categoryNames.Count != 0)
            {
                var rootItem = AddMenuItem("Move to category", null);

                foreach(var categoryName in categoryNames)
                {

                    // make a local var to prevent problems with delayed evaluation
                    string categoryNameEvaluated = categoryName;
                    AddChildMenuItem("Move to category", categoryName, () => MoveToCategory(categoryName));
                }
            }
        }

        private void AddChildMenuItem(string parent, string text, Action clickAction, string shortcut = null)
        {
            ToolStripMenuItem tsmi = CreateOldToolStripMenuItem(text, clickAction, shortcut);
            ToolStripMenuItem oldParentItem = null;
            foreach(var oldItem in OldMenuStrip.Items)
            {
                if(oldItem is ToolStripMenuItem itemTsmi && itemTsmi.Text == parent)
                {
                    oldParentItem = itemTsmi;
                    break;
                }
            }
            oldParentItem.DropDownItems.Add(tsmi);

            System.Windows.Controls.MenuItem menuItem = CreateNewToolStripMenuItem(text, clickAction, shortcut);
            var parentItem = NewMenuStrip.Items.FirstOrDefault(item => item is System.Windows.Controls.MenuItem itemMenu && itemMenu.Header.ToString() == parent)
                as System.Windows.Controls.MenuItem;
            parentItem.Items.Add(menuItem);
        }

        private ToolStripMenuItem AddMenuItem(string text, Action clickAction, string shortcut = null)
        {
            ToolStripMenuItem tsmi = CreateOldToolStripMenuItem(text, clickAction, shortcut);
            OldMenuStrip.Items.Add(tsmi);
            System.Windows.Controls.MenuItem menuItem = CreateNewToolStripMenuItem(text, clickAction, shortcut);
            NewMenuStrip.Items.Add(menuItem);



            return tsmi;
        }

        private static System.Windows.Controls.MenuItem CreateNewToolStripMenuItem(string text, Action clickAction, string shortcut)
        {
            var menuItem = new System.Windows.Controls.MenuItem
            {
                Header = text,
                InputGestureText = shortcut,
            };
            if(clickAction != null)
            {
                menuItem.Click += (_, _) => clickAction();
            }
            return menuItem;
        }

        private static ToolStripMenuItem CreateOldToolStripMenuItem(string text, Action clickAction, string shortcut)
        {
            var tsmi = new ToolStripMenuItem();
            tsmi.Text = text;
            tsmi.ShortcutKeyDisplayString = shortcut;
            if (clickAction != null)
            {
                tsmi.Click += delegate
                {
                    clickAction();
                };
            }

            return tsmi;
        }

        #endregion

        internal void AddStateClick()
        {
            GumCommands.Self.Edit.AddState();
        }
        
        private static void DuplicateStateClick()
        {
            // Is there a "custom" current state save, like an interpolation or animation?
            if(SelectedState.Self.CustomCurrentStateSave != null)
            {
                GumCommands.Self.GuiCommands.ShowMessage("Cannot duplicate state while a custom state is displaying. Are you creating or playing animations?");
                return;
            }
            ////////End Early Out///////////////

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

            GumCommands.Self.GuiCommands.RefreshStateTreeView();

            SelectedState.Self.SelectedStateSave = newState;

            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
        }

        public void RenameStateClick()
        {
            GumCommands.Self.Edit.AskToRenameState(SelectedState.Self.SelectedStateSave,
                SelectedState.Self.SelectedStateContainer);
        }

        public void RenameCategoryClick()
        {
            GumCommands.Self.Edit.AskToRenameStateCategory(
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

            var isMovingToCategory = categoryNameToMoveTo != mNoCategory;

            if (!isMovingToCategory)
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
            SelectedState.Self.SelectedStateSave = stateToMove;

            if(isMovingToCategory)
            {
                // make sure to propagate all variables in this new state and
                // also move all existing variables to the new state (use the first)
                foreach(var variable in stateToMove.Variables)
                {
                    VariableInCategoryPropagationLogic.Self.PropagateVariablesInCategory(variable.Name,
                        GumState.Self.SelectedState.SelectedElement, GumState.Self.SelectedState.SelectedStateCategorySave);
                }

                var firstState = categoryToMoveTo.States.FirstOrDefault();
                if (firstState != stateToMove)
                {
                    foreach (var variable in firstState.Variables)
                    {
                        VariableInCategoryPropagationLogic.Self.PropagateVariablesInCategory(variable.Name,
                            GumState.Self.SelectedState.SelectedElement, GumState.Self.SelectedState.SelectedStateCategorySave);
                    }
                }
            }

            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
        }
    }
}
