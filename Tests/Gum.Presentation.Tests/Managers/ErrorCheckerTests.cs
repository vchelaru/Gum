using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.ProjectServices;
using Gum.Services;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.Managers;

public class ErrorCheckerTests : BaseTestClass
{
    private readonly ErrorChecker _sut;

    public ErrorCheckerTests()
    {
        ITypeResolver typeResolver = new DefaultTypeResolver();
        IHeadlessErrorChecker headlessErrorChecker = new HeadlessErrorChecker(typeResolver);
        Mock<IPluginManager> mockPluginManager = new Mock<IPluginManager>();
        IErrorDocsRegistry errorDocsRegistry = new ErrorDocsRegistry();
        _sut = new ErrorChecker(headlessErrorChecker, mockPluginManager.Object, errorDocsRegistry, Mock.Of<IFileSystemRevealService>());
    }

    [Fact]
    public void GetErrorsFor_ShouldReportError_WhenComponentHasInvalidBaseType()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;

        ComponentSave component = new ComponentSave { Name = "DerivedComponent", BaseType = "NonExistentBase" };
        project.Components.Add(component);

        ErrorViewModel[] errors = _sut.GetErrorsFor(component, project);

        errors.Length.ShouldBe(1);
        errors[0].Message.ShouldContain("NonExistentBase");
        errors[0].ElementName.ShouldBe("DerivedComponent");
    }

    [Fact]
    public void GetErrorsFor_ShouldReportError_WhenComponentInstanceHasInvalidBaseType()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;

        ComponentSave validComponent = new ComponentSave { Name = "ValidType" };
        project.Components.Add(validComponent);

        ComponentSave component = new ComponentSave { Name = "TestComponent" };
        component.Instances.Add(new InstanceSave
        {
            Name = "GoodInstance",
            BaseType = "ValidType"
        });
        component.Instances.Add(new InstanceSave
        {
            Name = "BadInstance",
            BaseType = "NonExistentType"
        });
        project.Components.Add(component);

        ErrorViewModel[] errors = _sut.GetErrorsFor(component, project);

        errors.Length.ShouldBe(1);
        errors[0].Message.ShouldContain("NonExistentType");
    }

    [Fact]
    public void GetPluginErrors_LinksHelp_AndDoesNotRaiseErrorsChecked()
    {
        PluginBase plugin = new Mock<PluginBase>().Object;
        Mock<IPluginManager> mockPluginManager = new Mock<IPluginManager>();
        mockPluginManager
            .Setup(m => m.FillWithErrors(It.IsAny<List<ErrorViewModel>>(), plugin))
            .Callback<List<ErrorViewModel>, object?>(
                (list, _) => list.Add(new ErrorViewModel { Code = "GUM0005", Message = "plugin error" }));
        ErrorChecker sut = new ErrorChecker(Mock.Of<IHeadlessErrorChecker>(), mockPluginManager.Object,
            new ErrorDocsRegistry(), Mock.Of<IFileSystemRevealService>());
        int raisedCount = 0;
        sut.ErrorsChecked += (_, _) => raisedCount++;

        ErrorViewModel[] errors = sut.GetPluginErrors(plugin);

        errors.ShouldHaveSingleItem().HelpUrl.ShouldNotBeNull();
        raisedCount.ShouldBe(0);
    }

    [Fact]
    public void GetErrorsFor_RaisesErrorsChecked_WithTheElementsErrors()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;
        ComponentSave component = new ComponentSave { Name = "DerivedComponent", BaseType = "NonExistentBase" };
        project.Components.Add(component);
        List<(ElementSave, ErrorViewModel[])> raised = new List<(ElementSave, ErrorViewModel[])>();
        _sut.ErrorsChecked += (element, errors) => raised.Add((element, errors));

        ErrorViewModel[] returned = _sut.GetErrorsFor(component, project);

        (ElementSave checkedElement, ErrorViewModel[] reported) = raised.ShouldHaveSingleItem();
        checkedElement.ShouldBe(component);
        reported.ShouldBe(returned);
        reported.ShouldNotBeEmpty();
    }

    [Fact]
    public void GetProjectErrors_OffersToShowTheFile_WhenTheErrorNamesOne()
    {
        GumProjectSave project = new GumProjectSave();
        string filePath = "C:/Game/Localization/strings.csv";
        Mock<IHeadlessErrorChecker> headlessErrorChecker = new Mock<IHeadlessErrorChecker>();
        headlessErrorChecker.Setup(c => c.GetProjectErrors(project)).Returns(new[]
        {
            new ErrorResult { ElementName = "(project)", Code = "GUM0008", Message = "case", FilePath = filePath },
            new ErrorResult { ElementName = "(project)", Message = "no file" },
        });
        Mock<IFileSystemRevealService> revealService = new Mock<IFileSystemRevealService>();
        ErrorChecker sut = new ErrorChecker(headlessErrorChecker.Object, new Mock<IPluginManager>().Object,
            new ErrorDocsRegistry(), revealService.Object);

        ErrorViewModel[] errors = sut.GetProjectErrors(project);

        errors.Length.ShouldBe(2);
        errors[0].HelpUrl.ShouldNotBeNull();
        errors[1].HasAction.ShouldBeFalse();
        errors[0].ActionCommand!.Execute(null);
        revealService.Verify(r => r.RevealFile(filePath), Times.Once);
    }
}
