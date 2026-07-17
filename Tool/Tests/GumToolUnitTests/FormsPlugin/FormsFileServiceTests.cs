using Gum.ToolStates;
using GumFormsPlugin.Services;
using Moq;
using Shouldly;

namespace GumToolUnitTests.FormsPlugin;

public class FormsFileServiceTests : BaseTestClass
{
    [Fact]
    public void GetSourceDestinations_ReturnsEmpty_WhenProjectDirectoryIsNull()
    {
        var projectState = new Mock<IProjectState>();
        projectState.Setup(p => p.ProjectDirectory).Returns((string?)null);
        var formsFileService = new FormsFileService(projectState.Object);

        var sourceDestinations = formsFileService.GetSourceDestinations(
            FormsFileService.DefaultThemeName, isIncludeDemoScreenGum: false);

        sourceDestinations.ShouldBeEmpty();
        projectState.VerifyGet(p => p.ProjectDirectory, Times.AtLeastOnce);
    }

    [Fact]
    public void GetSourceDestinations_ReturnsEmpty_WhenThemeDirectoryDoesNotExist()
    {
        var projectState = new Mock<IProjectState>();
        projectState.Setup(p => p.ProjectDirectory).Returns("C:/SomeProject/");
        var formsFileService = new FormsFileService(projectState.Object);

        var sourceDestinations = formsFileService.GetSourceDestinations(
            "ThemeThatDoesNotExist", isIncludeDemoScreenGum: false);

        sourceDestinations.ShouldBeEmpty();
    }
}
