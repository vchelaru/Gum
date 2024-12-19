using Gum.Controls;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Mvvm;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;
using Gum.Plugins.InternalPlugins.StatePlugin.Views;
using Gum.PropertyGridHelpers;
using Gum.ToolStates;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Forms;

namespace Gum.Plugins.StatePlugin;

// This is new as of Oct 30, 2020
// I'd like to move all state logic to this plugin over time.
[Export(typeof(Gum.Plugins.BaseClasses.PluginBase))]
public class MainStatePlugin : InternalPlugin
{
    #region Fields/Properties

    StateView stateView;
    StateTreeView stateTreeView;
    StateTreeViewModel stateTreeViewModel;

    PluginTab pluginTab;
    PluginTab newPluginTab;

    StateTreeViewRightClickService _stateTreeViewRightClickService;
    private HotkeyManager _hotkeyManager;
    private readonly ISelectedState _selectedState;

    #endregion

    #region Initialize

    public MainStatePlugin()
    {
        _stateTreeViewRightClickService = new StateTreeViewRightClickService(GumState.Self.SelectedState);
        _hotkeyManager = HotkeyManager.Self;
        _selectedState = GumState.Self.SelectedState;
    }

    public override void StartUp()
    {
        AssignEvents();

        stateTreeViewModel = new StateTreeViewModel(_stateTreeViewRightClickService);

        CreateNewStateTab();

        CreateOldStateTab();

        // State Tree ViewManager needs init before MenuStripManager
        StateTreeViewManager.Self.Initialize(this.stateView.TreeView,
            this.stateView.StateContextMenuStrip,
            _stateTreeViewRightClickService,
            _hotkeyManager);
    }

    private void AssignEvents()
    {
        this.StateWindowTreeNodeSelected += HandleStateSelected;
        this.TreeNodeSelected += HandleTreeNodeSelected;
        this.RefreshStateTreeView += HandleRefreshStateTreeView;
        this.ReactToStateSaveSelected += HandleStateSelected;
        this.ReactToStateSaveCategorySelected += HandleStateSaveCategorySelected;
        this.StateRename += HandleStateRename;
        this.CategoryRename += HandleCategoryRename;
        this.ReactToStateStackingModeChange += HandleStateStackingModeChanged;
        this.BehaviorSelected += HandleBehaviorSelected;
        this.InstanceSelected += HandleInstanceSelected;
        this.ElementSelected += HandleElementSelected;
        this.StateMovedToCategory += HandleStateMovedToCategory;
        this.VariableSet += HandleVariableSet;
    }

    private void CreateOldStateTab()
    {
        stateView = new StateView(_stateTreeViewRightClickService, _hotkeyManager);
        _stateTreeViewRightClickService.OldMenuStrip = stateView.TreeViewContextMenu;
        stateView.StateStackingModeChange += (_, _) => GumState.Self.SelectedState.StateStackingMode = stateView.StateStackingMode;

        pluginTab = GumCommands.Self.GuiCommands.AddControl(stateView, "States", TabLocation.CenterTop);
    }

    private void CreateNewStateTab()
    {
        stateTreeView = new StateTreeView(stateTreeViewModel, _stateTreeViewRightClickService, _hotkeyManager, _selectedState);
        _stateTreeViewRightClickService.NewMenuStrip = stateTreeView.TreeViewContextMenu;
        newPluginTab = GumCommands.Self.GuiCommands.AddControl(stateTreeView, "States", TabLocation.CenterTop);
    }

    #endregion

    #region Event Handlers

    private void HandleStateMovedToCategory(StateSave stateSave, StateSaveCategory newCategory, StateSaveCategory oldCategory)
    {
        _stateTreeViewRightClickService.PopulateMenuStrip();
        stateTreeViewModel.RefreshTo(GumState.Self.SelectedState.SelectedStateContainer, GumState.Self.SelectedState);
    }

    private void HandleElementSelected(ElementSave save)
    {
        RefreshUI(SelectedState.Self.SelectedStateContainer, SelectedState.Self);
    }

    private void HandleInstanceSelected(ElementSave save1, InstanceSave save2)
    {
        RefreshUI(SelectedState.Self.SelectedStateContainer, SelectedState.Self);

        // A user could directly select an instance in
        // a different container such as going from a component
        // to a selected instance in a behavior. In that case we
        // still want to refresh the menu items.
        _stateTreeViewRightClickService.PopulateMenuStrip();
    }

    private void HandleBehaviorSelected(BehaviorSave behavior)
    {
        _stateTreeViewRightClickService.PopulateMenuStrip();
        HandleRefreshStateTreeView();
    }

    private void HandleStateStackingModeChanged(StateStackingMode mode)
    {
        stateView.StateStackingMode = mode;
        stateTreeViewModel.StateStackingMode = mode;
    }

