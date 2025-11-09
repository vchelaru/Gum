using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.Managers;
using Gum.DataTypes.Variables;
using System.Windows.Forms;
using Gum.Commands;
using Gum.Wireframe;
using Gum.Plugins;
using Gum.Debug;
using RenderingLibrary;
using Gum.DataTypes.Behaviors;
using Gum.Controls;
using Newtonsoft.Json.Linq;
using Gum.Events;
using Gum.Services;

namespace Gum.ToolStates;

public class SelectedState : ISelectedState
{
    #region Fields
    
    private readonly IGuiCommands _guiCommands;

    SelectedStateSnapshot snapshot = new SelectedStateSnapshot();

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

    public ElementSave? SelectedElement
    {
        get
        {
            return snapshot.SelectedElement;
        }
        set
        {
            if (value == null)
            {
                SelectedElements = new List<ElementSave>();
            }
            else
            {
                SelectedElements = new List<ElementSave> { value };
            }
        }
    }

    public IEnumerable<ElementSave> SelectedElements
    {
        get
        {
            return snapshot.SelectedElements;
        }
        set
        {

            HandleElementsSelected(value.ToList());
        }
    }

    private void HandleElementsSelected(List<ElementSave> value)
    {
        var elementsBefore = SelectedElements.ToList();
        var instancesBefore = SelectedInstances.ToList();

        if (value?.Count > 0)
        {
            snapshot.SelectedInstance = null;
            snapshot.SelectedBehavior = null;
        }



        var differ = elementsBefore.Count != value.Count;

        if (!differ)
        {
            for (int i = 0; i < elementsBefore.Count; i++)
            {
                if (elementsBefore[i] != value[i])
                {
                    differ = true;
                    break;
                }
            }
        }

        if(differ)
        {
            snapshot.SelectedBehaviorReference = null;
            UpdateToSelectedElements(value);
        }

        if (differ || (instancesBefore.Count > 0 && SelectedElement?.Instances.Count == 0))
        {
            PluginManager.Self.ElementSelected(SelectedElement);
        }
    }


