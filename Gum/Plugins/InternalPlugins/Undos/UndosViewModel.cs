using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ToolStates;
using Gum.Undo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ToolsUtilities;

namespace Gum.Plugins.Undos
{
    public class UndosViewModel : INotifyPropertyChanged
    {
        //ObservableCollection<string> mUndos = new ObservableCollection<string>();
        //public ObservableCollection<string> Undos
        //{
        //    get
        //    {
        //        return mUndos;
        //    }
        //}

        public List<string> HistoryItems
        {
            get
            {
                var elementHistory = UndoManager.Self.CurrentElementHistory;

                if (elementHistory == null || elementHistory.Undos.Count() == 0)
                {
                    return new List<string> { "No history" };
                }
                else
                {
                    List<string> undoStringList = GetUndoStringList(elementHistory);

                    return undoStringList;
                }
            }
        }

        public int UndoIndex
        {
            get
            {
                var elementHistory = UndoManager.Self.CurrentElementHistory;

                if(elementHistory == null)
                {
                    return -1;
                }
                else
                {
                    return elementHistory.UndoIndex;
                }
            }
        }

        private static List<string> GetUndoStringList(ElementHistory elementHistory)
        {
            ElementSave selectedElementClone = null;
            
            if(elementHistory.InitialState is ScreenSave screenSave)
            {
                selectedElementClone = FileManager.CloneSaveObject(screenSave);
            }
            else if(elementHistory.InitialState is ComponentSave componentSave)
            {
                selectedElementClone = FileManager.CloneSaveObject(componentSave);
            }
            else if(elementHistory.InitialState is StandardElementSave standard)
            {
                selectedElementClone = FileManager.CloneSaveObject(standard);
            }

            foreach (var item in selectedElementClone.AllStates)
            {
                item.FixEnumerations();
            }

            List<string> undoStringList = new List<string>();

            var undos = elementHistory.Undos;
            var count = undos.Count;

            // now figure out the history going forward:

            for(int i = 0; i < count; i++)
            {
                var undo = undos[i];
                var comparisonInformation = UndoSnapshot.CompareAgainst(selectedElementClone, undo.Element);

                var comparisonInformationDisplay = comparisonInformation.ToString();

                if(!string.IsNullOrEmpty(comparisonInformationDisplay))
                {
                    undoStringList.Add(comparisonInformation.ToString());
                }

                UndoManager.Self.ApplyUndoSnapshotToElement(undo, selectedElementClone, false);
            }

            if(UndoManager.Self.RecordedSnapshot != null && elementHistory.UndoIndex == elementHistory.Undos.Count-1)
            {
                var undo = UndoManager.Self.RecordedSnapshot;

                var comparisonInformation = UndoSnapshot.CompareAgainst(selectedElementClone, undo.Element);

                var comparisonInformationDisplay = comparisonInformation.ToString();

                if (!string.IsNullOrEmpty(comparisonInformationDisplay))
                {
                    undoStringList.Add(comparisonInformation.ToString());
                }
            }

            return undoStringList;
        }

        private static ElementSave GetSelectedElementClone()
        {
            ElementSave selectedElementClone = null;


            if (GumState.Self.SelectedState.SelectedElement != null)
            {
                if (GumState.Self.SelectedState.SelectedComponent != null)
                {
                    selectedElementClone = FileManager.CloneSaveObject(SelectedState.Self.SelectedComponent);
                }
                else if (GumState.Self.SelectedState.SelectedScreen != null)
                {
                    selectedElementClone = FileManager.CloneSaveObject(SelectedState.Self.SelectedScreen);
                }
                else if (GumState.Self.SelectedState.SelectedStandardElement != null)
                {
                    selectedElementClone = FileManager.CloneSaveObject(SelectedState.Self.SelectedStandardElement);
                }
            }

            if (selectedElementClone != null)
            {
                foreach (var state in selectedElementClone.AllStates)
                {
                    state.FixEnumerations();
                }
            }

            return selectedElementClone;
        }

        public UndosViewModel()
        {
            UndoManager.Self.UndosChanged += HandleUndosChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void HandleUndosChanged(object sender, EventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HistoryItems)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UndoIndex)));
            
        }
    }
}
