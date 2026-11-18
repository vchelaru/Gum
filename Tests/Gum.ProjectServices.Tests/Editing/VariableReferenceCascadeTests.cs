using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class VariableReferenceCascadeTests : BaseTestClass
{
    [Fact]
    public void GetChangedNames_ReturnsTheEditedNamesAndTheVariablesWhoseValueChangedOrAppeared()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        StateSave state = hud.DefaultState;
        state.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 10f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Height", Type = "float", Value = 20f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "X", Type = "float", Value = 5f, SetsValue = true });
        Dictionary<string, object?> valuesBefore = new Dictionary<string, object?> { ["Width"] = 10f, ["Height"] = 15f };

        HashSet<string> changed = new VariableReferenceCascade().GetChangedNames(state, valuesBefore, new[] { "VariableReferences" });

        changed.ShouldBe(new[] { "VariableReferences", "Height", "X" }, ignoreOrder: true);
    }
}
