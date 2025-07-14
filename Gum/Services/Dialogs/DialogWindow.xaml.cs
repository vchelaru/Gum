using System.Windows;
using System.Windows.Input;

namespace Gum.Services.Dialogs;

public partial class DialogWindow : Window
{
    public DialogWindow()
    {
        InitializeComponent();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (DataContext is DialogViewModel vm)
            {
                vm.NegativeCommand.Execute(null);
            }
            else
            {
                Close();
            }
        }
    }
}