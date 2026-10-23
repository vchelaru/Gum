using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Code tab over a ProjectCodeSettings.codsj that does not parse, for a PR's before/after table
/// (<c>Tools/pr-screenshots.ps1 -Filter UnreadableCodeSettingsScreenshotTests</c>). Uses only API that
/// main already has, so the same file renders the "before" side.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class UnreadableCodeSettingsScreenshotTests
{
    [SkippableFact]
    public void UnreadableCodeSettings() => PrScreenshot.Run(() =>
    {
        using CodeTabHarness code = new CodeTabHarness(projectFolder => File.WriteAllText(
            Path.Combine(projectFolder, "ProjectCodeSettings.codsj"),
            "<<<<<<< HEAD\n{ \"RootNamespace\": \"Mine\"\n======="));
        ComponentSave card = code.Project.AddComponent("Card");
        code.Tree.SaveAll();
        code.Select(card);
        PrScreenshot.SaveWindowAtZoomLevels(code.Input.Window, "code-settings-unreadable");
    });
}
