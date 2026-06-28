using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.StateAnimation.SaveClasses;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using ToolsUtilities;

namespace Gum.Undo;

/// <summary>
/// The element undo/redo track: granular per-state / per-instance diffing, category and behavior
/// list changes, rename propagation, hidden-variable changes, selection restore, and redo via the
/// explicit per-action <see cref="HistoryAction.RedoState"/>. Extracted verbatim from UndoManager in
/// #3403; this is the complex domain and is the orchestrator's fallback strategy.
/// </summary>
public class ElementUndoStrategy : IUndoStrategy
{
    private readonly ISelectedState _selectedState;
    private readonly IUndoRenameLogic _renameLogic;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly IMessenger _messenger;
    private readonly IUndoPluginNotifier _pluginNotifier;
    private readonly IAnimationUndoProvider _animationUndoProvider;
    private readonly Func<bool> _areUndoLocksActive;
    private readonly Action<UndoOperation> _raiseUndosChanged;

    bool isRecordingUndos = true;

    Dictionary<ElementSave, ElementHistory> mUndos = new Dictionary<ElementSave, ElementHistory>();

    UndoSnapshot? recordedSnapshot;

    public UndoSnapshot? RecordedSnapshot => recordedSnapshot;

    public ElementHistory CurrentElementHistory
    {
        get
        {
            ElementHistory history = null;

            if (_selectedState.SelectedElement != null && mUndos.ContainsKey(_selectedState.SelectedElement))
            {
                history = mUndos[_selectedState.SelectedElement];
            }

            return history;
        }
    }

    public bool AppliesToCurrentSelection => true;

    public ElementUndoStrategy(ISelectedState selectedState,
        IUndoRenameLogic renameLogic,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        IMessenger messenger,
        IUndoPluginNotifier pluginNotifier,
        IAnimationUndoProvider animationUndoProvider,
        Func<bool> areUndoLocksActive,
        Action<UndoOperation> raiseUndosChanged)
    {
        _selectedState = selectedState;
        _renameLogic = renameLogic;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _messenger = messenger;
        _pluginNotifier = pluginNotifier;
        _animationUndoProvider = animationUndoProvider;
        _areUndoLocksActive = areUndoLocksActive;
        _raiseUndosChanged = raiseUndosChanged;
    }

    /// <summary>
    /// Records the current state, which serves as the "restore point" when an undo occurs. This state will be compared
    /// with the current state in the RecordUndo call to see if any changes should be made.
    /// </summary>
    public void CaptureBaseline()
    {
        if (_areUndoLocksActive())
        {
            return;
        }
        recordedSnapshot = null;


        if (_selectedState.SelectedElement != null)
        {
            if (mUndos.ContainsKey(_selectedState.SelectedElement) == false)
            {

                var history = new ElementHistory();

                mUndos.Add(_selectedState.SelectedElement, history);
            }

            recordedSnapshot = new UndoSnapshot();
            recordedSnapshot.Element = CloneWithFixedEnumerations(_selectedState.SelectedElement);
        }

        if (recordedSnapshot != null)
        {
            foreach (var item in recordedSnapshot.Element.AllStates)
            {
                item.FixEnumerations();
            }
            recordedSnapshot.StateName = _selectedState.SelectedStateSave?.Name;
            recordedSnapshot.CategoryName = _selectedState.SelectedStateCategorySave?.Name;

            // Capture the element's animations as part of the same baseline. GetCurrentAnimations
            // returns the live tab contents when loaded, else the .ganx, else null when the element
            // has no animations. This is a freshly-produced object, so it is safe to hold as-is.
            recordedSnapshot.Animations = _animationUndoProvider.GetCurrentAnimations(_selectedState.SelectedElement);
        }

        //PrintStatus("RecordState");
    }

