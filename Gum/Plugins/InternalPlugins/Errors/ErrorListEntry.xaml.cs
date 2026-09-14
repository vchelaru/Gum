using System.Windows.Controls;

namespace Gum.Plugins.Errors
{
    /// <summary>
    /// One row of the Errors tab. The error code links to its help page through
    /// <see cref="AllErrorsViewModel.OpenHelpCommand"/>.
    /// </summary>
    public partial class ErrorListEntry : UserControl
    {
        public ErrorListEntry()
        {
            InitializeComponent();
        }
    }
}
