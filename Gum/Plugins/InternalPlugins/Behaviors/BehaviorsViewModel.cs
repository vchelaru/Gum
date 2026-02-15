using Gum.DataTypes;
using Gum.Managers;
using Gum.Mvvm;
using Gum.Services;
using Gum.ToolStates;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Gum.Plugins.Behaviors
{
    public class BehaviorsViewModel : ViewModel
    {
        private readonly ISelectedState _selectedState;
        private readonly IProjectManager _projectManager;

        public event EventHandler ApplyChangedValues;

        public ObservableCollection<string> AddedBehaviors { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<CheckListBehaviorItem> AllBehaviors { get; set; } = new ObservableCollection<CheckListBehaviorItem>();

        ElementSave ElementSave { get; set; }

        public string SelectedBehavior
        {
            get => Get<string>();
            set
            {
                if(Set(value) && ElementSave != null)
                {
                    var behaviorReference = ElementSave.Behaviors.FirstOrDefault(item => item.BehaviorName == value);

                    _selectedState.SelectedBehaviorReference = behaviorReference;
                }
            }
        }

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

        public BehaviorsViewModel(ISelectedState selectedState)
        {
            _selectedState = selectedState;
            _projectManager = Locator.GetRequiredService<IProjectManager>();
        }

        internal void HandleOkEditClick()
        {
            ApplyChangedValues?.Invoke(this, null);
        }

        public void UpdateTo(ComponentSave component)
        {
            ElementSave = component;
            AddedBehaviors.Clear();

            foreach (var behavior in component.Behaviors)
            {
                AddedBehaviors.Add(behavior.BehaviorName);
            }

            AllBehaviors.Clear();
            foreach (var behavior in _projectManager.GumProjectSave.Behaviors)
            {
                var newItem = new CheckListBehaviorItem();

                newItem.Name = behavior.Name;
                newItem.IsChecked = AddedBehaviors.Contains(behavior.Name);

                AllBehaviors.Add(newItem);
            }


        }
    }
}