    /// <summary>
    /// Records an undo if any values have changed. This should be called whenever some editing activity has finished.
    /// For example, whenever a value changes in a text box or whenever a drag has finished.
    /// </summary>
    public void TryRecord()
    {
        var canUndo = recordedSnapshot != null &&
            _selectedState.SelectedElement != null &&
            isRecordingUndos &&
            // We should allow undos when selected state is null. This can happen when
            // the user deletes an existing state
            //_selectedState.SelectedStateSave != null &&
            !_areUndoLocksActive();

        ///////////////////////////////////////Early Out//////////////////////////////////////
        if(!canUndo)
        {
            return;
        }
        /////////////////////////////////////End Early Out////////////////////////////////////

        StateSave newStateSave = _selectedState.SelectedStateSave;
        var currentCategory = _selectedState.SelectedStateCategorySave;
        ElementSave newElement = _selectedState.SelectedElement;

        StateSave oldState = null;

        if (newStateSave != null)
        {
            if (currentCategory != null)
            {
                var category = recordedSnapshot.Element.Categories.Find(item => item.Name == currentCategory.Name);
                oldState = category?.States.Find(item => item.Name == newStateSave.Name);
            }
            else
            {
                var stateName = newStateSave.Name;
                oldState = recordedSnapshot.Element.States.Find(item => item.Name == stateName);
            }
        }

        // The live animation state, diffed against the baseline captured on selection / after the
        // previous record. baselineAnimations restores on undo; currentAnimations re-applies on redo.
        var baselineAnimations = recordedSnapshot.Animations;
        var currentAnimations = _animationUndoProvider.GetCurrentAnimations(newElement);

        UndoSnapshot undoSnapshot = TryGetUndoSnapshotToAdd(newStateSave, newElement, oldState,
            recordedSnapshot.Element, recordedSnapshot.CategoryName, recordedSnapshot.StateName,
            baselineAnimations, currentAnimations);

        if (undoSnapshot != null)
        {
            if(mUndos.ContainsKey(_selectedState.SelectedElement))
            {
                var history = mUndos[_selectedState.SelectedElement];

                var isAtEndOfStack = history.UndoIndex == history.Actions.Count - 1;
                if (!isAtEndOfStack)
                {
                    // If we're not at the end of the stack, then we need to remove all the items after the current index
                    while (history.Actions.Count > history.UndoIndex + 1)
                    {
                        history.Actions.RemoveAt(history.Actions.Count - 1);
                    }
                }

                var action = new HistoryAction { UndoState = undoSnapshot };

                history.Actions.Add(action);
                history.UndoIndex = history.Actions.Count - 1;

                var redoSnapshot = TryGetUndoSnapshotToAdd(oldState, recordedSnapshot.Element, newStateSave, newElement, recordedSnapshot.CategoryName, recordedSnapshot.StateName,
                    currentAnimations, baselineAnimations);

                if(redoSnapshot != null)
                {
                    action.RedoState = redoSnapshot;
                }


                CaptureBaseline();

                _raiseUndosChanged(UndoOperation.HistoryAppended);
            }
            else
            {
                // This can happen when copy/pasting a new element and selecting it, so let's record the state
                // so future work an be undone
                CaptureBaseline();

            }
        }
    }

