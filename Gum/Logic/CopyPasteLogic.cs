using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Wireframe;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Gum.Commands;
using Gum.Services;
using Gum.Services.Dialogs;
using ToolsUtilities;
using Gum.Undo;
using System;

namespace Gum.Logic;

#region Copy Type

public enum CopyType
{
    InstanceOrElement = 1,
    State = 2,
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
    public ElementSave CopiedElement = null;
}

#endregion

public class CopyPasteLogic
{
    #region Fields/Properties

    private readonly ISelectedState _selectedState;
    private readonly IElementCommands _elementCommands;
    private readonly IDialogService _dialogService;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly ProjectCommands _projectCommands;
    private readonly IUndoManager _undoManager;

    public CopiedData CopiedData { get; private set; } = new CopiedData();

    CopyType mCopyType;

    // If the user copies/pastes, the pasted object
    // becomes the newly-selected instance. A user may
    // want to copy/paste multiple times to create multiple
    // (sibling) instances, but by default this would cause each
    // new instance to be the child of the previously-pasted instance.
    // This is not desirable, so we'll special-case pasting. If the user
    // is pasting on the last pasted object, paste on the parent of the last 
    // pasted object
    List<InstanceSave> LastPastedInstances = new List<InstanceSave>();

    #endregion

    public CopyPasteLogic(ISelectedState selectedState,
        IElementCommands elementCommands,
        IDialogService dialogService,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        ProjectCommands projectCommands,
        IUndoManager undoManager
        )
    {
        _selectedState = selectedState;
        _elementCommands = elementCommands;
        _dialogService = dialogService;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _projectCommands = projectCommands;
        _undoManager = undoManager;

    }

    #region Copy
    public void OnCopy(CopyType copyType)
    {
        StoreCopiedObject(copyType);
    }


