using Gum.DataTypes;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class InstanceCreationLogicTests : BaseTestClass
{
    [Fact]
    public void AddInstance_WithParent_SetsParentInDefaultState()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        InstanceSave stack = new InstanceSave { Name = "Stack", BaseType = "Container", ParentContainer = hud };
        InstanceSave counter = new InstanceSave { Name = "Counter1", BaseType = "Text", ParentContainer = hud };
        InstanceCreationLogic logic = new InstanceCreationLogic();

        logic.AddInstance(hud, stack, parentName: null, desiredIndex: null);
        logic.AddInstance(hud, counter, parentName: "Stack", desiredIndex: 0);

        hud.Instances.ShouldBe(new[] { counter, stack });
        hud.DefaultState.GetValue("Counter1.Parent").ShouldBe("Stack");
        hud.DefaultState.GetVariableSave("Stack.Parent").ShouldBeNull();
    }
}
