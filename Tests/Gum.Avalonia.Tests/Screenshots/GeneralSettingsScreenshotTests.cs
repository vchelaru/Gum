using Avalonia.Styling;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.SettingsWindow;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Application > General page of the Settings window in the real window and theme, one contact sheet
/// (zoom 9, 12, 16 and 24 on one PNG) per state and theme
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter GeneralSettingsScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class GeneralSettingsScreenshotTests
{
    [SkippableTheory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void GeneralPage(bool dark, bool optionsOn) => PrScreenshot.Run(() =>
    {
        GeneralSettingsViewModel general = TestAppBuilder.Services.GetRequiredService<GeneralSettingsViewModel>();
        bool originalAutoSave = general.AutoSave;
        bool originalPalette = general.UseStandardsPalette;
        try
        {
            general.AutoSave = optionsOn;
            general.UseStandardsPalette = optionsOn;
            SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
            using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, dark ? ThemeVariant.Dark : ThemeVariant.Light);
            viewModel.SelectPage(SettingsPageIds.General);
            // Tall enough that the whole page shows at the largest zoom.
            window.Window.SizeToContent = global::Avalonia.Controls.SizeToContent.Manual;
            window.Window.MaxHeight = double.PositiveInfinity;
            window.Window.Height = 1100;
            PrScreenshot.SaveContactSheet(window.Window,
                $"settings-general-{(optionsOn ? "on" : "off")}-{(dark ? "dark" : "light")}");
        }
        finally
        {
            general.AutoSave = originalAutoSave;
            general.UseStandardsPalette = originalPalette;
        }
    });

    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void GeneralPageWithSearch(bool dark) => PrScreenshot.Run(() =>
    {
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, dark ? ThemeVariant.Dark : ThemeVariant.Light);
        viewModel.SelectPage(SettingsPageIds.General);
        window.Window.SizeToContent = global::Avalonia.Controls.SizeToContent.Manual;
            window.Window.MaxHeight = double.PositiveInfinity;
            window.Window.Height = 1100;
        viewModel.SearchText = "project panel";
        try
        {
            PrScreenshot.SaveContactSheet(window.Window, $"settings-general-search-{(dark ? "dark" : "light")}");
        }
        finally
        {
            viewModel.SearchText = "";
        }
    });
}
