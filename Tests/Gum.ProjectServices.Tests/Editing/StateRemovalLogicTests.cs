using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class StateRemovalLogicTests : BaseTestClass
{
    private static StateRemovalLogic CreateLogic() =>
        new StateRemovalLogic(EditingTestProject.CreateReferenceFinder(), new ObjectFinderProjectProvider());

    [Fact]
    public void RemoveState_RemovesItAndTheValuesThatSetIt()
    {
        ComponentSave button = EditingTestProject.AddComponent(Project, "Button");
        StateSaveCategory visibility = EditingTestProject.AddCategory(button, "Visibility", "Shown", "Hidden");
        StateSave hidden = visibility.States[1];
        ScreenSave game = EditingTestProject.AddScreen(Project, "Game");
        EditingTestProject.AddInstance(game, "OkButton", "Button");
        EditingTestProject.AddInstance(game, "CancelButton", "Button");
        VariableSave setsHidden = new VariableSave { Name = "OkButton.VisibilityState", Type = "Visibility", Value = "Hidden" };
        VariableSave setsShown = new VariableSave { Name = "CancelButton.VisibilityState", Type = "Visibility", Value = "Shown" };
        game.DefaultState.Variables.Add(setsHidden);
        game.DefaultState.Variables.Add(setsShown);

        IReadOnlyList<RemovedVariable> removed = CreateLogic().RemoveState(hidden, button);

        visibility.States.Select(item => item.Name).ShouldBe(new[] { "Shown" });
        game.DefaultState.Variables.ShouldBe(new[] { setsShown });
        removed.ShouldHaveSingleItem().ShouldBe(new RemovedVariable(game, game.DefaultState, setsHidden));
    }

    [Fact]
    public void RemoveCategory_RemovesItAndTheValuesThatSetItsStates()
    {
        ComponentSave button = EditingTestProject.AddComponent(Project, "Button");
        StateSaveCategory visibility = EditingTestProject.AddCategory(button, "Visibility", "Shown", "Hidden");
        VariableSave ownSelector = new VariableSave { Name = "VisibilityState", Type = "Visibility", Value = "Shown" };
        button.DefaultState.Variables.Add(ownSelector);
        ScreenSave game = EditingTestProject.AddScreen(Project, "Game");
        EditingTestProject.AddInstance(game, "OkButton", "Button");
        VariableSave instanceValue = new VariableSave { Name = "OkButton.VisibilityState", Type = "Visibility", Value = "Hidden" };
        game.DefaultState.Variables.Add(instanceValue);

        IReadOnlyList<RemovedVariable> removed = CreateLogic().RemoveCategory(visibility, button);

        button.Categories.ShouldBeEmpty();
        button.DefaultState.Variables.ShouldBeEmpty();
        game.DefaultState.Variables.ShouldBeEmpty();
        removed.Select(item => item.Variable).ShouldBe(new[] { instanceValue, ownSelector }, ignoreOrder: true);
    }

    [Fact]
    public void GetWhyCannotRemove_DefaultStateOrWhatABehaviorNeeds_ExplainsWhy()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.Categories.Add(new StateSaveCategory { Name = "ButtonCategory", States = { new StateSave { Name = "Pushed" } } });
        Project.Behaviors.Add(behavior);
        ComponentSave button = EditingTestProject.AddComponent(Project, "Button");
        button.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });
        StateSaveCategory buttonCategory = EditingTestProject.AddCategory(button, "ButtonCategory", "Pushed", "Extra");
        StateSaveCategory looks = EditingTestProject.AddCategory(button, "Looks", "Plain");
        StateRemovalLogic logic = CreateLogic();

        logic.GetWhyCannotRemoveState(button.DefaultState, button)
            .ShouldBe("This state cannot be removed because it is the default state.");
        logic.GetWhyCannotRemoveState(buttonCategory.States[0], button)
            .ShouldBe("The state Pushed cannot be removed because it is needed by the following behavior(s):\nButtonBehavior");
        logic.GetWhyCannotRemoveState(buttonCategory.States[1], button).ShouldBeNull();
        logic.GetWhyCannotRemoveCategory(buttonCategory, button)
            .ShouldBe("The category ButtonCategory cannot be removed because it is needed by the following behavior(s):\nButtonBehavior");
        logic.GetWhyCannotRemoveCategory(looks, button).ShouldBeNull();
    }

}
