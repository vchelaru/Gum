using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class InstanceRemovalLogicTests : BaseTestClass
{
    private static InstanceRemovalLogic CreateLogic() =>
        new InstanceRemovalLogic(EditingTestProject.CreateReferenceFinder(), new VariableOverrideRemover());

    [Fact]
    public void RemoveInstance_RemovesItsDataAndValuesOtherElementsSetOnItsExposedVariables()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        InstanceSave counter = EditingTestProject.AddInstance(hud, "Counter1", "Text");
        EditingTestProject.AddInstance(hud, "Label", "Text");
        StateSaveCategory size = EditingTestProject.AddCategory(hud, "Size", "Big");
        hud.GetDefaultStateOrThrow().Variables.Add(new VariableSave { Name = "Counter1.Text", Type = "string", Value = "0", ExposedAsName = "Counter1Text" });
        hud.GetDefaultStateOrThrow().Variables.Add(new VariableSave { Name = "Label.Parent", Type = "string", Value = "Counter1" });
        size.States[0].Variables.Add(new VariableSave { Name = "Counter1.X", Type = "float", Value = 5f });
        hud.GetDefaultStateOrThrow().VariableLists.Add(new VariableListSave<string> { Name = "Counter1.VariableReferences", Type = "string" });
        hud.Events.Add(new EventSave { Name = "Counter1.Click" });
        ScreenSave game = EditingTestProject.AddScreen(Project, "Game");
        EditingTestProject.AddInstance(game, "HudInstance", "Hud");
        VariableSave gameValue = new VariableSave { Name = "HudInstance.Counter1Text", Type = "string", Value = "5" };
        game.GetDefaultStateOrThrow().Variables.Add(gameValue);

        IReadOnlyList<RemovedVariable> removed = CreateLogic().RemoveInstance(counter, hud);

        hud.Instances.Select(item => item.Name).ShouldBe(new[] { "Label" });
        hud.GetDefaultStateOrThrow().Variables.ShouldBeEmpty();
        size.States[0].Variables.ShouldBeEmpty();
        hud.GetDefaultStateOrThrow().VariableLists.ShouldBeEmpty();
        hud.Events.ShouldBeEmpty();
        game.GetDefaultStateOrThrow().Variables.ShouldBeEmpty();
        removed.ShouldHaveSingleItem().ShouldBe(new RemovedVariable(game, game.GetDefaultStateOrThrow(), gameValue));
    }

    [Fact]
    public void GetWhyCannotRemove_InstanceDefinedByBase_ExplainsWhy()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        InstanceSave inherited = EditingTestProject.AddInstance(hud, "Background", "Sprite");
        inherited.DefinedByBase = true;
        InstanceSave own = EditingTestProject.AddInstance(hud, "Label", "Text");
        InstanceRemovalLogic logic = CreateLogic();

        logic.GetWhyCannotRemove(inherited).ShouldBe("The instance Background cannot be deleted because it is defined in a base object.");
        logic.GetWhyCannotRemove(own).ShouldBeNull();
    }
}
