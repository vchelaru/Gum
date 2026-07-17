using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Plugins.InternalPlugins.Undos;
using Gum.StateAnimation.SaveClasses;
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
                var description = BehaviorUndoStrategy.GetBehaviorActionDescription(action);
                _historyItems.Add(new UndoItemViewModel { Display = description });
            }
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

            selectedElementClone = ElementUndoStrategy.CloneWithFixedEnumerations(elementToClone);

            List<string> undoStringList = new List<string>();

            var undos = elementHistory.Actions;
            var count = undos.Count;

            for (int i = undos.Count - 1; i > -1; i--)
            {
                var undo = undos[i];
                var comparisonInformation = UndoSnapshot.CompareAgainst(undo.UndoState.Element, selectedElementClone);

                var comparisonInformationDisplay = comparisonInformation.ToString();

                // The element diff above ignores animations (they live in the .ganx, not the element).
                // Describe the action's animation change directly from its snapshot: UndoState.Animations
                // is the pre-change state, RedoState.Animations the post-change state.
                var animationDisplay = DescribeAnimationChange(undo.UndoState.Animations, undo.RedoState?.Animations);

                var parts = new List<string>();
                if (!string.IsNullOrEmpty(comparisonInformationDisplay)) parts.Add(comparisonInformationDisplay);
                if (!string.IsNullOrEmpty(animationDisplay)) parts.Add(animationDisplay);

                undoStringList.Insert(0, parts.Count > 0 ? string.Join("\n    ", parts) : "Unknown Undo");

                _undoManager.ApplyUndoSnapshotToElement(undo.UndoState, selectedElementClone, false);

                if (undoStringList.Count >= numberOfItemsFromEnd)
                {
                    break;
                }
            }


            return undoStringList;
        }

        /// <summary>
        /// Describes how an action changed an element's animations, for the History tab. Compares the
        /// snapshot's pre-change animations (<see cref="UndoSnapshot.Animations"/> on the undo state)
        /// against the post-change animations (on the redo state). Returns an empty string when the
        /// action did not touch animations.
        /// </summary>
        internal static string DescribeAnimationChange(ElementAnimationsSave? before, ElementAnimationsSave? after)
        {
            if (before == null && after == null)
            {
                return string.Empty;
            }

            var beforeAnimations = before?.Animations ?? new List<AnimationSave>();
            var afterAnimations = after?.Animations ?? new List<AnimationSave>();

            var beforeNames = beforeAnimations.Select(item => item.Name).ToHashSet();
            var afterNames = afterAnimations.Select(item => item.Name).ToHashSet();

            var added = afterAnimations.Where(item => !beforeNames.Contains(item.Name)).Select(item => item.Name).ToList();
            var removed = beforeAnimations.Where(item => !afterNames.Contains(item.Name)).Select(item => item.Name).ToList();
            var modified = afterAnimations
                .Where(item => beforeNames.Contains(item.Name)
                    && !FileManager.AreSaveObjectsEqual(item, beforeAnimations.First(other => other.Name == item.Name)))
                .Select(item => item.Name)
                .ToList();

            var lines = new List<string>();
            if (added.Count > 0) lines.Add($"Add animation: {string.Join(", ", added)}");
            if (removed.Count > 0) lines.Add($"Remove animation: {string.Join(", ", removed)}");
            if (modified.Count > 0) lines.Add($"Modify animation: {string.Join(", ", modified)}");

            return string.Join("\n    ", lines);
        }

        private ElementSave GetSelectedElementClone()
        {
            ElementSave selectedElementClone = null;


            if (_selectedState.SelectedElement != null)
            {
                return ElementUndoStrategy.CloneWithFixedEnumerations(_selectedState.SelectedElement);
            }

            return null;
        }

        public UndosViewModel(ISelectedState selectedState, IUndoManager undoManager)
        {
            _selectedState = selectedState;
            _undoManager = undoManager;
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
