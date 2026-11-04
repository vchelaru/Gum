using Avalonia.Controls;
using Avalonia.Styling;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.SettingsWindow;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Application > Keyboard shortcuts page of the Settings window in the real window and theme, one
/// contact sheet (zoom 9, 12, 16 and 24 on one PNG) per state and theme
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter KeyboardShortcutsSettingsScreenshotTests</c>).
/// Uses only API that main already has, so the same file renders the "before" side.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class KeyboardShortcutsSettingsScreenshotTests
{
    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void RealShortcuts(bool dark) => PrScreenshot.Run(() =>
        Save(dark, $"settings-shortcuts-real-{(dark ? "dark" : "light")}", search: ""));

    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithSearch(bool dark) => PrScreenshot.Run(() =>
        Save(dark, $"settings-shortcuts-search-{(dark ? "dark" : "light")}", search: "undo"));

    private static void Save(bool dark, string name, string search)
    {
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, dark ? ThemeVariant.Dark : ThemeVariant.Light);
        viewModel.SelectPage(SettingsPageIds.KeyboardShortcuts);
        // Tall enough that the whole page shows at the largest zoom.
        window.Window.SizeToContent = SizeToContent.Manual;
        window.Window.MaxHeight = double.PositiveInfinity;
        window.Window.Height = 1700;
        viewModel.SearchText = search;
        try
        {
            PrScreenshot.SaveContactSheet(window.Window, name);
        }
        finally
        {
            viewModel.SearchText = "";
        }
    }
}
