using Gum.Extensions;
using Gum.Gui.Windows;
using Gum.Plugins;
using System;

namespace Gum.Services.Dialogs;

/// <summary>
/// WPF implementation of <see cref="IDeleteDialogService"/>. Owns the standalone
/// <see cref="DeleteOptionsWindow"/> and the plugin-host calls that compose and confirm it,
/// keeping <see cref="Managers.DeleteLogic"/> free of any WPF reference (ADR-0005). This is
/// shell/view code, so depending on the concrete <see cref="PluginManager"/> here is legitimate:
/// <see cref="IPluginManager"/> moved to the headless Gum.Presentation assembly, so it no longer
/// carries the two WPF-typed <c>ShowDeleteDialog</c>/<c>DeleteConfirmed</c> calls this service needs.
/// </summary>
internal class DeleteDialogService : IDeleteDialogService
{
    private readonly PluginManager _pluginManager;

    public DeleteDialogService(PluginManager pluginManager)
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
        // dialog is shown. Plugins that still add WPF controls use the DeleteOptionsWindowShow event;
        // the rest add neutral options, which are rendered into the same panel.
        _pluginManager.ShowDeleteDialog(window, objectsToDelete);

        DeleteOptionsDialogViewModel options = new()
        {
            Title = title,
            Message = message,
        };
        _pluginManager.ShowDeleteOptions(options, objectsToDelete);
        foreach (DeleteOptionChoiceViewModel choice in options.Choices)
        {
            window.MainStackPanel.Children.Add(choice.ToGroupBox());
        }
        foreach (DeleteOptionCheckboxViewModel checkBox in options.CheckBoxes)
        {
            window.MainStackPanel.Children.Add(checkBox.ToCheckBox());
        }

        bool? result = window.ShowDialog();

        return new DeleteDialogResult(window, options, result);
    }

    public void NotifyConfirmed(IDeleteDialogResult result, Array objectsToDelete)
    {
        DeleteDialogResult typed = (DeleteDialogResult)result;
        _pluginManager.DeleteConfirmed(typed.Window, objectsToDelete);
        _pluginManager.ConfirmDeleteOptions(typed.Options, objectsToDelete);
    }

    /// <summary>
    /// Concrete result returned by <see cref="ShowDeleteDialog"/>. Keeps a reference to the WPF
    /// window so <see cref="NotifyConfirmed"/> can hand it back to plugins, while exposing only
    /// the framework-neutral <see cref="IDeleteDialogResult.Result"/> to callers.
    /// </summary>
    private sealed class DeleteDialogResult : IDeleteDialogResult
    {
        public DeleteDialogResult(DeleteOptionsWindow window, DeleteOptionsDialogViewModel options, bool? result)
        {
            Window = window;
            Options = options;
            Result = result;
        }

        public DeleteOptionsWindow Window { get; }

        public DeleteOptionsDialogViewModel Options { get; }

        public bool? Result { get; }
    }
}
