using System.Windows.Controls;
using Gum.Services.Dialogs;

namespace Gum.Dialogs;

/// <summary>
/// [Dialog(typeof(NewProjectDialogViewModel))] is required because the view model lives in the
/// headless Gum.Presentation assembly — naming-convention matching only pairs types found within
/// the same scanned assembly.
/// </summary>
[Dialog(typeof(NewProjectDialogViewModel))]
public partial class NewProjectDialogView : UserControl
{
    public NewProjectDialogView()
    {
        InitializeComponent();
    }
}
