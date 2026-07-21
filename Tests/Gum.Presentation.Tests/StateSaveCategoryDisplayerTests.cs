using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Moq;
using Shouldly;
using System.Linq;

namespace Gum.Presentation.Tests;

public class StateSaveCategoryDisplayerTests : BaseTestClass
{
    private readonly Mock<IVariableInCategoryPropagationLogic> _propagationLogic = new();

    private StateSaveCategoryDisplayer CreateSut() => new(_propagationLogic.Object);

    [Fact]
    public void BuildCommonMembersCategory_ShouldReturnNull_WhenCategoryHasNoStates()
    {
        StateSaveCategoryDisplayer sut = CreateSut();
        var category = new StateSaveCategory { Name = "Coloring" };

        sut.BuildCommonMembersCategory(instance: null, category).ShouldBeNull();
    }

    [Fact]
    public void BuildCommonMembersCategory_ShouldIncludeVariableAndVariableListNames_WhenFirstStateHasThem()
    {
        StateSaveCategoryDisplayer sut = CreateSut();
        var category = new StateSaveCategory { Name = "Coloring" };
        var firstState = new StateSave();
        firstState.SetValue("Red", 255);
        firstState.VariableLists.Add(new VariableListSave<string> { Name = "Tags" });
        category.States.Add(firstState);

        var result = sut.BuildCommonMembersCategory(instance: null, category);

        result.ShouldNotBeNull();
        result!.Name.ShouldBe("Coloring Variables");
        result.Members.Select(m => m.Name).ShouldBe(new[] { "Red", "Tags" });
    }

    [Fact]
    public void BuildCommonMembersCategory_ShouldExcludeVariable_WhenExcludeFromInstancesIsSetAndInstanceProvided()
    {
        StateSaveCategoryDisplayer sut = CreateSut();
        var category = new StateSaveCategory { Name = "Coloring" };
        var firstState = new StateSave();
        firstState.SetValue("Red", 255);
        firstState.GetVariableSave("Red")!.ExcludeFromInstances = true;
        category.States.Add(firstState);

        var result = sut.BuildCommonMembersCategory(instance: new Gum.DataTypes.InstanceSave(), category);

        result.ShouldBeNull();
    }

    [Fact]
    public void BuildCommonMembersCategory_ShouldMarkRowsAsRemoveButtons_WhenBuilt()
    {
        StateSaveCategoryDisplayer sut = CreateSut();
        var category = new StateSaveCategory { Name = "Coloring" };
        var firstState = new StateSave();
        firstState.SetValue("Red", 255);
        category.States.Add(firstState);

        var result = sut.BuildCommonMembersCategory(instance: null, category);

        result!.Members.Single().PreferredDisplayerKindOverride
            .ShouldBe(VariableDisplayerKind.RemoveButton);
    }

    [Fact]
    public void BuildCommonMembersCategory_RowSet_ShouldAskToRemoveVariableFromAllStatesInCategory()
    {
        StateSaveCategoryDisplayer sut = CreateSut();
        var category = new StateSaveCategory { Name = "Coloring" };
        var firstState = new StateSave();
        firstState.SetValue("Red", 255);
        category.States.Add(firstState);

        var result = sut.BuildCommonMembersCategory(instance: null, category);
        result!.Members.Single().Set!.Invoke(null);

        _propagationLogic.Verify(x => x.AskRemoveVariableFromAllStatesInCategory("Red", category), Times.Once);
    }

    [Fact]
    public void BuildCommonMembersCategory_RowGet_ShouldReturnVariableName()
    {
        StateSaveCategoryDisplayer sut = CreateSut();
        var category = new StateSaveCategory { Name = "Coloring" };
        var firstState = new StateSave();
        firstState.SetValue("Red", 255);
        category.States.Add(firstState);

        var result = sut.BuildCommonMembersCategory(instance: null, category);

        result!.Members.Single().Get().ShouldBe("Red");
    }

    [Fact]
    public void GetCategoriesFor_ShouldReturnSingleEmptyCategory_ForBehaviorAndCategory()
    {
        // Pinning the original (odd, likely-dead) behavior: this always returns a single category
        // header with no members - ported unchanged from the WPF-typed class. See the PR description.
        var behavior = new BehaviorSave { Name = "MyBehavior" };
        var category = new StateSaveCategory { Name = "Coloring" };

        var result = StateSaveCategoryDisplayer.GetCategoriesFor(behavior, category);

        result.Single().Name.ShouldBe("Coloring Properties");
        result.Single().Members.ShouldBeEmpty();
    }
}
