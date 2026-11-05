using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins.InternalPlugins.StatePlugin;
using Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>The palette and the rules that find the color of a category, a "Category/State" name and a variable (#7591).</summary>
public class CategoryColorsTests
{
    private static StateSaveCategory Category(string name, string? color) => new StateSaveCategory { Name = name, ColorName = color };

    [Fact]
    public void Names_AreTheTenPaletteColors_NewOnesAppendedAfterTheOriginalEight()
    {
        CategoryColors.Names.ShouldBe(new[] { "Indigo", "Violet", "Magenta", "Rose", "Brown", "Green", "Teal", "Slate", "Orange", "Sky" });
    }

    [Fact]
    public void GetName_ReturnsNull_ForUnsetOrEmptyOrUnknownNames()
    {
        CategoryColors.GetName(Category("A", null)).ShouldBeNull();
        CategoryColors.GetName(Category("A", "")).ShouldBeNull();
        CategoryColors.GetName(Category("A", "Chartreuse")).ShouldBeNull();
        CategoryColors.GetName(null).ShouldBeNull();
    }

    [Fact]
    public void GetName_ReturnsTheName_ForAPaletteColor()
    {
        CategoryColors.GetName(Category("A", "Teal")).ShouldBe("Teal");
    }

    [Fact]
    public void GetNameOfState_FindsTheCategoryByTheNameBeforeTheSlash()
    {
        ComponentSave component = new ComponentSave();
        component.Categories.Add(Category("Size", "Indigo"));
        component.Categories.Add(Category("Misc", null));

        CategoryColors.GetNameOfState(component, "Size/Large").ShouldBe("Indigo");
        CategoryColors.GetNameOfState(component, "Misc/B").ShouldBeNull();
        CategoryColors.GetNameOfState(component, "Missing/B").ShouldBeNull();
        CategoryColors.GetNameOfState(component, "Uncategorized").ShouldBeNull();
        CategoryColors.GetNameOfState(component, null).ShouldBeNull();
    }

    [Fact]
    public void GetFirstName_TakesTheFirstColoredCategoryInOrder_AndSkipsUncolored()
    {
        List<StateSaveCategory> categories = new() { Category("A", null), Category("B", "Rose"), Category("C", "Green") };

        CategoryColors.GetFirstName(categories).ShouldBe("Rose");

        categories.Reverse();

        CategoryColors.GetFirstName(categories).ShouldBe("Green");
    }

    [Fact]
    public void GetFirstName_IsNull_WhenNoCategoryHasAKnownColor()
    {
        CategoryColors.GetFirstName(new[] { Category("A", null), Category("B", "NotAColor") }).ShouldBeNull();
    }

    [Fact]
    public void CategoryViewModel_ColorName_FollowsTheCategory_AndRaisesWhenRefreshed()
    {
        StateSaveCategory category = Category("Size", null);
        CategoryViewModel viewModel = new() { Data = category };
        List<string?> raised = new();
        viewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        viewModel.ColorName.ShouldBeNull();
        category.ColorName = "Violet";
        viewModel.RefreshColor();

        viewModel.ColorName.ShouldBe("Violet");
        raised.ShouldContain(nameof(CategoryViewModel.ColorName));
    }

    [Fact]
    public void CategoryViewModel_ColorName_IsNull_ForAnUnknownName()
    {
        CategoryViewModel viewModel = new() { Data = Category("Size", "FromANewerTool") };

        viewModel.ColorName.ShouldBeNull();
    }
}
