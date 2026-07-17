using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using GumRuntime;
using Gum.Managers;
using Gum.Messages;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using ToolsUtilities;

namespace Gum.Logic;

#region Copy Type

public enum CopyType
{
    InstanceOrElement = 1,
    State = 2,
    Category = 3,
}

public enum TopOrRecursive
{
    Top,
    Recursive
}

public class CopiedData
{
    /// <summary>
    /// The instances which were selected when the user pressed COPY
    /// </summary>
    public List<InstanceSave> CopiedInstancesSelected = new List<InstanceSave>();
    /// <summary>
    /// The instances which were selected along with all children of the selected instances.
    /// </summary>
    public List<InstanceSave> CopiedInstancesRecursive = new List<InstanceSave>();
    public List<StateSave> CopiedStates = new List<StateSave>();
    /// <summary>
    /// Cloned default states of the source element's BaseType chain — kept separate
    /// from <see cref="CopiedStates"/> (which holds the source's OWN selected state)
    /// so paste can filter base-level variables that are owned by reachable
    /// VariableReferences (same logic as the GUM0002 check). Directly-authored
    /// variables on the source's own state pass through untouched.
    /// </summary>
    public List<StateSave> CopiedBaseElementDefaultStates = new List<StateSave>();
    /// <summary>
    /// Qualified item names (variable names like <c>"TextInstance.FontSize"</c> and/or
    /// the list-marker <c>"TextInstance.VariableReferences"</c>) owned by reachable
    /// categorized states on the source instance — covering refs, direct scalars on
    /// those states, and the matched VariableReferences rows themselves. Used to drop
    /// items from base-element captures at paste time so the new instance picks up
    /// the active categorized state instead of the source's base-level orphans.
    /// See <c>ElementSaveExtensions.GetItemNamesOwnedByReachableCategorizedStates</c>.
    /// </summary>
    public HashSet<string> CopiedNamesOwnedByReachableStates = new HashSet<string>(StringComparer.Ordinal);
    public ElementSave CopiedElement = null;
    public StateSaveCategory CopiedCategory = null;
}

#endregion

public class CopyPasteLogic : ICopyPasteLogic
{
    #region Fields/Properties

    private readonly ISelectedState _selectedState;
    private readonly IElementCommands _elementCommands;
    private readonly IDialogService _dialogService;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly ICopyPasteProjectCommands _projectCommands;
    private readonly IUndoManager _undoManager;
    private readonly IDeleteLogic _deleteLogic;
    private readonly ICopyPastePluginNotifier _copyPastePluginNotifier;
    private readonly IMessenger _messenger;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly ICopyPasteProjectProvider _copyPasteProjectProvider;
    private readonly IStandardElementsManagerGumTool _standardElementsManagerGumTool;

    public CopiedData CopiedData { get; private set; } = new CopiedData();

    CopyType _copyType;

    /// <summary>
    /// Keeps track of whether the user has copied, moved selection, then pasted.
    /// If this value is false, then a copy/paste occurred and we should use the default
    /// behavior for pasting. 
    /// If this is true, then the user has explicitly selected a spot for pasting, so we should
    /// respect that.
    /// </summary>
    bool _hasChangedSelectionSinceCopy;

    #endregion

    #region Constructor
    public CopyPasteLogic(ISelectedState selectedState,
        IElementCommands elementCommands,
        IDialogService dialogService,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        ICopyPasteProjectCommands projectCommands,
        IUndoManager undoManager,
        IDeleteLogic deleteLogic,
        ICopyPastePluginNotifier copyPastePluginNotifier,
        IWireframeObjectManager wireframeObjectManager,
        IMessenger messenger,
        ICopyPasteProjectProvider copyPasteProjectProvider,
        IStandardElementsManagerGumTool standardElementsManagerGumTool
        )
    {
        _wireframeObjectManager = wireframeObjectManager;
        _selectedState = selectedState;
        _elementCommands = elementCommands;
        _dialogService = dialogService;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _projectCommands = projectCommands;
        _undoManager = undoManager;
        _deleteLogic = deleteLogic;
        _copyPastePluginNotifier = copyPastePluginNotifier;
        _messenger = messenger;
        _copyPasteProjectProvider = copyPasteProjectProvider;
        _standardElementsManagerGumTool = standardElementsManagerGumTool;


        _messenger.Register<SelectionChangedMessage>(
            this,
            (_, message) => HandleSelectionChanged());


    }

    #endregion

    private void HandleSelectionChanged()
    {
        if(!isSelectionCausedByPaste)
        {
            _hasChangedSelectionSinceCopy = true;

            if (lastPasteOriginalToParentAssociation.Count > 0)
            {
                lastPasteOriginalToParentAssociation = new Dictionary<InstanceSave, object>();
            }
            instancesSinceLastCopyOrSelection.Clear();
        }
    }

    /// <summary>
    /// Forces the CopyPasteLogic to treat the selection as having changed since the last copy, so that
    /// pastes will respect the current selection.
    /// </summary>
    public void ForceSelectionChanged()
    {
        _hasChangedSelectionSinceCopy = true;
    }

    #region Copy
    public void OnCopy(CopyType copyType)
    {
        StoreCopiedObject(copyType, _selectedState);

        _hasChangedSelectionSinceCopy = false;
        if(lastPasteOriginalToParentAssociation.Count > 0)
        {
            lastPasteOriginalToParentAssociation = new Dictionary<InstanceSave, object>();
        }
        instancesSinceLastCopyOrSelection.Clear();
    }


    private void StoreCopiedObject(CopyType copyType, ISelectedState selectedState)
    {
        _copyType = copyType;
        CopiedData.CopiedElement = null;
        CopiedData.CopiedInstancesRecursive.Clear();
        CopiedData.CopiedStates.Clear();
        CopiedData.CopiedBaseElementDefaultStates.Clear();
        CopiedData.CopiedNamesOwnedByReachableStates.Clear();
        CopiedData.CopiedCategory = null;

        if (copyType == CopyType.InstanceOrElement)
        {
            if (selectedState.SelectedInstances.Count() != 0)
            {
                StoreCopiedInstances(selectedState);
            }
            else if (selectedState.SelectedElement != null)
            {
                StoreCopiedElementSave();
            }
        }
        else if (copyType == CopyType.State)
        {
            StoreCopiedState();
        }
        else if (copyType == CopyType.Category)
        {
            StoreCopiedCategory();
        }
    }

