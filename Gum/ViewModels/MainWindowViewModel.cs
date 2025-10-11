using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Controls;
using Gum.Dialogs;
using Gum.Mvvm;

namespace Gum.ViewModels;

public class MainWindowViewModel : ViewModel, IRecipient<ThemeChangedMessage>
{
    public MainPanelViewModel MainPanelViewModel { get; }

    public string? IconSource
    {
        get => Get<string?>();
        set => Set(value);
    }
    public string? Title
    {
        get => Get<string?>();
        set => Set(value);   
    }

    public double Left
    {
        get => Get<double>();
        set => Set(value);
    }

    public double Top
    {
        get => Get<double>();
        set => Set(value);
    }

    public double? Width
    {
        get => Get<double?>();
        set => Set(value);
    }

    public double? Height
    {
        get => Get<double?>();
        set => Set(value);
    }

    public WindowState WindowState
    {
        get => Get<WindowState>();
        set => Set(value);
    }

    public MainWindowViewModel(MainPanelViewModel mainPanelViewModel, IMessenger messenger)
    {
        MainPanelViewModel = mainPanelViewModel;
        IconSource = "pack://application:,,,/GumLogo64.png";
        messenger.RegisterAll(this);
    }
    
    public void LoadWindowSettings()
    {
        var settings = ProjectManager.Self.GeneralSettingsFile;

        // Apply the window position and size settings only if a large enough portion of the
        // window would end up on the screen.
        var workingArea = Screen.GetWorkingArea(settings.MainWindowBounds);
        var intersection = Rectangle.Intersect(settings.MainWindowBounds, workingArea);
        
        if (intersection.Width > 100 && intersection.Height > 100)
        {
            Width = settings.MainWindowBounds.Width;
            Height = settings.MainWindowBounds.Height;
            Left = settings.MainWindowBounds.Left;
            Top = settings.MainWindowBounds.Top;
            WindowState = (WindowState)settings.MainWindowState;
        }
    }

    public void SaveWindowSettings()
    {
        var settings = ProjectManager.Self.GeneralSettingsFile;

        settings.MainWindowBounds = new((int)Left, (int)Top, (int)Width, (int)Height);
        settings.MainWindowState = (FormWindowState)WindowState;

        settings.Save();
    }

    void IRecipient<ThemeChangedMessage>.Receive(ThemeChangedMessage message)
    {
        IconSource = message.settings.Mode switch
        {
            ThemeMode.Light => "pack://application:,,,/GumLogo64Light.png",
            _ => "pack://application:,,,/GumLogo64.png"
        };
    }
}