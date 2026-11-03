using Avalonia.Styling;
using Avalonia.Threading;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Code tab's per-element rows with the Debug Only checkbox (#7200), both themes, at every zoom level.
/// The "before" run has no such row and renders the unchecked rows only, so the same file renders both sides.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class DebugOnlyElementScreenshotTests
{
    [SkippableFact]
    public void DebugOnlyRow() => PrScreenshot.Run(() =>
    {
        using CodeTabHarness code = new CodeTabHarness(projectFolder => File.WriteAllText(
            Path.Combine(projectFolder, "ProjectCodeSettings.codsj"),
            """{ "SyntaxVersion": "3", "Version": 1, "AppendFolderToNamespace": true }"""));
        ComponentSave card = code.Project.AddComponent("Card");
        code.Tree.SaveAll();
        code.Select(card);
        code.SetUpManualGeneration();

        foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            code.Input.Window.RequestedThemeVariant = theme;
            Dispatcher.UIThread.RunJobs();
            string themeName = theme.Key.ToString()!.ToLowerInvariant();
            PrScreenshot.SaveWindowAtZoomLevels(code.Input.Window, $"debug-only-unchecked-{themeName}");
        }

        if (code.ShownMemberNames().Contains("Debug Only"))
        {
            code.ClickCheckBox("Debug Only");
            foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
            {
                code.Input.Window.RequestedThemeVariant = theme;
                Dispatcher.UIThread.RunJobs();
                string themeName = theme.Key.ToString()!.ToLowerInvariant();
                PrScreenshot.SaveWindowAtZoomLevels(code.Input.Window, $"debug-only-checked-{themeName}");
            }
        }
    });
}
