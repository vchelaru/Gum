using Gum.Services.Dialogs;

namespace Gum.Diagnostics;

/// <summary>
/// "Gum froze last time and diagnostics are waiting to be reported." Affirmative opens the
/// diagnostics folder; either answer counts the files as shown. Opened by
/// <see cref="FreezeDiagnosticsPromptService"/>, not directly.
/// </summary>
public class FreezeDiagnosticsPromptViewModel : DialogViewModel
{
    public FreezeDiagnosticsPromptViewModel()
    {
        Title = "Gum Froze Last Time";
        Message = "";
        AffirmativeText = "Open Folder";
        NegativeText = "Not Now";
    }

    /// <summary>The window title.</summary>
    public string Title { get; }

    /// <summary>What happened and where the files are.</summary>
    public string Message { get => Get<string>(); set => Set(value); }

    /// <summary>Checked when the user wants no further prompts about freeze diagnostics.</summary>
    public bool IsDoNotAskAgainChecked { get => Get<bool>(); set => Set(value); }
}
