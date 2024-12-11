using Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;
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
        StateTreeViewModel ViewModel => DataContext as StateTreeViewModel;

        public event EventHandler SelectedItemChanged;

        public object SelectedItem => TreeViewInstance.SelectedItem;

        public ContextMenu TreeViewContextMenu
        {
            get => TreeViewInstance.ContextMenu;
        }

        public StateTreeView(StateTreeViewModel viewModel)
        {
            InitializeComponent();
            TreeViewInstance.ContextMenu = new ContextMenu();
            this.DataContext = viewModel;
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //var treeView = sender as System.Windows.Controls.TreeView;

            //ViewModel.SelectedItem = treeView.SelectedItem as StateTreeViewItem;

            //SelectedItemChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
