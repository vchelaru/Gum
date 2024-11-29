using Gum.DataTypes;
using Gum.ToolStates;
using Gum.Undo;
using System;
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

        public string DisplayText
        {
            get
            {
                var undos = UndoManager.Self.CurrentUndoStack;

                if (undos == null || undos.Count() == 0)
                {
                    return "No undos";
                }
                else
                {
                    var toReturn = $"Number of undos: {undos.Count()}";

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
                        for(int i = 0; i < undos.Count; i++)
                        {
                            var undo = undos.ElementAt(i);
                            var comparisonInformation = undo.CompareAgainst(selectedElementClone, undo.Element);
                            toReturn += $"\n{i+1}: {comparisonInformation}";

                            // apply it to the selected element so we have a "running state" that we can continually compare against
                            UndoManager.Self.ApplyUndoSnapshotToElement(undo, selectedElementClone);

                        }
                    }
                    return toReturn;
                }
            }
        }

        public UndosViewModel()
        {
            UndoManager.Self.UndosChanged += HandleUndosChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void HandleUndosChanged(object sender, EventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayText)));
        }
    }
}
