using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.Input;
using Gum.Managers;
using Gum.Menus;
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
