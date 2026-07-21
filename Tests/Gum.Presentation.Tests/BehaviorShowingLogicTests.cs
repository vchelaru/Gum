using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Plugins.VariableGrid;
using Gum.ToolStates;
using Moq;
using Shouldly;
using System.Linq;

namespace Gum.Presentation.Tests;

public class BehaviorShowingLogicTests : BaseTestClass
{
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly Mock<IProjectState> _projectState = new();

    private BehaviorShowingLogic CreateSut() => new(_fileCommands.Object, _projectState.Object);

    private static GumProjectSave CreateProjectWithComponentsImplementing(string behaviorName, params string[] componentNames)
    {
        var project = new GumProjectSave();
        foreach (var name in componentNames)
        {
            var component = new ComponentSave { Name = name };
            component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behaviorName });
            project.Components.Add(component);
        }
        return project;
    }

    [Fact]
    public void GetCategoriesFor_ShouldReturnSingleBehaviorPropertiesCategory()
    {
        var behavior = new BehaviorSave { Name = "MyBehavior" };
        _projectState.Setup(x => x.GumProjectSave).Returns(CreateProjectWithComponentsImplementing("MyBehavior"));

        var result = CreateSut().GetCategoriesFor(behavior);

        result.Single().Name.ShouldBe("Behavior Properties");
        result.Single().Members.Single().Name.ShouldBe(nameof(BehaviorSave.DefaultImplementation));
    }

    [Fact]
    public void GetCategoriesFor_CustomOptions_ShouldListImplementingComponentsWithLeadingNull()
    {
        var behavior = new BehaviorSave { Name = "MyBehavior" };
        _projectState.Setup(x => x.GumProjectSave)
            .Returns(CreateProjectWithComponentsImplementing("MyBehavior", "ComponentA", "ComponentB"));

        var result = CreateSut().GetCategoriesFor(behavior);

        var options = result.Single().Members.Single().CustomOptions;
        options.ShouldNotBeNull();
        options![0].ShouldBeNull();
        options.Skip(1).ShouldBe(new object[] { "ComponentA", "ComponentB" });
    }

    [Fact]
    public void GetCategoriesFor_RowGet_ShouldReturnBehaviorDefaultImplementation()
    {
        var behavior = new BehaviorSave { Name = "MyBehavior", DefaultImplementation = "SomeImpl" };
        _projectState.Setup(x => x.GumProjectSave).Returns(CreateProjectWithComponentsImplementing("MyBehavior"));

        var result = CreateSut().GetCategoriesFor(behavior);

        result.Single().Members.Single().Get().ShouldBe("SomeImpl");
    }

    [Fact]
    public void GetCategoriesFor_RowSet_ShouldUpdateBehaviorAndAutoSave()
    {
        var behavior = new BehaviorSave { Name = "MyBehavior" };
        _projectState.Setup(x => x.GumProjectSave).Returns(CreateProjectWithComponentsImplementing("MyBehavior"));

        var result = CreateSut().GetCategoriesFor(behavior);
        result.Single().Members.Single().Set!.Invoke("NewImpl");

        behavior.DefaultImplementation.ShouldBe("NewImpl");
        _fileCommands.Verify(x => x.TryAutoSaveBehavior(behavior), Times.Once);
    }
}