    private void StoreCopiedCategory()
    {
        if (_selectedState.SelectedStateCategorySave != null)
        {
            CopiedData.CopiedCategory = _selectedState.SelectedStateCategorySave.Clone();
        }
    }

    private void StoreCopiedState()
    {
        if (_selectedState.SelectedStateSave != null)
        {
            CopiedData.CopiedStates.Clear();
            CopiedData.CopiedStates.Add(_selectedState.SelectedStateSave.Clone());
        }
    }

    private void StoreCopiedInstances(ISelectedState selectedState)
    {
        if (selectedState.SelectedInstances.Any())
        {
            var element = selectedState.SelectedElement;

            var state = selectedState.SelectedStateSave;

            // a state may not be selected if the user selected a category.
            if (state == null)
            {
                state = element?.DefaultState;
            }

            CopiedData.CopiedStates.Clear();
            CopiedData.CopiedBaseElementDefaultStates.Clear();
            CopiedData.CopiedNamesOwnedByReachableStates.Clear();

            // Base element default states go in their OWN list. Paste filters these
            // by the ref-owned LHS set so the source's base-level orphans don't get
            // materialized as explicit overrides on the new instance.
            var baseElementsDerivedFirst = element != null ? ObjectFinder.Self.GetBaseElements(element) : new List<ElementSave>();
            for (int i = baseElementsDerivedFirst.Count - 1; i > -1; i--)
            {
                CopiedData.CopiedBaseElementDefaultStates.Add(baseElementsDerivedFirst[i].DefaultState.Clone());
            }

            if (state != null)
            {
                CopiedData.CopiedStates.Add(state.Clone());

                // Collect all LHSes owned by VariableReferences reachable from the source
                // instance(s) in this state — used to filter the base captures during paste.
                foreach (var sourceInstance in selectedState.SelectedInstances)
                {
                    CopiedData.CopiedNamesOwnedByReachableStates.UnionWith(
                        state.GetItemNamesOwnedByReachableCategorizedStates(sourceInstance));
                }
            }

            if (selectedState.SelectedStateCategorySave != null && selectedState.SelectedStateSave != null && element != null)
            {
                // it's categorized, so add the default:
                CopiedData.CopiedStates.Add(element.DefaultState.Clone());
            }

            List<InstanceSave> selected = new List<InstanceSave>();
            // When copying we want to grab all instances in the order that they are in their container.
            // That way when they're pasted they are pasted in the right order
            selected.AddRange(selectedState.SelectedInstances);

            CopiedData.CopiedInstancesSelected.Clear();
            CopiedData.CopiedInstancesSelected.AddRange(selectedState.SelectedInstances);

            var parentContainer = selected.FirstOrDefault()?.ParentContainer;
            if (parentContainer != null)
            {
                CopiedData.CopiedInstancesRecursive = GetAllInstancesAndChildrenOf(selected, selected.FirstOrDefault()?.ParentContainer)
                            // Sort by index in parent at the end so the children are sorted properly:
                            .OrderBy(item =>
                            {
                                return element?.Instances.IndexOf(item) ?? 0;
                            })
                            // clone after doing OrderBy
                            .Select(item => item.Clone())
                            .ToList();
            }
            else
            {
                CopiedData.CopiedInstancesRecursive.AddRange(CopiedData.CopiedInstancesSelected);
            }


            // Clear out any variables that don't pertain to the selected instance:
            foreach (var copiedState in CopiedData.CopiedStates)
            {
                for (int i = copiedState.Variables.Count - 1; i > -1; i--)
                {
                    if (CopiedData.CopiedInstancesRecursive.Any(item => item.Name == copiedState.Variables[i].SourceObject) == false)
                    {
                        copiedState.Variables.RemoveAt(i);
                    }
                }

                // And also any VariableLists:
                for (int i = copiedState.VariableLists.Count - 1; i > -1; i--)
                {
                    if (CopiedData.CopiedInstancesRecursive.Any(item => item.Name == copiedState.VariableLists[i].SourceObject) == false)
                    {
                        copiedState.VariableLists.RemoveAt(i);
                    }
                }

            }
        }
    }

    private void StoreCopiedElementSave()
    {
        if (_selectedState.SelectedElement != null)
        {
            if (_selectedState.SelectedElement is ScreenSave)
            {
                CopiedData.CopiedElement = ((ScreenSave)_selectedState.SelectedElement).Clone();
            }
            else if (_selectedState.SelectedElement is ComponentSave)
            {
                CopiedData.CopiedElement = ((ComponentSave)_selectedState.SelectedElement).Clone();
            }
        }
    }

    #endregion

    public void OnCut(CopyType copyType)
    {
        StoreCopiedObject(copyType, _selectedState);

        _hasChangedSelectionSinceCopy = false;
        if(lastPasteOriginalToParentAssociation.Count > 0)
        {
            lastPasteOriginalToParentAssociation = new Dictionary<InstanceSave, object>();
        }
        instancesSinceLastCopyOrSelection.Clear();

        ElementSave? sourceElement = _selectedState.SelectedElement;

        if(sourceElement != null)
        {
            if (CopiedData.CopiedInstancesRecursive.Any())
            {
                foreach (var clone in CopiedData.CopiedInstancesRecursive)
                {
                    // copied instances is a clone, so need to find by name:
                    var originalForCopy = sourceElement.Instances.FirstOrDefault(item => item.Name == clone.Name);
                    if (sourceElement.Instances.Contains(originalForCopy))
                    {
                        _deleteLogic.RemoveInstance(originalForCopy, sourceElement);
                    }
                }

                _fileCommands.TryAutoSaveElement(sourceElement);
                _wireframeObjectManager.RefreshAll(true);
                _guiCommands.RefreshVariables();
                _guiCommands.RefreshElementTreeView();
            }
        }


        // todo: need to handle cut Element saves, but I don't want to do it yet due to the danger of losing valid data...


    }

    #region Paste

    public void OnPaste(CopyType copyType, TopOrRecursive topOrRecursive = TopOrRecursive.Recursive)
    {
        ////////////////////Early Out
        if (_copyType != copyType)
        {
            return;
        }

        using var undoLock = _undoManager.RequestLock();

        // To make sure we didn't copy one type and paste another
        if (_copyType == CopyType.InstanceOrElement)
        {
            if (CopiedData.CopiedElement != null)
            {
                PasteCopiedElement();

            }
            // We need to both duplicate the InstanceSave, but we also need to duplicate all of the variables
            // that use the copied InstanceSave.
            else if (CopiedData.CopiedInstancesRecursive.Count != 0)
            {
                PasteCopiedInstanceSaves(topOrRecursive);
            }
        }
        else if (_copyType == CopyType.State && CopiedData.CopiedStates?.Count > 0)
        {
            PastedCopiedState();
        }
        else if (_copyType == CopyType.Category && CopiedData.CopiedCategory != null)
        {
            PasteCopiedCategory();
        }

    }

