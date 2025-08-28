using System.Windows;

namespace Gum.Services.Dialogs;

public interface IWindowDialogService
{
    void ShowMessage(string message);
    T CreateWindow<T>() where T : Window, new();
}

public class WindowDialogService : IWindowDialogService
{
    public void ShowMessage(string message) => MessageBox.Show(message);
    public T CreateWindow<T>() where T : Window, new() => new();

}