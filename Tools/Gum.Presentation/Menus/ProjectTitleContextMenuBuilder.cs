using System;
using System.Collections.Generic;
using System.IO;
using Gum.Services;
using Gum.ViewModels;

namespace Gum.Menus;

/// <summary>
/// Builds the project title's right-click menu ("View in explorer" / "Copy full path"), the
/// Avalonia head's twin of the WPF <c>TitleFilePathDisplay</c> context menu.
/// </summary>
public static class ProjectTitleContextMenuBuilder
{
    /// <summary>Builds the menu for <paramref name="fullPath"/>, disabling either item it can't act on.</summary>
    public static List<ContextMenuItemViewModel> Build(
        string? fullPath,
        IFileSystemRevealService revealService,
        IClipboardService clipboardService,
        Func<string, bool>? fileExists = null)
    {
        fileExists ??= File.Exists;
        bool hasPath = !string.IsNullOrEmpty(fullPath);
        bool canReveal = hasPath && fileExists(fullPath!);

        return new List<ContextMenuItemViewModel>
        {
            new ContextMenuItemViewModel
            {
                Text = "View in explorer",
                IsEnabled = canReveal,
                Action = canReveal ? () => revealService.RevealFile(fullPath!) : null,
            },
            new ContextMenuItemViewModel
            {
                Text = "Copy full path",
                IsEnabled = hasPath,
                Action = hasPath ? () => clipboardService.SetText(fullPath!) : null,
            },
        };
    }
}