    private void PasteCopiedCategory()
    {
        var targetElement = _selectedState.SelectedElement;

        if (targetElement == null)
        {
            return;
        }

        StateSaveCategory newCategory = CopiedData.CopiedCategory.Clone();
        newCategory.Name = StringFunctions.MakeStringUnique(
            newCategory.Name,
            targetElement.Categories.Select(item => item.Name));

        foreach (var state in newCategory.States)
        {
            state.ParentContainer = targetElement;
        }

        targetElement.Categories.Add(newCategory);

        var targetInstanceNames = new HashSet<string>(targetElement.Instances.Select(item => item.Name));
        var missingInstanceNames = newCategory.States
            .SelectMany(state => state.Variables)
            .Select(variable => variable.SourceObject)
            .Where(name => !string.IsNullOrEmpty(name) && !targetInstanceNames.Contains(name))
            .Distinct()
            .ToList();

        _guiCommands.RefreshStateTreeView();
        _fileCommands.TryAutoSaveElement(targetElement);

        if (missingInstanceNames.Count > 0)
        {
            string message =
                $"The category {newCategory.Name} was pasted, but the following instance(s) " +
                $"referenced by its states do not exist on {targetElement.Name}. " +
                $"These variables were copied but will not apply until matching instances exist:\n\n" +
                string.Join("\n", missingInstanceNames);

            _dialogService.ShowMessage(message, "Pasted category has missing instance references");
        }
    }

    private void PasteCopiedInstanceSaves(TopOrRecursive topOrRecursive)
    {
        var selectedElement = _selectedState.SelectedElement;

        if (selectedElement == null)
        {
            _dialogService.ShowMessage("Select a target Screen or component to paste the copied instance(s).");
        }
        else if (topOrRecursive == TopOrRecursive.Recursive)
        {
            PasteInstanceSaves(CopiedData.CopiedInstancesRecursive, CopiedData.CopiedStates, selectedElement, _selectedState.SelectedInstance,
                baseElementDefaultStates: CopiedData.CopiedBaseElementDefaultStates,
                itemsOwnedByReachableStates: CopiedData.CopiedNamesOwnedByReachableStates,
                instancesToSelectAfterPaste: CopiedData.CopiedInstancesSelected);
        }
        else
        {
            PasteInstanceSaves(CopiedData.CopiedInstancesSelected, CopiedData.CopiedStates, selectedElement, _selectedState.SelectedInstance,
                baseElementDefaultStates: CopiedData.CopiedBaseElementDefaultStates,
                itemsOwnedByReachableStates: CopiedData.CopiedNamesOwnedByReachableStates);
        }

        if (selectedElement != null)
        {
            _elementCommands.SortVariables(selectedElement);
            _fileCommands.TryAutoSaveElement(selectedElement);
        }
    }

    private void PastedCopiedState()
    {
        var container = _selectedState.SelectedElement;
        var targetCategory = _selectedState.SelectedStateCategorySave;

        /////////////////////Early Out//////////////////
        if (container == null)
        {
            return;
        }
        //////////////////End Early Out////////////////

        StateSave newStateSave = CopiedData.CopiedStates.First().Clone();

        var validationResponse = ValidateStatePaste(targetCategory, container, newStateSave);

        if (validationResponse.Succeeded == false)
        {
            _dialogService.ShowMessage(validationResponse.Message, "Failed to paste state");
        }
        else
        {

            newStateSave.Variables.RemoveAll(item => item.CanOnlyBeSetInDefaultState);


            newStateSave.ParentContainer = container;

            string name = CopiedData.CopiedStates.First().Name;



            if (targetCategory != null)
            {
                if (targetCategory.States.Any(item => item.Name == name))
                {
                    name = name + "Copy";
                }

                name = StringFunctions.MakeStringUnique(name, targetCategory.States.Select(item => item.Name));
                newStateSave.Name = name;

                targetCategory.States.Add(newStateSave);
            }
            else
            {
                // no longer alowd to paste here
            }

            _guiCommands.RefreshStateTreeView();

            //_selectedState.SelectedInstance = targetInstance;
            _selectedState.SelectedStateSave = newStateSave;

            _fileCommands.TryAutoSaveElement(container);
        }
    }

    private GeneralResponse ValidateStatePaste(StateSaveCategory targetCategory, ElementSave targetElement, StateSave newState)
    {
        var toReturn = GeneralResponse.SuccessfulResponse;

        if (targetCategory.States.Count > 0)
        {
            HashSet<string> existingVariables = targetCategory.States.SelectMany(item => item.Variables.Select(variable => variable.Name)).ToHashSet();

            var newVariablesNotPresentInTarget = newState.Variables
                .Where(item => !existingVariables.Contains(item.Name))
                .ToArray();

            if (newVariablesNotPresentInTarget.Length > 0)
            {
                toReturn.Succeeded = false;

                string variableVariables = newVariablesNotPresentInTarget.Length == 1 ? "variable" : "variables";
                string isAre = newVariablesNotPresentInTarget.Length == 1 ? "is" : "are";

                toReturn.Message = $"The state {newState.Name} can not be pasted in {targetCategory.Name} because " +
                    $"the state has the following {variableVariables} which {isAre} not already set in the category:\n";

                foreach (var item in newVariablesNotPresentInTarget)
                {
                    toReturn.Message += item.Name + "\n";
                }
            }
        }

        if (toReturn.Succeeded)
        {
            var newStateVariableNames = newState.Variables.Select(item => item.Name).ToArray();

            List<string> newUnsupportedVariables = new List<string>();

            foreach (var variableName in newStateVariableNames)
            {
                // this variable better exist..somewhere:
                var existingVariable = targetElement.DefaultState.GetVariableRecursive(variableName);

                if (existingVariable == null)
                {
                    newUnsupportedVariables.Add(variableName);
                }
            }

            if (newUnsupportedVariables.Count > 0)
            {
                toReturn.Succeeded = false;

                toReturn.Message = $"The state {newState.Name} can not be pasted in {targetCategory.Name} because " +
                    $"the state has the following unsupported variable(s):\n";

                foreach (var variableName in newUnsupportedVariables)
                {
                    toReturn.Message += variableName + "\n";
                }
            }
        }

        return toReturn;
    }

