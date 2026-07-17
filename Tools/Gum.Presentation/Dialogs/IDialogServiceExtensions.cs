namespace Gum.Services.Dialogs;

/// <summary>
/// Headless convenience wrapper over <see cref="IDialogService.ShowMessage"/>, relocated from
/// the WPF-project <c>IDialogServiceExt</c> (ADR-0005 Phase 3) so <c>DeleteLogic</c> does not
/// need a WPF reference. The remaining <c>IDialogServiceExt</c> members
/// (<c>Show&lt;T&gt;</c>, <c>ShowChoices&lt;T&gt;</c>) stay in the WPF project pending their own
/// dialog-viewmodel dependencies going headless.
/// </summary>
public static class IDialogServiceExtensions
{
    public static bool ShowYesNoMessage(this IDialogService dialogService, string message, string? title = null)
    {
        return dialogService.ShowMessage(message, title, MessageDialogStyle.YesNo) is MessageDialogResult.Affirmative;
    }
}
