﻿using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.Managers;
using Gum.DataTypes.Variables;
using System.Windows.Forms;
using Gum.Wireframe;
using Gum.Plugins;
using Gum.Debug;
using RenderingLibrary;
using Gum.DataTypes.Behaviors;
using Gum.Controls;
using Newtonsoft.Json.Linq;
using Gum.Events;

namespace Gum.ToolStates;

public class SelectedState : ISelectedState
{
    #region Fields

    static ISelectedState mSelf;

    SelectedStateSnapshot snapshot = new SelectedStateSnapshot();
    private MenuStripManager _menuStripManager;

    #endregion

    #region Elements (Screen, Component, StandardElement)

    public ScreenSave SelectedScreen
    {
        get
        {
            return snapshot.SelectedScreen;
        }
        set
        {
            // We don't want this to unset selected components or standards if this is set to null
            if (value != SelectedScreen && (value != null || SelectedScreen == null || SelectedScreen is ScreenSave))
            {
                SelectedElement = value;
            }
        }
    }

    public ComponentSave SelectedComponent
    {
        get
        {
            return snapshot.SelectedComponent;
        }
        set
        {
            if (value != SelectedComponent && (value != null || SelectedComponent == null || SelectedComponent is ComponentSave))
            {
                SelectedElement = value;
            }
        }
    }

    public StandardElementSave SelectedStandardElement
    {
        get
        {
            return snapshot.SelectedStandardElement;
        }
        set
        {
            if (value != SelectedStandardElement && (value != null || SelectedStandardElement == null || SelectedStandardElement is StandardElementSave))
            {
                SelectedElement = value;
            }
        }
    }

    public ElementSave SelectedElement
    {
        get
        {
            return snapshot.SelectedElement;
        }
        set
        {
            HandleElementSelected(value);
        }
    }

    private void HandleElementSelected(ElementSave value)
    {
        if(value != null)
        {
            snapshot.SelectedInstance = null;
            snapshot.SelectedBehavior = null;
        }
        UpdateToSelectedElement(value);

        if(value != null)
        {
            PluginManager.Self.ElementSelected(SelectedElement);
        }
    }

    private void UpdateToSelectedElement(ElementSave element)
    {
        if (snapshot.SelectedElement != element)
        {
            snapshot.SelectedElement = element;

            var stateBefore = SelectedStateSave;

            if (SelectedElement != null &&
                (SelectedStateSave == null || SelectedElement.AllStates.Contains(SelectedStateSave) == false) &&
                SelectedElement.States.Count > 0
                )
            {

                SelectedStateSave = SelectedElement.States[0];
            }
            else if (SelectedElement == null)
            {
                SelectedStateSave = null;
            }

            if (stateBefore == SelectedStateSave)
            {
                // If the state changed (element changed) then no need to force the UI again
                GumCommands.Self.GuiCommands.RefreshVariables();
            }

            WireframeObjectManager.Self.RefreshAll(true);

            SelectionManager.Self.Refresh();

            _menuStripManager.RefreshUI();
        }
    }

    #endregion

    #region Behavior

    public BehaviorSave SelectedBehavior
    {
        get
        {
            return snapshot.SelectedBehavior;
        }
        set
        {
            HandleBehaviorSelected(value);
        }
    }


    private void HandleBehaviorSelected(BehaviorSave behavior)
    {
        if (behavior != null && SelectedInstance != null)
        {
            SelectedInstance = null;
        }
        UpdateToSelectedBehavior(behavior);
        if(behavior != null)
        {
            PluginManager.Self.BehaviorSelected(SelectedBehavior);
        }
    }

    #endregion

    #region Properties

    public static ISelectedState Self
    {
        // We usually won't use this in the actual product, but useful for testing
        set
        {
            mSelf = value;
        }
        get
        {
            if (mSelf == null)
            {
                mSelf = new SelectedState();
            }
            return mSelf;
        }
    }

    public IStateContainer SelectedStateContainer
    {
        get
        {
            if (SelectedComponent != null)
            {
                return SelectedComponent;
            }
            else if (SelectedScreen != null)
            {
                return SelectedScreen;
            }
            else if (SelectedStandardElement != null)
            {
                return SelectedStandardElement;
            }
            else if (SelectedBehavior != null)
            {
                return SelectedBehavior;
            }

            return null;
        }
    }

    public List<ElementSave> SelectedElements
    {
        set
        {
            UpdateToSelectedElements(value);
        }
        get
        {
            return snapshot.SelectedElements;
        }
    }

    public StateSave CustomCurrentStateSave
    {
        get;
        set;
    }

