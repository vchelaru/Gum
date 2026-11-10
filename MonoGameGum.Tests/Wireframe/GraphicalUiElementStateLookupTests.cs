using System.Collections.Generic;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Wireframe;

/// <summary>
/// Pins how <see cref="GraphicalUiElement"/> finds a state by name inside a category or among its
/// uncategorized states: a missing name applies nothing, the first of two same-named states wins,
/// names are case-sensitive, and the bare-name overload searches uncategorized states and every category.
/// </summary>
public class GraphicalUiElementStateLookupTests : BaseTestClass
{
    private readonly List<string> _applied = new List<string>();

    private StateSave CreateState(string? name, string tag)
    {
        return new StateSave { Name = name!, Apply = () => _applied.Add(tag) };
    }

    private GraphicalUiElement CreateElementWithCategory()
    {
        GraphicalUiElement element = new GraphicalUiElement(new InvisibleRenderable());
        StateSaveCategory category = new StateSaveCategory { Name = "Mode" };
        category.States.Add(CreateState("Highlighted", "Mode.Highlighted.First"));
        category.States.Add(CreateState("Highlighted", "Mode.Highlighted.Second"));
        category.States.Add(CreateState("Pushed", "Mode.Pushed"));
        element.AddCategory(category);
        return element;
    }

    [Fact]
    public void ApplyState_CategoryAndName_ShouldApplyFirstOfDuplicateNames()
    {
        GraphicalUiElement element = CreateElementWithCategory();

        element.ApplyState("Mode", "Highlighted");

        _applied.ShouldBe(new[] { "Mode.Highlighted.First" });
    }

    [Fact]
    public void ApplyState_CategoryAndName_ShouldBeCaseSensitive()
    {
        GraphicalUiElement element = CreateElementWithCategory();

        element.ApplyState("Mode", "pushed");

        _applied.ShouldBeEmpty();
    }

    [Fact]
    public void ApplyState_CategoryAndMissingName_ShouldApplyNothing()
    {
        GraphicalUiElement element = CreateElementWithCategory();

        element.ApplyState("Mode", "Missing");

        _applied.ShouldBeEmpty();
    }

    [Fact]
    public void ApplyState_Name_ShouldApplyUncategorizedThenEveryCategoryMatch()
    {
        GraphicalUiElement element = CreateElementWithCategory();
        StateSaveCategory other = new StateSaveCategory { Name = "Other" };
        other.States.Add(CreateState("Pushed", "Other.Pushed"));
        element.AddCategory(other);
        element.AddStates(new List<StateSave> { CreateState("Pushed", "Uncategorized.Pushed") });

        element.ApplyState("Pushed");

        _applied.ShouldBe(new[] { "Uncategorized.Pushed", "Mode.Pushed", "Other.Pushed" });
    }

    [Fact]
    public void ApplyStateRecursive_ShouldApplyFirstOfDuplicateNames()
    {
        GraphicalUiElement element = CreateElementWithCategory();

        element.ApplyStateRecursive("Mode", "Highlighted");

        _applied.ShouldBe(new[] { "Mode.Highlighted.First" });
    }

    [Fact]
    public void SetProperty_CategoryState_ShouldApplyFirstOfDuplicateNames()
    {
        GraphicalUiElement element = CreateElementWithCategory();

        element.SetProperty("ModeState", "Highlighted");

        _applied.ShouldBe(new[] { "Mode.Highlighted.First" });
    }

    [Fact]
    public void SetProperty_CategoryStateWithWrongCase_ShouldApplyNothing()
    {
        GraphicalUiElement element = CreateElementWithCategory();

        element.SetProperty("ModeState", "highlighted");

        _applied.ShouldBeEmpty();
    }

    [Fact]
    public void SetProperty_CategoryState_ShouldMatchWholeCategoryName()
    {
        GraphicalUiElement element = CreateElementWithCategory();
        StateSaveCategory longer = new StateSaveCategory { Name = "ModeX" };
        longer.States.Add(CreateState("Pushed", "ModeX.Pushed"));
        element.AddCategory(longer);

        element.SetProperty("ModeXState", "Pushed");
        element.SetProperty("ModState", "Pushed");
        element.SetProperty("MissingState", "Pushed");

        _applied.ShouldBe(new[] { "ModeX.Pushed" });
    }

