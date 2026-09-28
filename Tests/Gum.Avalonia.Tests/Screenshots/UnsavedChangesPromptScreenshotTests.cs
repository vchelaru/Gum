using Avalonia.Styling;
using Gum.Avalonia.Tests.Harness;
using Gum.Managers;
using Gum.Services.Dialogs;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The prompt shown when an element, behavior or project file changes on disk while it has unsaved
/// edits (#5379, #5388), for a PR's screenshot table
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
            Save("Button", "Components/Button.gucx", theme, $"unsaved-changes-prompt-{suffix}");
        }
    });

    /// <summary>The same prompt before a behavior or the whole project reloads (#5388).</summary>
    [SkippableFact]
    public void UnsavedChangesPromptForBehaviorAndProject() => PrScreenshot.Run(() =>
    {
        Save("Clickable", "Behaviors/Clickable.behx", ThemeVariant.Light, "unsaved-changes-prompt-behavior");
        Save(FileChangeReactionLogic.ProjectPromptName, "MyGame.gumx", ThemeVariant.Light, "unsaved-changes-prompt-project");
    });

    private static void Save(string name, string relativeFile, ThemeVariant theme, string fileName)
    {
        MessageDialogStyle style = FileChangeReactionLogic.CreateUnsavedChangesPromptStyle();
        MessageDialogViewModel viewModel = new MessageDialogViewModel
        {
            Title = FileChangeReactionLogic.UnsavedChangesPromptTitle,
            Message = FileChangeReactionLogic.BuildUnsavedChangesPromptMessage(name, relativeFile),
            AffirmativeText = style.AffirmativeText,
            NegativeText = style.NegativeText,
        };
        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, theme);
        window.Save(fileName);
    }
}
