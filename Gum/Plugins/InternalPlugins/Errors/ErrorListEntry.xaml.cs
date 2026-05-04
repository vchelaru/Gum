using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Gum.Plugins.Errors
{
    /// <summary>
    /// Interaction logic for ErrorListEntry.xaml
    /// </summary>
    public partial class ErrorListEntry : UserControl
    {
        public ErrorListEntry()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            };
            Process.Start(startInfo);
            e.Handled = true;
        }
    }
}
