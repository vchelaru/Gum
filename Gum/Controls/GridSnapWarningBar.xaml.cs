using System.Windows.Controls;

namespace Gum.Controls
{
    /// <summary>
    /// Shows the "Snap to Grid: X uses non-pixel units..." warning above the main editor canvas.
    /// Its bindings are relative, so it inherits whatever DataContext the host control sets - that
    /// DataContext's type must expose HasGridSnapWarning/GridSnapWarningText (see EditorViewModel).
    /// Placeholder styling - colors/layout are expected to be revisited in a follow-up visual pass.
    /// </summary>
    public partial class GridSnapWarningBar : UserControl
    {
        public GridSnapWarningBar()
        {
            InitializeComponent();
        }
    }
}