    public StateSave SelectedStateSave
    {
        get
        {
            if (CustomCurrentStateSave != null)
            {
                return CustomCurrentStateSave;
            }
            else
            {
                return snapshot.SelectedStateSave;
            }
        }
        set
        {

            HandleStateSaveSelected(value);
        }
    }

    public StateSave SelectedStateSaveOrDefault
    {
        get
        {
            return SelectedStateSave ?? SelectedElement?.DefaultState;
        }
    }

    public StateSaveCategory SelectedStateCategorySave
    {
        get
        {
            return snapshot.SelectedStateCategorySave;
        }
        set
        {
            UpdateToSetSelectedStateSaveCategory(value);
            UpdateToSetSelectedStateSave(SelectedStateSave);

        }
    }

    public InstanceSave SelectedInstance
    {
        get
        {
            return snapshot.SelectedInstance;
        }
        set
        {
            if(value == null)
            {
                SelectedInstances = new List<InstanceSave> ();
            }
            else
            {
                SelectedInstances = new List<InstanceSave> { value };
            }
        }
    }

    public IEnumerable<InstanceSave> SelectedInstances
    {
        get
        {
            return snapshot.SelectedInstances;
        }
        set
        {
            HandleSelectedInstances(value?.ToList());
        }

    }

    public VariableSave SelectedVariableSave
    {
        set
        {
            snapshot.SelectedVariableSave = value;
        }
        get
        {
            return snapshot.SelectedVariableSave;
        }

    }

    /// <summary>
    /// Returns the name of the selected entry in the property grid.
    /// There may not be a VariableSave backing the selection as the 
    /// value may be null in the StateSave
    /// </summary>
    public string SelectedVariableName
    {
        get
        {
            return SelectedVariableSave?.GetRootName();
        }
    }

    public TreeNode SelectedTreeNode
    {
        get
        {
            return ElementTreeViewManager.Self.SelectedNode;
        }
    }

    public IEnumerable<TreeNode> SelectedTreeNodes
    {
        get
        {
            return ElementTreeViewManager.Self.SelectedNodes;
        }
    }

    public RecursiveVariableFinder SelectedRecursiveVariableFinder
    {
        get
        {
            if (SelectedInstance != null)
            {
                return new RecursiveVariableFinder(SelectedInstance, SelectedElement);
            }
            else
            {
                return new RecursiveVariableFinder(SelectedStateSave);
            }
        }
    }

    public IPositionedSizedObject SelectedIpso
    {
        get
        {
            return SelectionManager.Self.SelectedGue;
        }
        set
        {
            SelectionManager.Self.SelectedGue = value as GraphicalUiElement;
        }
    }

    public List<GraphicalUiElement> SelectedIpsos
    {
        get
        {
            return SelectionManager.Self.SelectedGues;
        }

    }

    public StateStackingMode StateStackingMode
    {
        get
        {
            return snapshot.StateStackingMode;
        }
        set
        {
            UpdateToSetSelectedStackingMode(value);
        }
    }

    public VariableSave SelectedBehaviorVariable
    {
        get
        {
            return snapshot.SelectedBehaviorVariable;
        }

        set
        {
            UpdateToSelectedBehaviorVariable(value);
        }
    }

    #endregion

    #region Instance

    private void HandleSelectedInstances(List<InstanceSave> value)
    {
        var instance = value?.FirstOrDefault();
        if(instance != null)
        {
            var elementAfter = ObjectFinder.Self.GetElementContainerOf(instance);
            var behaviorAfter = ObjectFinder.Self.GetBehaviorContainerOf(instance);

            snapshot.SelectedElement = elementAfter;
            snapshot.SelectedBehavior = behaviorAfter;
        }

        UpdateToSelectedInstances(value);
    }

    #endregion

    private void UpdateToSetSelectedStackingMode(StateStackingMode value)
    {
        var isSame = snapshot.StateStackingMode == value;
        if (!isSame)
        {
            snapshot.StateStackingMode = value;
            PluginManager.Self.ReactToStateStackingModeChange(value);
        }
    }

    private void UpdateToSetSelectedStateSaveCategory(StateSaveCategory selectedStateSaveCategory)
    {
        var isSame = snapshot.SelectedStateCategorySave == selectedStateSaveCategory;
        if (!isSame)
        {
            TakeSnapshot(selectedStateSaveCategory);
            PluginManager.Self.ReactToStateSaveCategorySelected(selectedStateSaveCategory);
        }
    }