    /// <summary>
    /// Checks if anything has changed and if so returns an UndoSnapshot
    /// </summary>
    private UndoSnapshot? TryGetUndoSnapshotToAdd(StateSave newState, ElementSave newElement,
        StateSave oldState, ElementSave oldElement, string categoryName, string stateName,
        ElementAnimationsSave? oldAnimations, ElementAnimationsSave? newAnimations)
    {
        bool doStatesDiffer = FileManager.AreSaveObjectsEqual(oldState, newState) == false;
        bool doStateCategoriesDiffer =
            FileManager.AreSaveObjectsEqual(oldElement.Categories, newElement.Categories) == false;
        bool doInstanceListsDiffer = FileManager.AreSaveObjectsEqual(oldElement.Instances, newElement.Instances) == false;
        bool doTypesDiffer = oldElement.BaseType != newElement.BaseType;
        bool doNamesDiffer = oldElement.Name != newElement.Name;
        bool doBehaviorsDiffer = FileManager.AreSaveObjectsEqual(oldElement.Behaviors, newElement.Behaviors) == false;
        bool doVariablesHiddenFromInstancesDiffer =
            FileManager.AreSaveObjectsEqual(oldElement.VariablesHiddenFromInstances, newElement.VariablesHiddenFromInstances) == false;
        bool doAnimationsDiffer = FileManager.AreSaveObjectsEqual(oldAnimations, newAnimations) == false;

        // Why do we care if the user selected a different state?
        // This seems to cause bugs, and we don't care about undoing selections...
        //bool doesSelectedStateDiffer = recordedSnapshot.CategoryName != currentCategory?.Name ||
        //    recordedSnapshot.StateName != currentStateSave?.Name;

        // todo : need to add behavior differences
        UndoSnapshot? snapshotToAdd = null;

        bool didAnythingChange = doStatesDiffer || doStateCategoriesDiffer || doInstanceListsDiffer || doTypesDiffer || doNamesDiffer ||
            doBehaviorsDiffer || doVariablesHiddenFromInstancesDiffer || doAnimationsDiffer
            //|| doesSelectedStateDiffer
            ;
        if (didAnythingChange)
        {
            var clone = CloneWithFixedEnumerations(oldElement);
            if (!doInstanceListsDiffer)
            {
                clone.Instances = null;
            }
            if (!doStatesDiffer)
            {
                clone.States = null;
            }

            if (!doStateCategoriesDiffer)
            {
                clone.Categories = null;
            }
            if (!doNamesDiffer)
            {
                clone.Name = null;
            }
            if (!doTypesDiffer)
            {
                clone.BaseType = null;
            }
            if(!doBehaviorsDiffer)
            {
                clone.Behaviors = null;
            }
            if (!doVariablesHiddenFromInstancesDiffer)
            {
                clone.VariablesHiddenFromInstances = null;
            }

            snapshotToAdd = new UndoSnapshot
            {
                Element = clone,
                CategoryName = categoryName,
                StateName = stateName,
                // Null when unchanged (mirrors the States/Instances trick); otherwise the animations
                // to restore. oldAnimations being null but differing means the element had no
                // animations before, so restore to an empty save rather than null (null would read as
                // "no animation change to apply").
                Animations = doAnimationsDiffer
                    ? (oldAnimations != null ? FileManager.CloneSaveObject(oldAnimations) : new ElementAnimationsSave())
                    : null
            };
        }

        return snapshotToAdd;
    }

    public static ElementSave CloneWithFixedEnumerations(ElementSave elementSave)
    {
        ElementSave cloned = null;
        if (elementSave is ScreenSave screenSave)
        {
            cloned = FileManager.CloneSaveObject(screenSave);
        }
        else if (elementSave is ComponentSave componentSave)
        {
            cloned = FileManager.CloneSaveObject(componentSave);
        }
        else if (elementSave is StandardElementSave standard)
        {
            cloned = FileManager.CloneSaveObject(standard);
        }
        if (cloned != null)
        {
            if(elementSave.States == null)
            {
                cloned.States = null;
            }
            if(elementSave.Instances == null)
            {
                cloned.Instances = null;
            }
            if(elementSave.Categories == null)
            {
                cloned.Categories = null;
            }
            if(elementSave.Events == null)
            {
                cloned.Events = null;
            }
            if(elementSave.Behaviors == null)
            {
                cloned.Behaviors = null;
            }
            if (elementSave.VariablesHiddenFromInstances == null)
            {
                cloned.VariablesHiddenFromInstances = null;
            }

            foreach (var state in cloned.AllStates)
            {
                state.FixEnumerations();
            }

        }
        return cloned;
    }

    public bool CanUndo()
    {
        var elementHistory = GetValidUndosForElement(_selectedState.SelectedElement);
        return CanUndo(elementHistory);
    }

    private bool CanUndo(ElementHistory? elementHistory)
    {
        if (elementHistory != null && elementHistory.Actions.Count != 0 && elementHistory.UndoIndex > -1)
        {
            return true;
        }

        return false;
    }

    private ElementHistory? GetValidUndosForElement(ElementSave? elementSave)
    {
        ElementHistory? elementHistory = null;

        if (elementSave != null && mUndos.ContainsKey(elementSave))
        {
            elementHistory = mUndos[elementSave];
        }

        return elementHistory;
    }

