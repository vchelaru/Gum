using Avalonia.Controls;
using Avalonia.Styling;
using Gum.Avalonia.Dialogs.Views;
using Gum.Avalonia.Tests.Harness;
using Gum.Diagnostics;
using Gum.Services;
using Moq;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Create diagnostics file dialog (brand-new UI, so no "before"): one contact sheet per theme at zoom
/// 9, 12, 16 and 24 (<c>Tools/pr-screenshots.ps1 -Filter CreateDiagnosticsScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class CreateDiagnosticsScreenshotTests
{
    private static CreateDiagnosticsViewModel CreateViewModel(bool projectsFound)
    {
        Mock<IDiagnosticsEnvironment> environment = new();
        environment.SetupGet(x => x.SaveFolder).Returns(Path.Combine(Path.GetTempPath(), "GumDiagShot-none"));
        environment.SetupGet(x => x.Today).Returns(new DateTime(2026, 11, 21));
        Mock<IDiagnosticsContentSource> source = new();
        source.SetupGet(x => x.GumProjectFile).Returns(projectsFound ? "Game.gumx" : null);
        source.SetupGet(x => x.CSharpProjectFolder).Returns(projectsFound ? "Game" : null);
        return new CreateDiagnosticsViewModel(environment.Object, source.Object, new Mock<IFileSystemRevealService>().Object);
    }

    [Fact]
    public void CreateDiagnostics() => PrScreenshot.Run(() =>
    {
        foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            string themeName = theme.Key.ToString()!.ToLowerInvariant();

            CreateDiagnosticsViewModel withProjects = CreateViewModel(projectsFound: true);
            withProjects.Description = "Dropping a PNG onto the canvas freezes Gum for a few seconds.";
            withProjects.IncludeGumProject = true;
            using (ScreenshotWindow window = PrScreenshot.ShowDialog(withProjects, theme))
            {
                PrScreenshot.SaveContactSheet(window.Window, $"diagnostics-{themeName}-checked");
            }

            using (ScreenshotWindow window = PrScreenshot.ShowDialog(CreateViewModel(projectsFound: false), theme))
            {
                PrScreenshot.SaveContactSheet(window.Window, $"diagnostics-{themeName}-no-projects");
            }
        }
    });
}
