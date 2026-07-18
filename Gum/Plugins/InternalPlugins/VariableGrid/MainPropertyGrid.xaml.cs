using Gum.Extensions;
using Gum.Plugins.VariableGrid;
using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace Gum
{
    /// <summary>
    /// Interaction logic for TestWpfControl.xaml
    /// </summary>
    public partial class MainPropertyGrid : UserControl
    {
        public event EventHandler AddVariableClicked;

        public event EventHandler SelectedBehaviorVariableChanged;

        private MainControlViewModel? _subscribedViewModel;

        public object Instance
        {
            get { return DataGrid.Instance; }
            set { DataGrid.Instance = value; }
        }


        public MainPropertyGrid()
        {
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
            }

            _subscribedViewModel = e.NewValue as MainControlViewModel;

            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.BehaviorVariablesContextMenuItems.CollectionChanged += HandleBehaviorVariablesContextMenuItemsChanged;
            }

            RebuildBehaviorVariablesContextMenu();
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
