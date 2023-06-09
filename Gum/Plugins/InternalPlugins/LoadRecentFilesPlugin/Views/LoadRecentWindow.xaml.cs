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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin.Views
{
    /// <summary>
    /// Interaction logic for LoadRecentWindow.xaml
    /// </summary>
    public partial class LoadRecentWindow : Window
    {
        LoadRecentViewModel ViewModel => DataContext as LoadRecentViewModel;

        public LoadRecentWindow()
        {
            InitializeComponent();

            this.Loaded += HandleLoaded;
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            //this.MoveToCursor();

            //this.SearchBar.FocusTextBox();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedItem != null)
            {
                this.DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void SearchBar_ClearSearchButtonClicked()
        {
            ViewModel.SearchBoxText = String.Empty;
        }

        private void SearchBar_ArrowKeyPushed(Key key)
        {

        }

        private void SearchBar_EnterPressed()
        {
            if (ViewModel.SelectedItem != null)
            {
                DialogResult = true;
            }
        }

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.SelectedItem != null)
            {
                DialogResult = true;
            }
        }

        private void SearchBar_EscapePressed()
        {
            DialogResult = false;
        }
    }
}
