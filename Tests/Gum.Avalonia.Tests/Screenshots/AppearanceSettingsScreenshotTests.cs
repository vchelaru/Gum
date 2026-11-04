using Avalonia.Styling;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.Dialogs;
using Gum.Services;
using Gum.SettingsWindow;
using Microsoft.Extensions.DependencyInjection;
using System.Drawing;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Application > Appearance page of the Settings window in the real window and theme, one contact
/// sheet (zoom 9, 12, 16 and 24 on one PNG) per state and theme
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter AppearanceSettingsScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class AppearanceSettingsScreenshotTests
{
    [SkippableTheory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void AppearancePage(bool dark, bool withCustomColors) => PrScreenshot.Run(() =>
    {
        IThemingService theming = TestAppBuilder.Services.GetRequiredService<IThemingService>();
        Color? originalAccent = theming.Accent;
        Color? originalGuide = theming.GuideLine;
        try
        {
            if (withCustomColors)
            {
                theming.Accent = Color.FromArgb(255, 0xC0, 0x40, 0x80);
                theming.GuideLine = Color.FromArgb(255, 0x40, 0xC0, 0x80);
            }
            SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
            using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, dark ? ThemeVariant.Dark : ThemeVariant.Light);
            viewModel.SelectPage(SettingsPageIds.Appearance);
            PrScreenshot.SaveContactSheet(window.Window,
                $"settings-appearance-{(withCustomColors ? "custom" : "default")}-{(dark ? "dark" : "light")}");
        }
        finally
        {
            theming.Accent = originalAccent;
            theming.GuideLine = originalGuide;
        }
    });
}