    public void PerformUndo()
    {
        var elementHistory = GetValidUndosForElement(_selectedState.SelectedElement);

        //////////////////////////////////////Early Out///////////////////////////////////////
        if (!CanUndo(elementHistory))
        {
            return;
        }
        ////////////////////////////////////End Early Out/////////////////////////////////////

        var isLast = elementHistory!.UndoIndex == elementHistory.Actions.Count - 1;

        if(isLast)
        {
            elementHistory.FinalState = CloneWithFixedEnumerations(_selectedState.SelectedElement);
        }

        var undoSnapshot = elementHistory.Actions.ElementAt(elementHistory.UndoIndex);



        ElementSave? toApplyTo = _selectedState.SelectedElement;

        AddedAndRemovedInstances? addedAndRemovedInstances = null;

        if (toApplyTo != null)
        {
            addedAndRemovedInstances = ApplyUndoSnapshotToElement(undoSnapshot.UndoState, toApplyTo, true,
                out bool shouldRefreshWireframe,
                out bool shouldRefreshStateTreeView,
                out bool shouldRefreshBehaviorView);

            //if (undoSnapshot.UndoState.CategoryName != _selectedState.SelectedStateCategorySave?.Name ||
            //    undoSnapshot.UndoState.StateName != _selectedState.SelectedStateSave?.Name)
            //{

            StateSave? stateToSelect = null;
            if(!string.IsNullOrEmpty(undoSnapshot.UndoState.CategoryName))
            {
                var category = toApplyTo.Categories.FirstOrDefault(item => item.Name == undoSnapshot.UndoState.CategoryName);
                if(category != null)
                {
                    stateToSelect = category.States.FirstOrDefault(item => item.Name == undoSnapshot.UndoState.StateName);
                }
            }
            else
            {
                stateToSelect = toApplyTo.States.FirstOrDefault(item => item.Name == undoSnapshot.UndoState.StateName);
            }
            if (stateToSelect != null)
            {
                isRecordingUndos = false;
                _selectedState.SelectedStateSave = stateToSelect;

                isRecordingUndos = true;
            }

            var newIndex = elementHistory.UndoIndex - 1;

            //if (isLast)
            //{
            //    RecordUndo();
            //}

            elementHistory.UndoIndex = newIndex;
            DoAfterUndoLogic(toApplyTo, shouldRefreshWireframe, shouldRefreshStateTreeView,
                shouldRefreshBehaviorView,
                addedAndRemovedInstances);
        }
    }

    private void DoAfterUndoLogic(ElementSave toApplyTo,
        bool shouldRefreshWireframe,
        bool shouldRefreshStateTreeView,
        bool shouldRefreshBehaviorView,
        AddedAndRemovedInstances? addedAndRemovedInstances)
    {
        CaptureBaseline();

        _raiseUndosChanged(UndoOperation.Undo);

        _messenger.Send(new AfterUndoMessage());

        _guiCommands.RefreshElementTreeView(toApplyTo);

        if(addedAndRemovedInstances != null)
        {
            foreach(var addedInstance in addedAndRemovedInstances.Value.Added)
            {
                _pluginNotifier.InstanceAdd(toApplyTo, addedInstance);
            }
            _pluginNotifier.InstancesDelete(toApplyTo, addedAndRemovedInstances.Value.Removed);
        }

        if (shouldRefreshStateTreeView)
        {
            _guiCommands.RefreshStateTreeView();
        }

        if(shouldRefreshBehaviorView)
        {
            _guiCommands.BroadcastRefreshBehaviorView();
        }

        //PrintStatus("PerformUndo");

        // If an instance is removed
        // through an undo and if that
        // instance is the selected instance
        // then we want to refresh that.
        if (toApplyTo != null && _selectedState.SelectedElement == null)
        {
            _selectedState.SelectedElement = toApplyTo;
        }

        _fileCommands.TryAutoSaveProject();
        _fileCommands.TryAutoSaveCurrentElement();
    }

    public bool CanRedo()
    {
        var elementHistory = GetValidUndosForElement(_selectedState.SelectedElement);
        UndoSnapshot? redoSnapshot = GetRedoSnapshot(elementHistory);
        return CanRedo(elementHistory, redoSnapshot);
    }

