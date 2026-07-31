using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolStates;
using Moq;
using Shouldly;
using StateAnimationPlugin.Managers;
using ToolsUtilities;
using Xunit;

namespace Gum.Presentation.Tests;

public class AnimationFilePathServiceTests
{
    private static AnimationFilePathService BuildSut(
        out Mock<ISelectedState> selectedStateMock,
        out Mock<IFileCommands> fileCommandsMock,
        string projectFullFileName = @"C:\Project\Project.gumx")
    {
        selectedStateMock = new Mock<ISelectedState>();
        fileCommandsMock = new Mock<IFileCommands>();

        Mock<IProjectManager> projectManagerMock = new Mock<IProjectManager>();
        projectManagerMock.Setup(p => p.GumProjectSave).Returns(new GumProjectSave { FullFileName = projectFullFileName });

        return new AnimationFilePathService(selectedStateMock.Object, fileCommandsMock.Object, projectManagerMock.Object);
    }

    [Fact]
    public void GetAbsoluteAnimationFileNameFor_ByElementName_ShouldReturnNull_WhenNoElementIsSelected()
    {
        AnimationFilePathService sut = BuildSut(out _, out _);

        FilePath? result = sut.GetAbsoluteAnimationFileNameFor("MyComponent");

        result.ShouldBeNull();
    }

    [Fact]
    public void GetAbsoluteAnimationFileNameFor_ByElementName_ShouldReturnGanxPath_WhenElementIsSelected()
    {
        AnimationFilePathService sut = BuildSut(out Mock<ISelectedState> selectedStateMock, out Mock<IFileCommands> fileCommandsMock);
        ComponentSave selectedElement = new ComponentSave { Name = "MyComponent" };
        selectedStateMock.Setup(s => s.SelectedElement).Returns(selectedElement);
        fileCommandsMock.Setup(f => f.GetFullPathXmlFile(selectedElement, "MyComponent"))
            .Returns(new FilePath(@"C:\Project\Components\MyComponent.gucx"));

        FilePath? result = sut.GetAbsoluteAnimationFileNameFor("MyComponent");

        result.ShouldNotBeNull();
        result!.FullPath.ShouldBe(new FilePath(@"C:\Project\Components\MyComponentAnimations.ganx").FullPath);
    }

    [Fact]
    public void GetAbsoluteAnimationFileNameFor_ByElementName_ShouldReturnGanjPath_WhenProjectIsJsonFormat()
    {
        // Issue #4182: a JSON-converted project's Animations tab must look for/create the .ganj
        // sidecar, not .ganx.
        AnimationFilePathService sut = BuildSut(
            out Mock<ISelectedState> selectedStateMock,
            out Mock<IFileCommands> fileCommandsMock,
            projectFullFileName: @"C:\Project\Project.gumj");
        ComponentSave selectedElement = new ComponentSave { Name = "MyComponent" };
        selectedStateMock.Setup(s => s.SelectedElement).Returns(selectedElement);
        fileCommandsMock.Setup(f => f.GetFullPathXmlFile(selectedElement, "MyComponent"))
            .Returns(new FilePath(@"C:\Project\Components\MyComponent.gucx"));

        FilePath? result = sut.GetAbsoluteAnimationFileNameFor("MyComponent");

        result.ShouldNotBeNull();
        result!.FullPath.ShouldBe(new FilePath(@"C:\Project\Components\MyComponentAnimations.ganj").FullPath);
    }

    [Fact]
    public void GetAbsoluteAnimationFileNameFor_ByElementSave_ShouldReturnNull_WhenXmlPathCannotBeResolved()
    {
        AnimationFilePathService sut = BuildSut(out _, out Mock<IFileCommands> fileCommandsMock);
        ComponentSave elementSave = new ComponentSave { Name = "MyComponent" };
        fileCommandsMock.Setup(f => f.GetFullPathXmlFile(elementSave, "MyComponent")).Returns((FilePath?)null);

        FilePath? result = sut.GetAbsoluteAnimationFileNameFor(elementSave);

        result.ShouldBeNull();
    }

    [Fact]
    public void GetAbsoluteAnimationFileNameFor_ByElementSave_ShouldReturnGanxPath_WhenXmlPathResolves()
    {
        AnimationFilePathService sut = BuildSut(out _, out Mock<IFileCommands> fileCommandsMock);
        ComponentSave elementSave = new ComponentSave { Name = "MyComponent" };
        fileCommandsMock.Setup(f => f.GetFullPathXmlFile(elementSave, "MyComponent"))
            .Returns(new FilePath(@"C:\Project\Components\MyComponent.gucx"));

        FilePath? result = sut.GetAbsoluteAnimationFileNameFor(elementSave);

        result.ShouldNotBeNull();
        result!.FullPath.ShouldBe(new FilePath(@"C:\Project\Components\MyComponentAnimations.ganx").FullPath);
    }

    [Fact]
    public void GetAbsoluteAnimationFileNameFor_ByElementSave_ShouldReturnGanjPath_WhenProjectIsJsonFormat()
    {
        AnimationFilePathService sut = BuildSut(
            out _,
            out Mock<IFileCommands> fileCommandsMock,
            projectFullFileName: @"C:\Project\Project.gumj");
        ComponentSave elementSave = new ComponentSave { Name = "MyComponent" };
        fileCommandsMock.Setup(f => f.GetFullPathXmlFile(elementSave, "MyComponent"))
            .Returns(new FilePath(@"C:\Project\Components\MyComponent.gucx"));

        FilePath? result = sut.GetAbsoluteAnimationFileNameFor(elementSave);

        result.ShouldNotBeNull();
        result!.FullPath.ShouldBe(new FilePath(@"C:\Project\Components\MyComponentAnimations.ganj").FullPath);
    }
}