    private void UpdateToSelectedElements(List<ElementSave> elements)
    {
        snapshot.SelectedElements = elements;

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
            _guiCommands.RefreshVariables();
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

    public ElementBehaviorReference SelectedBehaviorReference
    {
        get => snapshot.SelectedBehaviorReference;
        set => HandleBehaviorReferenceSelected(value);
    }

    private void HandleBehaviorReferenceSelected(ElementBehaviorReference behaviorReference)
    {
        snapshot.SelectedBehaviorReference = behaviorReference;

        PluginManager.Self.BehaviorReferenceSelected(behaviorReference, SelectedElement);
    }

    private void HandleBehaviorSelected(BehaviorSave behavior)
    {
        var behaviorBefore = SelectedBehavior;
        var instancesBefore = SelectedInstances.ToList();

        if (behavior != null && SelectedInstance != null)
        {
            SelectedInstance = null;
        }
        SelectedBehaviorReference = null;

        UpdateToSelectedBehavior(behavior);

        if (behavior != behaviorBefore || instancesBefore.Count != 0 && SelectedInstances.Count() == 0)
        {
            PluginManager.Self.BehaviorSelected(SelectedBehavior);
        }
    }

    private void UpdateToSelectedBehavior(BehaviorSave behavior)
    {
        if (behavior != snapshot.SelectedBehavior)
        {
            snapshot.SelectedBehavior = behavior;
            _guiCommands.RefreshStateTreeView();

            // todo : this should be handled by plugins, and should not be explicitly handled here:
            WireframeObjectManager.Self.RefreshAll(false);

            SelectedStateSave = null;
            SelectedStateCategorySave = null;
            if (SelectedBehavior != null)
            {
                SelectedElement = null;
            }
        }
    }


    #endregion

    #region Properties
    
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


    public ITreeNode SelectedTreeNode => SelectedTreeNodes.FirstOrDefault();

    public IEnumerable<ITreeNode> SelectedTreeNodes =>
        PluginManager.Self.GetSelectedNodes();

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

    public IPositionedSizedObject? SelectedIpso
    {
        get => PluginManager.Self.GetSelectedIpsos()?.FirstOrDefault();
    }

    #endregion

    public SelectedState(IGuiCommands guiCommands)
    {
        _guiCommands = guiCommands;
    }

    #region Instance


    public InstanceSave SelectedInstance
    {
        get
        {
            return snapshot.SelectedInstance;
        }
        set
        {
            if (value == null)
            {
                SelectedInstances = new List<InstanceSave>();
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

    private void HandleSelectedInstances(List<InstanceSave> value)
    {
        var instancesBefore = snapshot.SelectedInstances.ToList();
        var elementBefore = snapshot.SelectedElement;

        var newInstance = value?.FirstOrDefault();

        var behaviorBefore = SelectedBehavior;

        if (newInstance != null)
        {
            var elementAfter = ObjectFinder.Self.GetElementContainerOf(newInstance);
            var behaviorAfter = ObjectFinder.Self.GetBehaviorContainerOf(newInstance);

            if(elementAfter != elementBefore)
            {

                snapshot.SelectedElement = elementAfter;
            }

            snapshot.SelectedBehavior = behaviorAfter;

            snapshot.SelectedBehaviorReference = null;

        }

        if (behaviorBefore != SelectedBehavior && SelectedBehavior != null)
        {
            snapshot.SelectedStateCategorySave = null;
            snapshot.SelectedStateSave = null;
        }

        UpdateToSelectedInstances(value);

        ElementSave? element = null;

        if (newInstance != null)
        {
            element = ObjectFinder.Self.GetElementContainerOf(newInstance);
        }

        if (!AreSame(value, instancesBefore))
        {
            PluginManager.Self.InstanceSelected(element, newInstance);

            if(newInstance == null)
            {
                // If we forcefully set null instances, let's forcefully select the current element or behavior:
                if(SelectedElement != null || elementBefore != null)
                {
                    PluginManager.Self.ElementSelected(SelectedElement);
                }

                if(SelectedBehavior != null)
                {
                    PluginManager.Self.BehaviorSelected(SelectedBehavior);
                }
            }
        }

    }

    private static bool AreSame(List<InstanceSave> value, IEnumerable<InstanceSave> instancesBefore)
    {
        if(value.Count != instancesBefore.Count())
        {
            return false;
        }

        for(int i = 0; i < value.Count; i++)
        {
            if (value[i] != instancesBefore.ElementAt(i))
            {
                return false;
            }
        }

        return true;
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
            var isSame = snapshot.SelectedInstance == null;
            if (!isSame)
            {
                snapshot.SelectedInstance = null;
                PerformAfterSelectInstanceLogic();
            }
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

            // It's okay if the parent isn't part of the project, we could have deleted the entire component and instance
            // through the top level menu, which would mean the parent is no longer here:
            //ProjectVerifier.Self.AssertIsPartOfProject(parent);

            if (elementAfter != null || behaviorAfter != null)
            {
                if (stateContainerBefore != elementAfter && stateContainerBefore != behaviorAfter)
                {
                    _guiCommands.RefreshStateTreeView();
                }
            }
        }


        if (SelectedElement != null && (SelectedStateSave == null || SelectedElement.AllStates.Contains(SelectedStateSave) == false))
        {
            var shouldSelectDefault = true;
            // This can happen on a redo where the state gets re-created, so instances are not preserved. In this case, we should 
            // check if there are any matching states in matching categories.
            if(SelectedStateCategorySave != null)
            {
                var categoryInElement = SelectedElement.Categories.Find(item => item.Name == SelectedStateCategorySave.Name);

                if(SelectedStateSave != null)
                {
                    var state = categoryInElement?.States.Find(item => item.Name == SelectedStateSave.Name);

                    if(state != null)
                    {
                        SelectedStateSave = state;
                        shouldSelectDefault = false;
                    }
                }
            }

            if(shouldSelectDefault)
            {
                SelectedStateSave = SelectedElement.States[0];
            }
        }

        // todo - this should be handled by plugins and should not be explicitly called here
        if (WireframeObjectManager.Self.ElementShowing != this.SelectedElement)
        {
            WireframeObjectManager.Self.RefreshAll(false);
        }

        // This is needed for the wireframe manager, but this should be moved to a plugin
        GumEvents.Self.CallInstanceSelected();
    }


    #endregion

    #region StateSaveCategory


    public StateSaveCategory SelectedStateCategorySave
    {
        get
        {
            return snapshot.SelectedStateCategorySave;
        }
        set
        {
            HandleSelectedStateCategorySave(value);
        }
    }

    // Selected categories in animations vs treeview can "fight" and we want to avoid infinite recursion
    bool _isHandlingCategoryAssigment = false;
    private void HandleSelectedStateCategorySave(StateSaveCategory value)
    {
        if (_isHandlingCategoryAssigment) return;
        _isHandlingCategoryAssigment = true;
        try
        {
            var categoryBefore = SelectedStateCategorySave;

            if(value != null)
            {
                snapshot.SelectedStateSave = null;
                PluginManager.Self.ReactToStateSaveSelected(null);
            }
            UpdateToSetSelectedStateSaveCategory(value);

            if (categoryBefore != SelectedStateCategorySave)
            {
                PluginManager.Self.ReactToStateSaveCategorySelected(SelectedStateCategorySave);
            }
        }
        finally
        {
            _isHandlingCategoryAssigment = false;
        }
    }

    private void UpdateToSetSelectedStateSaveCategory(StateSaveCategory selectedStateSaveCategory)
    {
        var isSame = snapshot.SelectedStateCategorySave == selectedStateSaveCategory;
        if (!isSame)
        {
            TakeSnapshot(selectedStateSaveCategory);
        }
    }


    private void TakeSnapshot(StateSaveCategory stateSaveCategory)
    {
        snapshot.SelectedStateCategorySave = stateSaveCategory;
        snapshot.SelectedStateSave = null;
    }

    #endregion

    #region StateSave

    public StateSave CustomCurrentStateSave
    {
        get => snapshot.CustomCurrentStateSave;
        set
        {
            HandleCustomStateSaveSelected(value);

        }
    }

    private void HandleCustomStateSaveSelected(StateSave value)
    {
        snapshot.CustomCurrentStateSave = value;

        PluginManager.Self.ReactToCustomStateSaveSelected(value);

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

    private void HandleStateSaveSelected(StateSave stateSave)
    {
        StateSaveCategory category = null;
        var elementContainer =
            ObjectFinder.Self.GetStateContainerOf(stateSave);

        category = elementContainer?.Categories.FirstOrDefault(item => item.States.Contains(stateSave));

        if (category != null && category != snapshot.SelectedStateCategorySave)
        {
            snapshot.SelectedStateCategorySave = category;
        }

        var isSame = snapshot.SelectedStateSave == stateSave;
        if (!isSame)
        {
            if(stateSave != null)
            {
                snapshot.CustomCurrentStateSave = null;
            }
            TakeSnapshot(stateSave);
            PluginManager.Self.ReactToStateSaveSelected(stateSave);
        }

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

    #endregion

    #region Variables

    public VariableSave SelectedVariableSave
    {
        get
        {
            return snapshot.SelectedVariableSave;
        }

        set
        {
            HandleVariableSaveSelected(value);
        }
    }

    private void HandleVariableSaveSelected(VariableSave value)
    {
        snapshot.SelectedVariableSave = value;



        PluginManager.Self.VariableSelected(SelectedStateContainer, value);

    }

    public VariableSave SelectedBehaviorVariable
    {
        get
        {
            return snapshot.SelectedBehaviorVariable;
        }

        set
        {
            HandleSelectedBehaviorVariable(value);
        }
    }

    private void HandleSelectedBehaviorVariable(VariableSave value)
    {
        UpdateToSelectedBehaviorVariable(value);

        PluginManager.Self.BehaviorVariableSelected(value);
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


    private void UpdateToSelectedBehaviorVariable(VariableSave variable)
    {
        if (variable != snapshot.SelectedBehaviorVariable)
        {
            snapshot.SelectedBehaviorVariable = variable;

            // This should go through a plugin, an dshould be on the HandleSelect method
            PropertyGridManager.Self.SelectedBehaviorVariable = variable;
        }
    }

    #endregion

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
class SelectedStateSnapshot
{
    public ScreenSave? SelectedScreen
    {
        get => SelectedElement as ScreenSave;
        set => SelectedElement = value;
    }
    public ElementSave? SelectedElement
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

    public BehaviorSave? SelectedBehavior { get; set; }
    public ElementBehaviorReference? SelectedBehaviorReference { get; set; }

    public StateSave? CustomCurrentStateSave { get; set; }
    public StateSave? SelectedStateSave { get; set; }

    public StateSave? SelectedStateSaveOrDefault { get; set; }

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
    public IPositionedSizedObject? SelectedIpso { get; }

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

}
