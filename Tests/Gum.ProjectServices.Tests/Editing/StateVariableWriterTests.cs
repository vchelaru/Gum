using Gum.DataTypes.Behaviors;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class StateVariableWriterTests : BaseTestClass
{
    [Fact]
    public void SetVariable_UnsetInstanceVariable_TakesTheTypeFromTheRootStandard()
    {
        Project.StandardElements.Single(item => item.Name == "Text").DefaultState.Variables
            .Add(new VariableSave { Name = "FontSize", Type = "int", Value = 18 });
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        InstanceSave counter = new InstanceSave { Name = "Counter1", BaseType = "Text", ParentContainer = hud };
        hud.Instances.Add(counter);
        StateVariableWriter writer = new StateVariableWriter();

        writer.ResolveVariableType(hud, hud.DefaultState, "Counter1.FontSize").ShouldBe("int");
        writer.SetVariable(hud, hud.DefaultState, "Counter1.FontSize", 30, counter, fallbackType: null);

        VariableSave written = hud.DefaultState.GetVariableSave("Counter1.FontSize")!;
        written.Value.ShouldBe(30);
        written.Type.ShouldBe("int");
    }

    [Fact]
    public void SetVariable_FormsPropertyOfAnInstance_IsTypedByTheBehaviorDeclaration()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "SliderBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Name = "Maximum", Type = "double", Value = 100.0 });
        Project.Behaviors.Add(behavior);
        ComponentSave slider = EditingTestProject.AddComponent(Project, "Slider");
        slider.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "SliderBehavior" });
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        InstanceSave volume = EditingTestProject.AddInstance(hud, "Volume", "Slider");
        StateVariableWriter writer = new StateVariableWriter();

        writer.ResolveVariableType(hud, hud.DefaultState!, "Volume.Maximum").ShouldBe("double");
        writer.ResolveVariableType(hud, hud.DefaultState!, "Volume.Nope").ShouldBeNull();
        writer.SetVariable(hud, hud.DefaultState!, "Volume.Maximum", 10.0, volume, fallbackType: null);

        hud.DefaultState!.GetVariableSave("Volume.Maximum")!.Type.ShouldBe("double");
    }

    [Fact]
    public void SetVariable_ExposedVariableInANamedState_KeepsTheExposedName()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        hud.Instances.Add(new InstanceSave { Name = "Counter1", BaseType = "Text", ParentContainer = hud });
        hud.DefaultState.Variables.Add(new VariableSave { Name = "Counter1.Text", Type = "string", ExposedAsName = "Counter1Text", SetsValue = false });
        StateSave big = new StateSave { Name = "Big", ParentContainer = hud };
        hud.States.Add(big);

        new StateVariableWriter().SetVariable(hud, big, "Counter1.Text", "Hi", hud.Instances[0], fallbackType: null);

        big.GetVariableSave("Counter1.Text")!.ExposedAsName.ShouldBe("Counter1Text");
    }
}
