using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// ApplyState applies Parent variables first (by instance order), then category-state variables, then the rest
/// in list order, so a plain variable listed before a state still wins over what that state sets.
/// ApplyState_ParentVariables_ShouldAddChildrenInInstanceOrder covers the Parent ordering.
/// </summary>
public class ApplyStateVariableOrderTests : BaseTestClass
{
    private static (ContainerRuntime Root, StateSave State) Create()
    {
        ComponentSave component = new() { Name = "OrderComponent" };
        StateSaveCategory category = new() { Name = "Look" };
        StateSave moved = new() { Name = "Moved", ParentContainer = component };
        moved.Variables.Add(new VariableSave { Name = "X", Type = "float", Value = 5f, SetsValue = true });
        category.States.Add(moved);
        component.Categories.Add(category);

        ContainerRuntime root = new() { ElementSave = component };
        root.AddCategory(category);
        StateSave state = new() { Name = "Applied", ParentContainer = component };
        return (root, state);
    }

    [Fact]
    public void ApplyState_PlainVariableListedBeforeAStateVariable_PlainVariableWins()
    {
        (ContainerRuntime root, StateSave state) = Create();
        state.Variables.Add(new VariableSave { Name = "X", Type = "float", Value = 10f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "LookState", Type = "LookState", Value = "Moved", SetsValue = true });

        root.ApplyState(state);

        root.X.ShouldBe(10);
    }

    [Fact]
    public void ApplyState_TwoPlainVariablesForTheSameProperty_LaterOneWins()
    {
        (ContainerRuntime root, StateSave state) = Create();
        state.Variables.Add(new VariableSave { Name = "X", Type = "float", Value = 1f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "LookState", Type = "LookState", Value = "Moved", SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "X", Type = "float", Value = 2f, SetsValue = true });

        root.ApplyState(state);

        root.X.ShouldBe(2);
    }

    [Fact]
    public void ApplyState_SameVariableObjectListedTwice_AppliesItOnce()
    {
        (ContainerRuntime root, StateSave state) = Create();
        VariableSave x = new() { Name = "X", Type = "float", Value = 10f, SetsValue = true };
        state.Variables.Add(x);
        state.Variables.Add(new VariableSave { Name = "X", Type = "float", Value = 3f, SetsValue = true });
        // Applied only at its first position, so the 3 above still wins.
        state.Variables.Add(x);

        root.ApplyState(state);

        root.X.ShouldBe(3);
    }

    [Fact]
    public void ApplyState_VariableThatDoesNotSetValue_IsSkipped()
    {
        (ContainerRuntime root, StateSave state) = Create();
        state.Variables.Add(new VariableSave { Name = "X", Type = "float", Value = 10f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "X", Type = "float", Value = 20f, SetsValue = false });

        root.ApplyState(state);

        root.X.ShouldBe(10);
    }
}
