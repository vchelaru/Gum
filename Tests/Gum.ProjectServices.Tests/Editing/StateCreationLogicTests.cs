using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class StateCreationLogicTests : BaseTestClass
{
    private readonly StateCreationLogic _logic = new StateCreationLogic();

    [Fact]
    public void AddCategoryStateVariable_AddsSelectorOnce()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        StateSaveCategory category = _logic.AddCategory(hud, "Size");

        VariableSave? first = _logic.AddCategoryStateVariable(hud, category.Name);
        VariableSave? second = _logic.AddCategoryStateVariable(hud, category.Name);

        hud.Categories.ShouldBe(new[] { category });
        first.ShouldNotBeNull();
        first.Name.ShouldBe("SizeState");
        first.Type.ShouldBe("Size");
        second.ShouldBeNull();
        hud.DefaultState.Variables.Count(item => item.Name == "SizeState").ShouldBe(1);
    }

    [Fact]
    public void AddState_InCategory_SetsParentContainerAndInsertsAtIndex()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        StateSaveCategory category = _logic.AddCategory(hud, "Size");
        StateSave normal = new StateSave { Name = "Normal" };
        StateSave big = new StateSave { Name = "Big" };

        _logic.AddState(hud, category, normal, desiredIndex: null);
        _logic.AddState(hud, category, big, desiredIndex: 0);

        category.States.ShouldBe(new[] { big, normal });
        normal.ParentContainer.ShouldBe(hud);
    }
}
