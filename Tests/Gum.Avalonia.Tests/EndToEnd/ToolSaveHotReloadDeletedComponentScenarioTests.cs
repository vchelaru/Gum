using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Services.Dialogs;
using Gum.Wireframe;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// Hot reload when a component the shown screen uses is deleted in the tool.
/// </summary>
[Trait("Category", "EndToEnd")]
public class ToolSaveHotReloadDeletedComponentScenarioTests
{
    [AvaloniaFact]
    public void DeletingAComponentTheScreenShows_InTheTool_LeavesTheRunningTreeAsARestartWouldShowIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        tree.Project.AddInstance(card, "Title", "Text");
        ScreenSave main = tree.Project.AddScreen("Main");
        tree.Project.AddInstance(main, "Card1", "Card");
        tree.Project.AddInstance(main, "Heading", "Text");
        tree.SaveAll();
        using ToolSaveRuntimeWorld world = new ToolSaveRuntimeWorld(tree.Project.ProjectFilePath, "Main");

        tree.Click(tree.NodeFor(card));
        tree.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(_ => true);
        tree.RightClick(tree.NodeFor(card));
        tree.PickMenu("Delete");
        world.WaitForReload(TimeSpan.FromSeconds(2)).ShouldBeTrue();
        world.ReloadsDuring(TimeSpan.FromMilliseconds(500));

        ToolSaveRuntimeWorld.Describe(world.Screen).ShouldBe(world.DescribeAFreshLoad(), "the running tree should match what a restart would show");
    }
}