    // This keeps track of the parent of the pasted objects, where the key is the original instance
    // and the value is the parent, which could be an element or an instance.
    // This is used in situations where the user:
    // 1. Copies
    // 2. Selects a new instance
    // 3. Pastes (which uses the selection)
    // 4. Pastes again, which should not use the selection, but should also not use the original parents because we did select in step (2)
    Dictionary<InstanceSave, object> lastPasteOriginalToParentAssociation = new();
    List<InstanceSave> instancesPastedSinceLastSelection = new List<InstanceSave>();
    bool isSelectionCausedByPaste = false;
    List<InstanceSave> instancesSinceLastCopyOrSelection = new();

    /// <summary>
    /// Pastes copies of the argument instancesToCopy into the targetElement.
    /// </summary>
    /// <param name="instancesToCopy"></param>
    /// <param name="copiedStates"></param>
    /// <param name="targetElement"></param>
    /// <param name="selectedInstance"></param>
    /// <param name="instancesToSelectAfterPaste">The source instances whose new copies should end up
    /// selected after the paste. Pass the explicitly-selected (non-recursive) set so that pasting a parent
    /// does not also leave its recursively-dragged children selected. When null, every new instance is
    /// selected (the legacy behavior used by drag-drop and the top-level paste path).</param>
    /// <returns>The newly-created instances</returns>
    public List<InstanceSave> PasteInstanceSaves(List<InstanceSave> instancesToCopy,
        List<StateSave> copiedStates,
        ElementSave targetElement,
        InstanceSave? selectedInstance,
        ISelectedState? forcedSelectedState = null,
        List<StateSave>? baseElementDefaultStates = null,
        HashSet<string>? itemsOwnedByReachableStates = null,
        List<InstanceSave>? instancesToSelectAfterPaste = null)
    {
        /////////////////////////Early Out///////////////////////
        if (targetElement is StandardElementSave)
        {
            _dialogService.ShowMessage($"Cannot create an instance in {targetElement} because it is a standard element");
            return new List<InstanceSave>();
        }
        ///////////////////////End Early Out/////////////////////

        var selectedState = forcedSelectedState ?? _selectedState;

        Dictionary<string, string> oldNewNameDictionary = new Dictionary<string, string>();

        List<InstanceSave> newInstances = new List<InstanceSave>();
        Dictionary<object, int> nextIndexByParent = new();
        Dictionary<InstanceSave, object> newInstanceToParentDictionary = new();

        bool shouldFillLastPasteOriginalToParentAssociation = _hasChangedSelectionSinceCopy;

        // Build parent map from copied states - this is the source of truth for
        // parent relationships that works for both copy and cut operations.
        // After a cut, the source element's state no longer has this info.
        var copiedParentMap = new Dictionary<string, string>();
        foreach (var state in copiedStates)
        {
            foreach (var variable in state.Variables)
            {
                if (variable.GetRootName() == "Parent" && variable.Value is string parentValue)
                {
                    copiedParentMap[variable.SourceObject] = parentValue;
                }
            }
        }

        #region Create the new instance and add it to the target element

        foreach (var sourceInstance in instancesToCopy)
        {
            // This could be an instance in a behavior, so we can't clone:
            //InstanceSave newInstance = sourceInstance.Clone();
            InstanceSave newInstance = new InstanceSave();
            newInstance.Name = sourceInstance.Name;
            newInstance.BaseType = sourceInstance.BaseType;
            newInstance.DefinedByBase = sourceInstance.DefinedByBase;
            newInstance.Locked = sourceInstance.Locked;

            // the original may have been defined in a base component. The new instance will not be
            // derived in the base, so let's get rid of that:
            newInstance.DefinedByBase = false;

            newInstances.Add(newInstance);


            if (targetElement != null)
            {

                var oldName = newInstance.Name;
                newInstance.Name = StringFunctions.MakeStringUnique(newInstance.Name, targetElement.Instances.Select(item => item.Name));
                var newName = newInstance.Name;

                oldNewNameDictionary[oldName] = newName;

                newInstance.ParentContainer = targetElement;
                int newIndex = -1;

                object? parent;

                // The logic for selecting the parent is:

                if(!_hasChangedSelectionSinceCopy)
                {
                    if(lastPasteOriginalToParentAssociation.ContainsKey(sourceInstance))
                    {
                        parent = lastPasteOriginalToParentAssociation[sourceInstance];
                    }
                    else
                    {
                        parent = GetParentElementOrInstanceFor(sourceInstance, copiedParentMap, instancesToCopy);
                    }
                }
                else
                {
                    var originalParent = GetParentElementOrInstanceFor(sourceInstance, copiedParentMap, instancesToCopy);
                    // is this attached to any of the copied instances? If so, we need to 
                    // keep the pasted instance attached to the copied instance:
                    var shouldAttachToPastedInstance =
                        originalParent is InstanceSave originalParentInstance && instancesToCopy.Any(item => item.Name == originalParentInstance.Name);
                    if (shouldAttachToPastedInstance)
                    {
                        parent = originalParent;
                    }
                    else
                    {
                        parent = (object?)selectedState.SelectedInstance ??
                            selectedState.SelectedElement;
                    }
                }

                newInstanceToParentDictionary[newInstance] = parent;

                if(shouldFillLastPasteOriginalToParentAssociation)
                {
                    lastPasteOriginalToParentAssociation[sourceInstance] = parent;
                }

                if (nextIndexByParent.ContainsKey(parent))
                {
                    newIndex = nextIndexByParent[parent];
                }
                else
                {
                    if (parent is ElementSave parentElement)
                    {
                        // Get the index by placing it after copied items.
                        // Since instances can change, we need to use name, but
                        // only consider same-named instances if they both came from
                        // the same element. Otherwise the user could be copying an instance
                        // from one element to another, and both elements could have instances
                        // named the same thing.
                        newIndex = instancesToCopy.Max(item =>
                            parent == item.ParentContainer ?
                                GetIndexOfInstanceByName(parentElement, item)
                                : -1);

                        foreach (var item in selectedState.SelectedInstances)
                        {
                            if (GetParentElementOrInstanceFor(item, copiedParentMap, instancesToCopy) == parentElement)
                            {
                                newIndex = System.Math.Max(newIndex, GetIndexOfInstanceByName(targetElement, item));
                            }
                        }
                    }
                    else if (parent is InstanceSave parentInstance)
                    {
                        if(_hasChangedSelectionSinceCopy)
                        {
                            // add it to the end:
                            foreach(var item in targetElement.Instances)
                            {
                                if(GetParentElementOrInstanceFor(item, copiedParentMap, instancesToCopy) == parentInstance)
                                {
                                    newIndex = System.Math.Max(newIndex, GetIndexOfInstanceByName(targetElement, item));
                                }
                            }
                        }
                        else
                        {
                            List<InstanceSave> instancesWithThisParent = new();
                            foreach (var item in instancesToCopy)
                            {
                                if (GetParentElementOrInstanceFor(item, copiedParentMap, instancesToCopy) == parentInstance)
                                {
                                    newIndex = System.Math.Max(newIndex, GetIndexOfInstanceByName(targetElement, item));
                                }
                            }
                            // also make sure that we go after selection, in case we are copy/pasting multiple times
                            foreach(var item in selectedState.SelectedInstances)
                            {
                                if(GetParentElementOrInstanceFor(item, copiedParentMap, instancesToCopy) == parentInstance)
                                {
                                    newIndex = System.Math.Max(newIndex, GetIndexOfInstanceByName(targetElement, item));
                                }
                            }
                        }
                    }
                }

                if (newIndex != -1)
                {
                    targetElement.Instances.Insert(newIndex + 1, newInstance);

                    nextIndexByParent[parent] = newIndex + 1;
                }
                else
                {
                    targetElement.Instances.Add(newInstance);

                    nextIndexByParent[parent] = targetElement.Instances.Count-1;
                }
            }
        }

        #endregion

        foreach (var sourceInstance in instancesToCopy)
        {
            var sourceElement = sourceInstance.ParentContainer;

            var isPastingInNewElement = sourceElement != targetElement;
            var isSelectedInstancePartOfCopied = selectedInstance != null && instancesToCopy.Any(item =>
                item.Name == selectedInstance.Name &&
                item.ParentContainer == selectedInstance.ParentContainer);

            var shouldAttachToSelectedInstance = _hasChangedSelectionSinceCopy && (isPastingInNewElement || !isSelectedInstancePartOfCopied);


            var newInstance = newInstances.First(item => item.Name == oldNewNameDictionary[sourceInstance.Name]);

            if (targetElement != null)
            {
                // First pass: apply base-element default state captures, FILTERED by
                // refOwnedLhses. The source's own state has not been applied yet, so
                // these go in as fallbacks; the per-state loop below will overwrite
                // any of these that the source explicitly authored on its own state.
                if (baseElementDefaultStates != null && baseElementDefaultStates.Count > 0)
                {
                    foreach (var baseStateSave in baseElementDefaultStates)
                    {
                        StateSave baseTargetState = targetElement != sourceElement
                            ? targetElement.DefaultState
                            : (selectedState.SelectedElement.AllStates.FirstOrDefault(item => item.Name == baseStateSave.Name)
                                ?? selectedState.SelectedElement.DefaultState);

                        var baseVariables = baseStateSave.Variables.Where(item =>
                            item.SourceObject == sourceInstance.Name &&
                            item.GetRootName() != "Parent" &&
                            (itemsOwnedByReachableStates == null || !itemsOwnedByReachableStates.Contains(item.Name))).ToArray();
                        for (int i = baseVariables.Length - 1; i > -1; i--)
                        {
                            VariableSave baseSourceVar = baseVariables[i];
                            VariableSave copiedBase = baseSourceVar.Clone();
                            copiedBase.Name = newInstance.Name + "." + copiedBase.GetRootName();
                            copiedBase.ExposedAsName = null;
                            baseTargetState.Variables.RemoveAll(item => item.Name == copiedBase.Name);
                            baseTargetState.Variables.Add(copiedBase);
                        }
                        for (int i = baseStateSave.VariableLists.Count - 1; i > -1; i--)
                        {
                            VariableListSave baseSourceList = baseStateSave.VariableLists[i];
                            if (baseSourceList.SourceObject != sourceInstance.Name) continue;
                            // Drop base-snapshotted lists (e.g. the inherited VariableReferences row)
                            // when a reachable categorized state would otherwise be shadowed by it.
                            if (itemsOwnedByReachableStates != null &&
                                itemsOwnedByReachableStates.Contains(baseSourceList.Name)) continue;
                            VariableListSave copiedBaseList = baseSourceList.Clone();
                            copiedBaseList.Name = newInstance.Name + "." + copiedBaseList.GetRootName();
                            baseTargetState.VariableLists.RemoveAll(item => item.Name == copiedBaseList.Name);
                            baseTargetState.VariableLists.Add(copiedBaseList);
                        }
                    }
                }

                foreach (var stateSave in copiedStates)
                {

                    StateSave targetState;
                    // We now have to copy over the states
                    if (targetElement != sourceElement)
                    {
                        if (sourceElement != null && sourceElement.States.Count != 1)
                        {
                            _dialogService.ShowMessage("Only the default state variables will be copied since the source and target elements differ.");
                        }

                        targetState = targetElement.DefaultState;
                    }
                    else
                    {
                        var selectedElement = selectedState.SelectedElement;

                        targetState = selectedElement.AllStates.FirstOrDefault(item => item.Name == stateSave.Name) ??
                            selectedState.SelectedElement.DefaultState;
                        //_selectedState.SelectedStateSave ?? _selectedState.SelectedElement.DefaultState;

                    }

                    var variablesOnSourceInstance = stateSave.Variables.Where(item => item.SourceObject == sourceInstance.Name && 
                        // we handle parents down below, so skip it here:
                        item.GetRootName() != "Parent").ToArray();
                    // why reverse loop?
                    for (int i = variablesOnSourceInstance.Length - 1; i > -1; i--)
                    {
                        // We may have copied over a group of instances.  If so
                        // the copied state may have variables for multiple instances.
                        // We only want to apply the variables that work for the selected
                        // object.
                        VariableSave sourceVariable = variablesOnSourceInstance[i];

                        VariableSave copiedVariable = sourceVariable.Clone();
                        copiedVariable.Name = newInstance.Name + "." + copiedVariable.GetRootName();

                        // We don't want to copy exposed variables.
                        // If we did, the user would have 2 variables exposed with the same.
                        copiedVariable.ExposedAsName = null;

                        // this prevents double-adds like for Parent:
                        targetState.Variables.RemoveAll(item => item.Name == copiedVariable.Name);
                        targetState.Variables.Add(copiedVariable);
                    }
                    // Copy over the VariableLists too
                    for (int i = stateSave.VariableLists.Count - 1; i > -1; i--)
                    {

                        VariableListSave sourceVariableList = stateSave.VariableLists[i];
                        if (sourceVariableList.SourceObject == sourceInstance.Name)
                        {
                            VariableListSave copiedList = sourceVariableList.Clone();
                            copiedList.Name = newInstance.Name + "." + copiedList.GetRootName();

                            targetState.VariableLists.RemoveAll(item => item.Name == copiedList.Name);
                            targetState.VariableLists.Add(copiedList);
                        }
                    }

                    var desiredParent = newInstanceToParentDictionary[newInstance];

                    string? newParentName = null;

                    if(desiredParent is InstanceSave desiredParentInstance)
                    {
                        newParentName = desiredParentInstance.Name;

                        // If the source instance's original parent was a dotted path rooted at
                        // this same instance (e.g. "ComboBoxInstance.InnerPanelInstance"), preserve
                        // that suffix. Otherwise fall back to GetDefaultChildName so that pasting
                        // into a new parent still picks up its default sub-container.
                        if (copiedParentMap.TryGetValue(sourceInstance.Name, out var originalParentPath) &&
                            originalParentPath.StartsWith(desiredParentInstance.Name + "."))
                        {
                            newParentName += originalParentPath.Substring(desiredParentInstance.Name.Length);
                        }
                        else
                        {
                            var childName = ObjectFinder.Self.GetDefaultChildName(desiredParentInstance, selectedState.SelectedStateSave);

                            if (!string.IsNullOrEmpty(childName))
                            {
                                newParentName += "." + childName;
                            }
                        }
                    }

                    var desiredParentName = newParentName;

                    // do parent attachment here:
                    var desiredParentNameWithoutSubItem = desiredParentName;
                    if (desiredParentNameWithoutSubItem?.Contains(".") == true)
                    {
                        desiredParentNameWithoutSubItem = desiredParentNameWithoutSubItem.Substring(0, desiredParentNameWithoutSubItem.IndexOf("."));
                    }

                    bool isParentOfPastedInstanceAlsoAPastedInstance = desiredParentNameWithoutSubItem != null && oldNewNameDictionary.ContainsKey(desiredParentNameWithoutSubItem);

                    if(isParentOfPastedInstanceAlsoAPastedInstance && desiredParent is InstanceSave desiredParentInstance2)
                    {
                        isParentOfPastedInstanceAlsoAPastedInstance = instancesToCopy.Any(item => AreMatch(item, desiredParentInstance2));
                    }

                    bool AreMatch(InstanceSave instance1, InstanceSave instance2)
                    {
                        if(instance1 == instance2)
                        {
                            return true;
                        }
                        return
                            instance1.ParentContainer?.Name == instance2.ParentContainer?.Name &&
                            instance1.Name == instance2.Name;
                    }

                    if (isParentOfPastedInstanceAlsoAPastedInstance)
                    {
                        // this is a parent and it may be attached to a copy, so update the value
                        var remappedName = oldNewNameDictionary[desiredParentNameWithoutSubItem];
                        if (desiredParentName.Contains("."))
                        {
                            remappedName += desiredParentName.Substring(desiredParentName.IndexOf("."));
                        }
                        // Don't remap if it would create a self-reference (e.g. pasting into
                        // a previously-pasted instance that shares the same source name)
                        if (remappedName != newInstance.Name)
                        {
                            newParentName = remappedName;
                        }
                    }

                    if(!string.IsNullOrEmpty(newParentName))
                    {
                        targetState.SetValue($"{newInstance.Name}.Parent", newParentName, "string");
                    }
                }

                newInstance.ParentContainer = targetElement;
                // We need to call InstanceAdd before we select the new object - the Undo manager expects it
                // This includes before other managers refresh
                _copyPastePluginNotifier.InstanceAdd(targetElement, newInstance);
            }
        }

        _wireframeObjectManager.RefreshAll(true);
        _guiCommands.RefreshElementTreeView(targetElement);
        _fileCommands.TryAutoSaveElement(targetElement);

        //var hasSelectionChangedStore = _hasChangedSelectionSinceCopy;

        isSelectionCausedByPaste = true;
        selectedState.SelectedInstances = GetInstancesToSelectAfterPaste(newInstances, oldNewNameDictionary, instancesToSelectAfterPaste);
        _hasChangedSelectionSinceCopy = false;
        isSelectionCausedByPaste = false;

        return newInstances;
    }

