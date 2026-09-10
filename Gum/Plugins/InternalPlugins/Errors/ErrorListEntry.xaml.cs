using System.Windows.Controls;
using System.Windows.Navigation;
using Gum.Services;

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
            // View code-behind has no constructor injection; the WPF view goes away at cutover.
            Locator.GetRequiredService<IFileSystemRevealService>().OpenUrl(e.Uri.AbsoluteUri);
            e.Handled = true;
        }
    }
}
