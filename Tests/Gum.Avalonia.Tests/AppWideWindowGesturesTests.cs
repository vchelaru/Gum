using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Gum.Avalonia.Plugins.TreeView;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Managers;
using Gum.SelectionHistory;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Gestures the main window handles for everything inside it: the mouse's back/forward side
/// buttons step through selection history, and dropping a project file opens it. Each test hosts
/// the element tree, which handles its own presses and rejects file drags, so the window must act
/// before the control under the pointer does.
/// </summary>
public class AppWideWindowGesturesTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaFact]
    public void MouseSideButtons_OverTheTree_StepBackAndForwardThroughSelections()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumAppWideGestures");
        ComponentSave first = fixture.AddComponent("First");
        ComponentSave second = fixture.AddComponent("Second");
        fixture.SelectedState.SelectedElement.ShouldBeSameAs(second);
        using HeadlessWindowDriver driver = HostTree();
        AppWideWindowGestures.RouteSelectionHistoryButtons(driver.Window, Services.GetRequiredService<ISelectionHistory>());
        Point overTree = new Point(100, 100);

        driver.Window.MouseDown(overTree, MouseButton.XButton1);
        driver.Window.MouseUp(overTree, MouseButton.XButton1);
        Dispatcher.UIThread.RunJobs();

        fixture.SelectedState.SelectedElement.ShouldBeSameAs(first);

        driver.Window.MouseDown(overTree, MouseButton.XButton2);
        driver.Window.MouseUp(overTree, MouseButton.XButton2);
        Dispatcher.UIThread.RunJobs();

        fixture.SelectedState.SelectedElement.ShouldBeSameAs(second);

        // An ordinary click is the tree's, not a history step.
        driver.Window.MouseDown(overTree, MouseButton.Left);
        driver.Window.MouseUp(overTree, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        fixture.SelectedState.SelectedElement.ShouldBeSameAs(second);
    }

    [AvaloniaFact]
    public void DroppingAProjectFile_OverTheTree_OpensThatProject()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumAppWideGestures");
        string droppedFolder = Path.Combine(fixture.ProjectFolder, "Dropped");
        Directory.CreateDirectory(droppedFolder);
        string droppedProject = Path.Combine(droppedFolder, "Dropped.gumx");
        File.WriteAllText(droppedProject, "<?xml version=\"1.0\" encoding=\"utf-8\"?><GumProjectSave><Version>1</Version></GumProjectSave>");
        using HeadlessWindowDriver driver = HostTree();
        OpenDroppedProjects(driver.Window);
        DataTransfer data = FileDrop(droppedProject);
        Point overTree = new Point(100, 100);

        DragEffectsRecorder effects = new DragEffectsRecorder(driver.Window);

        driver.Window.DragDrop(overTree, RawDragEventType.DragEnter, data, DragDropEffects.Copy);
        effects.Last.ShouldBe(DragDropEffects.Copy);
        driver.Window.DragDrop(overTree, RawDragEventType.DragOver, data, DragDropEffects.Copy);
        effects.Last.ShouldBe(DragDropEffects.Copy);
        driver.Window.DragDrop(overTree, RawDragEventType.Drop, data, DragDropEffects.Copy);

        IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();
        PumpUntil(() => IsOpen(projectManager, droppedProject));
        IsOpen(projectManager, droppedProject).ShouldBeTrue();
    }

    [AvaloniaFact]
    public void DroppedProjectThatFailsToLoad_ShowsTheError()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumAppWideGestures");
        string droppedProject = Path.Combine(fixture.ProjectFolder, "Broken.gumx");
        Mock<IProjectFileDropLogic> dropLogic = new Mock<IProjectFileDropLogic>();
        dropLogic.Setup(logic => logic.GetProjectFileToOpen(It.IsAny<IEnumerable<string>>())).Returns(droppedProject);
        dropLogic.Setup(logic => logic.TryOpenDroppedProjectAsync(It.IsAny<IEnumerable<string>>()))
            .ThrowsAsync(new InvalidOperationException("disk on fire"));
        using HeadlessWindowDriver driver = HostTree();
        AppWideWindowGestures.OpenDroppedProjects(driver.Window, dropLogic.Object, fixture.Dialogs);
        fixture.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);

        DataTransfer data = FileDrop(droppedProject);
        Point overTree = new Point(100, 100);

        driver.Window.DragDrop(overTree, RawDragEventType.DragEnter, data, DragDropEffects.Copy);
        driver.Window.DragDrop(overTree, RawDragEventType.Drop, data, DragDropEffects.Copy);
        PumpUntil(() => fixture.Dialogs.Messages.Count > 0);

        fixture.Dialogs.Messages.ShouldHaveSingleItem().ShouldContain("disk on fire");
    }

    [AvaloniaFact]
    public void DroppingANonProjectFile_IsLeftToTheControlUnderneath()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumAppWideGestures");
        using HeadlessWindowDriver driver = HostTree();
        OpenDroppedProjects(driver.Window);
        DataTransfer data = FileDrop(Path.Combine(fixture.ProjectFolder, "Picture.png"));
        Point overTree = new Point(100, 100);

        DragEffectsRecorder effects = new DragEffectsRecorder(driver.Window);

        // A bare tree accepts no file (nothing listens for its external drops).
        driver.Window.DragDrop(overTree, RawDragEventType.DragOver, data, DragDropEffects.Copy);
        effects.Last.ShouldBe(DragDropEffects.None);
        driver.Window.DragDrop(overTree, RawDragEventType.Drop, data, DragDropEffects.Copy);
        Dispatcher.UIThread.RunJobs();

        Services.GetRequiredService<IProjectManager>().GumProjectSave.ShouldBeSameAs(fixture.Project);
    }

    private static HeadlessWindowDriver HostTree()
    {
        AvaloniaGumTreeView tree = new AvaloniaGumTreeView();
        tree.Nodes.Add(new GumTreeNode("Screens"));
        DragDrop.SetAllowDrop(tree, true);
        return new HeadlessWindowDriver(tree, width: 300, height: 300, framesFolderName: "GumAppWideGestures");
    }

    private static void OpenDroppedProjects(Window window) =>
        AppWideWindowGestures.OpenDroppedProjects(
            window,
            Services.GetRequiredService<IProjectFileDropLogic>(),
            Services.GetRequiredService<IDialogService>());

    private static DataTransfer FileDrop(string path)
    {
        Mock<IStorageFile> file = new Mock<IStorageFile>();
        file.SetupGet(f => f.Path).Returns(new Uri(path));
        DataTransfer data = new DataTransfer();
        data.Add(DataTransferItem.CreateFile(file.Object));
        return data;
    }

    // The effects a drag reports back to its source: what they are once the event has finished
    // routing, after every window handler registered before this one.
    private sealed class DragEffectsRecorder
    {
        public DragEffectsRecorder(Window window)
        {
            EventHandler<DragEventArgs> record = (_, e) => Last = e.DragEffects;
            window.AddHandler(DragDrop.DragEnterEvent, record, handledEventsToo: true);
            window.AddHandler(DragDrop.DragOverEvent, record, handledEventsToo: true);
        }

        public DragDropEffects? Last { get; private set; }
    }

    private static bool IsOpen(IProjectManager projectManager, string projectFile) =>
        projectManager.GumProjectSave?.FullFileName is { } open && new FilePath(open) == new FilePath(projectFile);

    private static void PumpUntil(Func<bool> done)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(10);
        while (!done() && DateTime.UtcNow < deadline)
        {
            Thread.Sleep(10);
            Dispatcher.UIThread.RunJobs();
        }
    }
}
