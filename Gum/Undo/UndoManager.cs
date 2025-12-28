using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.ToolStates;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Controls;
using ToolsUtilities;

namespace Gum.Undo;

#region UndoLock
public class UndoLock : IDisposable
{
    private readonly UndoManager _undoManager;

    public UndoLock(UndoManager manager)
    {
        _undoManager = manager;
    }
    
    public void Dispose()
    {
        _undoManager.UndoLocks.Remove(this);
    }
}

#endregion

#region ElementHistory

public class HistoryAction
{
    public UndoSnapshot UndoState { get; set; }
    public UndoSnapshot RedoState { get; set; }

    public override string ToString()
    {
        return $"Undo:{UndoState}";
    }
}

public class ElementHistory
{
    public ElementSave FinalState { get; set; }

    /// <summary>
    /// A list of actions for the current element, where the most recent action is at the end of the list.
    /// </summary>
    public List<HistoryAction> Actions { get; set; } = new List<HistoryAction>();

    /// <summary>
    /// The index of the next undo to perform. If this is -1, then there are no undos to perform.
    /// Note that this means that the next redo to perform is at UndoIndex + 1.
    /// </summary>
    public int UndoIndex { get; set; } = -1;
}

#endregion

#region UndoOperation

public enum UndoOperation
{
    Undo,
    Redo,
    EntireHistoryChange,
    HistoryAppended
}

public class UndoOperationEventArgs : EventArgs
{
    public UndoOperation Operation { get; set; }
}

#endregion

public class UndoManager : IUndoManager
{
    #region Fields

    private readonly ISelectedState _selectedState;
    private readonly IRenameLogic _renameLogic;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly IMessenger _messenger;

    internal ObservableCollection<UndoLock> UndoLocks { get; private set; }

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


    //StateSave mRecordedStateSave;
    //List<InstanceSave> mRecordedInstanceList;
    
    #endregion

    #region Events/Invokations

    public event EventHandler<UndoOperationEventArgs>? UndosChanged;

    public void BroadcastUndosChanged() => InvokeUndosChanged(UndoOperation.EntireHistoryChange);

    void InvokeUndosChanged(UndoOperation operation) => UndosChanged?.Invoke(this, new UndoOperationEventArgs { Operation = operation });

    #endregion

    public UndoManager(ISelectedState selectedState, 
        IRenameLogic renameLogic, 
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        IMessenger messenger)
    {
        _selectedState = selectedState;
        _renameLogic = renameLogic;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _messenger = messenger;

        UndoLocks = new ObservableCollection<UndoLock>();
        UndoLocks.CollectionChanged += HandleUndoLockChanged;
    }