    private void HandleStateSaveSelected(StateSave stateSave)
    {
        StateSaveCategory category = null;
        var elementContainer =
            ObjectFinder.Self.GetStateContainerOf(stateSave);

        category = elementContainer?.Categories.FirstOrDefault(item => item.States.Contains(stateSave));

        if(category != null && category != snapshot.SelectedStateCategorySave)
        {
            snapshot.SelectedStateCategorySave = category;
        }

        UpdateToSetSelectedStateSave(stateSave);
    }

    private void UpdateToSetSelectedStateSave(StateSave selectedStateSave)
    {
        var isSame = snapshot.SelectedStateSave == selectedStateSave;
        if (!isSame)
        {
            TakeSnapshot(selectedStateSave);
            PluginManager.Self.ReactToStateSaveSelected(selectedStateSave);
        }
    }


    private void UpdateToSetSelectedInstance(InstanceSave value)
    {
        var isSame = snapshot.SelectedInstance == value;
        if (!isSame)
        {
            snapshot.SelectedInstance = value;
            PerformAfterSelectInstanceLogic();
        }
    }

    private void PerformAfterSelectInstanceLogic()
    {
        if (snapshot.SelectedInstance != null)
        {
            var stateContainerBefore = snapshot.SelectedStateContainer;

            ElementSave parent = snapshot.SelectedInstance.ParentContainer;
            var elementAfter = ObjectFinder.Self.GetElementContainerOf(snapshot.SelectedInstance);
            var behaviorAfter = ObjectFinder.Self.GetBehaviorContainerOf(snapshot.SelectedInstance);

            ProjectVerifier.Self.AssertIsPartOfProject(parent);

            if(elementAfter != null || behaviorAfter != null)
            {
                if(stateContainerBefore != elementAfter && stateContainerBefore != behaviorAfter)
                {
                    GumCommands.Self.GuiCommands.RefreshStateTreeView();
                }
            }
        }


        if (SelectedElement != null && (SelectedStateSave == null || SelectedElement.AllStates.Contains(SelectedStateSave) == false))
        {
            SelectedStateSave = SelectedElement.States[0];
        }

        if (WireframeObjectManager.Self.ElementShowing != this.SelectedElement)
        {
            WireframeObjectManager.Self.RefreshAll(false);
        }

        SelectionManager.Self.Refresh();

        _menuStripManager.RefreshUI();


        // This is needed for the wireframe manager, but this should be moved to a plugin
        GumEvents.Self.CallInstanceSelected();

        if (snapshot.SelectedInstance != null)
        {
            var element = ObjectFinder.Self.GetElementContainerOf(snapshot.SelectedInstance);
            PluginManager.Self.InstanceSelected(element, snapshot.SelectedInstance);
        }
        else if (SelectedElement != null)
        {
            PluginManager.Self.ElementSelected(SelectedElement);
        }
    }

    private void TakeSnapshot(StateSaveCategory stateSaveCategory)
    {
        snapshot.SelectedStateCategorySave = stateSaveCategory;
        snapshot.SelectedStateSave = null;
    }

    private void TakeSnapshot(StateSave selectedStateSave)
    {
        snapshot.SelectedStateSave = selectedStateSave;
        var elementContainer =
            ObjectFinder.Self.GetStateContainerOf(selectedStateSave);
        StateSaveCategory category = null;
        category = elementContainer?.Categories.FirstOrDefault(item => item.States.Contains(selectedStateSave));
        snapshot.SelectedStateCategorySave = category;
    }


    private SelectedState()
    {

    }

    public void Initialize(MenuStripManager menuStripManager)
    {
        _menuStripManager = menuStripManager;
    }

    private void UpdateToSelectedElements(List<ElementSave> elements)
    {
        snapshot.SelectedElements = elements;
        if (elements?.Count > 0)
        {
            UpdateToSelectedElement(elements[0]);
        }
        else
        {
            UpdateToSelectedElement(null);
        }
    }



    private void UpdateToSelectedBehavior(BehaviorSave behavior)
    {
        if (behavior != snapshot.SelectedBehavior)
        {
            snapshot.SelectedBehavior = behavior;
            GumCommands.Self.GuiCommands.RefreshStateTreeView();

            WireframeObjectManager.Self.RefreshAll(false);

            _menuStripManager.RefreshUI();


            SelectedStateSave = null;
            SelectedStateCategorySave = null;
            if(SelectedBehavior != null)
            {
                SelectedElement = null;
            }
        }

    }

    private void UpdateToSelectedBehaviorVariable(VariableSave variable)
    {
        if (variable != snapshot.SelectedBehaviorVariable)
        {
            snapshot.SelectedBehaviorVariable = variable;
            PropertyGridManager.Self.SelectedBehaviorVariable = variable;
            _menuStripManager.RefreshUI();
        }
    }


