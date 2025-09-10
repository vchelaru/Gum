using Gum.Dialogs;
using Gum.Services.Dialogs;
using System.Windows.Controls;

namespace Gum.Views;

/// <summary>
/// Interaction logic for DisplayReferencesDialogView.xaml
/// </summary>
[Dialog(typeof(DisplayReferencesDialog))]
public partial class DisplayReferencesDialogView : UserControl
{
    public DisplayReferencesDialogView()
    {
        InitializeComponent();
    }
}
