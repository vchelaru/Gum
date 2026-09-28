using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Gum.Avalonia.Shell;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end drag and drop (inventory area DRAG): tree rows dragged onto other rows, folders and
/// the canvas, search results dragged onto the canvas, and files dropped from the file manager.
/// The tree starts each drag itself from a press and a move past its threshold; the harness stands
/// in for the platform's drag loop (<see cref="ProjectTreeHarness.BeginDrag"/>).
/// </summary>
[Trait("Category", "EndToEnd")]
public class DragDropScenarioTests
{
    #region Within the tree

    [AvaloniaFact]
    [Trait("Feature", "DRAG-001")]
    [Trait("Feature", "DRAG-002")]
    public void DraggingInstanceRows_ReordersThem_AndIntoAContainerReparentsThem_KeepingTheirOrder()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Project.AddInstance(button, "A", "Rectangle");
        tree.Project.AddInstance(button, "B", "Rectangle");
        tree.Project.AddInstance(button, "C", "Rectangle");
        tree.Project.AddInstance(button, "Panel", "Container");
        tree.Click(tree.NodeFor(button));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        // Dropped on the top of A's row: before A.
        tree.BeginDrag(tree.NodeFor(button.GetInstance("C")!));
        tree.DropOn(tree.NodeFor(button.GetInstance("A")!), fraction: 0.1).ShouldBe(DragDropEffects.Move);

        button.Instances.Select(instance => instance.Name).ShouldBe(new[] { "C", "A", "B", "Panel" });
        tree.ChildTexts(tree.NodeFor(button)).ShouldBe(new[] { "C", "A", "B", "Panel" });
        tree.Undo();
        tree.SnapshotFiles().ShouldMatch(start, "undoing the reorder should restore the files");
        tree.Redo();
        button.Instances.Select(instance => instance.Name).ShouldBe(new[] { "C", "A", "B", "Panel" });

        // Dropped on the middle of Panel's row: into Panel.
        tree.BeginDrag(tree.NodeFor(button.GetInstance("A")!));
        tree.DropOn(tree.NodeFor(button.GetInstance("Panel")!));
        button.DefaultState!.GetValue("A.Parent").ShouldBe("Panel");
        tree.ChildTexts(tree.NodeFor(button.GetInstance("Panel")!)).ShouldBe(new[] { "A" });

        // Two selected rows move together, in the order they had.
        tree.Click(tree.NodeFor(button.GetInstance("C")!));
        tree.Click(tree.NodeFor(button.GetInstance("B")!), RawInputModifiers.Control);
        tree.BeginDrag(tree.NodeFor(button.GetInstance("B")!));
        tree.DropOn(tree.NodeFor(button.GetInstance("Panel")!));
        button.DefaultState.GetValue("B.Parent").ShouldBe("Panel");
        button.DefaultState.GetValue("C.Parent").ShouldBe("Panel");
        tree.ChildTexts(tree.NodeFor(button.GetInstance("Panel")!)).ShouldBe(new[] { "A", "C", "B" });

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DRAG-003")]
    [Trait("Feature", "DRAG-004")]
    [Trait("Feature", "DRAG-005")]
    public void DraggingElementRows_OntoAScreenAddsAnInstance_OntoAFolderMovesTheFile_AndFoldersMoveIntoFolders()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave icon = tree.Project.AddComponent("Icon");
        tree.Project.AddComponent("Controls/Toggle");
        tree.Project.AddComponent("Shared/Placeholder");
        ScreenSave main = tree.Project.AddScreen("MainScreen");
        tree.Click(tree.NodeFor(main));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.BeginDrag(tree.NodeFor(icon));
        tree.DropOn(tree.NodeFor(main)).ShouldBe(DragDropEffects.Move);

        main.Instances.ShouldHaveSingleItem().BaseType.ShouldBe("Icon");
        tree.Undo();
        tree.SnapshotFiles().ShouldMatch(start, "undoing the added instance should restore the files");
        tree.Redo();
        main.Instances.ShouldHaveSingleItem().BaseType.ShouldBe("Icon");

        string components = Path.Combine(tree.Project.ProjectFolder, "Components");
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.BeginDrag(tree.NodeFor(icon));
        tree.DropOn(tree.FolderNode("Components", "Controls"));

        icon.Name.ShouldBe("Controls/Icon");
        File.Exists(Path.Combine(components, "Controls", "Icon.gucx")).ShouldBeTrue();
        File.Exists(Path.Combine(components, "Icon.gucx")).ShouldBeFalse();
        main.Instances.Single().BaseType.ShouldBe("Controls/Icon");
        tree.ChildTexts(tree.FolderNode("Components", "Controls")).ShouldBe(new[] { "Icon", "Toggle" });

        tree.BeginDrag(tree.FolderNode("Components", "Controls"));
        tree.DropOn(tree.FolderNode("Components", "Shared"));

