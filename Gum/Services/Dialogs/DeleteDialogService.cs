using Gum.Gui.Windows;
using Gum.Plugins;
using System;

namespace Gum.Services.Dialogs;

/// <summary>
/// WPF implementation of <see cref="IDeleteDialogService"/>. Owns the standalone
/// <see cref="DeleteOptionsWindow"/> and the plugin-host calls that compose and confirm it,
/// keeping <see cref="Managers.DeleteLogic"/> free of any WPF reference (ADR-0005). This is
/// shell/view code, so depending on the concrete plugin host (<see cref="IPluginManager"/>)
/// here is legitimate.
/// </summary>
internal class DeleteDialogService : IDeleteDialogService
{
    private readonly IPluginManager _pluginManager;

    public DeleteDialogService(IPluginManager pluginManager)
    {
        _pluginManager = pluginManager;
    }

    public IDeleteDialogResult ShowDeleteDialog(string title, string message, Array objectsToDelete)
    {
        DeleteOptionsWindow window = new()
        {
            Title = title,
            Message = message,
            ObjectsToDelete = objectsToDelete,
        };

        // Let plugins inject their checkboxes/options (e.g. "Delete XML file?") before the
        // dialog is shown — this fires the DeleteOptionsWindowShow plugin event.
        _pluginManager.ShowDeleteDialog(window, objectsToDelete);

        bool? result = window.ShowDialog();

        return new DeleteDialogResult(window, result);
    }

    public void NotifyConfirmed(IDeleteDialogResult result, Array objectsToDelete)
    {
        DeleteOptionsWindow window = ((DeleteDialogResult)result).Window;
        _pluginManager.DeleteConfirmed(window, objectsToDelete);
    }

    /// <summary>
    /// Concrete result returned by <see cref="ShowDeleteDialog"/>. Keeps a reference to the WPF
    /// window so <see cref="NotifyConfirmed"/> can hand it back to plugins, while exposing only
    /// the framework-neutral <see cref="IDeleteDialogResult.Result"/> to callers.
    /// </summary>
    private sealed class DeleteDialogResult : IDeleteDialogResult
    {
        public DeleteDialogResult(DeleteOptionsWindow window, bool? result)
        {
            Window = window;
            Result = result;
        }

        public DeleteOptionsWindow Window { get; }

        public bool? Result { get; }
    }
}
