using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// The font-usage questions the Fonts tab and GUM0010 share: which fonts the project's Texts use,
/// and which per-font range entries match none of them.
/// </summary>
public class FontRangeEntryCheckerTests : BaseTestClass
{
    [Fact]
    public void GetUsedFontNames_ListsEachFontOnce_ByTheNameOrPathAnEntryWouldUse()
    {
        AddComponentWithTextFont("Label", "Arial");
        AddComponentWithTextFont("Title", "Arial");
        AddComponentWithTextFont("Icon", "Fonts/Icons.ttf");

        IReadOnlyList<string> names = FontRangeEntryChecker.GetUsedFontNames(Project);

        names.OrderBy(name => name).ShouldBe(new[] { "Arial", "Fonts/Icons.ttf" });
    }

    [Fact]
    public void FindUnmatchedEntries_ReturnsAnEntryNoTextFontReaches()
    {
        AddComponentWithTextFont("Label", "Fonts/Icons.ttf");
        FontRangeEntry typo = new FontRangeEntry { FontFamily = "Fonts/Icon.ttf", Ranges = "57344-57400" };
        FontRangeEntry matched = new FontRangeEntry { FontFamily = "Fonts\\Icons.ttf", Ranges = "57344-57400" };
        Project.FontRangeEntries.Add(typo);
        Project.FontRangeEntries.Add(matched);

        IReadOnlyList<FontRangeEntry> unmatched = FontRangeEntryChecker.FindUnmatchedEntries(Project);

        unmatched.ShouldBe(new[] { typo });
    }

    [Fact]
    public void FindUnmatchedEntries_IgnoresTheProjectWideEntry()
    {
        IReadOnlyList<FontRangeEntry> unmatched = FontRangeEntryChecker.FindUnmatchedEntries(Project);

        unmatched.ShouldBeEmpty();
    }

    private void AddComponentWithTextFont(string componentName, string font)
    {
        ComponentSave component = new ComponentSave { Name = componentName };
        component.Instances.Add(new InstanceSave { Name = "MyText", BaseType = "Text" });
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave { Name = "MyText.Font", Value = font, Type = "string", SetsValue = true, IsFont = true });
        defaultState.Variables.Add(new VariableSave { Name = "MyText.FontSize", Value = 18, Type = "int", SetsValue = true });
        component.States.Add(defaultState);
        Project.Components.Add(component);
    }
}