    private void StoreCopiedObject(CopyType copyType)
    {
        mCopyType = copyType;
        CopiedData.CopiedElement = null;
        CopiedData.CopiedInstancesRecursive.Clear();
        CopiedData.CopiedStates.Clear();

        if (copyType == CopyType.InstanceOrElement)
        {
            if (_selectedState.SelectedInstances.Count() != 0)
            {
                StoreCopiedInstances();
            }
            else if (_selectedState.SelectedElement != null)
            {
                StoreCopiedElementSave();
            }
        }
        else if (copyType == CopyType.State)
        {
            StoreCopiedState();
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

    private void StoreCopiedInstances()
    {
        if (_selectedState.SelectedInstances.Any())
        {
            var element = _selectedState.SelectedElement;

            var state = _selectedState.SelectedStateSave;

            // a state may not be selected if the user selected a category.
            if (state == null)
            {
                state = element?.DefaultState;
            }

            CopiedData.CopiedStates.Clear();

            var baseElementsDerivedFirst = element != null ? ObjectFinder.Self.GetBaseElements(element) : new List<ElementSave>();
            // reverse loop:
            for (int i = baseElementsDerivedFirst.Count - 1; i > -1; i--)
            {
                CopiedData.CopiedStates.Add(baseElementsDerivedFirst[i].DefaultState.Clone());
            }

            if (state != null)
            {
                CopiedData.CopiedStates.Add(state.Clone());
            }

            if (_selectedState.SelectedStateCategorySave != null && _selectedState.SelectedStateSave != null && element != null)
            {
                // it's categorized, so add the default:
                CopiedData.CopiedStates.Add(element.DefaultState.Clone());
            }

            List<InstanceSave> selected = new List<InstanceSave>();
            // When copying we want to grab all instances in the order that they are in their container.
            // That way when they're pasted they are pasted in the right order
            selected.AddRange(_selectedState.SelectedInstances);

            CopiedData.CopiedInstancesSelected.Clear();
            CopiedData.CopiedInstancesSelected.AddRange(_selectedState.SelectedInstances);

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
        StoreCopiedObject(copyType);

        ElementSave sourceElement = _selectedState.SelectedElement;

        if (CopiedData.CopiedInstancesRecursive.Any())
        {
            foreach (var clone in CopiedData.CopiedInstancesRecursive)
            {
                // copied instances is a clone, so need to find by name:
                var originalForCopy = sourceElement.Instances.FirstOrDefault(item => item.Name == clone.Name);
                if (sourceElement.Instances.Contains(originalForCopy))
                {
                    _elementCommands.RemoveInstance(originalForCopy, sourceElement);
                }
            }

            _fileCommands.TryAutoSaveElement(sourceElement);
            WireframeObjectManager.Self.RefreshAll(true);
            _guiCommands.RefreshVariables();
            _guiCommands.RefreshElementTreeView();
        }

        // todo: need to handle cut Element saves, but I don't want to do it yet due to the danger of losing valid data...


    }

    #region Paste

    public void OnPaste(CopyType copyType, TopOrRecursive topOrRecursive = TopOrRecursive.Recursive)
    {
        ////////////////////Early Out
        if (mCopyType != copyType)
        {
            return;
        }

        using var undoLock = _undoManager.RequestLock();

        // To make sure we didn't copy one type and paste another
        if (mCopyType == CopyType.InstanceOrElement)
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
        else if (mCopyType == CopyType.State && CopiedData.CopiedStates?.Count > 0)
        {
            PastedCopiedState();
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
            PasteInstanceSaves(CopiedData.CopiedInstancesRecursive, CopiedData.CopiedStates, selectedElement, _selectedState.SelectedInstance);
        }
        else
        {
            PasteInstanceSaves(CopiedData.CopiedInstancesSelected, CopiedData.CopiedStates, selectedElement, _selectedState.SelectedInstance);
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

        var validationResponse = ValidateStatePaste(targetCategory, newStateSave);

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

                toReturn.Message = $"The state {newState.Name} can not be pasted in {targetCategory.Name} because " +
                    $"the state has the following variable(s) which is/are not already set in the category:\n";

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

    public void PasteInstanceSaves(List<InstanceSave> instancesToCopy, List<StateSave> copiedStates, ElementSave targetElement, InstanceSave selectedInstance)
    {
        if (targetElement is StandardElementSave)
        {
            _dialogService.ShowMessage($"Cannot create an instance in {targetElement} because it is a standard element");
            return;
        }

        if (LastPastedInstances.Contains(selectedInstance))
        {
            selectedInstance = selectedInstance?.GetParentInstance();
        }
        Dictionary<string, string> oldNewNameDictionary = new Dictionary<string, string>();

        List<InstanceSave> newInstances = new List<InstanceSave>();

        #region Create the new instance and add it to the target element

        List<string> sourceElementInstanceNames = null;

        foreach (var sourceInstance in instancesToCopy)
        {
            ElementSave sourceElement = sourceInstance.ParentContainer;

            if (sourceElementInstanceNames == null)
            {
                sourceElementInstanceNames = sourceElement?.Instances.Select(item => item.Name).ToList();
            }

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


                if (targetElement == sourceElement)
                {
                    int newIndex = -1;
                    if (sourceElementInstanceNames?.Contains(sourceInstance.Name) == true)
                    {
                        newIndex = sourceElementInstanceNames.IndexOf(sourceInstance.Name);
                    }

                    // The user is pasting again, so let's use the index of that:
                    // This pastes the new instance after the last pasted instance.
                    if (LastPastedInstances?.Count > 0 && sourceElement.Instances.ContainsAny(LastPastedInstances) &&
                        // But if we are coyping multiple instances, so let's skip this if there's more than one object
                        instancesToCopy.Count == 1)
                    {
                        newIndex = LastPastedInstances.Select(item => sourceElement.Instances.IndexOf(item)).Max();
                    }
                    if (newIndex != -1)
                    {
                        targetElement.Instances.Insert(newIndex + 1, newInstance);
                        sourceElementInstanceNames?.Insert(newIndex + 1, newInstance.Name);

                    }
                    else
                    {
                        targetElement.Instances.Add(newInstance);
                        sourceElementInstanceNames.Add(newInstance.Name);
                    }
                }
                else
                {
                    targetElement.Instances.Add(newInstance);
                }

            }
        }

        #endregion

        foreach (var sourceInstance in instancesToCopy)
        {
            ElementSave sourceElement = sourceInstance.ParentContainer;

            var isPastingInNewElement = sourceElement != targetElement;
            var isSelectedInstancePartOfCopied = selectedInstance != null && instancesToCopy.Any(item =>
                item.Name == selectedInstance.Name &&
                item.ParentContainer == selectedInstance.ParentContainer);

            var shouldAttachToSelectedInstance = isPastingInNewElement || !isSelectedInstancePartOfCopied;
            var newParentName = selectedInstance?.Name;

            if (selectedInstance != null)
            {
                var childName = ObjectFinder.Self.GetDefaultChildName(selectedInstance, _selectedState.SelectedStateSave);

                if (!string.IsNullOrEmpty(childName))
                {
                    newParentName += "." + childName;
                }
            }

            var newInstance = newInstances.First(item => item.Name == oldNewNameDictionary[sourceInstance.Name]);

            if (targetElement != null)
            {
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
                        var selectedElement = _selectedState.SelectedElement;

                        targetState = selectedElement.AllStates.FirstOrDefault(item => item.Name == stateSave.Name) ??
                            _selectedState.SelectedElement.DefaultState;
                        //_selectedState.SelectedStateSave ?? _selectedState.SelectedElement.DefaultState;

                    }

                    var variablesOnSourceInstance = stateSave.Variables.Where(item => item.SourceObject == sourceInstance.Name).ToArray();
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

                        var valueAsString = copiedVariable.Value as string;

                        if (copiedVariable.GetRootName() == "Parent" &&
                            string.IsNullOrWhiteSpace(valueAsString) == false)
                        {
                            var valueWithoutSubItem = valueAsString;
                            if (valueWithoutSubItem.Contains("."))
                            {
                                valueWithoutSubItem = valueWithoutSubItem.Substring(0, valueWithoutSubItem.IndexOf("."));
                            }

                            if (oldNewNameDictionary.ContainsKey(valueWithoutSubItem))
                            {
                                // this is a parent and it may be attached to a copy, so update the value
                                var newValue = oldNewNameDictionary[valueWithoutSubItem];
                                if (valueAsString.Contains("."))
                                {
                                    newValue += valueAsString.Substring(valueAsString.IndexOf("."));
                                }
                                copiedVariable.Value = newValue;
                                shouldAttachToSelectedInstance = false;

                            }

                        }
                        // Not sure why we are doing this. If the old contains the key, then attach to that every time (see above)
                        //if(copiedVariable.GetRootName() == "Parent" && shouldAttachToSelectedInstance && selectedInstance == null)
                        //{
                        //    // don't assign it because we're not pasting onto a particular instance and
                        //    // the copied instance already has a parent.
                        //    shouldAttachToSelectedInstance = false;
                        //}

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

                    if (shouldAttachToSelectedInstance)
                    {
                        targetState.SetValue($"{newInstance.Name}.Parent", newParentName, "string");
                    }
                }

                // This used to be done here when we paste, but now we're
                // going to remove it when the cut happens - just like text
                // editors.  Undo will handle this if we mess up.
                // bool shouldSaveSource = false;
                //if (mIsCtrlXCut)
                //{
                //    if (sourceElement.Instances.Contains(sourceInstance))
                //    {
                //        ElementCommands.Self.RemoveInstance(sourceInstance, sourceElement);
                //        shouldSaveSource = true;
                //    }
                //}

                newInstance.ParentContainer = targetElement;
                // We need to call InstanceAdd before we select the new object - the Undo manager expects it
                // This includes before other managers refresh
                PluginManager.Self.InstanceAdd(targetElement, newInstance);
            }
        }


        WireframeObjectManager.Self.RefreshAll(true);
        _guiCommands.RefreshElementTreeView(targetElement);
        _fileCommands.TryAutoSaveElement(targetElement);
        _selectedState.SelectedInstances = newInstances;

        LastPastedInstances.Clear();
        LastPastedInstances.AddRange(newInstances);
    }

    private void PasteCopiedElement()
    {
        ElementSave toAdd;

        if (CopiedData.CopiedElement is ScreenSave)
        {
            toAdd = ((ScreenSave)CopiedData.CopiedElement).Clone();
            toAdd.Initialize(null);
            StandardElementsManagerGumTool.Self.FixCustomTypeConverters(toAdd);
        }
        else
        {
            toAdd = ((ComponentSave)CopiedData.CopiedElement).Clone();
            ((ComponentSave)toAdd).InitializeDefaultAndComponentVariables();
            StandardElementsManagerGumTool.Self.FixCustomTypeConverters((ComponentSave)toAdd);

        }

        var strippedName = toAdd.StrippedName;

        var selectedNode = _selectedState.SelectedTreeNode;

        if (toAdd is ScreenSave && selectedNode.IsScreensFolderTreeNode())
        {
            var path = selectedNode.FullPath.Substring("Screens\\".Length);

            toAdd.Name = (path + "/" + strippedName).Replace("\\", "/");
        }
        else if (toAdd is ComponentSave && selectedNode.IsComponentsFolderTreeNode())
        {
            var path = selectedNode.FullPath.Substring("Components\\".Length);

            toAdd.Name = (path + "/" + strippedName).Replace("\\", "/");
        }

        List<string> allElementNames = new List<string>();
        allElementNames.AddRange(ProjectState.Self.GumProjectSave.Screens.Select(item => item.Name.ToLowerInvariant()));
        allElementNames.AddRange(ProjectState.Self.GumProjectSave.Components.Select(item => item.Name.ToLowerInvariant()));
        allElementNames.AddRange(ProjectState.Self.GumProjectSave.StandardElements.Select(item => item.Name.ToLowerInvariant()));

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

        PluginManager.Self.ElementDuplicate(CopiedData.CopiedElement, toAdd);

        _fileCommands.TryAutoSaveElement(toAdd);
        _fileCommands.TryAutoSaveProject();
    }

    #endregion

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
