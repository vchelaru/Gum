using System.Windows.Controls;

namespace Gum.Services.Dialogs;

/// <summary>
/// [Dialog(typeof(MessageDialogViewModel))] is required, not just naming-convention sugar: the
/// view model lives in the headless Gum.Presentation assembly, so <see cref="DialogViewResolver"/>
/// can only pair it with this view via the explicit attribute (naming-convention matching only
/// pairs types found within the same scanned assembly).
/// </summary>
[Dialog(typeof(MessageDialogViewModel))]
public partial class MessageDialogView : UserControl
{
    public MessageDialogView()
    {
        InitializeComponent();
    }
}