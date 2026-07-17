using System.Windows.Controls;
using Gum.Services.Dialogs;

namespace Gum.Dialogs;

/// <summary>
/// [Dialog(typeof(ExposeColorDialogViewModel))] is required, not just naming-convention sugar: the
/// view model lives in the headless Gum.Presentation assembly, so <see cref="DialogViewResolver"/>
/// can only pair it with this view via the explicit attribute (naming-convention matching only
/// pairs types found within the same scanned assembly).
/// </summary>
[Dialog(typeof(ExposeColorDialogViewModel))]
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
