using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class CategoryVariablePropagatorTests : BaseTestClass
{
    [Fact]
    public void PropagateAllVariablesInCategory_FillsSiblingStatesAndReportsEachAddition()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        hud.DefaultState.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 10f, SetsValue = true });
        StateSaveCategory category = new StateSaveCategory { Name = "Size" };
        StateSave normal = new StateSave { Name = "Normal", ParentContainer = hud };
        StateSave big = new StateSave { Name = "Big", ParentContainer = hud };
        big.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 50f, SetsValue = true });
        category.States.Add(normal);
        category.States.Add(big);
        hud.Categories.Add(category);

        IReadOnlyList<PropagatedVariable> added = new CategoryVariablePropagator().PropagateAllVariablesInCategory(hud, category);

        normal.GetValue("Width").ShouldBe(10f);
        big.GetValue("Width").ShouldBe(50f);
        added.Count.ShouldBe(1);
        added[0].State.ShouldBe(normal);
        added[0].Name.ShouldBe("Width");
    }
}
