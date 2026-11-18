using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class VariableRemovalLogicTests : BaseTestClass
{
    private static VariableRemovalLogic CreateLogic() => new VariableRemovalLogic(EditingTestProject.CreateReferenceFinder());

    [Fact]
    public void GetWhyCannotRemoveCustomVariable_InstanceValueCascades_VariableReferenceBlocks()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        VariableSave score = new VariableSave { Name = "Score", Type = "int", Value = 0, IsCustomVariable = true };
        hud.DefaultState.Variables.Add(score);
        ScreenSave game = EditingTestProject.AddScreen(Project, "Game");
        EditingTestProject.AddInstance(game, "HudInstance", "Hud");
        VariableSave instanceValue = new VariableSave { Name = "HudInstance.Score", Type = "int", Value = 5 };
        game.DefaultState.Variables.Add(instanceValue);
        VariableRemovalLogic logic = CreateLogic();

        logic.GetWhyCannotRemoveCustomVariable(score, hud, out List<VariableChange> cascading).ShouldBeNull();
        cascading.ShouldHaveSingleItem().Variable.ShouldBe(instanceValue);

        hud.DefaultState.VariableLists.Add(new VariableListSave<string> { Name = "VariableReferences", Type = "string", Value = new List<string> { "Width = Score" } });

        string? whyNot = logic.GetWhyCannotRemoveCustomVariable(score, hud, out cascading);
        whyNot.ShouldNotBeNull();
        whyNot.ShouldStartWith("Cannot delete variable Score because it is referenced by other elements.");
        whyNot.ShouldContain("Width = Score in Hud");
    }

    [Fact]
    public void RemoveCustomVariable_RemovesItFromTheDefaultState()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        VariableSave score = new VariableSave { Name = "Score", Type = "int", Value = 0, IsCustomVariable = true };
        hud.DefaultState.Variables.Add(score);

        CreateLogic().RemoveCustomVariable(score, hud);

        hud.DefaultState.Variables.ShouldBeEmpty();
    }
}