    private bool CanRedo(ElementHistory? elementHistory, UndoSnapshot? redoSnapshot)
    {
        if (redoSnapshot != null)
        {
            return true;
        }

        return false;
    }

    private UndoSnapshot? GetRedoSnapshot(ElementHistory elementHistory)
    {
        UndoSnapshot? redoSnapshot = null;

        if (elementHistory != null)
        {
            var indexToApply = elementHistory.UndoIndex + 1;


            if (indexToApply < elementHistory.Actions.Count)
            {
                redoSnapshot = elementHistory.Actions[indexToApply].RedoState;
            }
        }

        return redoSnapshot;
    }

    public void PerformRedo()
    {
        var elementHistory = GetValidUndosForElement(_selectedState.SelectedElement);

        UndoSnapshot? redoSnapshot = GetRedoSnapshot(elementHistory);

        //////////////////////////////////////Early Out//////////////////////////////////////////
        if (!CanRedo(elementHistory, redoSnapshot))
        {
            return;
        }
        ////////////////////////////////////End Early Out////////////////////////////////////////

        ElementSave toApplyTo = _selectedState.SelectedElement;

        AddedAndRemovedInstances? addedAndRemoved = null;

        if (toApplyTo != null)
        {
            addedAndRemoved = ApplyUndoSnapshotToElement(redoSnapshot, toApplyTo, true,
                out bool shouldRefreshWireframe,
                out bool shouldRefreshStateTreeView,
                out bool shouldRefreshBehaviorView);

            if (redoSnapshot.CategoryName != _selectedState.SelectedStateCategorySave?.Name ||
                redoSnapshot.StateName != _selectedState.SelectedStateSave?.Name)
            {
                var listOfStates = toApplyTo.States;
                var state = listOfStates?.FirstOrDefault(item => item.Name == redoSnapshot.StateName);

                if (state != null)
                {
                    isRecordingUndos = false;
                    _selectedState.SelectedStateSave = state;

                    isRecordingUndos = true;
                }
            }

            elementHistory.UndoIndex++;

            DoAfterUndoLogic(toApplyTo, shouldRefreshWireframe,
                shouldRefreshStateTreeView,
                shouldRefreshBehaviorView,
                addedAndRemoved);

        }

    }

    public void ApplyUndoSnapshotToElement(UndoSnapshot undoSnapshot, ElementSave toApplyTo, bool propagateNameChanges)
    {
        ApplyUndoSnapshotToElement(undoSnapshot, toApplyTo, propagateNameChanges, out bool _, out bool _, out bool _);
    }

