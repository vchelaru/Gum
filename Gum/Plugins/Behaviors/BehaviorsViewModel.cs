using Gum.Managers;
using Gum.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Gum.Plugins.Behaviors
{
    public class BehaviorsViewModel : ViewModel
    {
        public event EventHandler ApplyChangedValues;

        public ObservableCollection<string> AddedBehaviors { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<CheckListBehaviorItem> AllBehaviors { get; set; } = new ObservableCollection<CheckListBehaviorItem>();
        //public ObservableCollection<string> AllBehaviors { get; set; } = new ObservableCollection<string>();

        public bool IsEditing
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        [DependsOn(nameof(IsEditing))]
        public Visibility AddedListVisibility
        {
            get
            {
                if (IsEditing) return Visibility.Collapsed;
                else return Visibility.Visible;
            }
        }


        [DependsOn(nameof(IsEditing))]
        public Visibility EditListVisibility
        {
            get
            {
                if (IsEditing) return Visibility.Visible;
                else return Visibility.Hidden;
            }
        }

        public BehaviorsViewModel()
        {
        }

        internal void HandleOkEditClick()
        {
            ApplyChangedValues?.Invoke(this, null);
        }
    }
}
