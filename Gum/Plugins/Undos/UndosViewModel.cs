using Gum.Undo;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

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

                if(undos == null || undos.Count() == 0)
                {
                    return "No undos";
                }
                else
                {
                    return $"Number of undos: {undos.Count()}";
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
