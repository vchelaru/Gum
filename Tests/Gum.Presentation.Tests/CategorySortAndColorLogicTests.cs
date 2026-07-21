using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;

namespace Gum.Presentation.Tests;

public class CategorySortAndColorLogicTests
{
    private readonly CategorySortAndColorLogic _logic = new();

    [Fact]
    public void SortAndColorCategories_ShouldOrderKnownCategoriesBeforeUnknownOnes_AndAssignHexColors()
    {
        var unknown = new VariableCategoryDescriptor("SomeUnknownCategory");
        var position = new VariableCategoryDescriptor("Position");
        var general = new VariableCategoryDescriptor("General");

        var result = _logic.SortAndColorCategories(new List<VariableCategoryDescriptor> { unknown, position, general });

        result.Select(c => c.Name).ShouldBe(new[] { "General", "Position", "SomeUnknownCategory" });

        general.HeaderColorHex.ShouldStartWith("#");
        position.HeaderColorHex.ShouldNotBeNullOrEmpty();
        position.HeaderColorHex.ShouldStartWith("#");
        unknown.HeaderColorHex.ShouldBeNull("categories outside the known list get no color");
    }
}
