using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.Input;
using Gum.Managers;
using Gum.Menus;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests.Menus;

public class StandardMenuModelBuilderTests
{
    [Fact]
    public void Build_UndoAndRedo_CarryTheHotkeyManagersBindings()
    {
        KeyCombination undo = KeyCombination.Ctrl(GumKey.Z);
        KeyCombination redo = KeyCombination.Ctrl(GumKey.Y);
        Mock<IHotkeyManager> hotkeyManager = new Mock<IHotkeyManager>();
        hotkeyManager.Setup(h => h.Undo).Returns(undo);
        hotkeyManager.Setup(h => h.Redo).Returns(redo);
        StandardMenuModelBuilder builder = new StandardMenuModelBuilder(
            Mock.Of<ISelectedState>(),
            Mock.Of<IUndoManager>(),
            Mock.Of<IEditCommands>(),
            Mock.Of<IDialogService>(),
            Mock.Of<IFileCommands>(),
            Mock.Of<IProjectManager>(),
            Mock.Of<IMessenger>(),
            Mock.Of<IFileSystemRevealService>(),
            Mock.Of<IDispatcher>(),
            hotkeyManager.Object);

        MenuModel model = builder.Build();

        MenuItemModel edit = model.GetItem("Edit").ShouldNotBeNull();
        edit.Items.Single(i => i.Header == "Undo").Gesture.ShouldBeSameAs(undo);
        edit.Items.Single(i => i.Header == "Redo").Gesture.ShouldBeSameAs(redo);
    }

    [Fact]
    public void Build_ViewFocusOnSelection_AsksTheCanvasToFocus_AndShowsTheHotkey()
    {
        KeyCombination focus = KeyCombination.Pressed(GumKey.F);
        AutoMocker mocker = new AutoMocker();
        mocker.GetMock<IHotkeyManager>().Setup(h => h.FocusSelection).Returns(focus);
        WeakReferenceMessenger messenger = new WeakReferenceMessenger();
        mocker.Use<IMessenger>(messenger);
        ZoomCanvasToFitSelectionMessage? request = null;
        object recipient = new object();
        messenger.Register<ZoomCanvasToFitSelectionMessage>(recipient, (_, message) => request = message);
        StandardMenuModelBuilder builder = mocker.CreateInstance<StandardMenuModelBuilder>();

        MenuItemModel item = builder.Build().GetItem("View")!.Items.Single(i => i.Header == "Focus on Selection");
        item.Invoke();

        item.Gesture.ShouldBeSameAs(focus);
        request.ShouldNotBeNull();
        request.FrameSelectedInstances.ShouldBeTrue();
        request.MaxZoomPercent.ShouldBeGreaterThan(100);
    }

    [Theory]
    [InlineData("Zoom In", true)]
    [InlineData("Zoom Out", false)]
    public void Build_ViewZoomItems_AskTheCanvasToStepItsZoom_WithoutAKeyEquivalent(string header, bool zoomsIn)
    {
        AutoMocker mocker = new AutoMocker();
        WeakReferenceMessenger messenger = new WeakReferenceMessenger();
        mocker.Use<IMessenger>(messenger);
        List<StepCanvasZoomMessage> requests = new List<StepCanvasZoomMessage>();
        object recipient = new object();
        messenger.Register<StepCanvasZoomMessage>(recipient, (_, message) => requests.Add(message));
        StandardMenuModelBuilder builder = mocker.CreateInstance<StandardMenuModelBuilder>();

        MenuItemModel item = builder.Build().GetItem("View")!.Items.Single(i => i.Header == header);
        item.Invoke();

        requests.Count.ShouldBe(1);
        requests[0].ZoomIn.ShouldBe(zoomsIn);
        // The zoom hotkeys zoom the whole app instead of the canvas when the tree has focus, and a menu
        // key equivalent (macOS) would override that, so the items carry no gesture.
        item.Gesture.ShouldBeNull();
    }

    [Fact]
    public void Build_AddScreenAndComponent_GoThroughEditCommandsSaveGuard()
    {
        AutoMocker mocker = new AutoMocker();
        Mock<IEditCommands> editCommands = mocker.GetMock<IEditCommands>();
        StandardMenuModelBuilder builder = mocker.CreateInstance<StandardMenuModelBuilder>();

        MenuModel model = builder.Build();
        MenuItemModel add = model.GetItem("Edit")!.Items.Single(item => item.Header == "Add");
        add.Items.Single(item => item.Header == "Screen").Invoke();
        add.Items.Single(item => item.Header == "Component").Invoke();

        editCommands.Verify(e => e.ShowAddScreenDialog(), Times.Once);
        editCommands.Verify(e => e.ShowAddComponentDialog(), Times.Once);
    }

    [Fact]
    public void ShowAbout_ShowsTheVersionMessage_SameAsHelpAbout()
    {
        AutoMocker mocker = new AutoMocker();
        Mock<IDialogService> dialogService = mocker.GetMock<IDialogService>();
        StandardMenuModelBuilder builder = mocker.CreateInstance<StandardMenuModelBuilder>();

        builder.ShowAbout();

        dialogService.Verify(d => d.ShowMessage(It.Is<string>(m => m.StartsWith("Gum version")), "About", null), Times.Once);

        MenuModel model = builder.Build();
        MenuItemModel about = model.GetItem("Help")!.Items.Single(item => item.Header == "About...");
        about.Invoke();
        dialogService.Verify(d => d.ShowMessage(It.Is<string>(m => m.StartsWith("Gum version")), "About", null), Times.Exactly(2));
    }
}
