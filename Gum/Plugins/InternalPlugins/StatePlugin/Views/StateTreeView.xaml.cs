using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Gum.Plugins.InternalPlugins.StatePlugin.Views
{
    /// <summary>
    /// Interaction logic for StateTreeView.xaml
    /// </summary>
    public partial class StateTreeView : UserControl
    {
        #region Services

        private readonly StateTreeViewRightClickService _stateTreeViewRightClickService;
        private readonly HotkeyManager _hotkeyManager;
        private readonly ISelectedState _selectedState;
        private readonly CopyPasteLogic _copyPasteLogic;

        #endregion

        StateTreeViewModel ViewModel => DataContext as StateTreeViewModel;

        public event EventHandler SelectedItemChanged;

        public object SelectedItem => TreeViewInstance.SelectedItem;

        public ContextMenu TreeViewContextMenu
        {
            get => TreeViewInstance.ContextMenu;
        }

        public StateTreeView(StateTreeViewModel viewModel, 
            StateTreeViewRightClickService stateTreeViewRightClickService,
            HotkeyManager hotkeyManager, 
            ISelectedState selectedState,
            CopyPasteLogic copyPasteLogic)
        {
            _stateTreeViewRightClickService = stateTreeViewRightClickService;
            _hotkeyManager = hotkeyManager;
            _selectedState = selectedState;
            _copyPasteLogic = copyPasteLogic;
            InitializeComponent();
            TreeViewInstance.ContextMenu = new ContextMenu();
            this.DataContext = viewModel;
        }

        private void TreeView_SelectedItemChanged(object? sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //var treeView = sender as System.Windows.Controls.TreeView;

            //ViewModel.SelectedItem = treeView.SelectedItem as StateTreeViewItem;

            //SelectedItemChanged?.Invoke(this, EventArgs.Empty);
        }

        private void TreeViewInstance_KeyDown(object? sender, KeyEventArgs e)
        {
            if (_hotkeyManager.ReorderUp.IsPressed(e))
            {
                _stateTreeViewRightClickService.MoveStateInDirection(-1);
                e.Handled = true;
            }
            else if (_hotkeyManager.ReorderDown.IsPressed(e))
            {
                var stateSave = _selectedState.SelectedStateSave;
                bool isUncategorized = stateSave != null &&
                    _selectedState.SelectedStateContainer.UncategorizedStates.Contains(stateSave);

                if (!isUncategorized)
                {
                    _stateTreeViewRightClickService.MoveStateInDirection(1);
                }
                e.Handled = true;
            }
            else if (_hotkeyManager.Rename.IsPressed(e))
            {
                if (_selectedState.SelectedStateSave != null)
                {
                    var isUncategorized = _selectedState.SelectedStateContainer?.UncategorizedStates
                        .Contains(_selectedState.SelectedStateSave) == true;

                    if (!isUncategorized)
                    {
                        _stateTreeViewRightClickService.RenameStateClick();

                    }
                    e.Handled = true;

                }
                else if (_selectedState.SelectedStateCategorySave != null)
                {
                    _stateTreeViewRightClickService.RenameCategoryClick();
                    e.Handled = true;

                }
            }
            else if (_hotkeyManager.Delete.IsPressed(e))
            {
                if (_selectedState.SelectedStateSave != null)
                {
                    var isDefault = _selectedState.SelectedElement?.DefaultState == _selectedState.SelectedStateSave;

                    if (!isDefault)
                    {
                        _stateTreeViewRightClickService.DeleteStateClick();

                    }
                    e.Handled = true;

                }
                else if (_selectedState.SelectedStateCategorySave != null)
                {
                    _stateTreeViewRightClickService.DeleteCategoryClick();
                    e.Handled = true;

                }
            }
            else if(_hotkeyManager.Copy.IsPressed(e))
            {
                if(_selectedState.SelectedStateSave != null)
                {
                    var isDefault = _selectedState.SelectedElement?.DefaultState == _selectedState.SelectedStateSave;

                    if(!isDefault)
                    {
                        _copyPasteLogic.OnCopy(CopyType.State);
                    }

                    e.Handled = true;
                }
            }
            else if(_hotkeyManager.Paste.IsPressed(e))
            {
                _copyPasteLogic.OnPaste(CopyType.State);
                e.Handled = true;
            }
        }

        private void TreeViewInstance_PreviewMouseRightButtonDown(object? sender, MouseButtonEventArgs e)
        {
            // Find the TreeViewItem that was clicked
            var clickedItem = VisualUpwardSearch<TreeViewItem>(e.OriginalSource as DependencyObject);

            if (clickedItem != null)
            {
                // Select the clicked item
                clickedItem.IsSelected = true;
                e.Handled = true; // Prevent further processing if necessary
            }
        }

        // Helper method to search upward in the visual tree for the specified type
        private static T VisualUpwardSearch<T>(DependencyObject source) where T : DependencyObject
        {
            while (source != null && source.GetType() != typeof(T))
            {
                source = VisualTreeHelper.GetParent(source);
            }
            return source as T;
        }
    }
}
