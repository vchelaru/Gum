using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Messages;
using Gum.Plugins;
using Gum.Wireframe;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum.ToolStates;

public class SelectedState : ISelectedState
{
    #region Fields

    private readonly IGuiCommands _guiCommands;
    private readonly IPluginManager _pluginManager;
    private readonly IMessenger _messenger;
    // Lazy because PropertyGridManager depends on ISelectedState; this breaks the DI construction cycle.
    private readonly Lazy<IBehaviorVariablePropertyGridSink> _lazyPropertyGridManager;
    SelectedStateSnapshot snapshot = new SelectedStateSnapshot();

    #endregion

    #region Elements (Screen, Component, StandardElement)

    public ScreenSave? SelectedScreen
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

    public ComponentSave? SelectedComponent
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

    public StandardElementSave? SelectedStandardElement
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

        var instanceClearedAsSideEffect = false;
        if (value?.Count > 0)
        {
            // Setting an element implicitly deselects any selected instance. Fire
            // InstanceSelected with a null instance so listeners (notably the variable
            // grid) refresh — otherwise they keep displaying the deselected instance's
            // variables. See issue #2946.
            instanceClearedAsSideEffect = snapshot.SelectedInstance != null;
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

        if (instanceClearedAsSideEffect)
        {
            _pluginManager.InstanceSelected(SelectedElement, null);
        }

        if (differ || (instancesBefore.Count > 0 && SelectedElement?.Instances.Count == 0))
        {
            _pluginManager.ElementSelected(SelectedElement);

            _messenger.Send(new SelectionChangedMessage());
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

    public IStateContainer? SelectedStateContainer
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


    #endregion

    #region Behavior

    public BehaviorSave? SelectedBehavior
    {
        get
        {
            return snapshot.SelectedBehavior;
        }
        set
        {
            if (value == null)
            {
                SelectedBehaviors = new List<BehaviorSave>();
            }
            else
            {
                SelectedBehaviors = new List<BehaviorSave> { value };
            }
        }
    }

    public IEnumerable<BehaviorSave> SelectedBehaviors
    {
        get
        {
            return snapshot.SelectedBehaviors;
        }
        set
        {
            HandleBehaviorsSelected(value?.ToList());
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

        _pluginManager.BehaviorReferenceSelected(behaviorReference, SelectedElement);
    }

    private void HandleBehaviorsSelected(List<BehaviorSave>? value)
    {
        var behaviorBefore = SelectedBehaviors.ToList();
        var instancesBefore = SelectedInstances.ToList();

        if (value?.Count > 0 && SelectedInstance != null)
        {
            SelectedInstance = null;
        }

        var differ = behaviorBefore.Count != (value?.Count ?? 0);

        if (!differ && value != null)
        {
            for (int i = 0; i < behaviorBefore.Count; i++)
            {
                if (behaviorBefore[i] != value[i])
                {
                    differ = true;
                    break;
                }
            }
        }

        if(differ)
        {
            SelectedBehaviorReference = null;

            UpdateToSelectedBehaviors(value);
        }


        if (differ || instancesBefore.Count != 0 && SelectedInstances.Count() == 0)
        {
            _pluginManager.BehaviorSelected(SelectedBehavior);

            _messenger.Send(new SelectionChangedMessage());
        }
    }

    private void UpdateToSelectedBehaviors(List<BehaviorSave> behaviors)
    {
        snapshot.SelectedBehaviors = behaviors;

        _guiCommands.RefreshStateTreeView();

        SelectedStateSave = null;
        SelectedStateCategorySave = null;
        if (SelectedBehavior != null)
        {
            SelectedElement = null;
        }
    }


    #endregion

    #region Properties



    public ITreeNode? SelectedTreeNode => SelectedTreeNodes.FirstOrDefault();

    public IEnumerable<ITreeNode> SelectedTreeNodes =>
        _pluginManager.GetSelectedNodes();


    public IPositionedSizedObject? SelectedIpso
    {
        get => _pluginManager.GetSelectedIpsos()?.FirstOrDefault();
    }

    #endregion


    public SelectedState(IGuiCommands guiCommands,
        IPluginManager pluginManager,
        IMessenger messenger,
        Lazy<IBehaviorVariablePropertyGridSink> lazyPropertyGridManager)
    {
        _guiCommands = guiCommands;
        _pluginManager = pluginManager;
        _messenger = messenger;
        _lazyPropertyGridManager = lazyPropertyGridManager;
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
            _pluginManager.InstanceSelected(element, newInstance);

            if(newInstance == null)
            {
                // If we forcefully set null instances, let's forcefully select the current element or behavior:
                if(SelectedElement != null || elementBefore != null)
                {
                    _pluginManager.ElementSelected(SelectedElement);
                }

                if(SelectedBehavior != null)
                {
                    _pluginManager.BehaviorSelected(SelectedBehavior);
                }
            }

            _messenger.Send(new SelectionChangedMessage());
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

            var elementAfter = ObjectFinder.Self.GetElementContainerOf(snapshot.SelectedInstance);
            var behaviorAfter = ObjectFinder.Self.GetBehaviorContainerOf(snapshot.SelectedInstance);

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
    }


    #endregion

    #region StateSaveCategory


    public StateSaveCategory? SelectedStateCategorySave
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
    private void HandleSelectedStateCategorySave(StateSaveCategory? value)
    {
        if (_isHandlingCategoryAssigment) return;
        _isHandlingCategoryAssigment = true;
        try
        {
            var categoryBefore = SelectedStateCategorySave;

            if(value != null)
            {
                snapshot.SelectedStateSave = null;
                _pluginManager.ReactToStateSaveSelected(null);
            }
            UpdateToSetSelectedStateSaveCategory(value);

            if (categoryBefore != SelectedStateCategorySave)
            {
                _pluginManager.ReactToStateSaveCategorySelected(SelectedStateCategorySave);
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

        _pluginManager.ReactToCustomStateSaveSelected(value);

    }

    public StateSave? SelectedStateSave
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

    private void HandleStateSaveSelected(StateSave? stateSave)
    {
        StateSaveCategory? category = null;
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
            _pluginManager.ReactToStateSaveSelected(stateSave);
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



        _pluginManager.VariableSelected(SelectedStateContainer, value);

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

        _pluginManager.BehaviorVariableSelected(value);
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
            _lazyPropertyGridManager.Value.SelectedBehaviorVariable = variable;
        }
    }

    #endregion


}
