using Gum.Extensions;
using Gum.Plugins.InternalPlugins.VariableGrid;
using WpfDataUi;
using Gum.Plugins.VariableGrid;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Gum
{
    /// <summary>
    /// The WPF Variables tab view: the filter box, the variables and behavior grids, and the
    /// behavior-variable list. <see cref="Gum.Managers.PropertyGridManager"/> drives it through
    /// <see cref="IVariablesTabView"/>.
    /// </summary>
    public partial class MainPropertyGrid : UserControl, IVariablesTabView
    {
        public event EventHandler? AddVariableClicked;

        public event EventHandler? SelectedBehaviorVariableChanged;

        private MainControlViewModel? _subscribedViewModel;

        public MainPropertyGrid()
        {
            InitializeComponent();

            DataContextChanged += HandleDataContextChanged;
        }

        /// <inheritdoc/>
        object IVariablesTabView.Control => this;

        /// <inheritdoc/>
        IDataUiGrid IVariablesTabView.VariablesGrid => DataGrid;

        /// <inheritdoc/>
        IDataUiGrid IVariablesTabView.BehaviorGrid => BehaviorDataGrid;

        private void HandleAddVariableClicked(object? sender, RoutedEventArgs e)
        {
            AddVariableClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            SelectedBehaviorVariableChanged?.Invoke(this, EventArgs.Empty);
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

        /// <summary>
        /// Puts the caret in the filter box with any existing filter selected, so typing replaces it.
        /// Called by <see cref="Gum.Managers.PropertyGridManager"/> in response to the focus-filter
        /// hotkey.
        /// </summary>
        public void FocusVariableFilter()
        {
            // Dispatched at Loaded priority: the caller may have just brought a hidden Variables tab
            // forward, and a TextBox that hasn't been laid out yet refuses focus.
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                VariableFilterTextBox.Focus();
                VariableFilterTextBox.SelectAll();
            }));
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
