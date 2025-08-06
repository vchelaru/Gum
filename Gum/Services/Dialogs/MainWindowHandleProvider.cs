using System;

namespace Gum.Services.Dialogs;

public interface IMainWindowHandleProvider
{
    IntPtr MainWindowHandle { get; }
}

internal class MainFormWindowHandleProvider : IMainWindowHandleProvider
{
    private Lazy<MainWindow> MainWindow { get; }

    public IntPtr MainWindowHandle => MainWindow.Value.Handle;

    public MainFormWindowHandleProvider(Lazy<MainWindow> mainWindow)
    {
        MainWindow = mainWindow;
    }
}