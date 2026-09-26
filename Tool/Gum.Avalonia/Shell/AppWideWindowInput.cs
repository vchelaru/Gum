using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Gum.Avalonia.Services;
using Gum.Input;
using Gum.Managers;
using Gum.Services;

namespace Gum.Avalonia.Shell;

/// <summary>
/// The main window's app-wide behavior, in one place so a headless test window can run the same
/// code: key presses anywhere in the window go to the hotkey manager before the focused control
/// sees them, and the UI base font size reaches the window's text and the Fluent-styled controls.
/// </summary>
internal static class AppWideWindowInput
{
    /// <summary>Routes every key press in <paramref name="window"/> through <see cref="IHotkeyManager.PreviewKeyDownAppWide"/>.</summary>
    public static void RouteHotkeys(Window window, IHotkeyManager hotkeyManager, AvaloniaModifierKeyState modifierKeyState)
    {
        window.AddHandler(InputElement.KeyDownEvent, (_, e) =>
        {
            modifierKeyState.Current = e.KeyModifiers;
            GumKeyEventArgs keyArgs = e.ToGumKeyEventArgs();
            // Ctrl+= / Ctrl+- zoom the whole app unless a canvas that owns them has focus (phase 50).
            hotkeyManager.PreviewKeyDownAppWide(keyArgs, enableEntireAppZoom: CameraZoomScope.IsEntireAppZoomEnabledFor(e.Source));
            e.Handled = keyArgs.Handled;
        }, RoutingStrategies.Tunnel);
        window.AddHandler(InputElement.KeyUpEvent, (_, e) => modifierKeyState.Current = e.KeyModifiers, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Applies the base font size to <paramref name="window"/> now and whenever it changes. Dispose
    /// the result to stop following it.
    /// </summary>
    public static IDisposable FollowBaseFontSize(Window window, IAppScaleProvider appScaleProvider)
    {
        ApplyBaseFontSize(window, appScaleProvider.BaseFontSize);
        if (appScaleProvider is not AvaloniaAppScaleProvider scale)
        {
            return new Unsubscriber(() => { });
        }
        Action handler = () => ApplyBaseFontSize(window, scale.BaseFontSize);
        scale.BaseFontSizeChanged += handler;
        return new Unsubscriber(() => scale.BaseFontSizeChanged -= handler);
    }

    // The base font size reaches plain text by inheritance from the window, and the Fluent-styled
    // controls (menus, combo boxes, tabs) through the theme's font size resource.
    private static void ApplyBaseFontSize(Window window, double size)
    {
        window.FontSize = size;
        Themes.FrbThemeResources.SetBaseFontSize(global::Avalonia.Application.Current!.Resources, size);
    }

    private sealed class Unsubscriber : IDisposable
    {
        private Action? _unsubscribe;

        public Unsubscriber(Action unsubscribe) => _unsubscribe = unsubscribe;

        public void Dispose()
        {
            _unsubscribe?.Invoke();
            _unsubscribe = null;
        }
    }
}