    private void UpdateToSelectedInstances(IEnumerable<InstanceSave> instances)
    {
        snapshot.SelectedInstances = instances ?? new List<InstanceSave>();
        if (instances?.Count() > 0)
        {
            PerformAfterSelectInstanceLogic();
        }
        else
        {
            UpdateToSetSelectedInstance(null);
        }
    }



    public List<ElementWithState> GetTopLevelElementStack()
    {
        List<ElementWithState> toReturn = new List<ElementWithState>();

        if (SelectedElement != null)
        {
            ElementWithState item = new ElementWithState(SelectedElement);
            if (this.SelectedStateSave != null)
            {
                item.StateName = this.SelectedStateSave.Name;
            }
            toReturn.Add(item);


        }

        return toReturn;
    }


}

/// <summary>
/// Stores the selected state snapshot, allowing the selected state to be stored
/// without using any UI. 
/// </summary>
/// <remarks>
/// This is similar to how Glue stores its selections, and we're migrating to this
/// so that the UI can be swapped out more easily.
/// </remarks>
class SelectedStateSnapshot : ISelectedState
{
    public ScreenSave SelectedScreen
    {
        get => SelectedElement as ScreenSave;
        set
        {
            SelectedElement = value;
        }
    }
    public ElementSave SelectedElement
    {
        get => selectedElements.FirstOrDefault();
        set
        {
            selectedElements.Clear();
            if(value != null)
            {
                selectedElements.Add(value);
            }
        }
    }

    List<ElementSave> selectedElements = new List<ElementSave>();
    public List<ElementSave> SelectedElements 
    {
        get => selectedElements;
        set
        {
            selectedElements.Clear();
            if(value?.Count > 0)
            {
                selectedElements.AddRange(value);
            }
        }
    }

    public IStateContainer SelectedStateContainer
    {
        get => (IStateContainer)SelectedElement ?? SelectedBehavior;
        set
        {
            if (value is ElementSave elementSave)
            {
                SelectedElement = elementSave;
            }
            else if (value is BehaviorSave behaviorSave)
            {
                SelectedBehavior = behaviorSave;
            }
            else
            {
                SelectedElement = null;
                SelectedBehavior = null;
            }
        }
    }

    public BehaviorSave SelectedBehavior { get; set; }
    public StateSave CustomCurrentStateSave { get; set; }
    public StateSave SelectedStateSave { get; set; }

    public StateSave SelectedStateSaveOrDefault { get; set; }

    public StateSaveCategory SelectedStateCategorySave { get; set; }
    public ComponentSave SelectedComponent
    {
        get => SelectedElement as ComponentSave;
        set
        {
            SelectedElement = value;
        }
    }
    //public InstanceSave SelectedInstance { get; set; }
    public InstanceSave SelectedInstance
    {
        get => SelectedInstances.FirstOrDefault();
        set
        {
            selectedInstances.Clear();
            if(value != null)
            {
                selectedInstances.Add(value);
            }
        }
    }
    public IPositionedSizedObject SelectedIpso { get; set; }

    public List<GraphicalUiElement> SelectedIpsos { get; set; }

    List<InstanceSave> selectedInstances = new List<InstanceSave>();
    public IEnumerable<InstanceSave> SelectedInstances
    {
        get => selectedInstances;
        set
        {
            selectedInstances.Clear();
            if (value?.Count() > 0)
            {
                selectedInstances.AddRange(value);
            }
        }
    }

    public string SelectedVariableName { get; set; }

    public StandardElementSave SelectedStandardElement
    {
        get => SelectedElement as StandardElementSave;
        set
        {
            SelectedElement = value;
        }
    }
    public VariableSave SelectedVariableSave { get; set; }
    public VariableSave SelectedBehaviorVariable { get; set; }

    public TreeNode SelectedTreeNode { get; set; }

    public IEnumerable<TreeNode> SelectedTreeNodes { get; set; }

    public RecursiveVariableFinder SelectedRecursiveVariableFinder { get; set; }

    public StateStackingMode StateStackingMode { get; set; }

    public List<ElementWithState> GetTopLevelElementStack()
    {
        throw new NotImplementedException();
    }
}

public static class IEnumerableExtensionMethods
{
    public static int GetCount(this IEnumerable<InstanceSave> enumerable)
    {
        int toReturn = 0;


        foreach (var item in enumerable)
        {
            toReturn++;
        }

        return toReturn;
    }
}
