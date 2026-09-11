using Gum.Extensions;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;
using Gum.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Gum.Plugins.InternalPlugins.StatePlugin.Views
{
    /// <summary>
    /// The WPF states tree. Renders the shared right-click menu and passes key presses to the shared
    /// <see cref="StateTreeKeyboardHandler"/>; the tree itself binds to <see cref="StateTreeViewModel"/>.
    /// </summary>
    public partial class StateTreeView : UserControl
    {
        private readonly IStateTreeViewRightClickService _rightClickService;
        private readonly StateTreeKeyboardHandler _keyboardHandler;
        private readonly ContextMenu _contextMenu;

        public StateTreeView(StateTreeViewModel viewModel,
            IStateTreeViewRightClickService rightClickService,
            StateTreeKeyboardHandler keyboardHandler)
        {
            _rightClickService = rightClickService;
            _keyboardHandler = keyboardHandler;
            InitializeComponent();

            _contextMenu = new ContextMenu();
            TreeViewInstance.ContextMenu = _contextMenu;
            _rightClickService.MenuItemsChanged += RebuildContextMenu;
            // Nothing applies to the selection: suppress rather than open an empty popup.
            ContextMenuOpening += (_, args) =>
            {
                if (_contextMenu.Items.Count == 0)
                {
                    args.Handled = true;
                }
            };

            DataContext = viewModel;
        }

        private void RebuildContextMenu()
        {
            _contextMenu.Items.Clear();
            foreach (ContextMenuItemViewModel item in _rightClickService.MenuItems)
            {
                _contextMenu.Items.Add(item.ToMenuItem());
            }
        }

        private void TreeView_SelectedItemChanged(object? sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Selection flows through the items' two-way IsSelected binding to StateTreeViewModel.
        }

        private void TreeViewInstance_KeyDown(object? sender, KeyEventArgs e)
        {
            if (_keyboardHandler.HandleKeyDown(e.ToGumKeyEventArgs()))
            {
                e.Handled = true;
            }
        }

        private void TreeViewInstance_PreviewMouseRightButtonDown(object? sender, MouseButtonEventArgs e)
        {
            // Select the row under a right-click before its menu opens.
            TreeViewItem? clickedItem = VisualUpwardSearch<TreeViewItem>(e.OriginalSource as DependencyObject);

            if (clickedItem != null)
            {
                clickedItem.IsSelected = true;
                e.Handled = true;
            }
        }

        private static T? VisualUpwardSearch<T>(DependencyObject? source) where T : DependencyObject
        {
            while (source != null && source.GetType() != typeof(T))
            {
                source = VisualTreeHelper.GetParent(source);
            }
            return source as T;
        }
    }
}
