using System.Drawing;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.StateAnimation.SaveClasses;
using Gum.ToolStates;
using Moq;
using Shouldly;
using StateAnimationPlugin.ViewModels;

namespace Gum.Presentation.Tests;

/// <summary>
/// Where a category color shows beyond the States tab (#7591): the "Editing state" banner, the bar on a
/// Variables row set by a category, and the Animations list and keyframes.
/// </summary>
public class CategoryColorBannerAndRowTests : BaseTestClass
{
    private static StateEditingIndicatorInfo BannerFor(string? categoryColor, bool animatedCustomState = false)
    {
        ComponentSave element = new ComponentSave { Name = "Button" };
        StateSaveCategory category = new StateSaveCategory { Name = "Size", ColorName = categoryColor };
        StateSave state = new StateSave { Name = "Large" };
        category.States.Add(state);
        element.Categories.Add(category);
        Mock<ISelectedState> selectedState = new();
        selectedState.Setup(s => s.SelectedElement).Returns(element);
        selectedState.Setup(s => s.SelectedStateSave).Returns(state);
        selectedState.Setup(s => s.SelectedStateCategorySave).Returns(category);
        if (animatedCustomState)
        {
            selectedState.Setup(s => s.CustomCurrentStateSave).Returns(new StateSave());
        }
        return new StateEditingIndicatorService(selectedState.Object).GetInfo();
    }

    [Fact]
    public void Banner_CarriesTheCategoryColorName_ForAColoredCategory()
    {
        StateEditingIndicatorInfo info = BannerFor("Indigo");

        info.CategoryColorName.ShouldBe("Indigo");
        info.StateInformation.ShouldBe("Editing state Size/Large");
    }

    [Fact]
    public void Banner_KeepsTheYellowWithNoColorName_ForAnUncoloredOrUnknownCategory()
    {
        StateEditingIndicatorInfo uncolored = BannerFor(null);
        StateEditingIndicatorInfo unknown = BannerFor("FromANewerTool");

        uncolored.CategoryColorName.ShouldBeNull();
        uncolored.StateBackground.ShouldBe(Color.Yellow);
        unknown.CategoryColorName.ShouldBeNull();
        unknown.StateBackground.ShouldBe(Color.Yellow);
    }

    [Fact]
    public void Banner_StaysPinkWithNoColorName_ForAnAnimatedCustomState()
    {
        StateEditingIndicatorInfo info = BannerFor("Indigo", animatedCustomState: true);

        info.StateBackground.ShouldBe(Color.Pink);
        info.CategoryColorName.ShouldBeNull();
    }

    private static (ComponentSave Element, VariableSave Width) TwoCategoriesSettingWidth(string? firstColor, string? secondColor)
    {
        ComponentSave element = new ComponentSave { Name = "Button" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = element };
        VariableSave width = new VariableSave { Name = "Width", Type = "float", Value = 100f, SetsValue = true };
        defaultState.Variables.Add(width);
        element.States.Add(defaultState);
        foreach ((string name, string? color) in new[] { ("Size", firstColor), ("Layout", secondColor) })
        {
            StateSaveCategory category = new StateSaveCategory { Name = name, ColorName = color };
            StateSave state = new StateSave { Name = name + "A", ParentContainer = element };
            state.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 50f, SetsValue = true });
            category.States.Add(state);
            element.Categories.Add(category);
        }
        return (element, width);
    }

    [Fact]
    public void RowColor_FirstColoredCategoryInOrderWins_AndTheSetByTextStillListsAll()
    {
        (ComponentSave element, VariableSave width) = TwoCategoriesSettingWidth("Indigo", "Rose");
        SubtextLogic subtext = new SubtextLogic();

        subtext.GetCategoryColorName(width, "Width", element, null).ShouldBe("Indigo");
        subtext.GetDefaultSubtext(width, null, "Width", element, null).ShouldBe("Set by Size, Layout");
    }

    [Fact]
    public void RowColor_ReorderingCategoriesChangesTheWinner()
    {
        (ComponentSave element, VariableSave width) = TwoCategoriesSettingWidth("Indigo", "Rose");
        SubtextLogic subtext = new SubtextLogic();

        element.Categories.Reverse();

        subtext.GetCategoryColorName(width, "Width", element, null).ShouldBe("Rose");
    }

    [Fact]
    public void RowColor_SkipsAnUncoloredFirstCategory_AndIsNullWhenNoneIsColored()
    {
        (ComponentSave element, VariableSave width) = TwoCategoriesSettingWidth(null, "Rose");
        SubtextLogic subtext = new SubtextLogic();

        subtext.GetCategoryColorName(width, "Width", element, null).ShouldBe("Rose");

        element.Categories[1].ColorName = null;

        subtext.GetCategoryColorName(width, "Width", element, null).ShouldBeNull();
    }

    private static (ComponentSave Element, AnimationViewModel Animation) AnimationOver(params (string State, float Time)[] frames)
    {
        ComponentSave element = new ComponentSave { Name = "Button" };
        element.States.Add(new StateSave { Name = "Default" });
        element.Categories.Add(new StateSaveCategory { Name = "Size", ColorName = "Indigo", States = { new StateSave { Name = "Small" }, new StateSave { Name = "Large" } } });
        element.Categories.Add(new StateSaveCategory { Name = "Misc", States = { new StateSave { Name = "A" } } });
        AnimationSave save = new AnimationSave { Name = "Grow" };
        foreach ((string state, float time) in frames)
        {
            save.States.Add(new AnimatedStateSave { StateName = state, Time = time });
        }
        Mock<ISelectedState> selectedState = new();
        selectedState.Setup(s => s.SelectedElement).Returns(element);
        AnimationViewModel animation = AnimationViewModel.FromSave(save, element, Mock.Of<StateAnimationPlugin.Managers.IAnimationSaveRepository>(),
            selectedState.Object, Mock.Of<Gum.Wireframe.IWireframeObjectManager>());
        return (element, animation);
    }

    [Fact]
    public void Animation_TakesTheColorOfItsFirstKeyframesCategory_AndKeyframesGetTheirOwn()
    {
        (_, AnimationViewModel animation) = AnimationOver(("Size/Small", 0f), ("Misc/A", 0.5f));

        animation.CategoryColorName.ShouldBe("Indigo");
        animation.Keyframes.Select(k => k.CategoryColorName).ShouldBe(new string?[] { "Indigo", null });
    }

    [Fact]
    public void Animation_IsUncolored_WhenItsFirstKeyframeIsInAnUncoloredCategory()
    {
        (_, AnimationViewModel animation) = AnimationOver(("Misc/A", 0f), ("Size/Large", 0.5f));

        animation.CategoryColorName.ShouldBeNull();
        animation.Keyframes[1].CategoryColorName.ShouldBe("Indigo");
    }

    [Fact]
    public void Animation_RefreshErrors_PicksUpAChangedCategoryColor()
    {
        (ComponentSave element, AnimationViewModel animation) = AnimationOver(("Size/Small", 0f));

        element.Categories[0].ColorName = "Teal";
        animation.RefreshErrors(element);

        animation.CategoryColorName.ShouldBe("Teal");
        animation.Keyframes[0].CategoryColorName.ShouldBe("Teal");
    }
}
