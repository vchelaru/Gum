using Gum.Commands;
using Gum.DataTypes;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ProjectServices.Editing;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// HandleElementBaseType refreshes the state-variable converters whatever the new base type is.
/// </summary>
public class InheritanceLogicBaseTypeTests : BaseTestClass
{
    [Theory]
    [InlineData("Container")]
    [InlineData("")]
    [InlineData("DoesNotExist")]
    public void HandleElementBaseType_WithStandardEmptyOrMissingBase_FixesConvertersOnceAndKeepsOwnInstances(string baseType)
    {
        GumProjectSave project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);
        ObjectFinder.Self.GumProjectSave = project;
        ComponentSave component = new ComponentSave { Name = "Thing", BaseType = baseType };
        component.InitializeDefaultAndComponentVariables();
        component.Instances.Add(new InstanceSave { Name = "Own", BaseType = "Text", ParentContainer = component });
        project.Components.Add(component);
        Mock<IStandardElementsManagerGumTool> converters = new Mock<IStandardElementsManagerGumTool>();
        InheritanceLogic sut = new InheritanceLogic(
            new Mock<IFileCommands>().Object,
            new Mock<IGuiCommands>().Object,
            converters.Object,
            new Mock<IBaseTypeVariablePruner>().Object,
            new InheritanceSync());

        Should.NotThrow(() => sut.HandleElementBaseType(component));

        converters.Verify(x => x.FixCustomTypeConverters(component), Times.Once);
        component.Instances.Select(item => item.Name).ShouldBe(new[] { "Own" });
    }
}
