using Avalonia.Styling;
using Gum.Avalonia.Diagnostics.CrashReporting;
using Gum.Avalonia.Tests.Harness;
using Gum.Services.Dialogs;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>The crash-report consent prompt (#8444), built the way the dialog service builds a message.</summary>
[Trait("Category", PrScreenshot.Category)]
public class CrashReportingPromptScreenshotTests
{
    [Fact]
    public void CrashReportingPrompt() => PrScreenshot.Run(() =>
    {
        foreach (ThemeVariant theme in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        {
            MessageDialogViewModel viewModel = new MessageDialogViewModel
            {
                Title = CrashReportingConsent.PromptTitle,
                Message = CrashReportingConsent.PromptMessage,
                AffirmativeText = CrashReportingConsent.SendText,
                NegativeText = CrashReportingConsent.DontSendText,
            };
            using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, theme);
            PrScreenshot.SaveWindowAtZoomLevels(window.Window, "8444-crash-reporting-prompt-" + theme.Key.ToString()!.ToLowerInvariant());
        }
    });
}