    private AddedAndRemovedInstances? ApplyUndoSnapshotToElement(UndoSnapshot undoSnapshot, ElementSave toApplyTo,
        bool propagateNameChanges, out bool shouldRefreshWireframe,
        out bool shouldRefreshStateTreeView,
        out bool shouldRefreshBehaviorView)
    {

        var elementInUndoSnapshot = undoSnapshot.Element;

        // Undos can persist after they have been applied. When they persist, they are used
        // to figure out the undo history, and when this happens instances within the undo snapshot
        // may be modified. We want to make sure that we do not shrae any instances between the undo
        // snapshot and the actual instance, so we should clone it if it is a full applicatoin (propagateNameChanges == true);
        if(propagateNameChanges == true)
        {
            elementInUndoSnapshot = CloneWithFixedEnumerations(elementInUndoSnapshot);
        }

        shouldRefreshWireframe = false;
        shouldRefreshStateTreeView = false;
        shouldRefreshBehaviorView = false;

        Dictionary<string, string>? previousExposedNames = null;
        List<(string Name, string Type)>? previousCustomVariables = null;

        if (propagateNameChanges && elementInUndoSnapshot.States != null)
        {
            previousExposedNames = new Dictionary<string, string>();
            previousCustomVariables = new List<(string Name, string Type)>();
            foreach (var currentState in toApplyTo.States)
            {
                foreach (var variable in currentState.Variables)
                {
                    if (!string.IsNullOrEmpty(variable.ExposedAsName))
                        previousExposedNames[variable.Name] = variable.ExposedAsName;
                    if (variable.IsCustomVariable)
                        previousCustomVariables.Add((variable.Name, variable.Type));
                }
            }
        }

        if (elementInUndoSnapshot.States != null)
        {
            foreach (var state in elementInUndoSnapshot.States)
            {
                var matchingState = toApplyTo.States.Find(item => item.Name == state.Name);
                if (matchingState != null)
                {
                    ApplyStateVariables(state, matchingState, toApplyTo);
                }
            }

        }

        if (previousExposedNames != null)
        {
            PropagateVariableRenames(toApplyTo, previousExposedNames, previousCustomVariables!);
        }

        if (elementInUndoSnapshot.Categories != null)
        {
            AddAndRemoveCategories(elementInUndoSnapshot.Categories, toApplyTo.Categories, toApplyTo);
            shouldRefreshStateTreeView = true;
            // todo - need to handle renames
            if (propagateNameChanges)
            {

            }
        }

        if(elementInUndoSnapshot.Behaviors != null)
        {
            AddAndRemoveBehaviors(elementInUndoSnapshot.Behaviors, toApplyTo.Behaviors, toApplyTo);
            shouldRefreshStateTreeView = true;
            shouldRefreshBehaviorView = true;
        }

        if (elementInUndoSnapshot.VariablesHiddenFromInstances != null)
        {
            toApplyTo.VariablesHiddenFromInstances.Clear();
            toApplyTo.VariablesHiddenFromInstances.AddRange(elementInUndoSnapshot.VariablesHiddenFromInstances);
        }

        AddedAndRemovedInstances? addedAndRemovedInstances = null;

        if (elementInUndoSnapshot.Instances != null)
        {
            addedAndRemovedInstances = AddAndRemoveInstances(elementInUndoSnapshot.Instances, toApplyTo.Instances, toApplyTo);
            shouldRefreshWireframe = true;
        }
        if (!string.IsNullOrEmpty(elementInUndoSnapshot.Name))
        {
            string oldName = toApplyTo.Name;
            toApplyTo.Name = elementInUndoSnapshot.Name;
            if (propagateNameChanges)
            {
                _renameLogic.HandleRename(toApplyTo, (InstanceSave?)null, oldName, NameChangeAction.Rename, askAboutRename: false);
            }
        }
        if(elementInUndoSnapshot.BaseType != null)
        {
            toApplyTo.BaseType = elementInUndoSnapshot.BaseType;
            if(propagateNameChanges)
            {
                // todo?
            }
        }

        if (undoSnapshot.CategoryName != _selectedState.SelectedStateCategorySave?.Name ||
            undoSnapshot.StateName != _selectedState.SelectedStateSave?.Name)
        {
            var listOfStates = toApplyTo.States;
            if (!string.IsNullOrEmpty(undoSnapshot.CategoryName))
            {
                listOfStates = toApplyTo.Categories
                    .FirstOrDefault(item => item.Name == undoSnapshot.CategoryName)?.States;
            }
        }

        // Restore animations only on a real apply. The History tab calls this with
        // propagateNameChanges == false to dry-run snapshots against a throwaway element clone; doing
        // the .ganx write there would corrupt the on-disk animations just to build a description.
        if (propagateNameChanges && undoSnapshot.Animations != null)
        {
            _animationUndoProvider.ApplyAnimations(toApplyTo, undoSnapshot.Animations);
        }

        return addedAndRemovedInstances;
    }

    void ApplyStateVariables(StateSave undoStateSave, StateSave toApplyTo, ElementSave parent)
    {
        UndoStateHelper.SetStateContentsFrom(toApplyTo, undoStateSave);
    }

