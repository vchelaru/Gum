using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using GumRuntime;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace GumExpressions.Tests;

public class GumExpressionServiceTests : BaseTestClass
{
    public GumExpressionServiceTests()
    {
        GumExpressionService.Initialize();
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        base.Dispose();
    }

    [Fact]
    public void GetAllVariableReferenceBranches_BranchReferencesMissingVariable_OmitsThatBranch()
    {
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave state = new StateSave { ParentContainer = screen };
        screen.States.Add(state);
        state.Variables.Add(new VariableSave { Name = "IsOn", Value = true, Type = "bool", SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Text", Value = "A", Type = "string", SetsValue = true });

        (string VariableName, IEnumerable<object> Values)? result =
            ElementSaveExtensions.GetAllVariableReferenceBranches(null, "Text = IsOn ? Missing : \"B\"", state);

        result.ShouldNotBeNull();
        result.Value.Values.ToList().ShouldBe(new List<object> { "B" });
    }
}
