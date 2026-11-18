using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class VariableClearLogicTests : BaseTestClass
{
    [Fact]
    public void ClearVariable_KeepsOnlyWhatAnEntryIsNeededFor()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        EditingTestProject.AddInstance(hud, "Label", "Text");
        StateSaveCategory size = EditingTestProject.AddCategory(hud, "Size", "Big");
        VariableSave width = new VariableSave { Name = "Width", Type = "float", Value = 100f };
        VariableSave exposed = new VariableSave { Name = "Label.Text", Type = "string", Value = "Hi", ExposedAsName = "LabelText" };
        VariableSave custom = new VariableSave { Name = "Score", Type = "int", Value = 7, IsCustomVariable = true };
        VariableSave height = new VariableSave { Name = "Height", Type = "float", Value = 50f };
        hud.GetDefaultStateOrThrow().Variables.AddRange(new[] { width, exposed, custom, height });
        VariableSave bigHeight = new VariableSave { Name = "Height", Type = "float", Value = 80f };
        size.States[0].Variables.Add(bigHeight);
        VariableClearLogic logic = new VariableClearLogic();

        logic.ClearVariable(hud, hud.GetDefaultStateOrThrow(), width, isPartOfCategory: false).Message.ShouldBeNull();
        logic.ClearVariable(hud, hud.GetDefaultStateOrThrow(), exposed, isPartOfCategory: false);
        logic.ClearVariable(hud, hud.GetDefaultStateOrThrow(), custom, isPartOfCategory: false);
        VariableClearResult categoryResult = logic.ClearVariable(hud, size.States[0], bigHeight, isPartOfCategory: true);

        hud.GetDefaultStateOrThrow().Variables.ShouldNotContain(width);
        exposed.SetsValue.ShouldBeFalse();
        exposed.Value.ShouldBeNull();
        hud.GetDefaultStateOrThrow().Variables.ShouldContain(exposed);
        custom.Value.ShouldBe(0);
        bigHeight.Value.ShouldBe(50f);
        size.States[0].Variables.ShouldContain(bigHeight);
        categoryResult.Message.ShouldBe("The variable Height is part of the category Size so it cannot be removed. Instead, the value has been set to the value in the default state");
    }
}
