using Gum.DataTypes;
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

        void RefreshHistoryItems()
        {
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

        private void HandleUndosChanged(object sender, UndoOperationEventArgs e)
        {
            if (e.Operation == UndoOperation.EntireHistoryChange)
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
