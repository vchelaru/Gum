using System.Windows.Controls;

namespace Gum.Controls
{
    /// <summary>
    /// Shows the "Editing state X" / "Displaying custom (animated) state" banner. Its bindings are
    /// relative, so it inherits whatever DataContext the host control sets - that DataContext's
    /// type must expose HasStateInformation/StateInformation/StateBackground (see MainControlViewModel,
    /// AlignmentViewModel).
    /// </summary>
    public partial class StateEditingIndicatorBar : UserControl
    {
        public StateEditingIndicatorBar()
        {
            InitializeComponent();
        }
    }
}
