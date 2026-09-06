using System.Windows.Controls;
using System.Windows.Input;

namespace Gum.Plugins.Errors
{
    /// <summary>
    /// Interaction logic for ErrorDisplay.xaml
    /// </summary>
    public partial class ErrorDisplay : UserControl
    {
        public ErrorDisplay()
        {
            InitializeComponent();
        }

        private void HandleItemPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item)
            {
                item.IsSelected = true;
            }
        }
    }
}
