using System.Drawing;
using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Mvvm;
using Gum.Settings;
using Gum.ViewModels;

namespace Gum.Avalonia.Shell;

/// <summary>
/// State of the main window that is not a plugin tab: title, progress text, and the persisted
/// window placement. The framework-neutral counterpart of the WPF <c>MainWindowViewModel</c>.
/// </summary>
public class ShellViewModel : ViewModel, IRecipient<ApplicationTeardownMessage>
{
    private readonly IWritableOptions<LayoutSettings> _layoutSettings;

    /// <summary>Creates the view model and registers for teardown so placement is saved.</summary>
    public ShellViewModel(IMessenger messenger, IWritableOptions<LayoutSettings> layoutSettings)
    {
        _layoutSettings = layoutSettings;
        ProgressText = "";
        Left = 0;
        Top = 0;
        Width = WindowSettings.DefaultWidth;
        Height = WindowSettings.DefaultHeight;
        WindowState = GumWindowState.Normal;
        messenger.RegisterAll(this);
    }

    /// <summary>The loaded project's full file name; null while no project is open.</summary>
    public string? ProjectFilePath { get => Get<string?>(); set => Set(value); }

    /// <summary>
    /// The window title, which the taskbar shows too: the project's name without its path or
    /// extension, or "Gum" while no project is open.
    /// </summary>
    [DependsOn(nameof(ProjectFilePath))]
    public string Title => string.IsNullOrEmpty(ProjectFilePath) ? "Gum" : Path.GetFileNameWithoutExtension(ProjectFilePath);

    /// <summary>Text for the status bar's progress slot; empty when idle.</summary>
    public string ProgressText { get => Get<string>(); set => Set(value); }

    /// <summary>Window left edge in device-independent units.</summary>
    public double Left { get => Get<double>(); set => Set(value); }

    /// <summary>Window top edge in device-independent units.</summary>
    public double Top { get => Get<double>(); set => Set(value); }

    /// <summary>Window width in device-independent units.</summary>
    public double Width { get => Get<double>(); set => Set(value); }

    /// <summary>Window height in device-independent units.</summary>
    public double Height { get => Get<double>(); set => Set(value); }

    /// <summary>Normal or maximized.</summary>
    public GumWindowState WindowState { get => Get<GumWindowState>(); set => Set(value); }

    /// <summary>True until the saved placement has been applied, so the first show can center.</summary>
    public bool IsFirstLaunch { get; private set; } = true;

    /// <summary>
    /// Applies the saved placement, clamped into <paramref name="workingArea"/> (the screen the
    /// saved position lands on, in the same units). Called by the window once it can query screens.
    /// </summary>
    public void LoadWindowSettings(WindowSettings settings, Rectangle workingArea)
    {
        if (WindowSettingsLogic.IsFirstLaunch(settings))
        {
            IsFirstLaunch = true;
            return;
        }

        IsFirstLaunch = false;
        int width = (int)System.Math.Min(settings.Width, workingArea.Width);
        int height = (int)System.Math.Min(settings.Height, workingArea.Height);
        int left = System.Math.Max(workingArea.Left, System.Math.Min((int)settings.Left!.Value, workingArea.Right - width));
        int top = System.Math.Max(workingArea.Top, System.Math.Min((int)settings.Top!.Value, workingArea.Bottom - height));

        Left = left;
        Top = top;
        Width = width;
        Height = height;
        WindowState = settings.IsMaximized ? GumWindowState.Maximized : GumWindowState.Normal;
    }

    /// <summary>The placement to persist, from the current values.</summary>
    public WindowSettings ToWindowSettings() => WindowSettingsLogic.WithUsableSize(new WindowSettings
    {
        Left = Left,
        Top = Top,
        Width = Width,
        Height = Height,
        IsMaximized = WindowState == GumWindowState.Maximized,
    });

    void IRecipient<ApplicationTeardownMessage>.Receive(ApplicationTeardownMessage message)
    {
        message.OnTearDown(() => _layoutSettings.Update(l => l.MainWindow = ToWindowSettings()));
    }
}
