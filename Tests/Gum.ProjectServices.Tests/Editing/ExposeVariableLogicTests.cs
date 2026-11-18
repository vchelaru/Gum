using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class ExposeVariableLogicTests : BaseTestClass
{
    [Fact]
    public void Expose_UnsetVariable_AddsItWithoutSettingAValue()
    {
        Project.StandardElements.Single(item => item.Name == "Text").DefaultState.Variables
            .Add(new VariableSave { Name = "Text", Type = "string", Value = "Hello" });
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        InstanceSave counter = new InstanceSave { Name = "Counter1", BaseType = "Text", ParentContainer = hud };
        hud.Instances.Add(counter);
        ExposeVariableLogic logic = new ExposeVariableLogic();

        VariableSave exposed = logic.Expose(hud, counter, "Text", "Counter1Text");

        exposed.Name.ShouldBe("Counter1.Text");
        exposed.Type.ShouldBe("string");
        exposed.SetsValue.ShouldBeFalse();
        exposed.ExposedAsName.ShouldBe("Counter1Text");
        hud.DefaultState.Variables.ShouldContain(exposed);
        logic.GetUnauthoredVariableType(counter, "Nope", hud).ShouldBeNull();
    }

    [Fact]
    public void Unexpose_ClearsTheExposedName_AndReturnsTheValuesSetThroughIt()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        EditingTestProject.AddInstance(hud, "Label", "Text");
        VariableSave exposed = new VariableSave { Name = "Label.Text", Type = "string", Value = "Hi", ExposedAsName = "LabelText" };
        hud.DefaultState.Variables.Add(exposed);
        ScreenSave game = EditingTestProject.AddScreen(Project, "Game");
        EditingTestProject.AddInstance(game, "HudInstance", "Hud");
        VariableSave instanceValue = new VariableSave { Name = "HudInstance.LabelText", Type = "string", Value = "Yo" };
        game.DefaultState.Variables.Add(instanceValue);
        ExposeVariableLogic logic = new ExposeVariableLogic();
        VariableChangeResponse references = EditingTestProject.CreateReferenceFinder()
            .GetReferencesToVariable(hud, exposed.Name, "LabelText");

        string? whyNot = logic.GetWhyCannotUnexpose(references, "LabelText", out List<VariableChange> valuesToClear);
        logic.Unexpose(hud, exposed);

        whyNot.ShouldBeNull();
        valuesToClear.Select(item => item.Variable).ShouldBe(new[] { instanceValue });
        exposed.ExposedAsName.ShouldBeNull();
        hud.DefaultState.Variables.ShouldContain(exposed);
    }

    [Fact]
    public void GetWhyCannotUnexpose_WhenAVariableReferenceUsesTheName_NamesTheLine()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        EditingTestProject.AddInstance(hud, "Label", "Text");
        hud.DefaultState.Variables.Add(new VariableSave { Name = "Label.Text", Type = "string", ExposedAsName = "LabelText" });
        ScreenSave game = EditingTestProject.AddScreen(Project, "Game");
        EditingTestProject.AddInstance(game, "HudInstance", "Hud");
        EditingTestProject.AddInstance(game, "Title", "Text");
        game.DefaultState.VariableLists.Add(new VariableListSave<string>
        {
            Name = "HudInstance.VariableReferences",
            Type = "string",
            Value = new List<string> { "LabelText = Title.Text" }
        });
        VariableChangeResponse references = EditingTestProject.CreateReferenceFinder()
            .GetReferencesToVariable(hud, "Label.Text", "LabelText");

        string? whyNot = new ExposeVariableLogic().GetWhyCannotUnexpose(references, "LabelText", out List<VariableChange> valuesToClear);

        whyNot.ShouldBe("Cannot unexpose variable LabelText because it is referenced by:\n\n" +
            "LabelText = Title.Text in HudInstance.VariableReferences (Game)\n");
        valuesToClear.ShouldBeEmpty();
    }
}
