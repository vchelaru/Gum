using Gum.DataTypes;
using Gum.Managers;
using Gum.Mvvm;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
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

        public ObservableCollection<CheckListBehaviorItem> AddedBehaviors { get; set; } = new ObservableCollection<CheckListBehaviorItem>();
        public ObservableCollection<CheckListBehaviorItem> AllBehaviors { get; set; } = new ObservableCollection<CheckListBehaviorItem>();

        ElementSave ElementSave { get; set; }

        public CheckListBehaviorItem? SelectedBehavior
        {
            get => Get<CheckListBehaviorItem?>();
            set
            {
                if(Set(value) && ElementSave != null)
                {
                    var behaviorReference = ElementSave.Behaviors.FirstOrDefault(item => item.BehaviorName == value?.Name);

                    _selectedState.SelectedBehaviorReference = behaviorReference;
                }
            }
        }

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

        public BehaviorsViewModel(ISelectedState selectedState, IProjectManager projectManager)
        {
            _selectedState = selectedState;
            _projectManager = projectManager;
        }

        internal void HandleOkEditClick()
        {
            ApplyChangedValues?.Invoke(this, null);
        }

        public void UpdateTo(ComponentSave component)
        {
            ElementSave = component;

            HashSet<string> projectBehaviorNames = new HashSet<string>(
                _projectManager.GumProjectSave?.Behaviors.Select(b => b.Name) ?? Enumerable.Empty<string>());

            // Read-only view: each reference on the component, with orphan flag so the
            // missing-behavior styling shows up without the user having to enter Edit.
            AddedBehaviors.Clear();
            foreach (var reference in component.Behaviors)
            {
                AddedBehaviors.Add(new CheckListBehaviorItem
                {
                    Name = reference.BehaviorName,
                    IsChecked = true,
                    IsOrphaned = !projectBehaviorNames.Contains(reference.BehaviorName),
                });
            }

            // Edit view: project behaviors first (checked iff referenced), then orphan
            // references on the component that no longer exist in the project. Orphans
            // are still shown so the user can uncheck them; otherwise the stale
            // reference (and its error icon) is invisible.
            AllBehaviors.Clear();

            HashSet<string> componentBehaviorNames = new HashSet<string>(
                component.Behaviors.Select(b => b.BehaviorName));

            foreach (var name in projectBehaviorNames)
            {
                AllBehaviors.Add(new CheckListBehaviorItem
                {
                    Name = name,
                    IsChecked = componentBehaviorNames.Contains(name),
                    IsOrphaned = false,
                });
            }

            foreach (var reference in component.Behaviors)
            {
                if (!projectBehaviorNames.Contains(reference.BehaviorName))
                {
                    AllBehaviors.Add(new CheckListBehaviorItem
                    {
                        Name = reference.BehaviorName,
                        IsChecked = true,
                        IsOrphaned = true,
                    });
                }
            }
        }
    }
}
