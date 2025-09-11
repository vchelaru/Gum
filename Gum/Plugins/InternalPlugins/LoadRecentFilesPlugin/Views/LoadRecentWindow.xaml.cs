using Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Gum.Services.Dialogs;

namespace Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin.Views
{
    /// <summary>
    /// Interaction logic for LoadRecentWindow.xaml
    /// </summary>
    [Dialog(typeof(LoadRecentViewModel))]
    public partial class LoadRecentWindow : UserControl
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

        //private void SearchBar_ClearSearchButtonClicked()
        //{
        //    ViewModel.SearchBoxText = String.Empty;
        //}

        //private void SearchBar_ArrowKeyPushed(Key key)
        //{

        //}

        //private void SearchBar_EnterPressed()
        //{
        //    if (ViewModel.SelectedItem != null)
        //    {
        //        DialogResult = true;
        //    }
        //}

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.CanExecuteAffirmative())
            {
                ViewModel.AffirmativeCommand.Execute(null);
            }
        }

        //private void SearchBar_EscapePressed()
        //{
        //    DialogResult = false;
        //}
    }
}
