using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Variables tab's Text row for the selected Text right after the project gets a localization
/// file, for a PR's before/after table (<c>Tools/pr-screenshots.ps1 -Filter LocalizedTextRowScreenshotTests</c>).
/// Uses only API that main already has, so the same file renders the "before" side.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class LocalizedTextRowScreenshotTests
{
    [SkippableFact]
    public void TextRow_RightAfterAddingALocalizationFile() => PrScreenshot.Run(() =>
    {
        LocalizationService localization = TestAppBuilder.Services.GetRequiredService<LocalizationService>();
        using VariableGridHarness grid = new VariableGridHarness();
        try
        {
            ComponentSave button = grid.Project.AddComponent("Button");
            InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
            grid.Select(label);

            File.WriteAllText(Path.Combine(grid.Project.ProjectFolder, "Strings.csv"), "String ID,English\nT_Play,Play\nT_Quit,Quit\n");
            grid.Project.Project.LocalizationFiles.Add("Strings.csv");
            TestAppBuilder.Services.GetRequiredService<IFileCommands>().LoadLocalizationFile();
            grid.Settle();
            grid.ViewModel.VariableFilterText = "Text";
            grid.Settle();

            PrScreenshot.SaveWindow(grid.Input.Window, "text-row-after-adding-localization");
        }
        finally
        {
            localization.Clear();
            localization.CurrentLanguage = 0;
        }
    });
}
