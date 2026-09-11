using System;
using Gum.Managers;
using Gum.Services.Dialogs;

namespace Gum.Avalonia.Dialogs;

/// <summary>
/// Delete confirmation for this head: a <see cref="DeleteOptionsDialogViewModel"/> that plugins fill
/// with their options through <see cref="IDeletePluginNotifier.ShowDeleteOptions"/>, shown through
/// <see cref="IDialogService"/>.
/// </summary>
public class AvaloniaDeleteDialogService : IDeleteDialogService
{
    private readonly IDialogService _dialogService;
    private readonly IDeletePluginNotifier _deletePluginNotifier;

    /// <summary>Creates the service over the dialog service and the plugin host.</summary>
    public AvaloniaDeleteDialogService(IDialogService dialogService, IDeletePluginNotifier deletePluginNotifier)
    {
        _dialogService = dialogService;
        _deletePluginNotifier = deletePluginNotifier;
    }

    /// <inheritdoc/>
    public IDeleteDialogResult ShowDeleteDialog(string title, string message, Array objectsToDelete)
    {
        DeleteOptionsDialogViewModel options = new DeleteOptionsDialogViewModel
        {
            Title = title,
            Message = message,
        };
        _deletePluginNotifier.ShowDeleteOptions(options, objectsToDelete);

        bool confirmed = _dialogService.Show(options);
        return new DeleteDialogResult(options, confirmed);
    }

    /// <inheritdoc/>
    public void NotifyConfirmed(IDeleteDialogResult result, Array objectsToDelete) =>
        _deletePluginNotifier.ConfirmDeleteOptions(((DeleteDialogResult)result).Options, objectsToDelete);

    private sealed class DeleteDialogResult : IDeleteDialogResult
    {
        public DeleteDialogResult(DeleteOptionsDialogViewModel options, bool confirmed)
        {
            Options = options;
            Result = confirmed;
        }

        public DeleteOptionsDialogViewModel Options { get; }

        public bool? Result { get; }
    }
}
