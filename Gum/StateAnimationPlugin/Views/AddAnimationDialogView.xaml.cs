using Gum.Services.Dialogs;
using StateAnimationPlugin.ViewModels;
using System.Windows.Controls;

namespace StateAnimationPlugin.Views;

/// <summary>
/// Interaction logic for AddAnimationDialogView.xaml
/// </summary>
[Dialog(typeof(AddAnimationDialogViewModel))]
public partial class AddAnimationDialogView : UserControl
{
    public AddAnimationDialogView()
    {
        InitializeComponent();
    }
}