    // Narrows the post-paste selection to mirror what was selected at copy time. Pasting a parent
    // also recursively duplicates its children, but only the originally-selected instances (passed in
    // sourceInstancesToSelect) should remain selected. Falls back to selecting every new instance when
    // no source selection is supplied (drag-drop / top-level paste) or when none of the selected source
    // names map to a new instance (a defensive guard against odd repeat-paste states).
    private static List<InstanceSave> GetInstancesToSelectAfterPaste(
        List<InstanceSave> newInstances,
        Dictionary<string, string> oldNewNameDictionary,
        List<InstanceSave>? sourceInstancesToSelect)
    {
        if (sourceInstancesToSelect == null || sourceInstancesToSelect.Count == 0)
        {
            return newInstances;
        }

        HashSet<string> newNamesToSelect = new();
        foreach (InstanceSave sourceInstance in sourceInstancesToSelect)
        {
            if (oldNewNameDictionary.TryGetValue(sourceInstance.Name, out string? newName))
            {
                newNamesToSelect.Add(newName);
            }
        }

        List<InstanceSave> toSelect = newInstances
            .Where(item => newNamesToSelect.Contains(item.Name))
            .ToList();

        return toSelect.Count > 0 ? toSelect : newInstances;
    }

    int GetIndexOfInstanceByName(ElementSave element, InstanceSave instance)
    {
        for(int i = 0; i < element.Instances.Count; i++)
        {
            if (element.Instances[i].Name == instance.Name)
            {
                return i;
            }
        }
        return -1;
    }

