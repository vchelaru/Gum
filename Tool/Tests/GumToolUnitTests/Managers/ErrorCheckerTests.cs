using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.ProjectServices;
using Moq;
using Shouldly;

namespace GumToolUnitTests.Managers;

public class ErrorCheckerTests : BaseTestClass
{
    private readonly ErrorChecker _sut;

    public ErrorCheckerTests()
    {
        ITypeResolver typeResolver = new DefaultTypeResolver();
        IHeadlessErrorChecker headlessErrorChecker = new HeadlessErrorChecker(typeResolver);
        Mock<IPluginManager> mockPluginManager = new Mock<IPluginManager>();
        IErrorDocsRegistry errorDocsRegistry = new ErrorDocsRegistry();
        _sut = new ErrorChecker(headlessErrorChecker, mockPluginManager.Object, errorDocsRegistry);
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
}
