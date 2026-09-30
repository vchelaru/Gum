using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Code tab's project-wide settings with a Code Project Root set, for a PR's before/after table
/// (<c>Tools/pr-screenshots.ps1 -Filter CodeSettingsScreenshotTests</c>). Uses only API that main
/// already has, so the same file renders the "before" side.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class CodeSettingsScreenshotTests
{
    [SkippableFact]
    public void CodeSettings() => PrScreenshot.Run(() =>
    {
        // A pinned syntax version keeps the detection row from showing the temp folder's path.
        using CodeTabHarness code = new CodeTabHarness(projectFolder => File.WriteAllText(
            Path.Combine(projectFolder, "ProjectCodeSettings.codsj"),
            """{ "SyntaxVersion": "3", "Version": 1, "AppendFolderToNamespace": true }"""));
        ComponentSave card = code.Project.AddComponent("Card");
        code.Tree.SaveAll();
        code.Select(card);
        code.SetUpManualGeneration();
        PrScreenshot.SaveWindow(code.Input.Window, "code-settings");

        // Clicking the selected scope choice again must leave it selected (#5479).
        code.ClickButton("All");
        code.ClickButton("All");
        PrScreenshot.SaveWindow(code.Input.Window, "code-scope-all-clicked-twice");
    });
}
