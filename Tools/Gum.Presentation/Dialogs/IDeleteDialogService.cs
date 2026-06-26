using System;

namespace Gum.Services.Dialogs;

/// <summary>
/// Headless seam over the delete-confirmation dialog so DeleteLogic does not depend on WPF
/// (the standalone DeleteOptionsWindow) or the concrete plugin host. The WPF implementation
/// (DeleteDialogService) lives in the Gum shell; this interface and its opaque result type
/// must stay free of any WPF/WinForms reference (ADR-0005). Mirrors <see cref="IDialogService"/>.
/// </summary>
public interface IDeleteDialogService
{
    /// <summary>
    /// Builds and shows the modal delete-confirmation dialog — letting plugins inject their
    /// options first — and blocks until the user responds. The returned handle carries the
    /// user's choice via <see cref="IDeleteDialogResult.Result"/> and is otherwise opaque;
    /// pass it back to <see cref="NotifyConfirmed"/> to fire the post-confirmation plugin event.
    /// </summary>
    IDeleteDialogResult ShowDeleteDialog(string title, string message, Array objectsToDelete);

    /// <summary>
    /// Notifies plugins that the delete was confirmed, for the dialog represented by
    /// <paramref name="result"/>. Timing matters: callers fire this at specific points in the
    /// delete flow (some before removal, some after) so plugins can still read parent references.
    /// </summary>
    void NotifyConfirmed(IDeleteDialogResult result, Array objectsToDelete);
}

/// <summary>
/// Opaque handle to a shown delete dialog. Exposes only the user's choice; the WPF window it
/// wraps stays hidden behind the implementation so this type carries no WPF dependency.
/// </summary>
public interface IDeleteDialogResult
{
    /// <summary>
    /// The dialog's result: <c>true</c> = confirmed, <c>false</c> = cancelled,
    /// <c>null</c> = closed without choosing.
    /// </summary>
    bool? Result { get; }
}
