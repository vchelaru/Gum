using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// ApplyState hands an instance variable ("Child.X") to the instance by the VariableSave's own split of its name;
/// what reaches the instance must be what SetProperty("Child.X", value) delivers.
/// </summary>
public class ApplyStateInstanceVariableTests : BaseTestClass
{
    private static (ContainerRuntime Root, ContainerRuntime Child, ContainerRuntime Grandchild, StateSave State) Create()
    {
        ComponentSave component = new() { Name = "InstanceComponent" };
        ContainerRuntime root = new() { ElementSave = component };
        ContainerRuntime child = new() { Name = "Child" };
        ContainerRuntime grandchild = new() { Name = "Grandchild" };
        root.Children.Add(child);
        child.Children.Add(grandchild);
        StateSave state = new() { Name = "Applied", ParentContainer = component };
        return (root, child, grandchild, state);
    }

    [Fact]
    public void ApplyState_InstanceVariable_SetsTheInstance()
    {
        (ContainerRuntime root, ContainerRuntime child, _, StateSave state) = Create();
        state.Variables.Add(new VariableSave { Name = "Child.X", Type = "float", Value = 12f, SetsValue = true });

        root.ApplyState(state);

        child.X.ShouldBe(12);
        root.X.ShouldBe(0);
    }

    [Fact]
    public void ApplyState_VariableOfANestedInstance_SetsTheNestedInstance()
    {
        (ContainerRuntime root, _, ContainerRuntime grandchild, StateSave state) = Create();
        state.Variables.Add(new VariableSave { Name = "Child.Grandchild.Y", Type = "float", Value = 9f, SetsValue = true });

        root.ApplyState(state);

        grandchild.Y.ShouldBe(9);
    }

    [Fact]
    public void ApplyState_ExposedVariable_ForwardsToTheInstance()
    {
        (ContainerRuntime root, ContainerRuntime child, _, StateSave state) = Create();
        root.AddExposedVariable("ChildX", "Child.X");
        state.Variables.Add(new VariableSave { Name = "ChildX", Type = "float", Value = 21f, SetsValue = true });

        root.ApplyState(state);

        child.X.ShouldBe(21);
    }

    [Fact]
    public void ApplyState_InstanceThatDoesNotExist_IsSkipped()
    {
        (ContainerRuntime root, ContainerRuntime child, _, StateSave state) = Create();
        state.Variables.Add(new VariableSave { Name = "Missing.X", Type = "float", Value = 5f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Child.X", Type = "float", Value = 6f, SetsValue = true });

        root.ApplyState(state);

        child.X.ShouldBe(6);
    }
}
