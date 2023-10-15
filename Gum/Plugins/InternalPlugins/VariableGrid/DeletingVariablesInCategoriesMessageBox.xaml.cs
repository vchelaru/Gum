using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace Gum.Plugins.VariableGrid
{
    /// <summary>
    /// Interaction logic for DeletingVariablesInCategoriesMessageBox.xaml
    /// </summary>
    public partial class DeletingVariablesInCategoriesMessageBox : Window
    {
        public DeletingVariablesInCategoriesMessageBox()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void HandleOkClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
