using Gum.Services.Dialogs;
using StateAnimationPlugin.ViewModels;
using System.Windows.Controls;

namespace StateAnimationPlugin.Views;

/// <summary>
/// Interaction logic for SubAnimationSelectionWindow.xaml
/// </summary>
[Dialog(typeof(SubAnimationSelectionDialogViewModel))]
public partial class SubAnimationSelectionWindow : UserControl
{
    public SubAnimationSelectionWindow()
    {
        InitializeComponent();
    }

}