    [Fact]
    public void TryGetStateByName_ShouldPreferUncategorizedThenFirstCategoryMatch()
    {
        GraphicalUiElement element = CreateElementWithCategory();

        element.TryGetStateByName("Highlighted", out StateSave? fromCategory).ShouldBeTrue();
        fromCategory!.Name.ShouldBe("Highlighted");
        fromCategory.Apply!();
        _applied.ShouldBe(new[] { "Mode.Highlighted.First" });

        element.TryGetStateByName("highlighted", out StateSave? wrongCase).ShouldBeFalse();
        wrongCase.ShouldBeNull();
    }

    [Fact]
    public void SetProperty_BareState_ShouldNotApplyUncategorizedState()
    {
        GraphicalUiElement element = new GraphicalUiElement(new InvisibleRenderable());
        element.AddStates(new List<StateSave> { CreateState("Big", "Big") });

        element.SetProperty("State", "Big");

        _applied.ShouldBeEmpty();
    }

    [Fact]
    public void SetProperty_CategoryNamedLikeVariable_ShouldApplyStateAndKeepVariable()
    {
        GraphicalUiElement element = new GraphicalUiElement(new InvisibleRenderable());
        StateSaveCategory category = new StateSaveCategory { Name = "Width" };
        category.States.Add(CreateState("A", "Width.A"));
        element.AddCategory(category);

        element.SetProperty("WidthState", "A");
        element.SetProperty("Width", 17f);

        _applied.ShouldBe(new[] { "Width.A" });
        element.Width.ShouldBe(17f);
    }

    [Fact]
    public void SetProperty_NameEndingInStateWithoutCategory_ShouldApplyNothing()
    {
        GraphicalUiElement element = new GraphicalUiElement(new InvisibleRenderable());
        StateSaveCategory category = new StateSaveCategory { Name = "Mode" };
        category.States.Add(CreateState("A", "Mode.A"));
        element.AddCategory(category);

        element.SetProperty("ModeXState", "A");
        element.SetProperty("ModState", "A");
        element.SetProperty("XModeState", "A");

        _applied.ShouldBeEmpty();
    }

    [Fact]
    public void ApplyState_NullNameAndEmptyCategory_ShouldMatchFirstOrDefault()
    {
        GraphicalUiElement element = new GraphicalUiElement(new InvisibleRenderable());
        StateSaveCategory category = new StateSaveCategory { Name = "Mode" };
        category.States.Add(CreateState("A", "Mode.A"));
        category.States.Add(CreateState(null, "Mode.Null"));
        element.AddCategory(category);
        element.AddCategory(new StateSaveCategory { Name = "Empty" });

        element.ApplyState("Mode", null!);
        element.ApplyState("Empty", "A");

        _applied.ShouldBe(new[] { "Mode.Null" });
    }

    [Fact]
    public void ApplyState_VariablesPath_ShouldApplyVariablesAndNestedCategoryState()
    {
        GraphicalUiElement element = new GraphicalUiElement(new InvisibleRenderable());
        StateSaveCategory category = new StateSaveCategory { Name = "Mode" };
        category.States.Add(CreateState("A", "Mode.A"));
        element.AddCategory(category);
        StateSave state = new StateSave { Name = "V" };
        state.Variables.Add(new VariableSave { Name = "Width", Value = 33f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Height", Value = 44f, SetsValue = false });
        state.Variables.Add(new VariableSave { Name = "ModeState", Value = "A", SetsValue = true });

        element.ApplyState(state);

        element.Width.ShouldBe(33f);
        element.Height.ShouldNotBe(44f);
        _applied.ShouldBe(new[] { "Mode.A" });
        element.IsLayoutSuspended.ShouldBeFalse();
    }
}