    private void HandleUndoLockChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (UndoLocks.Count == 0)
        {
            RecordUndo();
        }
    }

    /// <summary>
    /// Records the current state, which serves as the "restore point" when an undo occurs. This state will be compared
    /// with the current state in the RecordUndo call to see if any changes should be made.
    /// </summary>
    public void RecordState()
    {
        if (UndoLocks.Count > 0)
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
        }

        //PrintStatus("RecordState");
    }

    /// <summary>
    /// Records an undo if any values have changed. This should be called whenever some editing activity has finished.
    /// For example, whenever a value changes in a text box or whenever a drag has finished.
    /// </summary>
    public void RecordUndo()
    {
        var canUndo = recordedSnapshot != null &&
            _selectedState.SelectedElement != null &&
            isRecordingUndos &&
            // We should allow undos when selected state is null. This can happen when
            // the user deletes an existing state
            //_selectedState.SelectedStateSave != null &&
            UndoLocks.Count == 0;

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

        UndoSnapshot undoSnapshot = TryGetUndoSnapshotToAdd(newStateSave, newElement, oldState,
            recordedSnapshot.Element, recordedSnapshot.CategoryName, recordedSnapshot.StateName);

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

                var redoSnapshot = TryGetUndoSnapshotToAdd(oldState, recordedSnapshot.Element, newStateSave, newElement, recordedSnapshot.CategoryName, recordedSnapshot.StateName);

                if(redoSnapshot != null)
                {
                    action.RedoState = redoSnapshot;
                }


                RecordState();

                InvokeUndosChanged(UndoOperation.HistoryAppended);
            }
            else
            {
                // This can happen when copy/pasting a new element and selecting it, so let's record the state
                // so future work an be undone
                RecordState();

            }
        }
    }

    /// <summary>
    /// Checks if anything has changed and if so returns an UndoSnapshot
    /// </summary>
    /// <param name="newState"></param>
    /// <param name="newElement"></param>
    /// <param name="oldState"></param>
    /// <param name="oldElement"></param>
    /// <param name="categoryName"></param>
    /// <param name="stateName"></param>
    /// <returns></returns>
    private UndoSnapshot? TryGetUndoSnapshotToAdd(StateSave newState, ElementSave newElement, 
        StateSave oldState, ElementSave oldElement, string categoryName, string stateName)
    {
        bool doStatesDiffer = FileManager.AreSaveObjectsEqual(oldState, newState) == false;
        bool doStateCategoriesDiffer =
            FileManager.AreSaveObjectsEqual(oldElement.Categories, newElement.Categories) == false;
        bool doInstanceListsDiffer = FileManager.AreSaveObjectsEqual(oldElement.Instances, newElement.Instances) == false;
        bool doTypesDiffer = oldElement.BaseType != newElement.BaseType;
        bool doNamesDiffer = oldElement.Name != newElement.Name;
        bool doBehaviorsDiffer = FileManager.AreSaveObjectsEqual(oldElement.Behaviors, newElement.Behaviors) == false;

        // Why do we care if the user selected a different state?
        // This seems to cause bugs, and we don't care about undoing selections...
        //bool doesSelectedStateDiffer = recordedSnapshot.CategoryName != currentCategory?.Name ||
        //    recordedSnapshot.StateName != currentStateSave?.Name;

        // todo : need to add behavior differences
        UndoSnapshot? snapshotToAdd = null;

        bool didAnythingChange = doStatesDiffer || doStateCategoriesDiffer || doInstanceListsDiffer || doTypesDiffer || doNamesDiffer ||
            doBehaviorsDiffer
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

            snapshotToAdd = new UndoSnapshot
            {
                Element = clone,
                CategoryName = categoryName,
                StateName = stateName
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

            foreach (var state in cloned.AllStates)
            {
                state.FixEnumerations();
            }

        }
        return cloned;
    }

    public UndoLock RequestLock()
    {
        var undoLock = new UndoLock(this);

        UndoLocks.Add(undoLock);

        return undoLock;
    }



    
    public bool CanUndo()
    {
        var elementHistory = GetValidUndosForElement(_selectedState.SelectedElement);

        return CanUndo(elementHistory);
    }

    public bool CanUndo(ElementHistory? elementHistory)
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



        ElementSave toApplyTo = _selectedState.SelectedElement;

        ApplyUndoSnapshotToElement(undoSnapshot.UndoState, toApplyTo, true, 
            out bool shouldRefreshWireframe, 
            out bool shouldRefreshStateTreeView,
            out bool shouldRefreshBehaviorView);

        //if (undoSnapshot.UndoState.CategoryName != _selectedState.SelectedStateCategorySave?.Name ||
        //    undoSnapshot.UndoState.StateName != _selectedState.SelectedStateSave?.Name)
        //{

        StateSave stateToSelect = null;
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
        DoAfterUndoLogic(toApplyTo, shouldRefreshWireframe, shouldRefreshStateTreeView, shouldRefreshBehaviorView);
    }

    private void DoAfterUndoLogic(ElementSave toApplyTo,
        bool shouldRefreshWireframe, 
        bool shouldRefreshStateTreeView, 
        bool shouldRefreshBehaviorView)
    {
        RecordState();

        InvokeUndosChanged(UndoOperation.Undo);

        _messenger.Send(new AfterUndoMessage());

        _guiCommands.RefreshElementTreeView(toApplyTo);

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

        // Don't do this anymore due to filtering through search
        //ElementTreeViewManager.Self.VerifyComponentsAreInTreeView(ProjectManager.Self.GumProjectSave);
    }

    public bool CanRedo()
    {
        var elementHistory = GetValidUndosForElement(_selectedState.SelectedElement);

        UndoSnapshot? redoSnapshot = GetRedoSnapshot(elementHistory);

        return CanRedo(elementHistory, redoSnapshot);
    }

    public bool CanRedo(ElementHistory? elementHistory, UndoSnapshot? redoSnapshot)
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

        ApplyUndoSnapshotToElement(redoSnapshot, toApplyTo, true, 
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

        DoAfterUndoLogic(toApplyTo, shouldRefreshWireframe, shouldRefreshStateTreeView, 
            shouldRefreshBehaviorView);
    }

    public void ApplyUndoSnapshotToElement(UndoSnapshot undoSnapshot, ElementSave toApplyTo, bool propagateNameChanges)
    {
        ApplyUndoSnapshotToElement(undoSnapshot, toApplyTo, propagateNameChanges, out bool _, out bool _, out bool _);
    }

    private void ApplyUndoSnapshotToElement(UndoSnapshot undoSnapshot, ElementSave toApplyTo,
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

        if (elementInUndoSnapshot.Instances != null)
        {
            AddAndRemoveInstances(elementInUndoSnapshot.Instances, toApplyTo.Instances, toApplyTo);
            shouldRefreshWireframe = true;
        }
        if (!string.IsNullOrEmpty(elementInUndoSnapshot.Name))
        {
            string oldName = toApplyTo.Name;
            toApplyTo.Name = elementInUndoSnapshot.Name;
            if (propagateNameChanges)
            {
                _renameLogic.HandleRename(toApplyTo, (InstanceSave)null, oldName, NameChangeAction.Rename, askAboutRename: false);
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
    }

    // Normally we wouldn't do this in the pattern but the originator itself
    // can't be found without project-wide knowledge...so we have to do some type-specific
    // logic.
    ElementSave GetWhatToApplyTo(UndoObject undoObject)
    {
        object parentAsObject = undoObject.Parent;
        ElementSave parentAsElementSave = parentAsObject as ElementSave;
        return parentAsElementSave;

        //if (undoObject.StateSave != null)
        //{
        //    if (parentAsElementSave != null)
        //    {
        //        // for now we will just assume the default state
        //        return parentAsElementSave.DefaultState;
        //    }
        //}
        //if (undoObject.Instances != null)
        //{
        //    if (parentAsElementSave != null)
        //    {
        //        return parentAsElementSave.Instances;
        //    }
        //}

        //return null;

    }

    void ApplyStateVariables(StateSave undoStateSave, StateSave toApplyTo, ElementSave parent)
    {
        toApplyTo.SetFrom(undoStateSave);

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

    void AddAndRemoveInstances(List<InstanceSave> undoList, List<InstanceSave> listToApplyTo, ElementSave parent)
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

    void PrintStatus(string reason)
    {
        List<HistoryAction> stack = null;

        if (_selectedState.SelectedElement != null)
        {
            if (mUndos.ContainsKey(_selectedState.SelectedElement))
            {
                stack = mUndos[_selectedState.SelectedElement].Actions;
            }

            if (stack == null)
            {
                System.Console.Out.WriteLine("No undos for " + _selectedState.SelectedElement);

            }
            else
            {
                string whatToWrite = reason + "\n\tUndos: " + stack.Count;

                System.Console.Out.WriteLine(whatToWrite);
            }
        }
    }

    public void ClearAll()
    {
        mUndos.Clear();
    }
}
