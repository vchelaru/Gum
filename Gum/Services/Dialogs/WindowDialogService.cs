using System.Windows;

namespace Gum.Services.Dialogs;

public interface IWindowDialogService
{
    void ShowMessage(string message);
    bool? ShowDialog(Window window);
}

public class WindowDialogService : IWindowDialogService
{
    public void ShowMessage(string message) => MessageBox.Show(message);
    public bool? ShowDialog(Window window) => window.ShowDialog();
}