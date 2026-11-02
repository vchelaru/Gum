using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Services.Dialogs;
using Gum.Wireframe;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// Hot reload when the screen the game is showing no longer exists in the saved project. Red on
/// main: the old UI stays on screen and nothing is said.
/// </summary>
[Trait("Category", "EndToEnd")]
public class ToolSaveHotReloadShowingScreenScenarioTests
{
    private static ScreenSave Build(ProjectTreeHarness tree)
    {
        ScreenSave main = tree.Project.AddScreen("Main");
        tree.Project.AddInstance(main, "Heading", "Text");
        tree.SaveAll();
        return main;
    }

    [AvaloniaFact]
    public void RenamingTheShowingScreen_InTheTool_IsReportedByTheRunningGame()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave main = Build(tree);
        using ToolSaveRuntimeWorld world = new ToolSaveRuntimeWorld(tree.Project.ProjectFilePath, "Main");

        tree.Click(tree.NodeFor(main));
        tree.Dialogs.AnswerNext<RenameElementDialogViewModel>(dialog => { dialog.Value = "Home"; return true; });
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.Press(Key.F2, PhysicalKey.F2);
        world.WaitForReload(TimeSpan.FromSeconds(2));
        world.ReloadsDuring(TimeSpan.FromMilliseconds(500));

        world.ErrorOutput.ShouldContain("Main", customMessage: "the game shows the screen Main, which no longer exists, and nothing says so");
    }

    [AvaloniaFact]
    public void DeletingTheShowingScreen_InTheTool_IsReportedByTheRunningGame()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave main = Build(tree);
        tree.Project.AddScreen("Other");
        tree.SaveAll();
        using ToolSaveRuntimeWorld world = new ToolSaveRuntimeWorld(tree.Project.ProjectFilePath, "Main");

        tree.Click(tree.NodeFor(main));
        tree.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(_ => true);
        tree.RightClick(tree.NodeFor(main));
        tree.PickMenu("Delete");
        world.WaitForReload(TimeSpan.FromSeconds(2));
        world.ReloadsDuring(TimeSpan.FromMilliseconds(500));

        world.ErrorOutput.ShouldContain("Main", customMessage: "the game shows the screen Main, which no longer exists, and nothing says so");
    }
}
