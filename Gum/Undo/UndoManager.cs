using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes.Variables;
using Gum.ToolStates;
using Gum.DataTypes;
using Gum.Wireframe;
using ToolsUtilities;
using Gum.Logic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Gum.Undo
{
    #region UndoLock
    public class UndoLock : IDisposable
    {
        public void Dispose()
        {
            UndoManager.Self.UndoLocks.Remove(this);
        }
    }

    #endregion

    #region ElementHistory

    public class ElementHistory
    {
        public ElementSave InitialState { get; set; }
        public List<UndoSnapshot> Undos { get; set; } = new List<UndoSnapshot>();
        public int UndoIndex { get; set; } = -1;
    }

    #endregion

    public class UndoManager
    {
        #region Fields

        internal ObservableCollection<UndoLock> UndoLocks { get; private set; }

        bool isRecordingUndos = true;

        Dictionary<ElementSave, ElementHistory> mUndos = new Dictionary<ElementSave, ElementHistory>();

        static UndoManager mSelf;

        UndoSnapshot recordedSnapshot;
        public UndoSnapshot RecordedSnapshot => recordedSnapshot;
        public ElementHistory CurrentElementHistory
        {
            get
            {
                ElementHistory history = null;

                if (SelectedState.Self.SelectedElement != null && mUndos.ContainsKey(SelectedState.Self.SelectedElement))
                {
                    history = mUndos[SelectedState.Self.SelectedElement];
                }

                return history;
            }
        }


        //StateSave mRecordedStateSave;
        //List<InstanceSave> mRecordedInstanceList;

        #endregion

        public event EventHandler UndosChanged;
        public void BroadcastUndosChanged() => UndosChanged?.Invoke(this, null);

        public static UndoManager Self { get; private set; } = new UndoManager();

        public UndoManager()
        {
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


            if (SelectedState.Self.SelectedElement != null)
            {
                if (mUndos.ContainsKey(SelectedState.Self.SelectedElement) == false)
                {
                    var cloned = CloneWithFixedEnumerations(SelectedState.Self.SelectedElement);

                    var history = new ElementHistory
                    {
                        InitialState = cloned
                    };

                    mUndos.Add(SelectedState.Self.SelectedElement, history);
                }

                if (SelectedState.Self.SelectedComponent != null)
                {
                    recordedSnapshot = new UndoSnapshot();
                    recordedSnapshot.Element = FileManager.CloneSaveObject(SelectedState.Self.SelectedComponent);
                }
                else if (SelectedState.Self.SelectedScreen != null)
                {
                    recordedSnapshot = new UndoSnapshot();
                    recordedSnapshot.Element = FileManager.CloneSaveObject(SelectedState.Self.SelectedScreen);
                }
                else if (SelectedState.Self.SelectedStandardElement != null)
                {
                    recordedSnapshot = new UndoSnapshot();
                    recordedSnapshot.Element = FileManager.CloneSaveObject(SelectedState.Self.SelectedStandardElement);
                }
            }

            if (recordedSnapshot != null)
            {
                foreach (var item in recordedSnapshot.Element.AllStates)
                {
                    item.FixEnumerations();
                }
                recordedSnapshot.StateName = SelectedState.Self.SelectedStateSave?.Name;
                recordedSnapshot.CategoryName = SelectedState.Self.SelectedStateCategorySave?.Name;
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
                SelectedState.Self.SelectedElement != null &&
                isRecordingUndos &&
                // We should allow undos when selected state is null. This can happen when
                // the user deletes an existing state
                //SelectedState.Self.SelectedStateSave != null &&
                UndoLocks.Count == 0;

            if (canUndo)
            {
                StateSave currentStateSave = SelectedState.Self.SelectedStateSave;
                var currentCategory = SelectedState.Self.SelectedStateCategorySave;
                ElementSave selectedElement = SelectedState.Self.SelectedElement;

                StateSave oldStateToCompareAgainst = null;

                if (currentStateSave != null)
                {
                    if (currentCategory != null)
                    {
                        var category = recordedSnapshot.Element.Categories.Find(item => item.Name == currentCategory.Name);
                        oldStateToCompareAgainst = category?.States.Find(item => item.Name == currentStateSave.Name);
                    }
                    else
                    {
                        var stateName = currentStateSave.Name;
                        oldStateToCompareAgainst = recordedSnapshot.Element.States.Find(item => item.Name == stateName);
                    }
                }



                bool doStatesDiffer = FileManager.AreSaveObjectsEqual(oldStateToCompareAgainst, currentStateSave) == false;
                bool doStateCategoriesDiffer =
                    FileManager.AreSaveObjectsEqual(recordedSnapshot.Element.Categories, selectedElement.Categories) == false;
                bool doInstanceListsDiffer = FileManager.AreSaveObjectsEqual(recordedSnapshot.Element.Instances, selectedElement.Instances) == false;
                bool doTypesDiffer = recordedSnapshot.Element.BaseType != selectedElement.BaseType;
                bool doNamesDiffer = recordedSnapshot.Element.Name != selectedElement.Name;

                // Why do we care if the user selected a different state?
                // This seems to cause bugs, and we don't care about undoing selections...
                //bool doesSelectedStateDiffer = recordedSnapshot.CategoryName != currentCategory?.Name ||
                //    recordedSnapshot.StateName != currentStateSave?.Name;

                // todo : need to add behavior differences


                bool didAnythingChange = doStatesDiffer || doStateCategoriesDiffer || doInstanceListsDiffer || doTypesDiffer || doNamesDiffer
                    //|| doesSelectedStateDiffer
                    ;
                if (didAnythingChange)
                {
                    if (!doInstanceListsDiffer)
                    {
                        recordedSnapshot.Element.Instances = null;
                    }
                    if (!doStatesDiffer)
                    {
                        recordedSnapshot.Element.States = null;
                    }

                    if (!doStateCategoriesDiffer)
                    {
                        recordedSnapshot.Element.Categories = null;
                    }
                    if (!doNamesDiffer)
                    {
                        recordedSnapshot.Element.Name = null;
                    }
                    if (!doTypesDiffer)
                    {
                        recordedSnapshot.Element.BaseType = null;
                    }

                    var history = mUndos[SelectedState.Self.SelectedElement];

                    var isAtEndOfStack = history.UndoIndex == history.Undos.Count - 1;
                    if (!isAtEndOfStack)
                    {
                        // If we're not at the end of the stack, then we need to remove all the items after the current index
                        while (history.Undos.Count > history.UndoIndex + 1)
                        {
                            history.Undos.RemoveAt(history.Undos.Count - 1);
                        }
                    }

                    history.Undos.Add(recordedSnapshot);
                    history.UndoIndex = history.Undos.Count - 1;
                    RecordState();

                    UndosChanged?.Invoke(this, null);
                }


                //PrintStatus("RecordUndo");
            }

        }

        public ElementSave CloneWithFixedEnumerations(ElementSave elementSave)
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
                foreach (var state in cloned.AllStates)
                {
                    state.FixEnumerations();
                }
            }
            return cloned;
        }

        public UndoLock RequestLock()
        {
            var undoLock = new UndoLock();

            UndoLocks.Add(undoLock);

            return undoLock;
        }

        public void PerformUndo()
        {
            ElementHistory elementHistory = null;

            if (SelectedState.Self.SelectedElement != null && mUndos.ContainsKey(SelectedState.Self.SelectedElement))
            {
                elementHistory = mUndos[SelectedState.Self.SelectedElement];
            }

            if (elementHistory != null && elementHistory.Undos.Count != 0 && elementHistory.UndoIndex > -1)
            {
                var isLast = elementHistory.UndoIndex == elementHistory.Undos.Count - 1;

                var undoSnapshot = elementHistory.Undos.ElementAt(elementHistory.UndoIndex);



                ElementSave toApplyTo = SelectedState.Self.SelectedElement;

                bool shouldRefreshWireframe, shouldRefreshStateTreeView;
                ApplyUndoSnapshotToElement(undoSnapshot, toApplyTo, true, out shouldRefreshWireframe, out shouldRefreshStateTreeView);

                if (undoSnapshot.CategoryName != SelectedState.Self.SelectedStateCategorySave?.Name ||
                    undoSnapshot.StateName != SelectedState.Self.SelectedStateSave?.Name)
                {
                    var listOfStates = toApplyTo.States;
                    var state = listOfStates?.FirstOrDefault(item => item.Name == undoSnapshot.StateName);

                    if (state != null)
                    {
                        isRecordingUndos = false;
                        SelectedState.Self.SelectedStateSave = state;

                        isRecordingUndos = true;
                    }
                }

                var newIndex = elementHistory.UndoIndex - 1;

                if (isLast)
                {
                    RecordUndo();
                }

                elementHistory.UndoIndex = newIndex;
                DoAfterUndoLogic(toApplyTo, shouldRefreshWireframe, shouldRefreshStateTreeView);
            }
        }

        private void DoAfterUndoLogic(ElementSave toApplyTo, bool shouldRefreshWireframe, bool shouldRefreshStateTreeView)
        {
            RecordState();

            UndosChanged?.Invoke(this, null);

            Plugins.PluginManager.Self.AfterUndo();

            GumCommands.Self.GuiCommands.RefreshElementTreeView();
            SelectedState.Self.UpdateToSelectedStateSave();

            // The instances may have changed.  We will want 
            // to refresh the wireframe since the IPSOs in the 
            // wireframe have tags.
            if (shouldRefreshWireframe)
            {
                WireframeObjectManager.Self.RefreshAll(true);
            }
            if (shouldRefreshStateTreeView)
            {
                GumCommands.Self.GuiCommands.RefreshStateTreeView();
            }

            //PrintStatus("PerformUndo");

            // If an instance is removed
            // through an undo and if that
            // instance is the selected instance
            // then we want to refresh that.
            if (toApplyTo != null && SelectedState.Self.SelectedElement == null)
            {
                SelectedState.Self.SelectedElement = toApplyTo;
            }

            GumCommands.Self.FileCommands.TryAutoSaveProject();
            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();

            // Don't do this anymore due to filtering through search
            //ElementTreeViewManager.Self.VerifyComponentsAreInTreeView(ProjectManager.Self.GumProjectSave);
        }

        public void PerformRedo()
        {
            ElementHistory elementHistory = null;

            if (SelectedState.Self.SelectedElement != null && mUndos.ContainsKey(SelectedState.Self.SelectedElement))
            {
                elementHistory = mUndos[SelectedState.Self.SelectedElement];
            }
            UndoSnapshot undoSnapshot = null;

            if (elementHistory != null)
            {
                var indexToApply = elementHistory.UndoIndex + 2;


                if (indexToApply < elementHistory.Undos.Count)
                {
                    undoSnapshot = elementHistory.Undos[indexToApply];
                }
            }

            if(undoSnapshot != null)
            {


                ElementSave toApplyTo = SelectedState.Self.SelectedElement;

                bool shouldRefreshWireframe, shouldRefreshStateTreeView;
                ApplyUndoSnapshotToElement(undoSnapshot, toApplyTo, true, out shouldRefreshWireframe, out shouldRefreshStateTreeView);

                if (undoSnapshot.CategoryName != SelectedState.Self.SelectedStateCategorySave?.Name ||
                    undoSnapshot.StateName != SelectedState.Self.SelectedStateSave?.Name)
                {
                    var listOfStates = toApplyTo.States;
                    var state = listOfStates?.FirstOrDefault(item => item.Name == undoSnapshot.StateName);

                    if (state != null)
                    {
                        isRecordingUndos = false;
                        SelectedState.Self.SelectedStateSave = state;

                        isRecordingUndos = true;
                    }
                }

                elementHistory.UndoIndex++;

                DoAfterUndoLogic(toApplyTo, shouldRefreshWireframe, shouldRefreshStateTreeView);


            }


        }

        public void ApplyUndoSnapshotToElement(UndoSnapshot undoSnapshot, ElementSave toApplyTo, bool propagateNameChanges)
        {
            ApplyUndoSnapshotToElement(undoSnapshot, toApplyTo, propagateNameChanges, out bool _, out bool _);
        }

        private void ApplyUndoSnapshotToElement(UndoSnapshot undoSnapshot, ElementSave toApplyTo,
            bool propagateNameChanges, out bool shouldRefreshWireframe, out bool shouldRefreshStateTreeView)
        {
            var elementInUndoSnapshot = undoSnapshot.Element;


            shouldRefreshWireframe = false;
            shouldRefreshStateTreeView = false;
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
                // todo - need to handle renames
                if (propagateNameChanges)
                {

                }
            }
            if (elementInUndoSnapshot.Instances != null)
            {
                AddAndRemoveInstances(elementInUndoSnapshot.Instances, toApplyTo.Instances, toApplyTo);
                shouldRefreshWireframe = true;
            }
            if (elementInUndoSnapshot.Name != null)
            {
                string oldName = toApplyTo.Name;
                toApplyTo.Name = elementInUndoSnapshot.Name;
                if (propagateNameChanges)
                {
                    RenameLogic.HandleRename(toApplyTo, (InstanceSave)null, oldName, NameChangeAction.Rename, askAboutRename: false);
                }
            }

            if (undoSnapshot.CategoryName != SelectedState.Self.SelectedStateCategorySave?.Name ||
                undoSnapshot.StateName != SelectedState.Self.SelectedStateSave?.Name)
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
            List<UndoSnapshot> stack = null;

            if (SelectedState.Self.SelectedElement != null)
            {
                if (mUndos.ContainsKey(SelectedState.Self.SelectedElement))
                {
                    stack = mUndos[SelectedState.Self.SelectedElement].Undos;
                }

                if (stack == null)
                {
                    System.Console.Out.WriteLine("No undos for " + SelectedState.Self.SelectedElement);

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
}
