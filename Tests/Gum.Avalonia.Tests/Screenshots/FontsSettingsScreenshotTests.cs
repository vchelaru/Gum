using Avalonia.Controls;
using Avalonia.Styling;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Plugins.FontsPlugin;
using Gum.Services.Dialogs;
using Gum.SettingsWindow;
using Gum.ToolStates;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Project > Fonts page of the Settings window in the real window and theme, one contact sheet
/// (zoom 9, 12, 16 and 24 on one PNG) per state and theme
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter FontsSettingsScreenshotTests</c>). Uses only API
/// that main already has, so the same file renders the "before" side.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class FontsSettingsScreenshotTests
{
    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void Typical(bool dark) => PrScreenshot.Run(() =>
    {
        FontsViewModel fonts = FontsViewModelFor(new GumProjectSave());
        fonts.UpdateFontUsage(new[] { "Arial", "Noto Sans SC", "Material Icons" }, unmatchedFamilies: new string[0]);
        Save(fonts, dark, $"settings-fonts-typical-{(dark ? "dark" : "light")}");
    });

    // The character file in use, an empty row, a duplicate row, and a row for a font no Text uses.
    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithProblems(bool dark) => PrScreenshot.Run(() =>
    {
        FontsViewModel fonts = FontsViewModelFor(new GumProjectSave { UseFontCharacterFile = true });
        fonts.AddEntry();
        fonts.Entries[3].FontFamily = "Roboto";
        fonts.Entries[3].Ranges = "32-126";
        fonts.AddEntry();
        fonts.Entries[4].FontFamily = "Noto Sans SC";
        fonts.Entries[4].Ranges = "32-126,19968-";
        fonts.AddEntry();
        fonts.UpdateFontUsage(new[] { "Arial", "Noto Sans SC" }, unmatchedFamilies: new[] { "Roboto", "Material Icons" });
        Save(fonts, dark, $"settings-fonts-problems-{(dark ? "dark" : "light")}");
    });

    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithSearch(bool dark) => PrScreenshot.Run(() =>
    {
        FontsViewModel fonts = FontsViewModelFor(new GumProjectSave());
        fonts.UpdateFontUsage(new[] { "Arial", "Noto Sans SC", "Material Icons" }, unmatchedFamilies: new string[0]);
        Save(fonts, dark, $"settings-fonts-search-{(dark ? "dark" : "light")}", search: "spacing");
    });

    private static FontsViewModel FontsViewModelFor(GumProjectSave project)
    {
        project.FontRangeEntries.Add(new FontRangeEntry { FontFamily = "Noto Sans SC", Ranges = "32-126,19968-40959" });
        project.FontRangeEntries.Add(new FontRangeEntry { FontFamily = "Material Icons", Ranges = "57344-63743" });
        FontsViewModel viewModel = new FontsViewModel();
        viewModel.SetFrom(project);
        return viewModel;
    }

    private static void Save(FontsViewModel fonts, bool dark, string name, string search = "")
    {
        IServiceProvider services = TestAppBuilder.Services;
        SettingsPageRegistry registry = new SettingsPageRegistry();
        registry.Register(new SettingsPage(SettingsPageIds.Fonts, "Fonts", SettingsSection.Project, fonts, order: 2));
        Mock<IProjectState> projectState = new Mock<IProjectState>();
        projectState.SetupGet(x => x.GumProjectSave).Returns(new GumProjectSave { FullFileName = "C:/project/Project.gumx" });
        SettingsDialogViewModel viewModel = new SettingsDialogViewModel(
            registry,
            projectState.Object,
            services.GetRequiredService<GeneralSettingsViewModel>(),
            services.GetRequiredService<ThemingDialogViewModel>(),
            services.GetRequiredService<PluginsDialogViewModel>());

        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, dark ? ThemeVariant.Dark : ThemeVariant.Light);
        viewModel.SelectPage(SettingsPageIds.Fonts);
        // Tall enough that the whole page shows at the largest zoom.
        window.Window.SizeToContent = SizeToContent.Manual;
        window.Window.MaxHeight = double.PositiveInfinity;
        window.Window.Height = 1700;
        viewModel.SearchText = search;
        PrScreenshot.SaveContactSheet(window.Window, name);
    }
}
