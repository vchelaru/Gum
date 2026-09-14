using System.Windows.Controls;

namespace Gum.Plugins.AlignmentButtons
{
    /// <summary>
    /// The Alignment tab. Its DataContext is the AlignmentViewModel the shared plugin hands to the tab
    /// manager (TabViewRegistry sets it).
    /// </summary>
    public partial class AlignmentPluginControl : UserControl
    {
        public AlignmentPluginControl()
        {
            InitializeComponent();
        }
    }
}
