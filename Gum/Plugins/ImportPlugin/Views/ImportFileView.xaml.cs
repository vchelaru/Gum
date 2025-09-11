using Gum.Plugins.ImportPlugin.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Gum.Services;
using Gum.Services.Dialogs;

namespace Gum.Plugins.ImportPlugin.Views
{
    /// <summary>
    /// Interaction logic for ImportFileView.xaml
    /// </summary>
    public partial class ImportFileView : Window
    {
        public ImportFileViewModel ViewModel => DataContext as ImportFileViewModel;

        public ImportFileView()
        {
            InitializeComponent();
        }

        private void DoAcceptLogic()
        {
            var selectedItem = ViewModel.SelectedListBoxItem;

            if (!string.IsNullOrEmpty(selectedItem))
            {
                ViewModel.SelectedFiles.Clear();
                ViewModel.SelectedFiles.Add(ViewModel.ContentFolder + ViewModel.SelectedListBoxItem);
                this.DialogResult = true;
            }
            else
            {
                Locator.GetRequiredService<IDialogService>().ShowMessage("Select a file or click the Browse button");
            }
        }

        private void OkButtonClicked(object sender, RoutedEventArgs e)
        {
            DoAcceptLogic();
        }


        private void CancelButtonClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    DoAcceptLogic();
                    break;
                case Key.Escape:
                    this.DialogResult = false;
                    break;
            }
        }

        private void TextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down:
                    e.Handled = true;
                    {
                        var index = ViewModel.FilteredFileList.IndexOf(ViewModel.SelectedListBoxItem);

                        if (index < ViewModel.FilteredFileList.Count - 1)
                        {
                            ViewModel.SelectedListBoxItem = ViewModel.FilteredFileList[index + 1];
                        }
                    }
                    break;
                case Key.Up:
                    e.Handled = true;
                    {
                        var index = ViewModel.FilteredFileList.IndexOf(ViewModel.SelectedListBoxItem);

                        if (index > 0)
                        {
                            ViewModel.SelectedListBoxItem = ViewModel.FilteredFileList[index - 1];
                        }
                    }
                    break;
            }
        }

        private void ListBoxInstance_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBoxInstance.ScrollIntoView(ViewModel.SelectedListBoxItem);

        }

        private void HandleBrowseClicked(object sender, RoutedEventArgs e)
        {
            // add externally built file, add external file, add built file
            OpenFileDialog openFileDialog = new OpenFileDialog();

            if(!string.IsNullOrWhiteSpace( ViewModel.BrowseFileFilter))
            {
                openFileDialog.Filter = ViewModel.BrowseFileFilter;
            }

                // false for now. Components support it, but screens don't, and we're going
                // to use this view on both
            //openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ViewModel.SelectedFiles.Clear();
                ViewModel.SelectedFiles.AddRange(openFileDialog.FileNames);
                this.DialogResult = true;
            }
        }



    }
}
