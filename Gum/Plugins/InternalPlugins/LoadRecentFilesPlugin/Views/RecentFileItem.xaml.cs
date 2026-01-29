using Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

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

        private void Image_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            //ViewModel.IsFavorite = !ViewModel.IsFavorite;
            //e.Handled = true;
        }

        private void Image_PreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            ViewModel.IsFavorite = !ViewModel.IsFavorite;
            e.Handled = true;
        }
    }
}
