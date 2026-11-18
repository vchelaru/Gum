using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class ElementCreationLogicTests : BaseTestClass
{
    private readonly ElementCreationLogic _logic = new ElementCreationLogic();

    [Fact]
    public void AddScreen_AddsReferenceAndDefaultState_SortedByName()
    {
        _logic.AddScreen(Project, new ScreenSave { Name = "Zeta" });
        _logic.AddScreen(Project, new ScreenSave { Name = "Alpha" });

        Project.ScreenReferences.Select(item => item.Name).ShouldBe(new[] { "Alpha", "Zeta" });
        Project.Screens.Select(item => item.Name).ShouldBe(new[] { "Alpha", "Zeta" });
        Project.Screens[0].DefaultState.ShouldNotBeNull();
    }

    [Fact]
    public void PrepareNewComponent_ClearsPositionAndTurnsOnEvents()
    {
        ComponentSave component = new ComponentSave();
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave { Name = "X", Type = "float", Value = 5f, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "HasEvents", Type = "bool", Value = false, SetsValue = true });
        component.States.Add(defaultState);

        _logic.PrepareNewComponent(component, "Controls/Counter", "Text");
        _logic.AddComponent(Project, component);

        component.Name.ShouldBe("Controls/Counter");
        component.BaseType.ShouldBe("Text");
        component.DefaultState.GetVariableSave("X")!.SetsValue.ShouldBeFalse();
        component.DefaultState.GetValue("X").ShouldBeNull();
        component.DefaultState.GetVariableSave("HasEvents")!.Value.ShouldBe(true);
        Project.ComponentReferences.Single().Name.ShouldBe("Controls/Counter");
    }
}
