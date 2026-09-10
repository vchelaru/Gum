using System;
using Gum.Services.Dialogs;

namespace Gum.Avalonia.Dialogs;

/// <summary>
/// Delete confirmation for this head. Until plugins run here (phase 40) there are no plugin
/// options to inject, so the dialog is a plain yes/no message and confirmation has no listeners.
/// </summary>
public class AvaloniaDeleteDialogService : IDeleteDialogService
{
    private readonly IDialogService _dialogService;

    /// <summary>Creates the service over the generic dialog service.</summary>
    public AvaloniaDeleteDialogService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    /// <inheritdoc/>
    public IDeleteDialogResult ShowDeleteDialog(string title, string message, Array objectsToDelete)
    {
        MessageDialogResult result = _dialogService.ShowMessage(message, title, MessageDialogStyle.YesNo);
        return new DeleteDialogResult(result switch
        {
            MessageDialogResult.Affirmative => true,
            MessageDialogResult.Negative => false,
            _ => null,
        });
    }

    /// <inheritdoc/>
    public void NotifyConfirmed(IDeleteDialogResult result, Array objectsToDelete) { }

    private sealed class DeleteDialogResult : IDeleteDialogResult
    {
        public DeleteDialogResult(bool? result)
        {
            Result = result;
        }

        public bool? Result { get; }
    }
}
