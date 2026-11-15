using Avalonia.Controls;
using Avalonia.Styling;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.Localization;
using Gum.Plugins.PropertiesWindowPlugin;
using Gum.Services;
using Gum.SettingsWindow;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Project > Localization page of the Settings window in the real window and theme, one contact sheet
/// (zoom 9, 12, 16 and 24 on one PNG) per state and theme
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter LocalizationSettingsScreenshotTests</c>). Uses only
/// API that main already has, so the same file renders the "before" side.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class LocalizationSettingsScreenshotTests
{
    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void Empty(bool dark) => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        Save(dark, "settings-localization-empty-" + Theme(dark));
    });

    // Two files, three languages, one of them picked.
    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void FewLanguages(bool dark) => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        LocalizationService localization = TestAppBuilder.Services.GetRequiredService<LocalizationService>();
        try
        {
            AddCsv(tree);
            Save(dark, "settings-localization-few-" + Theme(dark));
        }
        finally
        {
            localization.Clear();
            localization.CurrentLanguage = 0;
        }
    });

    // Eight files, to judge how fast one is found in a longer list.
    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ManyFiles(bool dark) => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        LocalizationService localization = TestAppBuilder.Services.GetRequiredService<LocalizationService>();
        try
        {
            AddFiles(tree, 8);
            Save(dark, "settings-localization-many-" + Theme(dark));
        }
        finally
        {
            localization.Clear();
            localization.CurrentLanguage = 0;
        }
    });

    // Two CSVs: not supported, so loading is skipped (reported in the Output tab) and no language is offered.
    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void Problems(bool dark) => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        LocalizationService localization = TestAppBuilder.Services.GetRequiredService<LocalizationService>();
        try
        {
            File.WriteAllText(Path.Combine(tree.Project.ProjectFolder, "A.csv"), "String ID,English\nT_A,A\n");
            File.WriteAllText(Path.Combine(tree.Project.ProjectFolder, "B.csv"), "String ID,English\nT_B,B\n");
            SettingsPagesForTests.ProjectProperties().LocalizationFiles = new List<string> { "A.csv", "B.csv", "Missing.csv" };
            Save(dark, "settings-localization-problems-" + Theme(dark));
        }
        finally
        {
            localization.Clear();
            localization.CurrentLanguage = 0;
        }
    });

    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithSearch(bool dark) => PrScreenshot.Run(() =>
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        LocalizationService localization = TestAppBuilder.Services.GetRequiredService<LocalizationService>();
        try
        {
            AddCsv(tree);
            Save(dark, "settings-localization-search-" + Theme(dark), search: "language");
        }
        finally
        {
            localization.Clear();
            localization.CurrentLanguage = 0;
        }
    });

    private static string Theme(bool dark) => dark ? "dark" : "light";

    // Several files load only when all are .resx; a CSV is one file with a column per language.
    private static void AddCsv(ProjectTreeHarness tree)
    {
        File.WriteAllText(Path.Combine(tree.Project.ProjectFolder, "Strings.csv"),
            "String ID,English,French,German\nT_Play,Play,Jouer,Spielen\n");
        SettingsPagesForTests.ProjectProperties().LocalizationFiles = new List<string> { "Strings.csv" };
        SettingsPagesForTests.ProjectProperties().LanguageName = "French";
    }

    private static void AddFiles(ProjectTreeHarness tree, int count)
    {
        List<string> files = new List<string>();
        for (int i = 0; i < count; i++)
        {
            string name = $"Menu{i}.resx";
            File.WriteAllText(Path.Combine(tree.Project.ProjectFolder, name),
                $"<root><data name=\"T_Play{i}\"><value>Play</value></data></root>");
            File.WriteAllText(Path.Combine(tree.Project.ProjectFolder, $"Menu{i}.fr.resx"),
                $"<root><data name=\"T_Play{i}\"><value>Jouer</value></data></root>");
            files.Add(name);
        }
        SettingsPagesForTests.ProjectProperties().LocalizationFiles = files;
        SettingsPagesForTests.ProjectProperties().LanguageName = "French";
    }

    private static void Save(bool dark, string name, string search = "")
    {
        SettingsDialogViewModel viewModel = TestAppBuilder.Services.GetRequiredService<SettingsDialogViewModel>();
        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, dark ? ThemeVariant.Dark : ThemeVariant.Light);
        viewModel.SelectPage(SettingsPageIds.Localization);
        // Tall enough that the whole page shows at the largest zoom.
        window.Window.SizeToContent = SizeToContent.Manual;
        window.Window.MaxHeight = double.PositiveInfinity;
        window.Window.Height = 1100;
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
