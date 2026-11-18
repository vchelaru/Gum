using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class InstanceChildDetacherTests : BaseTestClass
{
    [Fact]
    public void DetachChildren_MovesChildrenToTheInstancesParent_AndDropsChildrenOfItsNamedObjects()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        EditingTestProject.AddInstance(hud, "Stack", "Container");
        InstanceSave panel = EditingTestProject.AddInstance(hud, "Panel", "Container");
        EditingTestProject.AddInstance(hud, "Child", "Text");
        EditingTestProject.AddInstance(hud, "Anchored", "Text");
        hud.DefaultState.Variables.Add(new VariableSave { Name = "Panel.Parent", Type = "string", Value = "Stack" });
        hud.DefaultState.Variables.Add(new VariableSave { Name = "Child.Parent", Type = "string", Value = "Panel" });
        hud.DefaultState.Variables.Add(new VariableSave { Name = "Anchored.Parent", Type = "string", Value = "Panel.Inner" });

        new InstanceChildDetacher().DetachChildren(panel, hud);

        hud.DefaultState.GetValue("Child.Parent").ShouldBe("Stack");
        hud.DefaultState.GetVariableSave("Anchored.Parent").ShouldBeNull();
        hud.DefaultState.GetValue("Panel.Parent").ShouldBe("Stack");
    }

    [Fact]
    public void DetachChildren_InstanceWithoutParent_LeavesChildrenOnTheElement()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        InstanceSave panel = EditingTestProject.AddInstance(hud, "Panel", "Container");
        EditingTestProject.AddInstance(hud, "Child", "Text");
        hud.DefaultState.Variables.Add(new VariableSave { Name = "Child.Parent", Type = "string", Value = "Panel" });

        new InstanceChildDetacher().DetachChildren(panel, hud);

        hud.DefaultState.GetVariableSave("Child.Parent").ShouldBeNull();
    }
}
