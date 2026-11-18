using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class ApplyStateReentrancyTests : BaseTestClass
{
    [Fact]
    public void ApplyState_StateVariableAppliesNestedStates_OuterVariablesStillApply()
    {
        ComponentSave component = new() { Name = "NestComponent" };
        StateSaveCategory inner = new() { Name = "Inner" };
        StateSave innerState = new() { Name = "On", ParentContainer = component };
        innerState.Variables.Add(new VariableSave { Name = "Y", Type = "float", Value = 7f, SetsValue = true });
        innerState.Variables.Add(new VariableSave { Name = "Height", Type = "float", Value = 33f, SetsValue = true });
        inner.States.Add(innerState);
        StateSaveCategory middle = new() { Name = "Middle" };
        StateSave middleState = new() { Name = "On", ParentContainer = component };
        middleState.Variables.Add(new VariableSave { Name = "InnerState", Type = "InnerState", Value = "On", SetsValue = true });
        middleState.Variables.Add(new VariableSave { Name = "Rotation", Type = "float", Value = 12f, SetsValue = true });
        middle.States.Add(middleState);
        component.Categories.Add(inner);
        component.Categories.Add(middle);
        ContainerRuntime root = new() { ElementSave = component };
        root.AddCategory(inner);
        root.AddCategory(middle);
        StateSave outer = new() { Name = "Outer", ParentContainer = component };
        outer.Variables.Add(new VariableSave { Name = "X", Type = "float", Value = 10f, SetsValue = true });
        outer.Variables.Add(new VariableSave { Name = "MiddleState", Type = "MiddleState", Value = "On", SetsValue = true });
        outer.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 55f, SetsValue = true });

        root.ApplyState(outer);
        root.ApplyState(outer);

        root.X.ShouldBe(10);
        root.Width.ShouldBe(55);
        root.Y.ShouldBe(7);
        root.Height.ShouldBe(33);
        root.Rotation.ShouldBe(12);
    }

    [Fact]
    public void ApplyState_OnSecondThread_AppliesVariables()
    {
        ComponentSave component = new() { Name = "ThreadComponent" };
        ContainerRuntime root = new() { ElementSave = component };
        StateSave state = new() { Name = "S", ParentContainer = component };
        state.Variables.Add(new VariableSave { Name = "X", Type = "float", Value = 4f, SetsValue = true });
        System.Threading.Thread thread = new(() => root.ApplyState(state));
        thread.Start();
        thread.Join();
        root.X.ShouldBe(4);
    }
}
