using Gum.Menus;
using Gum.Services.Dialogs;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests.Menus;

public class StandardMenuModelBuilderTests
{
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