    private void HandleCategoryRename(StateSaveCategory category, string arg2)
    {
        stateTreeViewModel.HandleRename(category);
    }

    private void HandleStateRename(StateSave save, string oldName)
    {
        stateTreeViewModel.HandleRename(save);
    }

    private void HandleStateSelected(StateSave state)
    {
        StateTreeViewManager.Self.Select(state);

        stateTreeViewModel.SetSelectedState(state);
    }

    private void HandleStateSaveCategorySelected(StateSaveCategory stateSaveCategory)
    {
        StateTreeViewManager.Self.Select(stateSaveCategory);

        stateTreeViewModel.SetSelectedStateSaveCategory(stateSaveCategory);
    }

    private void HandleRefreshStateTreeView()
    {
        RefreshUI(SelectedState.Self.SelectedStateContainer, SelectedState.Self);
    }

    private void HandleTreeNodeSelected(TreeNode node)
    {
        var element = SelectedState.Self.SelectedElement;
        string desiredTitle = "States";
        if (element != null)
        {
            desiredTitle = $"{element.Name} States";
        }

        pluginTab.Title = desiredTitle + " (old)";
        newPluginTab.Title = desiredTitle;
    }

    private void HandleStateSelected(TreeNode stateTreeNode)
    {
        var currentCategory = SelectedState.Self.SelectedStateCategorySave;
        var currentState = SelectedState.Self.SelectedStateSave;

        if (currentCategory != null && currentState != null)
        {
            PropagateVariableForCategorizedState(currentState);
        }
        else if (currentCategory != null)
        {
            foreach (var state in currentCategory.States)
            {
                PropagateVariableForCategorizedState(state);
            }
        }


    }

    private void HandleVariableSet(ElementSave elementSave, InstanceSave instance, string variableName, object oldValue)
    {
        // Do this to refresh the yellow highlights - We may not need to do more than this:
        stateTreeViewModel.RefreshTo(elementSave, GumState.Self.SelectedState);
    }
    #endregion

    private void PropagateVariableForCategorizedState(StateSave currentState)
    {
        foreach (var variable in currentState.Variables)
        {
            VariableInCategoryPropagationLogic.Self.PropagateVariablesInCategory(variable.Name, 
                GumState.Self.SelectedState.SelectedElement, GumState.Self.SelectedState.SelectedStateCategorySave);
        }
    }

    IStateContainer mLastElementRefreshedTo;
    void RefreshUI(IStateContainer stateContainer, ISelectedState selectedState)
    {

        bool changed = stateContainer != mLastElementRefreshedTo;

        mLastElementRefreshedTo = stateContainer;

        StateSave lastStateSave = SelectedState.Self.SelectedStateSave;
        InstanceSave instance = SelectedState.Self.SelectedInstance;

        stateTreeViewModel.RefreshTo(stateContainer, selectedState);

        if (stateContainer != null)
        {
            RemoveUnnecessaryNodes(stateContainer);

            AddNeededNodes(stateContainer);

            FixNodeOrder(stateContainer);

            foreach (var state in stateContainer.AllStates)
            {
                UpdateStateTreeNode(lastStateSave, instance, state);
            }

            foreach (var category in stateContainer.Categories)
            {
                UpdateCategoryTreeNode(category);
            }

        }
        else
        {
            this.stateView.TreeView.Nodes.Clear();
        }
    }

    private void RemoveUnnecessaryNodes(IStateContainer stateContainer)
    {
        var allNodes = this.stateView.TreeView.Nodes.AllNodes().ToList();

        foreach (var node in allNodes)
        {
            if (node.Tag is StateSave)
            {
                // First check to see if this doesn't exist at all...
                bool shouldRemove = stateContainer.AllStates.Contains(node.Tag as StateSave) == false;

                // ... and if it does exist, see if it's part of the wrong category
                if (!shouldRemove)
                {
                    if (node.Parent != null)
                    {
                        var category = node.Parent.Tag as StateSaveCategory;

                        if (!category.States.Contains(node.Tag as StateSave))
                        {
                            shouldRemove = true;
                        }
                    }
                    else
                    {
                        shouldRemove = stateContainer.Categories.Any(item => item.States.Contains(node.Tag as StateSave));
                    }
                }

                if (shouldRemove)
                {
                    var parent = ParentOf(node);

                    parent.Remove(node);
                }
            }
            else if (node.Tag is StateSaveCategory && stateContainer.Categories.Contains(node.Tag as StateSaveCategory) == false)
            {
                if (node.Parent == null)
                {
                    this.stateView.TreeView.Nodes.Remove(node);
                }
                else
                {
                    node.Parent.Nodes.Remove(node);
                }
            }
        }

    }

