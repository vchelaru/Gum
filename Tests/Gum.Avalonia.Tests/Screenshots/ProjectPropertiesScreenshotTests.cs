using Avalonia.Styling;
using Gum.Avalonia.Panels;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Plugins.PropertiesWindowPlugin;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Project Properties tab over a default project, for a PR's before/after table
/// (<c>Tools/pr-screenshots.ps1 -Filter ProjectPropertiesScreenshotTests</c>). Uses only API that
/// main already has, so the same file renders the "before" side.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class ProjectPropertiesScreenshotTests
{
    [SkippableFact]
    public void ProjectProperties() => PrScreenshot.Run(() =>
    {
        ProjectPropertiesViewModel viewModel = new ProjectPropertiesViewModel();
        viewModel.SetFrom(autoSave: true, new GumProjectSave());
        ProjectPropertiesView view = new ProjectPropertiesView { DataContext = viewModel };

        using ScreenshotWindow window = PrScreenshot.Show(view, width: 520, height: 1100, ThemeVariant.Dark);
        window.Save("project-properties-dark");
    });
}
