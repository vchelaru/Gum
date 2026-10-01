using Avalonia.Styling;
using Gum.Avalonia.Tests.Harness;
using Gum.Services.Dialogs;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The choice prompt with a long option (#5558), for a PR's screenshot table
/// (<c>Tools/pr-screenshots.ps1 -Filter ChoiceDialogScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class ChoiceDialogScreenshotTests
{
    [SkippableFact]
    public void ChoiceDialogWithLongOption() => PrScreenshot.Run(() =>
    {
        ChoiceDialogViewModel viewModel = new ChoiceDialogViewModel
        {
            Title = "Import conflict",
            Message = "The project already has a component named Button. What should the import do?",
        };
        viewModel.SetOptions(new Dictionary<int, string>
        {
            [0] = "Skip Button and keep the existing component",
            [1] = "Overwrite the existing Button component with the imported one, replacing its states, instances and behaviors, and update every screen and component that references it",
            [2] = "Import it under a new name",
        });
        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, ThemeVariant.Dark);
        window.Save("choice-dialog-long-option");
    });
}
