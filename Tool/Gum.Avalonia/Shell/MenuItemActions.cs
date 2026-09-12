using System;
using Avalonia.Threading;

namespace Gum.Avalonia.Shell;

/// <summary>
/// Runs a menu item's action once its click has finished routing, so the menu has closed and
/// released its light-dismiss before the action runs. Many actions show a dialog synchronously
/// (a nested dispatcher loop); invoked inside the click, the dialog opened under a still-open
/// menu, whose light-dismiss swallowed the first click into the dialog.
/// </summary>
public static class MenuItemActions
{
    /// <summary>Posts <paramref name="action"/> to run after the current input event completes.</summary>
    public static void InvokeAfterClose(Action action) => Dispatcher.UIThread.Post(action);
}
