using System;
using System.Drawing;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Gum.Settings;
using Gum.ViewModels;

namespace Gum.Avalonia.Shell;

/// <summary>
/// Keeps a window's placement in <see cref="ShellViewModel"/> as the user moves, resizes or
/// maximizes it, and applies the placement the last session saved. The shell view model writes it
/// to the layout settings at teardown.
/// </summary>
public sealed class WindowPlacementTracker
{
    private readonly Window _window;
    private readonly ShellViewModel _shell;
    private readonly IWritableOptions<LayoutSettings> _layoutSettings;
    private readonly Func<bool> _isSuspended;

    /// <param name="isSuspended">
    /// True while the window's placement is not the user's (an unattended run parks it off-screen),
    /// so moves and resizes are not recorded.
    /// </param>
    public WindowPlacementTracker(Window window, ShellViewModel shell, IWritableOptions<LayoutSettings> layoutSettings, Func<bool> isSuspended)
    {
        _window = window;
        _shell = shell;
        _layoutSettings = layoutSettings;
        _isSuspended = isSuspended;

        window.PositionChanged += (_, _) =>
        {
            if (IsTracking)
            {
                _shell.Left = window.Position.X;
                _shell.Top = window.Position.Y;
            }
        };
        window.SizeChanged += (_, _) =>
        {
            if (IsTracking)
            {
                _shell.Width = window.Bounds.Width;
                _shell.Height = window.Bounds.Height;
            }
        };
        window.PropertyChanged += (_, e) =>
        {
            if (e.Property == Window.WindowStateProperty)
            {
                _shell.WindowState = window.WindowState == WindowState.Maximized ? GumWindowState.Maximized : GumWindowState.Normal;
            }
        };
    }

    private bool IsTracking => _window.WindowState == WindowState.Normal && !_isSuspended();

    /// <summary>
    /// Moves and sizes the window to the saved placement, clamped onto the screen it lands on, or
    /// centers it when nothing has been saved yet. Call once the window is open.
    /// </summary>
    public void Restore()
    {
        WindowSettings saved = _layoutSettings.CurrentValue.MainWindow;
        PixelPoint probe = saved.Left is double left && saved.Top is double top ? new PixelPoint((int)left, (int)top) : _window.Position;
        Screen? screen = _window.Screens.ScreenFromPoint(probe) ?? _window.Screens.Primary;
        Rectangle workingArea = screen == null
            ? new Rectangle(0, 0, (int)WindowSettings.DefaultWidth, (int)WindowSettings.DefaultHeight)
            : new Rectangle(screen.WorkingArea.X, screen.WorkingArea.Y, screen.WorkingArea.Width, screen.WorkingArea.Height);

        _shell.LoadWindowSettings(saved, workingArea);
        if (_shell.IsFirstLaunch)
        {
            _window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        _window.Position = new PixelPoint((int)_shell.Left, (int)_shell.Top);
        _window.Width = _shell.Width;
        _window.Height = _shell.Height;
        _window.WindowState = _shell.WindowState == GumWindowState.Maximized ? WindowState.Maximized : WindowState.Normal;
    }
}