    object GetParentElementOrInstanceFor(InstanceSave instance,
        Dictionary<string, string> copiedParentMap,
        List<InstanceSave> instancesToCopy)
    {
        var element = instance.ParentContainer;

        if(element == null)
        {
            throw new InvalidOperationException($"The instance {instance} must have a valid parent (its ParentContainer must be non-null)");
        }

        // Check the copied parent map first (source of truth for copied/cut instances).
        // For instances not in the copied data (e.g. selected instances, target element
        // instances used for index calculation), fall back to the element state.
        if(!copiedParentMap.TryGetValue(instance.Name, out var parentName))
        {
            parentName = element.DefaultState.GetValueRecursive($"{instance.Name}.Parent") as string;
        }

        if(string.IsNullOrEmpty(parentName))
        {
            return element;
        }

        // When the parent is a dotted path (e.g. "ComboBoxInstance.InnerPanelInstance"),
        // the root segment is the top-level instance name. The suffix is handled separately
        // when building the parent variable value during paste.
        var parentInstanceName = parentName.Contains(".")
            ? parentName.Substring(0, parentName.IndexOf('.'))
            : parentName;

        // Try element first (works for copy), then copied instances (needed for cut
        // where the parent was also removed from the element)
        return (object?)element.GetInstance(parentInstanceName)
            ?? instancesToCopy.FirstOrDefault(item => item.Name == parentInstanceName)
            ?? (object)element;
    }

