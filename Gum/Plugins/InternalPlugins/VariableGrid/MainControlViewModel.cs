using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Mvvm;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Gum.Plugins.VariableGrid
{
    public class MainControlViewModel : ViewModel
    {
        public Visibility HasStateInformation
        {
            get => Get<Visibility>();
            set => Set(value);
        }

        public Visibility HasErrors
        {
            get => Get<Visibility>();
            set => Set(value); 
        }

        public string StateInformation
        {
            get => Get<string>();
            set => Set(value);
        }

        public Brush StateBackground
        {
            get => Get<Brush>();
            set => Set(value);
        }

        public string ErrorInformation
        {
            get => Get<string>();
            set => Set(value); 
        }

        #region Behaviors

        public Visibility ShowBehaviorUi
        {
            get => Get<Visibility>(); 
            set => Set(value); 
        }

        public BehaviorSave BehaviorSave { get; set; }

        public ObservableCollection<VariableSave> BehaviorVariables
        {
            get;
            private set;
        } = new ObservableCollection<VariableSave>();

        public VariableSave SelectedBehaviorVariable
        {
            get => Get<VariableSave>(); 
            set => Set(value); 
        }

        [DependsOn(nameof(SelectedBehaviorVariable))]
        public List<MenuItem> BehaviorVariablesContextMenuItems
        {
            get
            {
                if(SelectedBehaviorVariable == null)
                {
                    return new List<MenuItem>();
                }
                else
                {
                    return new List<MenuItem>()
                    {
                        EditVariableMenuItem,
                        DeleteVariableMenuItem
                    };
                }
            }
        }

        MenuItem EditVariableMenuItem;
        MenuItem DeleteVariableMenuItem;
        private readonly IDeleteVariableService _deleteVariableService;
        private IEditVariableService _editVariableService;

        #endregion

        public Visibility ShowVariableGrid
        {
            //get => Visibility.Hidden;
            get => Get<Visibility>();
            set => Set(value);
        }
        public Visibility AddVariableButtonVisibility
        {
            get => Get<Visibility>();
            set => Set(value);
        }

        public VariableSave EffectiveSelectedBehaviorVariable
        {
            get
            {
                if(ShowBehaviorUi == Visibility.Visible)
                {
                    return SelectedBehaviorVariable;
                } 
                else
                {
                    return null;
                }
            }
        }

        public MainControlViewModel(IDeleteVariableService deleteVariableService, IEditVariableService editVariableService)
        {
            EditVariableMenuItem = new MenuItem();
            EditVariableMenuItem.Header = "Edit Variable";
            EditVariableMenuItem.Click += HandleEditVariableClicked;

            DeleteVariableMenuItem = new MenuItem();
            DeleteVariableMenuItem.Header = "Delete Variable";
            DeleteVariableMenuItem.Click += HandleDeleteVariableClicked;

            _deleteVariableService = deleteVariableService;
            _editVariableService = editVariableService;
        }

        private void HandleDeleteVariableClicked(object? sender, RoutedEventArgs e)
        {
            if(BehaviorSave != null)
            {
                _deleteVariableService.DeleteVariable(SelectedBehaviorVariable, BehaviorSave);
            }
        }

        private void HandleEditVariableClicked(object? sender, RoutedEventArgs e)
        {
            var editModes = 
                _editVariableService.GetAvailableEditModeFor(SelectedBehaviorVariable, BehaviorSave);

            if(editModes != VariableEditMode.None)
            {
                _editVariableService.ShowEditVariableWindow(SelectedBehaviorVariable, BehaviorSave);
            }
        }
    }
}
