using Avalonia.Styling;
using Gum.Avalonia.Tests.Harness;
using Gum.Managers;
using Gum.Services.Dialogs;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The prompt shown when an element file changes on disk while the element has unsaved edits
/// (#5379), for a PR's screenshot table
/// (<c>Tools/pr-screenshots.ps1 -Filter UnsavedChangesPromptScreenshotTests -NoBefore</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class UnsavedChangesPromptScreenshotTests
{
    [SkippableFact]
    public void UnsavedChangesPrompt() => PrScreenshot.Run(() =>
    {
        foreach ((ThemeVariant theme, string suffix) in new[] { (ThemeVariant.Light, "light"), (ThemeVariant.Dark, "dark") })
        {
            MessageDialogStyle style = FileChangeReactionLogic.CreateUnsavedChangesPromptStyle();
            MessageDialogViewModel viewModel = new MessageDialogViewModel
            {
                Title = FileChangeReactionLogic.UnsavedChangesPromptTitle,
                Message = FileChangeReactionLogic.BuildUnsavedChangesPromptMessage("Button", "Components/Button.gucx"),
                AffirmativeText = style.AffirmativeText,
                NegativeText = style.NegativeText,
            };
            using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, theme);
            window.Save($"unsaved-changes-prompt-{suffix}");
        }
    });
}