        tree.Project.Project.Components.Select(component => component.Name)
            .ShouldBe(new[] { "Shared/Controls/Icon", "Shared/Controls/Toggle", "Shared/Placeholder" }, ignoreOrder: true);
        File.Exists(Path.Combine(components, "Shared", "Controls", "Toggle.gucx")).ShouldBeTrue();
        Directory.Exists(Path.Combine(components, "Controls")).ShouldBeFalse();
        main.Instances.Single().BaseType.ShouldBe("Shared/Controls/Icon");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DRAG-006")]
    [Trait("Feature", "DRAG-007")]
    public void DraggingABehaviorOntoAComponent_AddsIt_AndAnInstanceOntoTheBehavior_RequiresIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Dialogs.AnswerNextUserString("Clickable");
        tree.RightClick(tree.RootNode("Behaviors"));
        tree.PickMenu("Add Behavior");
        BehaviorSave clickable = tree.Project.Project.Behaviors.Single();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Project.AddInstance(button, "Background", "NineSlice");

        tree.BeginDrag(tree.NodeFor(clickable));
        tree.DropOn(tree.NodeFor(button)).ShouldBe(DragDropEffects.Move);

        button.Behaviors.Select(behavior => behavior.BehaviorName).ShouldBe(new[] { "Clickable" });
        File.ReadAllText(Path.Combine(tree.Project.ProjectFolder, "Components", "Button.gucx")).ShouldContain("Clickable");

        tree.BeginDrag(tree.NodeFor(button.GetInstance("Background")!));
        tree.DropOn(tree.NodeFor(clickable)).ShouldBe(DragDropEffects.Move);

