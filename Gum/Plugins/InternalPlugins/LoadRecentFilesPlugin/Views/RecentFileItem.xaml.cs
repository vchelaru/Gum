using Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin.ViewModels;
using System;
using System.Collections.Generic;
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

namespace Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin.Views
{
    /// <summary>
    /// Interaction logic for RecentFileItem.xaml
    /// </summary>
    public partial class RecentFileItem : UserControl
    {
        RecentItemViewModel ViewModel => DataContext as RecentItemViewModel;

        public RecentFileItem()
        {
            InitializeComponent();
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //ViewModel.IsFavorite = !ViewModel.IsFavorite;
            //e.Handled = true;
        }

        private void Image_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel.IsFavorite = !ViewModel.IsFavorite;
            e.Handled = true;
        }
    }
}