    private void PasteCopiedElement()
    {
        ElementSave toAdd;

        if (CopiedData.CopiedElement is ScreenSave)
        {
            toAdd = ((ScreenSave)CopiedData.CopiedElement).Clone();
            toAdd.Initialize(null);
            _standardElementsManagerGumTool.FixCustomTypeConverters(toAdd);
        }
        else
        {
            toAdd = ((ComponentSave)CopiedData.CopiedElement).Clone();
            ((ComponentSave)toAdd).InitializeDefaultAndComponentVariables();
            _standardElementsManagerGumTool.FixCustomTypeConverters((ComponentSave)toAdd);

        }

        var strippedName = toAdd.StrippedName;

        var selectedNode = _selectedState.SelectedTreeNode;

        if (toAdd is ScreenSave && selectedNode.IsScreensFolderTreeNode())
        {
            var path = selectedNode!.FullPath.Substring("Screens\\".Length);

            toAdd.Name = (path + "/" + strippedName).Replace("\\", "/");
        }
        else if (toAdd is ComponentSave && selectedNode.IsComponentsFolderTreeNode())
        {
            var path = selectedNode.FullPath.Substring("Components\\".Length);

            toAdd.Name = (path + "/" + strippedName).Replace("\\", "/");
        }

        List<string> allElementNames = new List<string>();
        allElementNames.AddRange(_copyPasteProjectProvider.GumProjectSave.Screens.Select(item => item.Name.ToLowerInvariant()));
        allElementNames.AddRange(_copyPasteProjectProvider.GumProjectSave.Components.Select(item => item.Name.ToLowerInvariant()));
        allElementNames.AddRange(_copyPasteProjectProvider.GumProjectSave.StandardElements.Select(item => item.Name.ToLowerInvariant()));

        while (allElementNames.Contains(toAdd.Name.ToLowerInvariant()))
        {
            toAdd.Name = StringFunctions.IncrementNumberAtEnd(toAdd.Name);
        }

        if (toAdd is ScreenSave)
        {
            _projectCommands.AddScreen(toAdd as ScreenSave);
        }
        else
        {
            _projectCommands.AddComponent(toAdd as ComponentSave);
        }

        _selectedState.SelectedElement = toAdd;

        _copyPastePluginNotifier.ElementDuplicate(CopiedData.CopiedElement, toAdd);

        _fileCommands.TryAutoSaveElement(toAdd);
        _fileCommands.TryAutoSaveProject();
    }

    #endregion

