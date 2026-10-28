using Avalonia.Styling;
using Gum.Avalonia.Tests.Harness;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The prompt shown when a project's Shaders/Blur.slang differs from the built-in one (#7009), at
/// every zoom level, in both themes
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter BuiltInShaderReplaceDialogScreenshotTests</c>).
/// The dialog is built by the real <see cref="BuiltInShaderFiles"/>, so the text is the shipped text.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class BuiltInShaderReplaceDialogScreenshotTests
{
    // IDialogService.Show has an out parameter, which Moq can only populate through a callback
    // delegate whose signature mirrors the method.
    private delegate void ShowChoiceCallback(Action<ChoiceDialogViewModel>? initializer, out ChoiceDialogViewModel viewModel);

    [SkippableFact]
    public void ReplaceDialog_Dark() => PrScreenshot.Run(() => CaptureReplaceDialog(ThemeVariant.Dark, "builtin-shader-replace-dark"));

    [SkippableFact]
    public void ReplaceDialog_Light() => PrScreenshot.Run(() => CaptureReplaceDialog(ThemeVariant.Light, "builtin-shader-replace-light"));

    [SkippableFact]
    public void OldYesNoBox_Dark() => PrScreenshot.Run(() =>
    {
        MessageDialogStyle style = MessageDialogStyle.YesNo;
        MessageDialogViewModel viewModel = new MessageDialogViewModel
        {
            AffirmativeText = style.AffirmativeText,
            NegativeText = style.NegativeText,
            Title = "Replace shader?",
            Message = "Replace your Shaders/Blur.slang with the built-in?",
        };
        using ScreenshotWindow window = PrScreenshot.ShowDialog(viewModel, ThemeVariant.Dark);
        PrScreenshot.SaveWindowAtZoomLevels(window.Window, "before-builtin-shader-replace-dark");
    });

    private static void CaptureReplaceDialog(ThemeVariant theme, string name)
    {
        string root = Path.Combine(Path.GetTempPath(), "GumBuiltInShaderShot_" + Guid.NewGuid().ToString("N"));
        string builtInFolder = Path.Combine(root, "builtin");
        string projectDirectory = Path.Combine(root, "project") + "/";
        Directory.CreateDirectory(builtInFolder);
        Directory.CreateDirectory(Path.Combine(projectDirectory, "Shaders"));
        try
        {
            File.WriteAllText(Path.Combine(builtInFolder, "Blur.slang"), "built-in blur");
            File.WriteAllText(Path.Combine(projectDirectory, "Shaders", "Blur.slang"), "my edited blur");

            Mock<IProjectState> projectState = new Mock<IProjectState>();
            projectState.SetupGet(state => state.ProjectDirectory).Returns(projectDirectory);

            // Records the view model the real code configures instead of opening it; the test shows it below.
            ChoiceDialogViewModel? shown = null;
            Mock<IDialogService> dialogService = new Mock<IDialogService>();
            dialogService
                .Setup(dialogs => dialogs.Show(It.IsAny<Action<ChoiceDialogViewModel>?>(), out It.Ref<ChoiceDialogViewModel>.IsAny))
                .Callback(new ShowChoiceCallback((Action<ChoiceDialogViewModel>? initializer, out ChoiceDialogViewModel viewModel) =>
                {
                    viewModel = new ChoiceDialogViewModel();
                    initializer?.Invoke(viewModel);
                    shown = viewModel;
                }))
                .Returns(false);

            BuiltInShaderFiles shaders = new BuiltInShaderFiles(
                projectState.Object, dialogService.Object, new Mock<IOutputManager>().Object, builtInFolder);
            shaders.CopyIntoProject("Blur.slang");

            shown.ShouldNotBeNull();
            using ScreenshotWindow window = PrScreenshot.ShowDialog(shown, theme);
            PrScreenshot.SaveWindowAtZoomLevels(window.Window, name);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
