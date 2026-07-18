using System;
using System.Collections.Generic;

namespace Gum.Services.Dialogs;

/// <summary>
/// Headless convenience wrappers over <see cref="IDialogService"/>, relocated from the
/// WPF-project <c>IDialogServiceExt</c> (ADR-0005 Phase 3) so headless consumers (e.g.
/// <c>DeleteLogic</c>, <c>ImportFromGumxViewModel</c>) don't need a WPF reference. All members
/// were originally gated on their dialog ViewModels (<see cref="ChoiceDialogViewModel"/>) going
/// headless first; that has since happened, so the whole class moved (issue #3754).
/// </summary>
public static class IDialogServiceExtensions
{
    public static bool ShowYesNoMessage(this IDialogService dialogService, string message, string? title = null)
    {
        return dialogService.ShowMessage(message, title, MessageDialogStyle.YesNo) is MessageDialogResult.Affirmative;
    }

    public static bool Show<T>(this IDialogService dialogService) where T : DialogViewModel
    {
        return dialogService.Show<T>(null, out _);
    }

    public static bool Show<T>(this IDialogService dialogService, Action<T> initializer) where T : DialogViewModel
    {
        return dialogService.Show<T>(initializer, out _);
    }

    public static T? ShowChoices<T>(this IDialogService dialogService, string message, Dictionary<T, string> options,
        string? title = null,
        bool canCancel = false) where T : notnull
    {
        dialogService.Show(Configure, out ChoiceDialogViewModel dialog);
        return (T?)dialog.SelectedKey;

        void Configure(ChoiceDialogViewModel d)
        {
            d.SetOptions(options);
            d.Title = title ?? "Gum";
            d.Message = message;
            d.CanCancel = canCancel;
        }
    }
}