    private void PropagateVariableRenames(ElementSave parent,
        Dictionary<string, string> previousExposedNames,
        List<(string Name, string Type)> previousCustomVariables)
    {
        var elementsNeedingSave = new HashSet<ElementSave>();

        // Handle exposed variable renames
        foreach (var state in parent.States)
        {
            foreach (var variable in state.Variables)
            {
                if (!string.IsNullOrEmpty(variable.ExposedAsName) &&
                    previousExposedNames.TryGetValue(variable.Name, out var previousExposedAsName) &&
                    previousExposedAsName != variable.ExposedAsName)
                {
                    var varChanges = _renameLogic.GetChangesForRenamedVariable(parent, variable.Name, previousExposedAsName);
                    _renameLogic.ApplyVariableRenameChanges(varChanges, previousExposedAsName, variable.ExposedAsName, elementsNeedingSave);
                }
            }
        }

        // Handle custom variable renames (matched by type heuristic)
        var currentCustomVariables = parent.States
            .SelectMany(s => s.Variables.Where(v => v.IsCustomVariable))
            .Select(v => (v.Name, v.Type))
            .Distinct()
            .ToList();

        var disappearedCustomVars = previousCustomVariables
            .Where(prev => !currentCustomVariables.Any(c => c.Name == prev.Name))
            .ToList();
        var appearedCustomVars = currentCustomVariables
            .Where(curr => !previousCustomVariables.Any(prev => prev.Name == curr.Name))
            .ToList();

        foreach (var disappeared in disappearedCustomVars)
        {
            var matchingAppeared = appearedCustomVars.FirstOrDefault(a => a.Type == disappeared.Type);
            if (matchingAppeared == default) continue;
            appearedCustomVars.Remove(matchingAppeared);
            var varChanges = _renameLogic.GetChangesForRenamedVariable(parent, disappeared.Name, disappeared.Name);
            _renameLogic.ApplyVariableRenameChanges(varChanges, disappeared.Name, matchingAppeared.Name, elementsNeedingSave);
        }

        foreach (var element in elementsNeedingSave)
        {
            _fileCommands.TryAutoSaveElement(element);
        }
    }

    private void AddAndRemoveBehaviors(List<ElementBehaviorReference> undoList, List<ElementBehaviorReference> listToApplyTo, ElementSave parent)
    {
        if (listToApplyTo != null && undoList != null)
        {
            listToApplyTo.Clear();

            foreach (var behavior in undoList)
            {
                listToApplyTo.Add(behavior);
            }
        }
    }

    private void AddAndRemoveCategories(List<StateSaveCategory> undoList, List<StateSaveCategory> listToApplyTo, ElementSave parent)
    {
        if (listToApplyTo != null && undoList != null)
        {
            listToApplyTo.Clear();

            foreach (var category in undoList)
            {
                foreach (var state in category.States)
                {
                    state.ParentContainer = parent;
                }
                listToApplyTo.Add(category);
            }
        }
    }

    private void AddAndRemoveStates(List<StateSave> undoList, List<StateSave> listToApplyTo, ElementSave parent)
    {
        if (listToApplyTo != null && undoList != null)
        {
            listToApplyTo.Clear();

            foreach (var undoItem in undoList)
            {
                undoItem.ParentContainer = parent;
                listToApplyTo.Add(undoItem);
            }
        }
    }

    record struct AddedAndRemovedInstances(InstanceSave[] Added, InstanceSave[] Removed);

    AddedAndRemovedInstances AddAndRemoveInstances(List<InstanceSave>? undoList, List<InstanceSave>? listToApplyTo, ElementSave parent)
    {
        InstanceSave[] added = Array.Empty<InstanceSave>();
        InstanceSave[] removed = Array.Empty<InstanceSave>();

        if (listToApplyTo != null && undoList != null)
        {
            var oldNames = new HashSet<string>(listToApplyTo.Select(item => item.Name));
            var newNames = new HashSet<string>(undoList.Select(item => item.Name));

            var namesToRemove = oldNames.Except(newNames).ToList();

            removed = listToApplyTo.Where(item => namesToRemove.Contains(item.Name)).ToArray();
            added = undoList.Where(item => !oldNames.Contains(item.Name)).ToArray();

            listToApplyTo.Clear();

            foreach (var undoItem in undoList)
            {
                undoItem.ParentContainer = parent;
                listToApplyTo.Add(undoItem);
            }
        }

        return new AddedAndRemovedInstances(added, removed);
    }

    public void Clear()
    {
        mUndos.Clear();
        recordedSnapshot = null;
    }
}