        BehaviorInstanceSave required = clickable.RequiredInstances.ShouldHaveSingleItem();
        (required.Name, required.BaseType).ShouldBe(("Background", "NineSlice"));
        File.ReadAllText(Path.Combine(tree.Project.ProjectFolder, "Behaviors", "Clickable.behx")).ShouldContain("Background");
        button.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Background" }, "the instance is required, not moved");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DRAG-010")]
    public void ScreenFileFromTheFileManager_DroppedOnScreens_IsImported()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        string incoming = Path.Combine(Path.GetTempPath(), "GumEndToEnd", "Incoming-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(incoming);
        try
        {
            ScreenSave outside = new ScreenSave { Name = "Imported" };
            outside.States.Add(new StateSave { Name = "Default", ParentContainer = outside });
            string file = Path.Combine(incoming, "Imported.gusx");
            outside.Save(file);

            // No pointer moves over the tree while a file is dragged in from another application.
            tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
            tree.DropFilesOn(tree.RootNode("Screens"), FileDrop(file)).ShouldBe(DragDropEffects.Copy);

            tree.Project.Project.Screens.Select(screen => screen.Name).ShouldBe(new[] { "Imported" });
            File.Exists(Path.Combine(tree.Project.ProjectFolder, "Screens", "Imported.gusx")).ShouldBeTrue();
            tree.ChildTexts(tree.RootNode("Screens")).ShouldBe(new[] { "Imported" });

            tree.AssertOracles();
        }
        finally
        {
            Directory.Delete(incoming, recursive: true);
        }
    }

    [AvaloniaFact]
    [Trait("Feature", "DRAG-015")]
    public void ProjectFileDroppedOnTheWindow_OpensIt_AndAScreenFileInTheSameDropIsNotImported()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Menu");
        tree.SaveAll();
        // A copy of the project elsewhere, and a screen file beside it.
        string otherFolder = Path.Combine(tree.Project.ProjectFolder, "Other");
        CopyFolder(tree.Project.ProjectFolder, otherFolder);
        string otherProject = Path.Combine(otherFolder, Path.GetFileName(tree.Project.ProjectFilePath));
        ScreenSave looseScreen = new ScreenSave { Name = "Loose" };
        looseScreen.States.Add(new StateSave { Name = "Default", ParentContainer = looseScreen });
        string looseFile = Path.Combine(otherFolder, "Loose.gusx");
        looseScreen.Save(looseFile);
        // What the main window does with a drop anywhere in it.
        AppWideWindowGestures.OpenDroppedProjects(tree.Input.Window,
            TestAppBuilder.Services.GetRequiredService<IProjectFileDropLogic>(),
            TestAppBuilder.Services.GetRequiredService<IDialogService>());
        IProjectManager projectManager = TestAppBuilder.Services.GetRequiredService<IProjectManager>();

        tree.DropFilesOn(tree.RootNode("Screens"), FileDrop(looseFile, otherProject)).ShouldBe(DragDropEffects.Copy);
        tree.WaitUntil(() => projectManager.GumProjectSave?.FullFileName is { } open && new FilePath(open) == new FilePath(otherProject),
            TimeSpan.FromSeconds(10), "the dropped project to open");

        projectManager.GumProjectSave!.Screens.ShouldBeEmpty("the screen file is not imported into either project");
        tree.ChildTexts(tree.RootNode("Components")).ShouldBe(new[] { "Menu" });
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Screens", "Loose.gusx")).ShouldBeFalse();

        tree.AssertOracles();
    }

    private static void CopyFolder(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (string file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(from, file);
            string target = Path.Combine(to, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

    #endregion

    #region Onto the canvas

    [SkippableFact]
    [Trait("Feature", "DRAG-009")]
    [Trait("Feature", "DRAG-011")]
    [Trait("Feature", "DRAG-014")]
    public void TreeRowsSearchResultsAndAnimationFiles_DroppedOnTheCanvas_AddInstancesWhereTheyLand()
    {
        Skip.IfNot(CanvasHarness.CanRun, CanvasHarness.SkipReason);
        CanvasHarness.OnUiThread(() =>
        {
            using CanvasHarness canvas = new CanvasHarness();
            ComponentSave icon = canvas.Project.AddComponent("Icon");
            ComponentSave button = canvas.Project.AddComponent("Button");
            canvas.Tree.Click(canvas.Tree.NodeFor(button));
            ProjectFileSnapshot start = canvas.Tree.SnapshotFiles();

            // A component's row dragged from the tree onto the canvas.
            canvas.Tree.BeginDrag(canvas.Tree.NodeFor(icon));
            canvas.DropOnCanvas(canvas.WindowPointOf(60, 70), canvas.Tree.CurrentDrag).ShouldBe(DragDropEffects.Copy);
            canvas.Tree.EndDrag(DragDropEffects.Copy);

            InstanceSave fromTree = button.Instances.ShouldHaveSingleItem();
            fromTree.BaseType.ShouldBe("Icon");
            canvas.SavedValue(button, $"{fromTree.Name}.X").ShouldBe(60f);
            canvas.SavedValue(button, $"{fromTree.Name}.Y").ShouldBe(70f);
            canvas.Project.SelectedState.SelectedElement.ShouldBeSameAs(button, "the canvas keeps showing the element dropped into");
            canvas.Undo();
            canvas.Tree.SnapshotFiles().ShouldMatch(start, "undoing the drop should restore the files");
            canvas.Redo();

            // A search result dragged onto the canvas.
            canvas.Tree.Search("Icon");
            canvas.Tree.BeginSearchResultDrag("Icon (Container)");
            canvas.DropOnCanvas(canvas.WindowPointOf(200, 70), canvas.Tree.CurrentDrag).ShouldBe(DragDropEffects.Copy);
            canvas.Tree.EndDrag(DragDropEffects.Copy);
            canvas.Tree.View.ClearSearchText();

            InstanceSave fromSearch = button.Instances.Single(instance => instance.Name != fromTree.Name);
            fromSearch.BaseType.ShouldBe("Icon");
            canvas.SavedValue(button, $"{fromSearch.Name}.X").ShouldBe(200f);

            // An animation chain file: a sprite showing it, animating its first chain.
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Content", "ExampleSpriteFrame.png"), Path.Combine(canvas.Project.ProjectFolder, "Hero.png"));
            string chains = Path.Combine(canvas.Project.ProjectFolder, "Hero.achx");
            File.WriteAllText(chains, """
                <AnimationChainArraySave>
                  <AnimationChain>
                    <Name>Walk</Name>
                    <Frame>
                      <TextureName>Hero.png</TextureName>
                      <FrameLength>0.1</FrameLength>
                      <RightCoordinate>1</RightCoordinate>
                      <BottomCoordinate>1</BottomCoordinate>
                    </Frame>
                  </AnimationChain>
                  <AnimationChain>
                    <Name>Run</Name>
                  </AnimationChain>
                </AnimationChainArraySave>
                """);
            canvas.DropOnCanvas(canvas.WindowPointOf(300, 300), FileDrop(chains)).ShouldBe(DragDropEffects.Copy);

            InstanceSave sprite = button.Instances.SingleOrDefault(instance => instance.BaseType == "Sprite")
                ?? throw new InvalidOperationException($"The drop added no sprite. Output: {canvas.Tree.OutputWritten}; {canvas.Describe()}");
            canvas.SavedValue(button, $"{sprite.Name}.SourceFile").ShouldBe("Hero.achx");
            canvas.SavedValue(button, $"{sprite.Name}.Animate").ShouldBe(true);
            canvas.SavedValue(button, $"{sprite.Name}.CurrentChainName").ShouldBe("Walk");

            canvas.AssertOracles();
        });
    }

    #endregion

    // What a file manager puts in its drag.
    private static DataTransfer FileDrop(params string[] paths)
    {
        DataTransfer data = new DataTransfer();
        foreach (string path in paths)
        {
            Mock<IStorageFile> file = new Mock<IStorageFile>();
            file.SetupGet(f => f.Path).Returns(new Uri(path));
            data.Add(DataTransferItem.CreateFile(file.Object));
        }
        return data;
    }
}
