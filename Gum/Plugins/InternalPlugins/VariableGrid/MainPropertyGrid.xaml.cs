using Gum.Extensions;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Plugins.VariableGrid;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Gum
{
    /// <summary>
    /// Interaction logic for TestWpfControl.xaml
    /// </summary>
    public partial class MainPropertyGrid : UserControl
    {
        public event EventHandler AddVariableClicked;

        public event EventHandler SelectedBehaviorVariableChanged;

        private readonly IVariableFilterService _variableFilterService;

        private MainControlViewModel? _subscribedViewModel;

        public object Instance
        {
            get { return DataGrid.Instance; }
            set { DataGrid.Instance = value; }
        }


        public MainPropertyGrid(IVariableFilterService variableFilterService)
        {
            _variableFilterService = variableFilterService;

            InitializeComponent();

            DataContextChanged += HandleDataContextChanged;
        }

        private void HandleAddVariableClicked(object? sender, RoutedEventArgs e)
        {
            AddVariableClicked?.Invoke(this, null);
        }

        private void ListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            SelectedBehaviorVariableChanged?.Invoke(this, null);
        }

        /// <summary>
        /// MainControlViewModel exposes its behavior-variable right-click menu as framework-neutral
        /// <see cref="Gum.ViewModels.ContextMenuItemViewModel"/>s (ADR-0005, issue #3754) rather than
        /// WPF MenuItems, so the View is responsible for turning them into real MenuItems here -- via
        /// the shared <see cref="ContextMenuItemViewModelExtensions.ToMenuItem"/> helper.
        /// </summary>
        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.BehaviorVariablesContextMenuItems.CollectionChanged -= HandleBehaviorVariablesContextMenuItemsChanged;
                _subscribedViewModel.PropertyChanged -= HandleViewModelPropertyChanged;
            }

            _subscribedViewModel = e.NewValue as MainControlViewModel;

            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.BehaviorVariablesContextMenuItems.CollectionChanged += HandleBehaviorVariablesContextMenuItemsChanged;
                _subscribedViewModel.PropertyChanged += HandleViewModelPropertyChanged;
            }

            RebuildBehaviorVariablesContextMenu();
            RefreshVariableFilter();
        }

        private void HandleViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainControlViewModel.VariableFilterText))
            {
                RefreshVariableFilter();
            }
        }

        private void HandleClearVariableFilterClicked(object? sender, RoutedEventArgs e)
        {
            ClearVariableFilter();

            // Focus stays in the box so the user can type a different filter straight away.
            VariableFilterTextBox.Focus();
        }

        private void ClearVariableFilter()
        {
            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.VariableFilterText = "";
            }
        }

        private void HandleVariableFilterPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
            {
                return;
            }

            ClearVariableFilter();

            DataGrid.Focus();
            e.Handled = true;
        }

        /// <summary>
        /// Turns what the user typed into a row predicate. The predicate lives here rather than on the
        /// ViewModel because rows are WPF <see cref="WpfDataUi.DataTypes.InstanceMember"/>s, which the
        /// headless assembly holding the ViewModel cannot reference.
        /// </summary>
        private void RefreshVariableFilter()
        {
            string? filterText = _subscribedViewModel?.VariableFilterText;

            if (!_variableFilterService.HasFilter(filterText))
            {
                DataGrid.ApplyMemberFilter(null);
                return;
            }

            DataGrid.ApplyMemberFilter(member =>
                _variableFilterService.IsMatch(filterText, member.Name ?? "", member.DisplayName));
        }

        private void HandleBehaviorVariablesContextMenuItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
            RebuildBehaviorVariablesContextMenu();

        private void RebuildBehaviorVariablesContextMenu()
        {
            ListBoxContextMenu.Items.Clear();
            if (_subscribedViewModel != null)
            {
                foreach (var item in _subscribedViewModel.BehaviorVariablesContextMenuItems)
                {
                    ListBoxContextMenu.Items.Add(item.ToMenuItem());
                }
            }
        }
    }
}