    private void AddNeededNodes(IStateContainer stateContainer)
    {

        foreach (var category in stateContainer.Categories)
        {
            if (GetTreeNodeForTag(category) == null)
            {
                var treeNode = this.stateView.TreeView.Nodes.Add(category.Name);
                treeNode.Tag = category;

                
                treeNode.ImageIndex = ElementTreeViewManager.FolderImageIndex;
            }
        }

        foreach (var state in stateContainer.UncategorizedStates)
        {
            // uncategorized
            if (GetTreeNodeForTag(state) == null)
            {
                var treeNode = this.stateView.TreeView.Nodes.Add(state.Name);
                treeNode.Tag = state;
                treeNode.ImageIndex = ElementTreeViewManager.StateImageIndex;
            }
        }

        foreach (var category in stateContainer.Categories)
        {
            foreach (var state in category.States)
            {
                // uncategorized
                if (GetTreeNodeForTag(state) == null)
                {
                    var toAddTo = GetTreeNodeForTag(category);

                    var treeNode = toAddTo.Nodes.Add(state.Name);
                    treeNode.ImageIndex = ElementTreeViewManager.StateImageIndex;

                    treeNode.Tag = state;
                }
            }
        }
    }

    private void FixNodeOrder(IStateContainer stateContainer)
    {
        // first make sure categories come first
        int desiredIndex = 0;


        foreach (var category in stateContainer.Categories.OrderBy(item => item.Name))
        {
            {

                var node = GetTreeNodeForTag(category);

                var parent = ParentOf(node);

                var nodeIndex = parent.IndexOf(node);

                if (nodeIndex != desiredIndex)
                {
                    parent.Remove(node);
                    parent.Insert(desiredIndex, node);
                }


                FixNodeOrderInCategory(category);
            }

            desiredIndex++;
        }

        // do uncategorized states
        for (int i = 0; i < stateContainer.UncategorizedStates.Count(); i++)
        {

            var state = stateContainer.UncategorizedStates.ElementAt(i);

            var node = GetTreeNodeForTag(state);

            var parent = ParentOf(node);

            int nodeIndex = parent.IndexOf(node);


            if (nodeIndex != desiredIndex)
            {
                parent.Remove(node);
                parent.Insert(desiredIndex, node);
            }

            desiredIndex++;
        }


    }

    private void UpdateStateTreeNode(StateSave lastStateSave, InstanceSave instance, StateSave state)
    {
        string stateName = state.Name;
        if (string.IsNullOrEmpty(stateName))
        {
            stateName = "Default";
        }

        var node = GetTreeNodeForTag(state);

        if (node.Text != stateName)
        {
            node.Text = stateName;
        }
        if (node.Tag != state)
        {
            node.Tag = state;
        }

        node.ImageIndex = ElementTreeViewManager.StateImageIndex;

        if (state == lastStateSave)
        {
        }
        else if (!node.IsSelected && this.stateView.TreeView.SelectedNode != node)
        {
            System.Drawing.Color desiredColor = System.Drawing.Color.White;
            if (instance != null && state.Variables.Any(item => item.Name.StartsWith(instance.Name + ".")))
            {
                desiredColor = System.Drawing.Color.Yellow;
            }

            if (node.BackColor != desiredColor)
            {
                node.BackColor = desiredColor;
            }
        }

    }

    private TreeNodeCollection ParentOf(TreeNode node)
    {
        var toReturn = stateView.TreeView.Nodes;

        if (node.Parent != null)
        {
            toReturn = node.Parent.Nodes;
        }

        return toReturn;
    }

    private void FixNodeOrderInCategory(StateSaveCategory category)
    {
        for (int i = 0; i < category.States.Count; i++)
        {
            var state = category.States[i];
            var node = GetTreeNodeForTag(state);

            var parent = ParentOf(node);

            var nodeIndex = parent.IndexOf(node);

            if (nodeIndex != i)
            {
                parent.Remove(node);
                parent.Insert(i, node);
            }
        }
    }

    private void UpdateCategoryTreeNode(StateSaveCategory category)
    {
        var node = GetTreeNodeForTag(category);

        if (node.Text != category.Name)
        {
            node.Text = category.Name;
        }
    }

    public TreeNode GetTreeNodeForTag(object tag)
    {
        if (tag == null)
        {
            return null;
        }
        // Will need to expand this when we add categories
        foreach (TreeNode node in stateView.TreeView.Nodes)
        {
            if (node.Tag == tag)
            {
                return node;
            }

            foreach (TreeNode subnode in node.Nodes)
            {
                if (subnode.Tag == tag)
                {
                    return subnode;
                }
            }
        }
        return null;
    }
}
