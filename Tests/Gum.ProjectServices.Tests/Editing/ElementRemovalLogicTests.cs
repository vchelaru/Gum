using Gum.DataTypes;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class ElementRemovalLogicTests : BaseTestClass
{
    [Fact]
    public void RemoveElement_ScreenOrComponent_RemovesItAndItsReference()
    {
        ScreenSave game = EditingTestProject.AddScreen(Project, "Game");
        ComponentSave button = EditingTestProject.AddComponent(Project, "Controls/Button");
        Project.ScreenReferences.Add(new ElementReference { Name = "Game", ElementType = ElementType.Screen });
        Project.ComponentReferences.Add(new ElementReference { Name = "Controls/Button", ElementType = ElementType.Component });
        Project.ComponentReferences.Add(new ElementReference { Name = "Other", ElementType = ElementType.Component });
        ElementRemovalLogic logic = new ElementRemovalLogic();

        bool removedScreen = logic.RemoveElement(Project, game);
        bool removedComponent = logic.RemoveElement(Project, button);

        removedScreen.ShouldBeTrue();
        removedComponent.ShouldBeTrue();
        Project.Screens.ShouldNotContain(game);
        Project.Components.ShouldNotContain(button);
        Project.ScreenReferences.ShouldBeEmpty();
        Project.ComponentReferences.Select(item => item.Name).ShouldBe(new[] { "Other" });
    }

    [Fact]
    public void RemoveElement_StandardElement_IsKept()
    {
        StandardElementSave text = Project.StandardElements.First(item => item.Name == "Text");
        int referenceCount = Project.StandardElementReferences.Count;

        bool removed = new ElementRemovalLogic().RemoveElement(Project, text);

        removed.ShouldBeFalse();
        Project.StandardElements.ShouldContain(text);
        Project.StandardElementReferences.Count.ShouldBe(referenceCount);
    }
}
