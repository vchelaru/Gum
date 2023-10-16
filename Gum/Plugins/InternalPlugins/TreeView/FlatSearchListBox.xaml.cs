using Gum.Plugins.InternalPlugins.TreeView.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Gum.Plugins.InternalPlugins.TreeView
{
    /// <summary>
    /// Interaction logic for FlatSearchListBox.xaml
    /// </summary>
    public partial class FlatSearchListBox : UserControl
    {

        public event Action<SearchItemViewModel> SelectSearchNode;

        public FlatSearchListBox()
        {
            InitializeComponent();
        }

        private void FlatList_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var objectPushed = e.OriginalSource;
            var frameworkElementPushed = (objectPushed as FrameworkElement);

            var searchNodePushed = frameworkElementPushed?.DataContext as SearchItemViewModel;
            SelectSearchNode(searchNodePushed);
        }
    }
}
