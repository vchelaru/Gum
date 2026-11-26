using Gum.Menus;

namespace Gum.Avalonia.Diagnostics.CrashReporting;

/// <summary>Whether the user lets Gum send crash reports, asked once and changeable from Help > Crash reporting.</summary>
public interface ICrashReportingConsent
{
    /// <summary>True only when the user chose to send reports.</summary>
    bool IsSendingAllowed { get; }

    /// <summary>
    /// Asks the user and saves the answer when they have not answered yet. Closing the prompt counts
    /// as don't send. Does nothing in an unattended run, which has nobody to answer.
    /// </summary>
    void PromptIfUnset(bool isUnattended);

    /// <summary>Saves the user's choice.</summary>
    void SetSendingAllowed(bool isAllowed);

    /// <summary>Adds the checkable Help > Crash reporting item.</summary>
    void AddHelpMenuItem(MenuModel menu);
}
