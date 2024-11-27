using Gum.ToolStates;
using Gum.Undo;
using System;
using System.ComponentModel;
using System.Linq;

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

                    if (undos.Count > 0 && GumState.Self.SelectedState.SelectedElement != null)
                    {
                        var firstUndo = undos.First();
                        toReturn += $"\n1: {firstUndo.CompareAgainst(GumState.Self.SelectedState.SelectedElement, firstUndo.Element)}";

                        for (int i = 1; i < undos.Count; i++)
                        {
                            var previous = undos.ElementAt(i - 1);
                            var current = undos.ElementAt(i);

                            var comparison = current.CompareAgainst(previous);

                            toReturn += $"\n{i + 1}: {comparison}";

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
