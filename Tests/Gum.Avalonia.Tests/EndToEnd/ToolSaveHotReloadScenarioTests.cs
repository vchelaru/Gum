using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.DataTypes;
using Gum.Services.Dialogs;
using Gum.Dialogs;
using Gum.Wireframe;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.Commands;
using Gum.DataTypes.Variables;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Diagnostics;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// The seam between the tool and a running game: the edits a demo makes in the tool, saved by the
/// tool's own code, reach a runtime that watches the same folder with hot reload on. The tool and the
/// runtime run in one process (<see cref="ToolSaveRuntimeWorld"/>); the file watcher, the debounce
/// and the reload are real.
/// </summary>
[Trait("Category", "EndToEnd")]
public class ToolSaveHotReloadScenarioTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    // The debounce is 200 ms; the rest is the watcher's event and the reload itself.
    private static readonly TimeSpan _reloadBudget = TimeSpan.FromMilliseconds(1500);

    /// <summary>A Card component (a Text and a rectangle) and a screen that shows one, a heading and a spacer.</summary>
    private static (ComponentSave Card, ScreenSave Main) Build(ProjectTreeHarness tree)
    {
        ComponentSave card = tree.Project.AddComponent("Card");
        tree.Project.AddInstance(card, "Title", "Text");
        tree.Project.AddInstance(card, "Fill", "Rectangle");
        ScreenSave main = tree.Project.AddScreen("Main");
        tree.Project.AddInstance(main, "Card1", "Card");
        tree.Project.AddInstance(main, "Heading", "Text");
        tree.Project.AddInstance(main, "Spacer", "Container");
        tree.SaveAll();
        return (card, main);
    }

    private static void ShouldReloadWithinTheDebounceBudget(ToolSaveRuntimeWorld world, string what)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        world.WaitForReload(_reloadBudget).ShouldBeTrue($"{what} should reach the running game");
        stopwatch.Elapsed.ShouldBeLessThan(_reloadBudget);
    }

    [AvaloniaFact]
    public void ValueEditsMadeInTheTool_ReachTheRunningTree_AndKeepWhatTheGameDid()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        (ComponentSave card, ScreenSave main) = Build(tree);
        using ToolSaveRuntimeWorld world = new ToolSaveRuntimeWorld(tree.Project.ProjectFilePath, "Main");
        GraphicalUiElement heading = world.Find("Heading")!;
        GraphicalUiElement runtimeOnly = new GraphicalUiElement { Name = "RuntimeOnly" };
        runtimeOnly.Parent = world.Screen;
        heading.X = 77;

        // Text typed in the tool
        tree.Click(tree.NodeFor(main.GetInstance("Heading")!));
        tree.Grid.TypeAndLeave("Text", "Hello Gum");
        ShouldReloadWithinTheDebounceBudget(world, "a Text edit");
        world.TextOf("Heading").ShouldBe("Hello Gum");
        world.Find("Heading").ShouldBeSameAs(heading);

        // Width typed in the tool
        tree.Click(tree.NodeFor(main.GetInstance("Spacer")!));
        tree.Grid.TypeAndEnter("Width", "123");
        ShouldReloadWithinTheDebounceBudget(world, "a Width edit");
        world.Find("Spacer")!.Width.ShouldBe(123f);

        // A color typed inside the component reaches the Card instance's child
        tree.Click(tree.NodeFor(card.GetInstance("Title")!));
        tree.Grid.Input.TypeAndEnter(tree.Grid.Editor<ColorDisplay>("Color").HexTextBox, "C86400");
        ShouldReloadWithinTheDebounceBudget(world, "a color edit in a component");
        ((RecordingRenderable)world.Find("Card1")!.GetGraphicalUiElementByName("Title")!.RenderableComponent).Red.ShouldBe(200);

        // What the game did stays: its own child, and a value the project never changed
        runtimeOnly.Parent.ShouldBeSameAs(world.Screen);
        world.Screen.Children.ShouldContain(runtimeOnly);
        heading.X.ShouldBe(77f);
        heading.X = 0;
        world.ErrorOutput.ShouldBeEmpty();
        ToolSaveRuntimeWorld.Describe(world.Screen).ShouldBe(world.DescribeAFreshLoad(), "the running tree should match what a restart would show");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void StructuralEditsMadeInTheTool_ReachTheRunningTree()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        (ComponentSave card, ScreenSave main) = Build(tree);
        using ToolSaveRuntimeWorld world = new ToolSaveRuntimeWorld(tree.Project.ProjectFilePath, "Main");
        GraphicalUiElement spacer = world.Find("Spacer")!;

        // Add an instance
        InstanceSave extra = tree.Project.AddInstance(main, "Extra", "Text");
        ShouldReloadWithinTheDebounceBudget(world, "an added instance");
        world.Find("Extra").ShouldNotBeNull();

        // Rename it
        tree.Click(tree.NodeFor(extra));
        tree.Dialogs.AnswerNextUserString("Subtitle");
        tree.Press(Key.F2, PhysicalKey.F2);
        ShouldReloadWithinTheDebounceBudget(world, "a renamed instance");
        world.Find("Subtitle").ShouldNotBeNull();
        world.Find("Extra").ShouldBeNull();

        // Delete it
        tree.Click(tree.NodeFor(main.GetInstance("Subtitle")!));
        tree.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(_ => true);
        tree.Press(Key.Delete, PhysicalKey.Delete);
        ShouldReloadWithinTheDebounceBudget(world, "a deleted instance");
        world.Find("Subtitle").ShouldBeNull();

        // The instances nobody touched are the same objects
        world.Find("Spacer").ShouldBeSameAs(spacer);
        world.ErrorOutput.ShouldBeEmpty();
        ToolSaveRuntimeWorld.Describe(world.Screen).ShouldBe(world.DescribeAFreshLoad(), "the running tree should match what a restart would show");
        tree.AssertOracles();
    }
    [AvaloniaFact]
    public void ANewComponent_ItsRenameAndItsMoveIntoAFolder_ReachTheInstancesThatShowIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        (ComponentSave card, ScreenSave main) = Build(tree);
        tree.Project.AddComponent("Controls/Toggle");
        tree.SaveAll();
        using ToolSaveRuntimeWorld world = new ToolSaveRuntimeWorld(tree.Project.ProjectFilePath, "Main");

        // A component made and used straight away: its file, the project file and the screen are three saves
        ComponentSave badge = tree.Project.AddComponent("Badge");
        tree.Project.AddInstance(badge, "Label", "Text");
        tree.Project.AddInstance(main, "Badge1", "Badge");
        world.WaitForReload(_reloadBudget).ShouldBeTrue("a new component and its first instance should reach the running game");
        world.ReloadsDuring(TimeSpan.FromMilliseconds(500));
        world.Find("Badge1")!.ElementSave!.Name.ShouldBe("Badge");
        world.Find("Badge1")!.GetGraphicalUiElementByName("Label").ShouldNotBeNull();

        // Renamed in the tool
        tree.Click(tree.NodeFor(badge));
        tree.Dialogs.AnswerNext<RenameElementDialogViewModel>(dialog => { dialog.Value = "Tag"; return true; });
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.Press(Key.F2, PhysicalKey.F2);
        world.WaitForReload(_reloadBudget).ShouldBeTrue("a component rename should reach the running game");
        world.ReloadsDuring(TimeSpan.FromMilliseconds(500));
        world.Find("Badge1")!.ElementSave!.Name.ShouldBe("Tag");
        world.Find("Badge1")!.GetGraphicalUiElementByName("Label").ShouldNotBeNull();

        // Dragged into a folder
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.BeginDrag(tree.NodeFor(badge));
        tree.DropOn(tree.FolderNode("Components", "Controls"));
        world.WaitForReload(_reloadBudget).ShouldBeTrue("a component moved into a folder should reach the running game");
        world.ReloadsDuring(TimeSpan.FromMilliseconds(500));
        world.Find("Badge1")!.ElementSave!.Name.ShouldBe("Controls/Tag");
        world.Find("Badge1")!.GetGraphicalUiElementByName("Label").ShouldNotBeNull();

        // Each rename rebuilt Badge1's visual, and the game is told so
        world.ErrorOutput.ShouldContain("'Badge1' in 'Main' is now a 'Tag' (was 'Badge')");
        world.ErrorOutput.ShouldContain("'Badge1' in 'Main' is now a 'Controls/Tag' (was 'Tag')");
        ToolSaveRuntimeWorld.Describe(world.Screen).ShouldBe(world.DescribeAFreshLoad(), "the running tree should match what a restart would show");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void ComponentRenamedInTheTool_ReportsOneRebuiltInstance_AndTheTreeStillMatchesARestart()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        (ComponentSave card, ScreenSave main) = Build(tree);
        using ToolSaveRuntimeWorld world = new ToolSaveRuntimeWorld(tree.Project.ProjectFilePath, "Main");

        tree.Click(tree.NodeFor(card));
        tree.Dialogs.AnswerNext<RenameElementDialogViewModel>(dialog => { dialog.Value = "Panel"; return true; });
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.Press(Key.F2, PhysicalKey.F2);
        world.WaitForReload(_reloadBudget).ShouldBeTrue("a component rename should reach the running game");
        world.ReloadsDuring(TimeSpan.FromMilliseconds(500));

        List<HotReloadIssue> issues = world.Results.SelectMany(result => result.Issues).ToList();
        HotReloadIssue rebuilt = issues.Single(issue => issue.Kind == HotReloadIssueKind.InstanceRebuilt);
        rebuilt.ElementName.ShouldBe("Main");
        rebuilt.InstanceName.ShouldBe("Card1");
        rebuilt.Message.ShouldContain("is now a 'Panel' (was 'Card')");
        world.Results.ShouldAllBe(result => result.MatchesRestart);
        world.ErrorOutput.ShouldContain(rebuilt.Message);
        world.Find("Card1")!.ElementSave!.Name.ShouldBe("Panel");
        world.Find("Card1")!.GetGraphicalUiElementByName("Title").ShouldNotBeNull();
        world.Screen.Children.Select(child => child.Name).ShouldBe(new[] { "Card1", "Heading", "Spacer" });

        // An ordinary edit afterwards is clean again
        tree.Click(tree.NodeFor(main.GetInstance("Heading")!));
        tree.Grid.TypeAndLeave("Text", "After");
        ShouldReloadWithinTheDebounceBudget(world, "a Text edit");
        world.Results[world.Results.Count - 1].IsClean.ShouldBeTrue();
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void ComponentBaseTypeChangedInTheTool_IsListed_BecauseTheTreeDiffersFromARestart()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave pad = tree.Project.AddComponent("Pad");
        pad.DefaultState.SetValue("Width", 222f, "float");
        ComponentSave card = tree.Project.AddComponent("Card", "Pad");
        tree.Project.AddInstance(card, "Title", "Text");
        ScreenSave main = tree.Project.AddScreen("Main");
        tree.Project.AddInstance(main, "Card1", "Card");
        tree.SaveAll();
        using ToolSaveRuntimeWorld world = new ToolSaveRuntimeWorld(tree.Project.ProjectFilePath, "Main");
        world.Find("Card1")!.Width.ShouldBe(222f);

        card.BaseType = "Container";
        tree.SaveAll();
        world.WaitForReload(_reloadBudget).ShouldBeTrue("a base type change should reach the running game");
        world.ReloadsDuring(TimeSpan.FromMilliseconds(500));

        ToolSaveRuntimeWorld.Describe(world.Screen).ShouldNotBe(world.DescribeAFreshLoad(), "sanity: the visual keeps what it inherited");
        HotReloadIssue issue = world.Results.SelectMany(result => result.Issues)
            .First(candidate => candidate.Kind == HotReloadIssueKind.ComponentBaseTypeChanged);
        issue.ElementName.ShouldBe("Card");
        world.Results[world.Results.Count - 1].MatchesRestart.ShouldBeFalse();
        world.ErrorOutput.ShouldContain(issue.Message);
    }

    [AvaloniaFact]
    public void ManyEditsThenSaveAll_AndTwoQuickSaves_LeaveTheRunningTreeOnTheLastSave()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        (ComponentSave card, ScreenSave main) = Build(tree);
        using ToolSaveRuntimeWorld world = new ToolSaveRuntimeWorld(tree.Project.ProjectFilePath, "Main");

        // Several edits with Auto Save on, then File > Save All, with no pause between them
        tree.Click(tree.NodeFor(main.GetInstance("Spacer")!));
        tree.Grid.TypeAndEnter("Width", "10");
        tree.Grid.TypeAndEnter("Width", "20");
        tree.Grid.TypeAndEnter("Width", "30");
        tree.Click(tree.NodeFor(main.GetInstance("Heading")!));
        tree.Grid.TypeAndLeave("Text", "Quick");
        tree.SaveAll();
        tree.SaveAll();
        world.WaitForReload(_reloadBudget).ShouldBeTrue("a burst of saves should reach the running game");
        world.ReloadsDuring(TimeSpan.FromMilliseconds(600)).ShouldBe(0, "the burst should have been one reload, not a trailing second one");
        world.Find("Spacer")!.Width.ShouldBe(30f);
        world.TextOf("Heading").ShouldBe("Quick");
        world.ErrorOutput.ShouldBeEmpty();
        ToolSaveRuntimeWorld.Describe(world.Screen).ShouldBe(world.DescribeAFreshLoad(), "the running tree should match what a restart would show");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void AProjectInAFolderWithASpaceInItsName_ReloadsLikeAnyOther()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness(folderName: "Gum Project With Spaces");
        (ComponentSave card, ScreenSave main) = Build(tree);
        using ToolSaveRuntimeWorld world = new ToolSaveRuntimeWorld(tree.Project.ProjectFilePath, "Main");

        tree.Click(tree.NodeFor(main.GetInstance("Heading")!));
        tree.Grid.TypeAndLeave("Text", "With spaces");

        ShouldReloadWithinTheDebounceBudget(world, "an edit in a folder with a space");
        world.TextOf("Heading").ShouldBe("With spaces");
        world.ErrorOutput.ShouldBeEmpty();
    }

    [AvaloniaFact]
    public void AStateAddedAndEditedInTheTool_CanBeAppliedByTheRunningGame()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        (ComponentSave card, ScreenSave main) = Build(tree);
        using ToolSaveRuntimeWorld world = new ToolSaveRuntimeWorld(tree.Project.ProjectFilePath, "Main");

        StateSaveCategory look = tree.Project.AddCategory(card, "Look");
        StateSave big = tree.Project.AddState(card, look, "Big");
        big.SetValue("Title.Text", "Big!", "string");
        Services.GetRequiredService<IFileCommands>().TryAutoSaveElement(card);

        world.WaitForReload(_reloadBudget).ShouldBeTrue("a new state should reach the running game");
        world.Find("Card1")!.ApplyState("Look", "Big");
        ((RecordingRenderable)world.Find("Card1")!.GetGraphicalUiElementByName("Title")!.RenderableComponent).Text.ShouldBe("Big!");
        world.ErrorOutput.ShouldBeEmpty();
    }
}
