using Gum.DataTypes;
using Gum.Mvvm;
using System;
using System.Collections.ObjectModel;
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
        public Visibility EditListVisibility => IsEditing.ToVisibility();

        public BehaviorsViewModel()
        {
        }

        internal void HandleOkEditClick()
        {
            ApplyChangedValues?.Invoke(this, null);
        }

        public void UpdateTo(ComponentSave component)
        {
            AddedBehaviors.Clear();

            foreach (var behavior in component.Behaviors)
            {
                AddedBehaviors.Add(behavior.BehaviorName);
            }

            AllBehaviors.Clear();
            foreach (var behavior in ProjectManager.Self.GumProjectSave.Behaviors)
            {
                var newItem = new CheckListBehaviorItem();

                newItem.Name = behavior.Name;
                newItem.IsChecked = AddedBehaviors.Contains(behavior.Name);

                AllBehaviors.Add(newItem);
            }


        }
    }
}