    /// <summary>
    /// Variable root names that describe an instance's placement relative to its parent. When an
    /// instance is promoted into a component, these stay on the replacement instance rather than
    /// moving to the component root — a component's root has no meaningful parent-relative position
    /// (see <see cref="ICopyPasteProjectCommands.PrepareNewComponentSave"/>, which likewise nulls
    /// X/Y on new component roots). There is no data-driven "is positional" flag on authored
    /// variables (the Category is only populated on the standard-element definitions), so this set
    /// is maintained explicitly. It mirrors the "Position" category in
    /// <see cref="Managers.StandardElementsManager"/> plus the Parent attachment.
    /// </summary>
    private static readonly HashSet<string> PositionalRootNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "X", "Y", "XUnits", "YUnits", "XOrigin", "YOrigin", "Parent"
    };

    /// <inheritdoc/>
    public ComponentSave CreateComponentFromInstance(InstanceSave instance, string componentName, bool replaceWithInstance)
    {
        ElementSave sourceElement = instance.ParentContainer
            ?? throw new InvalidOperationException(
                $"The instance {instance.Name} must have a ParentContainer to be promoted into a component.");
        StateSave sourceDefault = sourceElement.DefaultState;

        // The instance's type becomes the component's base type; the rest of the new-component
        // setup (default-state seeding, type converters, nulling the root position) is shared
        // with the regular "add component" flow.
        ComponentSave component = new ComponentSave();
        _projectCommands.PrepareNewComponentSave(component, componentName, instance.BaseType);
        StateSave componentDefault = component.DefaultState;

        // GetAllInstancesAndChildrenOf includes the instance itself - but the instance BECOMES the
        // component root rather than one of its children, so exclude it from the copied set.
        List<InstanceSave> descendants = GetAllInstancesAndChildrenOf(new List<InstanceSave> { instance }, sourceElement)
            .Where(item => item.Name != instance.Name)
            .ToList();
        HashSet<string> descendantNames = new HashSet<string>(descendants.Select(item => item.Name), StringComparer.Ordinal);

        foreach (InstanceSave descendant in descendants)
        {
            component.Instances.Add(new InstanceSave
            {
                Name = descendant.Name,
                BaseType = descendant.BaseType,
                Locked = descendant.Locked,
                ParentContainer = component,
            });
        }

        // Copy each descendant's variables into the component, re-rooting Parent attachments:
        // a direct child of the promoted instance now attaches to the component root.
        foreach (VariableSave variable in sourceDefault.Variables)
        {
            if (string.IsNullOrEmpty(variable.SourceObject) || !descendantNames.Contains(variable.SourceObject))
            {
                continue;
            }

            VariableSave clone = variable.Clone();
            clone.ExposedAsName = null;

            if (clone.GetRootName() == "Parent" && clone.Value is string parentValue)
            {
                if (parentValue == instance.Name)
                {
                    // Attaches to the new component root - no Parent variable needed.
                    continue;
                }
                if (parentValue.StartsWith(instance.Name + "."))
                {
                    // Was attached to a default child container of the promoted instance; keep only
                    // the child-container suffix now that that instance is the root.
                    clone.Value = parentValue.Substring(instance.Name.Length + 1);
                }
            }

            componentDefault.Variables.Add(clone);
        }

        foreach (VariableListSave variableList in sourceDefault.VariableLists)
        {
            // Skip VariableReferences: a reference materializes its resolved value as a hard scalar
            // in the same state (copied above as a normal variable), so the value is preserved while
            // the reference row itself — which may point at the now-promoted instance — is dropped.
            if (variableList.GetRootName() == "VariableReferences")
            {
                continue;
            }
            if (!string.IsNullOrEmpty(variableList.SourceObject) && descendantNames.Contains(variableList.SourceObject))
            {
                componentDefault.VariableLists.Add(variableList.Clone());
            }
        }

        // Copy the promoted instance's own intrinsic variables onto the component root (unqualified).
        // Parent-relative position is deliberately skipped - it stays with the instance (see
        // PositionalRootNames) and is re-applied to the replacement instance below if needed.
        foreach (VariableSave variable in sourceDefault.Variables)
        {
            if (variable.SourceObject != instance.Name)
            {
                continue;
            }
            string rootName = variable.GetRootName();
            if (PositionalRootNames.Contains(rootName))
            {
                continue;
            }
            VariableSave clone = variable.Clone();
            clone.Name = rootName;
            clone.ExposedAsName = null;
            componentDefault.Variables.RemoveAll(item => item.Name == rootName);
            componentDefault.Variables.Add(clone);
        }

        foreach (VariableListSave variableList in sourceDefault.VariableLists)
        {
            if (variableList.SourceObject != instance.Name)
            {
                continue;
            }
            string rootName = variableList.GetRootName();
            // See note above: VariableReferences are excluded; their materialized scalar carries the value.
            if (rootName == "VariableReferences")
            {
                continue;
            }
            VariableListSave clone = variableList.Clone();
            clone.Name = rootName;
            componentDefault.VariableLists.RemoveAll(item => item.Name == rootName);
            componentDefault.VariableLists.Add(clone);
        }

        _standardElementsManagerGumTool.FixCustomTypeConverters(component);

        if (replaceWithInstance)
        {
            // The undo for the replace is recorded against the SOURCE element. Its baseline snapshot
            // was captured when the user selected the instance. AddComponent selects the new
            // component, and if that selection change happened outside the undo lock, the undo
            // baseline would be overwritten to the component — leaving RecordUndo to diff the source
            // element against a component snapshot (a corrupt, unusable undo). Holding the lock
            // across AddComponent suppresses that RecordState so the source-element baseline survives.
            using var undoLock = _undoManager.RequestLock();
            _projectCommands.AddComponent(component);
            ReplaceSubtreeWithComponentInstance(instance, sourceElement, sourceDefault, descendants, component);
        }
        else
        {
            _projectCommands.AddComponent(component);
        }

        return component;
    }

    /// <summary>
    /// Phase 2 of <see cref="CreateComponentFromInstance"/>: removes the promoted instance and all
    /// its descendants from the source element and drops in a single instance of the new component,
    /// preserving the original instance's name and parent-relative position. Recorded as one undo.
    /// </summary>
    private void ReplaceSubtreeWithComponentInstance(InstanceSave instance, ElementSave sourceElement,
        StateSave sourceDefault, List<InstanceSave> descendants, ComponentSave component)
    {
        // The undo lock is taken by the caller (CreateComponentFromInstance) so that AddComponent's
        // selection change is also covered — see the comment there.

        // The instance's position was NOT moved to the component root, so capture it before the
        // delete (which strips the instance's variables) and re-apply it to the replacement.
        List<VariableSave> positionalVariables = sourceDefault.Variables
            .Where(item => item.SourceObject == instance.Name && PositionalRootNames.Contains(item.GetRootName()))
            .Select(item => item.Clone())
            .ToList();

        foreach (InstanceSave descendant in descendants)
        {
            _deleteLogic.RemoveInstance(descendant, sourceElement);
        }
        _deleteLogic.RemoveInstance(instance, sourceElement);

        InstanceSave replacement = new InstanceSave
        {
            Name = instance.Name,
            BaseType = component.Name,
            ParentContainer = sourceElement,
        };
        sourceElement.Instances.Add(replacement);

        foreach (VariableSave positionalVariable in positionalVariables)
        {
            sourceDefault.Variables.RemoveAll(item => item.Name == positionalVariable.Name);
            sourceDefault.Variables.Add(positionalVariable);
        }

        _copyPastePluginNotifier.InstanceAdd(sourceElement, replacement);
        _fileCommands.TryAutoSaveElement(sourceElement);

        // Rebuild the wireframe + tree so the replacement renders with its component's children
        // right away (otherwise it stays blank until the element is reselected), and leave the new
        // instance selected — AddComponent had selected the new component and the delete cleared the
        // instance selection.
        _wireframeObjectManager.RefreshAll(true);
        _guiCommands.RefreshElementTreeView(sourceElement);
        _selectedState.SelectedInstance = replacement;
    }

    private List<InstanceSave> GetAllInstancesAndChildrenOf(List<InstanceSave> explicitlySelectedInstances, ElementSave container)
    {
        List<InstanceSave> listToFill = new List<InstanceSave>();

        foreach (var instance in explicitlySelectedInstances)
        {
            if (listToFill.Any(item => item.Name == instance.Name) == false)
            {
                listToFill.Add(instance);

                FillWithChildrenOf(instance, listToFill, container);
            }
        }

        return listToFill;
    }

    private void FillWithChildrenOf(InstanceSave instance, List<InstanceSave> listToFill, ElementSave container)
    {
        var defaultState = container.DefaultState;

        foreach (var variable in defaultState.Variables)
        {
            if (variable.GetRootName() == "Parent")
            {
                var value = variable.Value as string;

                if (!string.IsNullOrEmpty(value) && (value == instance.Name || value.StartsWith(instance.Name + ".")))
                {
                    var foundObject = container.GetInstance(variable.SourceObject);

                    if (foundObject != null && listToFill.Any(item => item.Name == foundObject.Name) == false)
                    {
                        listToFill.Add(foundObject);
                        FillWithChildrenOf(foundObject, listToFill, container);
                    }
                }
            }
        }
    }
}
