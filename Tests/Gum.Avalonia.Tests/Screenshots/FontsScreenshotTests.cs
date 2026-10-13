using Avalonia.Styling;
using Gum.Avalonia.Panels;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Plugins.FontsPlugin;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Fonts tab at every zoom level, for a PR's screenshots
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter FontsScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class FontsScreenshotTests
{
    [SkippableFact]
    public void FontsTab() => PrScreenshot.Run(() =>
    {
        FontsViewModel viewModel = FontsViewModelFor(new GumProjectSave());
        viewModel.UpdateFontUsage(new[] { "Arial", "Noto Sans SC", "Material Icons" }, unmatchedFamilies: new string[0]);

        SaveAtZoomLevels(viewModel, "fonts-tab");
    });

    [SkippableFact]
    public void FontsTab_WithProblemsAndACharacterFile() => PrScreenshot.Run(() =>
    {
        FontsViewModel viewModel = FontsViewModelFor(new GumProjectSave { UseFontCharacterFile = true });
        viewModel.AddEntry();
        viewModel.Entries[3].FontFamily = "Roboto";
        viewModel.Entries[3].Ranges = "32-126";
        viewModel.AddEntry();
        viewModel.Entries[4].FontFamily = "Arial";
        viewModel.Entries[4].Ranges = "32-126,19968-";
        viewModel.AddEntry();
        viewModel.Entries[5].FontFamily = "Noto Sans SC";
        viewModel.UpdateFontUsage(new[] { "Arial", "Noto Sans SC" }, unmatchedFamilies: new[] { "Roboto", "Material Icons" });

        SaveAtZoomLevels(viewModel, "fonts-tab-problems");
    });

    private static FontsViewModel FontsViewModelFor(GumProjectSave project)
    {
        project.FontRangeEntries.Add(new FontRangeEntry { FontFamily = "Noto Sans SC", Ranges = "32-126,19968-40959" });
        project.FontRangeEntries.Add(new FontRangeEntry { FontFamily = "Material Icons", Ranges = "57344-63743" });
        FontsViewModel viewModel = new FontsViewModel();
        viewModel.SetFrom(project);
        return viewModel;
    }

    private static void SaveAtZoomLevels(FontsViewModel viewModel, string name)
    {
        FontsView view = new FontsView { DataContext = viewModel };
        using ScreenshotWindow window = PrScreenshot.Show(view, width: 520, height: 780, ThemeVariant.Dark);
        PrScreenshot.SaveWindowAtZoomLevels(window.Window, name);
    }
}
