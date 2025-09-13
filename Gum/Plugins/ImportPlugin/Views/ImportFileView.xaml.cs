using Gum.Plugins.ImportPlugin.ViewModel;
using System.Windows.Controls;
using System.Windows.Input;
using Gum.Services.Dialogs;

namespace Gum.Plugins.ImportPlugin.Views
{
    /// <summary>
    /// Interaction logic for ImportFileView.xaml
    /// </summary>
    [Dialog(typeof(ImportComponentDialog))]
    [Dialog(typeof(ImportScreenDialog))]
    [Dialog(typeof(ImportBehaviorDialog))]

    public partial class ImportFileView : UserControl
    {
        public ImportBaseDialogViewModel ViewModel => DataContext as ImportBaseDialogViewModel;

        public ImportFileView()
        {
            InitializeComponent();
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            int current = ListBoxInstance.SelectedIndex;
            int max = ListBoxInstance.Items.Count - 1;

            int? direction = e.Key switch
            {
                Key.Down when current < max => 1,
                Key.Up when current > 0 => -1,
                _ => null
            };

            if (direction.HasValue)
            {
                ListBoxInstance.SelectedIndex += direction.Value;
            }

        }
    }
}
