using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Plugins.InternalPlugins.Undos;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using ToolsUtilities;

namespace Gum.Plugins.Undos
{
    public class UndosViewModel : INotifyPropertyChanged
    {
        private readonly ISelectedState _selectedState;
        private readonly IUndoManager _undoManager;
        //ObservableCollection<string> mUndos = new ObservableCollection<string>();
        //public ObservableCollection<string> Undos
        //{
        //    get
        //    {
        //        return mUndos;
        //    }
        //}

        ObservableCollection<UndoItemViewModel> _historyItems = new ObservableCollection<UndoItemViewModel>();
        public ObservableCollection<UndoItemViewModel> HistoryItems
        {
            get
            {
                return _historyItems;
            }
        }

        public int GetIndexOfCurrent() => _historyItems.LastOrDefault(x => x.UndoOrRedo is UndoOrRedo.Undo) is { } current
            ? _historyItems.IndexOf(current)
            : -1;

        void RefreshHistoryItems()
        {
            if (_selectedState.SelectedBehavior != null)
            {
                RefreshBehaviorHistoryItems();
                return;
            }

            var elementHistory = _undoManager.CurrentElementHistory;

            if (elementHistory == null || elementHistory.Actions.Count() == 0)
            {
                _historyItems = new ObservableCollection<UndoItemViewModel>
                    {
                        new UndoItemViewModel { Display = "No history" }
                    };
            }
            else
            {
                _historyItems.Clear();
                List<string> undoStringList = GetUndoStringList(elementHistory);
                List<UndoItemViewModel> toReturn = new List<UndoItemViewModel>();
                for (int i = 0; i < undoStringList.Count; i++)
                {
                    var item = undoStringList[i];
                    var undoItem = new UndoItemViewModel { Display = item };
                    _historyItems.Add(undoItem);
                }
            }
        }

        private void RefreshBehaviorHistoryItems()
        {
            _historyItems.Clear();

            var behaviorHistory = _undoManager.CurrentBehaviorHistory;

            if (behaviorHistory == null || behaviorHistory.Actions.Count == 0)
            {
                _historyItems.Add(new UndoItemViewModel { Display = "No history" });
                return;
            }

            foreach (var action in behaviorHistory.Actions)
            {
                var description = GetBehaviorActionDescription(action);
                _historyItems.Add(new UndoItemViewModel { Display = description });
            }
        }

        private static string GetBehaviorActionDescription(BehaviorHistoryAction action)
        {
            var before = action.UndoState.Behavior;
            var after = action.RedoState?.Behavior;

            if (after == null)
            {
                return "Behavior change";
            }

            var parts = new List<string>();

            var beforeCategoryNames = before.Categories.Select(c => c.Name).ToHashSet();
            var afterCategoryNames = after.Categories.Select(c => c.Name).ToHashSet();

            var addedCategoryNames = afterCategoryNames.Except(beforeCategoryNames).ToList();
            var removedCategoryNames = beforeCategoryNames.Except(afterCategoryNames).ToList();

            if (addedCategoryNames.Count > 0)
            {
                parts.Add($"Add categories: {string.Join(", ", addedCategoryNames)}");
            }
            if (removedCategoryNames.Count > 0)
            {
                parts.Add($"Remove categories: {string.Join(", ", removedCategoryNames)}");
            }

            var addedStateNames = new List<string>();
            var removedStateNames = new List<string>();
            foreach (var afterCategory in after.Categories)
            {
                var beforeCategory = before.Categories.FirstOrDefault(c => c.Name == afterCategory.Name);
                if (beforeCategory == null)
                {
                    continue;
                }

                var beforeStateNames = beforeCategory.States.Select(s => s.Name).ToHashSet();
                var afterStateNames = afterCategory.States.Select(s => s.Name).ToHashSet();

                addedStateNames.AddRange(afterStateNames.Except(beforeStateNames));
                removedStateNames.AddRange(beforeStateNames.Except(afterStateNames));
            }

            if (addedStateNames.Count > 0)
            {
                parts.Add($"Add states: {string.Join(", ", addedStateNames)}");
            }
            if (removedStateNames.Count > 0)
            {
                parts.Add($"Remove states: {string.Join(", ", removedStateNames)}");
            }

            var beforeInstanceNames = before.RequiredInstances.Select(i => i.Name).ToHashSet();
            var afterInstanceNames = after.RequiredInstances.Select(i => i.Name).ToHashSet();

            var addedInstanceNames = afterInstanceNames.Except(beforeInstanceNames).ToList();
            var removedInstanceNames = beforeInstanceNames.Except(afterInstanceNames).ToList();

            if (addedInstanceNames.Count > 0)
            {
                parts.Add($"Add instances: {string.Join(", ", addedInstanceNames)}");
            }
            if (removedInstanceNames.Count > 0)
            {
                parts.Add($"Remove instances: {string.Join(", ", removedInstanceNames)}");
            }

            return parts.Count > 0 ? string.Join("\n    ", parts) : "Behavior change";
        }

