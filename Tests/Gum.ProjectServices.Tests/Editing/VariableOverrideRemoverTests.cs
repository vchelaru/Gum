using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class VariableOverrideRemoverTests : BaseTestClass
{
    [Fact]
    public void Remove_RemovesInstanceValues_SkipsOnesWhoseInstanceIsGone()
    {
        ScreenSave game = EditingTestProject.AddScreen(Project, "Game");
        EditingTestProject.AddInstance(game, "HudInstance", "Hud");
        VariableSave kept = new VariableSave { Name = "Missing.Score", Type = "int", Value = 1 };
        VariableSave removedValue = new VariableSave { Name = "HudInstance.Score", Type = "int", Value = 5 };
        game.DefaultState.Variables.Add(kept);
        game.DefaultState.Variables.Add(removedValue);
        List<VariableChange> overrides = new List<VariableChange>
        {
            new VariableChange { Container = game, State = game.DefaultState, Variable = kept },
            new VariableChange { Container = game, State = game.DefaultState, Variable = removedValue },
        };

        IReadOnlyList<RemovedVariable> removed = new VariableOverrideRemover().Remove(overrides);

        removed.ShouldHaveSingleItem().ShouldBe(new RemovedVariable(game, game.DefaultState, removedValue));
        game.DefaultState.Variables.ShouldBe(new[] { kept });
    }
}
