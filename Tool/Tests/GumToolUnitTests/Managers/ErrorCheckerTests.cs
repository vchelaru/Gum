using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Reflection;
using Moq;
using Shouldly;

namespace GumToolUnitTests.Managers;

public class ErrorCheckerTests : BaseTestClass
{
    private readonly ErrorChecker _sut;

    public ErrorCheckerTests()
    {
        var mockTypeManager = new Mock<ITypeManager>();
        var mockPluginManager = new Mock<IPluginManager>();
        _sut = new ErrorChecker(mockTypeManager.Object, mockPluginManager.Object);
    }

    [Fact]
    public void GetErrorsFor_ShouldReportError_WhenComponentInstanceHasInvalidBaseType()
    {
        var project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;

        var validComponent = new ComponentSave { Name = "ValidType" };
        project.Components.Add(validComponent);

        var component = new ComponentSave { Name = "TestComponent" };
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

        var errors = _sut.GetErrorsFor(component, project);

        errors.Length.ShouldBe(1);
        errors[0].Message.ShouldContain("NonExistentType");
    }
}
