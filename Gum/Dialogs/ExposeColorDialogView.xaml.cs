using System.Windows.Controls;

namespace Gum.Dialogs;

public partial class ExposeColorDialogView : UserControl
{
    public ExposeColorDialogView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            BaseNameTextBox.Focus();
            BaseNameTextBox.SelectAll();
        };
    }
}
