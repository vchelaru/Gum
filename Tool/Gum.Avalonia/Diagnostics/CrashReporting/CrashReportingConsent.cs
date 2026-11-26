using Gum.Menus;
using Gum.Services.Dialogs;
using Gum.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Diagnostics.CrashReporting;

/// <inheritdoc/>
public class CrashReportingConsent : ICrashReportingConsent
{
    /// <summary>The prompt's window title.</summary>
    public const string PromptTitle = "Help fix crashes in Gum";

    /// <summary>The prompt's text.</summary>
    public const string PromptMessage =
        "When Gum hits an error, it can send a report to the Gum developers automatically.\n\n" +
        "A report includes:\n" +
        "• The error and where in Gum it happened\n" +
        "• Gum version, OS, and .NET version\n\n" +
        "Your project files are never sent, and file paths have your username and project name removed. " +
        "You can change this anytime in Help > Crash reporting.";

    /// <summary>The button that opts in.</summary>
    public const string SendText = "Send crash reports";

    /// <summary>The button that opts out.</summary>
    public const string DontSendText = "Don't send";

    /// <summary>The Help menu item's header.</summary>
    public const string MenuHeader = "Crash reporting";

    private readonly IWritableOptions<CrashReportingSettings> _settings;
    private readonly IDialogService _dialogService;

    /// <summary>Creates the consent over the saved setting.</summary>
    public CrashReportingConsent(IWritableOptions<CrashReportingSettings> settings, IDialogService dialogService)
    {
        _settings = settings;
        _dialogService = dialogService;
    }

    /// <summary>Registers the consent, unless <paramref name="dsn"/> is empty, which leaves crash reporting off.</summary>
    public static void AddServices(IServiceCollection services, string dsn)
    {
        if (!string.IsNullOrWhiteSpace(dsn))
        {
            services.AddSingleton<ICrashReportingConsent, CrashReportingConsent>();
        }
    }

    /// <inheritdoc/>
    public bool IsSendingAllowed => _settings.CurrentValue.SendCrashReports == true;

    /// <inheritdoc/>
    public void PromptIfUnset(bool isUnattended)
    {
        if (isUnattended || _settings.CurrentValue.SendCrashReports != null)
        {
            return;
        }

        MessageDialogResult result = _dialogService.ShowMessage(PromptMessage, PromptTitle,
            new MessageDialogStyle { AffirmativeText = SendText, NegativeText = DontSendText });
        SetSendingAllowed(result == MessageDialogResult.Affirmative);
    }

    /// <inheritdoc/>
    public void SetSendingAllowed(bool isAllowed) =>
        _settings.Update(settings => settings.SendCrashReports = isAllowed);

    /// <inheritdoc/>
    public void AddHelpMenuItem(MenuModel menu)
    {
        MenuItemModel? item = null;
        item = menu.AddMenuItem(new[] { "Help", MenuHeader }, () => SetSendingAllowed(item!.IsChecked));
        item.IsCheckable = true;
        item.IsChecked = IsSendingAllowed;
        item.ToolTip = "Send a report to the Gum developers when Gum hits an error";
    }
}
