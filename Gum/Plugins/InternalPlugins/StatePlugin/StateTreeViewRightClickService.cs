using System;
using System.Linq;
using Gum.DataTypes.Variables;
using Gum.ToolStates;
using System.Windows.Forms;
using ToolsUtilities;
using Gum.DataTypes;
using Gum.PropertyGridHelpers;
using Gum.ToolCommands;
using Gum.DataTypes.Behaviors;
using Gum.Plugins;

namespace Gum.Managers;


public class StateTreeViewRightClickService
{
    const string mNoCategory = "<no category>";
    private readonly ISelectedState _selectedState;

    public ContextMenuStrip OldMenuStrip { get; internal set; }

    public System.Windows.Controls.ContextMenu NewMenuStrip { get; internal set; }

    public StateTreeViewRightClickService(ISelectedState selectedState)
    {
        _selectedState = selectedState;
    }

    #region Add to menu

    internal void PopulateMenuStrip()
    {
        ClearMenuStrip();

        ///////////////////////early out/////////////////////////
        if(_selectedState.SelectedStateContainer == null)
        {
            return;
        }
        /////////////////////end early out///////////////////////

        if (_selectedState.SelectedStateCategorySave != null)
        {
            // As of 5/24/2023, we no longer support uncategorized states
            AddMenuItem("Add State", GumCommands.Self.GuiCommands.ShowAddStateWindow);
        }

        AddMenuItem("Add Category", GumCommands.Self.GuiCommands.ShowAddCategoryWindow);

        if (_selectedState.SelectedStateSave != null)
        {
            bool isDefault = _selectedState.SelectedStateSave == _selectedState.SelectedElement?.DefaultState;
            
            if (!isDefault)
            {
                AddSplitter();
                AddMenuItem("Rename State", RenameStateClick);
                AddMenuItem("Delete " + _selectedState.SelectedStateSave.Name, DeleteStateClick);
                AddMenuItem("Duplicate State", DuplicateStateClick);

                AddMoveToCategoryItems();

                AddSplitter();

                if (GetIfCanMoveUp(_selectedState.SelectedStateSave, _selectedState.SelectedStateCategorySave))
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
        if (_selectedState.SelectedStateCategorySave != null && _selectedState.SelectedStateSave == null)
        {

            AddSplitter();

            AddMenuItem("Rename Category", RenameCategoryClick);


            AddMenuItem("Delete " + _selectedState.SelectedStateCategorySave.Name, DeleteCategoryClick);
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
        var state = _selectedState.SelectedStateSave;
        var list = _selectedState.SelectedStateContainer.UncategorizedStates;
        if(_selectedState.SelectedStateCategorySave != null)
        {
            list = _selectedState.SelectedStateCategorySave.States;
        }

        if(list != null && list.Contains(state))
        {
            int oldIndex = list.IndexOf(state);

            bool shouldSave = false;

            if(direction == -1 && GetIfCanMoveUp(state, _selectedState.SelectedStateCategorySave))
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

                GumCommands.Self.FileCommands.TryAutoSaveCurrentObject();
            }
        }
    }

    bool GetIfCanMoveUp(StateSave state, StateSaveCategory category)
    {
        var list = _selectedState.SelectedStateCategorySave.States;
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

        var categoryNames = SelectedState.Self.SelectedStateContainer?.Categories
            .Where(item=>item != SelectedState.Self.SelectedStateCategorySave)
            .Select(item => item.Name).ToList();

        // As of before 2024 we no longer allow uncategorized non-default states
        //if(SelectedState.Self.SelectedStateCategorySave != null)
        //{
        //    categoryNames.Insert(0, mNoCategory);
        //}

        if(categoryNames?.Count != 0)
        {
            AddSplitter();

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
        GumCommands.Self.GuiCommands.ShowAddStateWindow();
    }
    
    private void DuplicateStateClick()
    {
        // Is there a "custom" current state save, like an interpolation or animation?
        if(_selectedState.CustomCurrentStateSave != null)
        {
            GumCommands.Self.GuiCommands.ShowMessage("Cannot duplicate state while a custom state is displaying. Are you creating or playing animations?");
            return;
        }
        if(_selectedState.SelectedStateCategorySave == null)
        {
            GumCommands.Self.GuiCommands.ShowMessage("Cannot duplicate uncategorized states. Select a state in a category first.");
            return;
        }
        ////////End Early Out///////////////

        StateSave newState = _selectedState.SelectedStateSave.Clone();


        newState.ParentContainer = _selectedState.SelectedElement;

        int index = _selectedState.SelectedStateCategorySave.States.IndexOf(_selectedState.SelectedStateSave);

        while (_selectedState.SelectedStateContainer.AllStates.Any(item => item != newState && item.Name == newState.Name))
        {
            newState.Name = StringFunctions.IncrementNumberAtEnd(newState.Name);
        }

        ElementCommands.Self.AddState(_selectedState.SelectedStateContainer, _selectedState.SelectedStateCategorySave, newState, index + 1);

        GumCommands.Self.GuiCommands.RefreshStateTreeView();

        _selectedState.SelectedStateSave = newState;

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
        var stateContainer = SelectedState.Self.SelectedStateContainer;

        var newCategory = stateContainer.Categories
            .FirstOrDefault(item=>item.Name == categoryNameToMoveTo);

        var oldCategory = stateContainer.Categories
            .FirstOrDefault(item=>item.States.Contains(stateToMove));
        ////////////////////Early Out //////////////////////
        if(stateToMove == null || categoryNameToMoveTo == null || oldCategory == null)
        {
            return;
        }
        //////////////////End Early Out /////////////////////



        oldCategory.States.Remove(stateToMove);
        newCategory.States.Add(stateToMove);

        GumCommands.Self.GuiCommands.RefreshStateTreeView();
        SelectedState.Self.SelectedStateSave = stateToMove;

        // make sure to propagate all variables in this new state and
        // also move all existing variables to the new state (use the first)
        if(stateContainer is ElementSave element)
        {
            foreach(var variable in stateToMove.Variables)
            {
                VariableInCategoryPropagationLogic.Self.PropagateVariablesInCategory(variable.Name,
                    element, GumState.Self.SelectedState.SelectedStateCategorySave);
            }


            var firstState = newCategory.States.FirstOrDefault();
            if (firstState != stateToMove)
            {
                foreach (var variable in firstState.Variables)
                {
                    VariableInCategoryPropagationLogic.Self.PropagateVariablesInCategory(variable.Name,
                        element, GumState.Self.SelectedState.SelectedStateCategorySave);
                }
            }
        }

        PluginManager.Self.StateMovedToCategory(stateToMove, newCategory, oldCategory);

        if (stateContainer is BehaviorSave behavior)
        {
            GumCommands.Self.FileCommands.TryAutoSaveBehavior(behavior);

        }
        else if(stateContainer is ElementSave asElement)
        {
            GumCommands.Self.FileCommands.TryAutoSaveElement(asElement);
        }
    }
}