        void TrimAtIndex()
        {
            // a new item has been added, and the new index is the one that should
            // be highlighted. If there's any items past this index, we need to remove them
            while(_historyItems.Count > UndoIndex)
            {
                _historyItems.RemoveAt(_historyItems.Count - 1);
            }
        }

        void AppendOneHistoryItem()
        {
            var elementHistory = _undoManager.CurrentElementHistory;

            if (elementHistory == null || elementHistory.Actions.Count() == 0)
            {
                _historyItems.Clear();

                _historyItems.Add(new UndoItemViewModel { Display = "No history" } );
            }
            else
            {
                if(elementHistory.Actions.Count == 1)
                {
                    // clear it so we can get rid of "no history'
                    _historyItems.Clear();
                }
                List<string> undoStringList = GetUndoStringList(elementHistory, 1);
                for (int i = 0; i < undoStringList.Count; i++)
                {
                    var item = undoStringList[i];
                    var undoItem = new UndoItemViewModel { Display = item };
                    _historyItems.Add(undoItem);
                }

            }
        }

        void RefreshIndexes()
        {
            for (int i = 0; i < _historyItems.Count; i++)
            {
                var item = _historyItems[i];
                item.UndoOrRedo = i <= this.UndoIndex
                    ? UndoOrRedo.Undo : UndoOrRedo.Redo;
            }
        }

        public int UndoIndex
        {
            get
            {
                if (_selectedState.SelectedBehavior != null)
                {
                    return _undoManager.CurrentBehaviorHistory?.UndoIndex ?? -1;
                }

                var elementHistory = _undoManager.CurrentElementHistory;

                if (elementHistory == null)
                {
                    return -1;
                }
                else
                {
                    return elementHistory.UndoIndex;
                }
            }
        }

        private List<string> GetUndoStringList(ElementHistory elementHistory, int? numberOfItemsFromEnd = null)
        {

            ElementSave selectedElementClone = null;

            var elementToClone =
                //elementHistory.InitialState;
                _selectedState.SelectedElement;

            if (this.UndoIndex < elementHistory.Actions.Count - 1)
            {
                // we're viewing something before the end, so use the final state as the starting point:
                elementToClone = elementHistory.FinalState;
            }

            selectedElementClone = UndoManager.CloneWithFixedEnumerations(elementToClone);

            List<string> undoStringList = new List<string>();

            var undos = elementHistory.Actions;
            var count = undos.Count;

            for (int i = undos.Count - 1; i > -1; i--)
            {
                var undo = undos[i];
                var comparisonInformation = UndoSnapshot.CompareAgainst(undo.UndoState.Element, selectedElementClone);

                var comparisonInformationDisplay = comparisonInformation.ToString();

                if (string.IsNullOrEmpty(comparisonInformationDisplay))
                {
                    undoStringList.Insert(0, "Unknown Undo");
                }
                else
                {
                    undoStringList.Insert(0, comparisonInformation.ToString());
                }

                _undoManager.ApplyUndoSnapshotToElement(undo.UndoState, selectedElementClone, false);

                if (undoStringList.Count >= numberOfItemsFromEnd)
                {
                    break;
                }
            }


            return undoStringList;
        }

        private ElementSave GetSelectedElementClone()
        {
            ElementSave selectedElementClone = null;


            if (_selectedState.SelectedElement != null)
            {
                return UndoManager.CloneWithFixedEnumerations(_selectedState.SelectedElement);
            }

            return null;
        }

        public UndosViewModel()
        {
            _selectedState = Locator.GetRequiredService<ISelectedState>();
            _undoManager = Locator.GetRequiredService<IUndoManager>();
            _undoManager.UndosChanged += HandleUndosChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void HandleUndosChanged(object? sender, UndoOperationEventArgs e)
        {
            if (_selectedState.SelectedBehavior != null)
            {
                RefreshHistoryItems();
            }
            else if (e.Operation == UndoOperation.EntireHistoryChange)
            {
                RefreshHistoryItems();
            }
            else if (e.Operation == UndoOperation.HistoryAppended)
            {
                TrimAtIndex();
                AppendOneHistoryItem();
            }
            RefreshIndexes();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HistoryItems)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UndoIndex)));
        }


    }
}
