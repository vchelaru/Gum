using System.Windows.Controls;
using Gum.Services.Dialogs;

namespace Gum.Dialogs;

/// <summary>
/// [Dialog(typeof(ThemingDialogViewModel))] is required, not just naming-convention sugar: the
/// view model lives in the headless Gum.Presentation assembly, so <see cref="DialogViewResolver"/>
/// can only pair it with this view via the explicit attribute (naming-convention matching only
/// pairs types found within the same scanned assembly).
/// </summary>
[Dialog(typeof(ThemingDialogViewModel))]
public partial class ThemingDialogView : UserControl
{
    public ThemingDialogView()
    {
        InitializeComponent();
    }
}