using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;

namespace Gum.Bundle.Tests;

/// <summary>
/// A non-default state of the collected element reaches a nested component's Text only through the
/// variables the state sets on the component instance that the component passes down. Pins which
/// states contribute fonts, and that a state setting only unrelated variables (position, visibility)
/// does not walk the nested component again (#7623).
/// </summary>
public class FontReferenceCollectorNestedStateTests : IDisposable
{
    private const string Font = "Arial";
    private const int DefaultSize = 18;

    public FontReferenceCollectorNestedStateTests()
    {
        StandardElementsManager.Self.Initialize();
    }

    [Fact]
    public void Collect_finds_a_font_size_an_exposed_variable_sets_in_a_non_default_state()
    {
        ScreenSave screen = BuildScreenWithButton(out ComponentSave button);
        ExposeFontSize(button, "TextSize");
        AddState(screen, "Layout", "Big", ("StartButton.TextSize", 30, "int"));

        Dictionary<string, BmfcSave> fonts = Collect(button, screen, out _);

        fonts.Keys.ShouldBe(new[] { PlainKey(DefaultSize), PlainKey(30) }, ignoreOrder: true);
    }

    [Fact]
    public void Collect_finds_a_style_an_exposed_state_variable_assigns_in_a_non_default_state()
    {
        ScreenSave screen = BuildScreenWithButton(out ComponentSave button);
        VariableSave exposedStyle = new VariableSave
        {
            Name = "Label.StyleCategoryState", Type = "StyleCategory", Value = "", SetsValue = false, ExposedAsName = "Style"
        };
        button.DefaultState.Variables.Add(exposedStyle);
        AddState(screen, "Layout", "Emphasis", ("StartButton.Style", "Strong", "StyleCategory"));

        Dictionary<string, BmfcSave> fonts = Collect(button, screen, out _);

        fonts.Keys.ShouldBe(new[] { PlainKey(DefaultSize), StrongKey(24) }, ignoreOrder: true);
    }

    [Fact]
    public void Collect_finds_a_BBCode_font_an_exposed_text_sets_in_a_non_default_state()
    {
        ScreenSave screen = BuildScreenWithButton(out ComponentSave button);
        button.DefaultState.Variables.Add(new VariableSave
        {
            Name = "Label.Text", Type = "string", Value = "", SetsValue = false, ExposedAsName = "Caption"
        });
        AddState(screen, "Layout", "Rich", ("StartButton.Caption", "[Font=Courier]x[/Font]", "string"));

        Dictionary<string, BmfcSave> fonts = Collect(button, screen, out _);

        fonts.Keys.ShouldBe(
            new[]
            {
                PlainKey(DefaultSize),
                BmfcSave.GetFontCacheFileNameFor(DefaultSize, "Courier", 0, true, false, false, null)
            },
            ignoreOrder: true);
    }

    [Fact]
    public void Collect_finds_a_font_a_conditional_reference_in_a_non_default_state_drives()
    {
        ScreenSave screen = BuildScreenWithButton(out ComponentSave button);
        ExposeFontSize(button, "TextSize");
        StateSave state = AddState(screen, "Layout", "Cond", ("IsBig", false, "bool"), ("StartButton.TextSize", 12, "int"));
        VariableListSave<string> references = new VariableListSave<string> { Name = "StartButton.VariableReferences", Type = "string" };
        references.ValueAsIList.Add("TextSize = IsBig ? 30 : 12");
        state.VariableLists.Add(references);

        Dictionary<string, BmfcSave> fonts = Collect(button, screen, out _);

        fonts.Keys.ShouldBe(new[] { PlainKey(DefaultSize), PlainKey(12), PlainKey(30) }, ignoreOrder: true);
    }

    [Fact]
    public void Collect_ignores_a_non_default_state_that_only_sets_variables_the_component_does_not_pass_down()
    {
        ScreenSave screen = BuildScreenWithButton(out ComponentSave button);
        ExposeFontSize(button, "TextSize");

        Dictionary<string, BmfcSave> fonts = Collect(button, screen, out _);
        fonts.Keys.ShouldBe(new[] { PlainKey(DefaultSize) });

        AddState(screen, "Layout", "Moved", ("StartButton.X", 10f, "float"), ("StartButton.Visible", false, "bool"));
        Dictionary<string, BmfcSave> withInertState = Collect(button, screen, out _);

        withInertState.Keys.ShouldBe(fonts.Keys);
    }

    [Fact]
    public void Collect_does_not_walk_the_nested_component_again_for_states_that_only_set_unrelated_variables()
    {
        ScreenSave screen = BuildScreenWithButton(out ComponentSave button);
        ExposeFontSize(button, "TextSize");
        Collect(button, screen, out int resolveCallsWithoutStates);

        for (int i = 0; i < 10; i++)
        {
            AddState(screen, "Layout", "Moved" + i, ("StartButton.X", (float)i, "float"), ("StartButton.Visible", i % 2 == 0, "bool"));
        }
        Collect(button, screen, out int resolveCallsWithTenStates);

        // Each state still costs the cheap lookup of the instance itself, but not another nested walk.
        (resolveCallsWithTenStates - resolveCallsWithoutStates).ShouldBeLessThanOrEqualTo(10 * 4);
    }

    [Fact]
    public void Collect_keeps_the_raw_component_font_when_the_default_state_references_a_font_and_another_state_only_moves_the_instance()
    {
        // The default state's conditional reference sets the exposed size per branch (12 and 30); a state
        // that only moves the instance sees the component's own size (18), which the default walk never emits.
        ScreenSave screen = BuildScreenWithButton(out ComponentSave button);
        ExposeFontSize(button, "TextSize");
        button.DefaultState.Variables.Find(v => v.Name == "Label.FontSize")!.SetsValue = false;
        screen.DefaultState.Variables.Add(new VariableSave { SetsValue = true, Name = "IsBig", Type = "bool", Value = false });
        VariableListSave<string> references = new VariableListSave<string> { Name = "StartButton.VariableReferences", Type = "string" };
        references.ValueAsIList.Add("TextSize = IsBig ? 30 : 12");
        screen.DefaultState.VariableLists.Add(references);
        AddState(screen, "Layout", "Moved", ("StartButton.X", 10f, "float"));

        Dictionary<string, BmfcSave> fonts = Collect(button, screen, out _);

        fonts.Keys.ShouldBe(new[] { PlainKey(DefaultSize), PlainKey(12), PlainKey(30) }, ignoreOrder: true);
    }

    [Fact]
    public void Collect_finds_a_font_set_through_an_alias_exposed_at_both_levels_of_nesting()
    {
        ScreenSave screen = BuildScreenWithTwoLevels(outerAlias: "TextSize", out ComponentSave[] components);
        AddState(screen, "Layout", "Big", ("Panel.TextSize", 30, "int"));

        Dictionary<string, BmfcSave> fonts = Collect(components, screen, out _);

        fonts.Keys.ShouldBe(new[] { PlainKey(DefaultSize), PlainKey(30) }, ignoreOrder: true);
    }

    [Fact]
    public void Collect_ignores_a_value_set_under_the_name_the_outer_component_exposes_it_as_when_it_is_not_the_inner_name()
    {
        ScreenSave screen = BuildScreenWithTwoLevels(outerAlias: "Other", out ComponentSave[] components);
        AddState(screen, "Layout", "Big", ("Panel.Other", 30, "int"));

        Dictionary<string, BmfcSave> fonts = Collect(components, screen, out _);

        fonts.Keys.ShouldBe(new[] { PlainKey(DefaultSize) });
    }

    [Fact]
    public void Collect_ignores_a_leftover_value_for_a_variable_only_the_inner_component_exposes()
    {
        // Intended difference from before #7623: Panel sets TextSize, which Button exposes but Panel does
        // not. The tool cannot show or remove such a variable, so a font it names is no longer reported.
        ScreenSave screen = BuildScreenWithTwoLevels(outerAlias: null, out ComponentSave[] components);
        AddState(screen, "Layout", "Big", ("Panel.TextSize", 31, "int"));

        Dictionary<string, BmfcSave> fonts = Collect(components, screen, out _);

        fonts.Keys.ShouldBe(new[] { PlainKey(DefaultSize) });
    }

    /// <summary>A Panel containing a Button whose Label's size is exposed as TextSize; Panel re-exposes it as <paramref name="outerAlias"/> (not at all when null).</summary>
    private static ScreenSave BuildScreenWithTwoLevels(string? outerAlias, out ComponentSave[] components)
    {
        ComponentSave button = TestProjectBuilder.BuildComponent("Button");
        button.Instances.Add(new InstanceSave { Name = "Label", BaseType = "Text", ParentContainer = button });
        ExposeFontSize(button, "TextSize");

        ComponentSave panel = TestProjectBuilder.BuildComponent("Panel");
        panel.Instances.Add(new InstanceSave { Name = "Inner", BaseType = "Button", ParentContainer = panel });
        if (outerAlias != null)
        {
            panel.DefaultState.Variables.Add(new VariableSave
            {
                Name = "Inner.TextSize", Type = "int", Value = DefaultSize, SetsValue = false, ExposedAsName = outerAlias
            });
        }

        ScreenSave screen = TestProjectBuilder.BuildScreen("MainMenu");
        screen.Instances.Add(new InstanceSave { Name = "Panel", BaseType = "Panel", ParentContainer = screen });
        components = new[] { button, panel };
        return screen;
    }

    private static ScreenSave BuildScreenWithButton(out ComponentSave button)
    {
        button = TestProjectBuilder.BuildComponent("Button");
        for (int i = 0; i < 20; i++)
        {
            button.Instances.Add(new InstanceSave { Name = "Label" + i, BaseType = "Text", ParentContainer = button });
        }
        button.Instances.Add(new InstanceSave { Name = "Label", BaseType = "Text", ParentContainer = button });
        ScreenSave screen = TestProjectBuilder.BuildScreen("MainMenu");
        screen.Instances.Add(new InstanceSave { Name = "StartButton", BaseType = "Button", ParentContainer = screen });
        return screen;
    }

    private static void ExposeFontSize(ComponentSave button, string alias)
    {
        button.DefaultState.Variables.Add(new VariableSave
        {
            Name = "Label.FontSize", Type = "int", Value = DefaultSize, SetsValue = true, ExposedAsName = alias
        });
    }

    private static StateSave AddState(ScreenSave screen, string category, string name, params (string Name, object Value, string Type)[] variables)
    {
        StateSaveCategory? stateCategory = screen.Categories.Find(item => item.Name == category);
        if (stateCategory == null)
        {
            stateCategory = new StateSaveCategory { Name = category };
            screen.Categories.Add(stateCategory);
        }
        StateSave state = new StateSave { Name = name, ParentContainer = screen };
        foreach ((string variableName, object value, string type) in variables)
        {
            state.Variables.Add(new VariableSave { SetsValue = true, Name = variableName, Value = value, Type = type });
        }
        stateCategory.States.Add(state);
        return state;
    }

    private static string StrongKey(int size) =>
        BmfcSave.GetFontCacheFileNameFor(size, Font, 0, true, false, true, null);

    private static string PlainKey(int size) =>
        BmfcSave.GetFontCacheFileNameFor(size, Font, 0, true, false, false, null);

    private static Dictionary<string, BmfcSave> Collect(ComponentSave button, ScreenSave screen, out int resolveCalls) =>
        Collect(new[] { button }, screen, out resolveCalls);

    private static Dictionary<string, BmfcSave> Collect(ComponentSave[] components, ScreenSave screen, out int resolveCalls)
    {
        StandardElementSave text = TestProjectBuilder.BuildStandard("Text");
        text.DefaultState.Variables.Add(new VariableSave { SetsValue = true, Name = "Font", Type = "string", Value = Font });
        text.DefaultState.Variables.Add(new VariableSave { SetsValue = true, Name = "FontSize", Type = "int", Value = DefaultSize });
        StateSaveCategory styleCategory = new StateSaveCategory { Name = "StyleCategory" };
        StateSave strong = new StateSave { Name = "Strong" };
        strong.Variables.Add(new VariableSave { Name = "FontSize", Type = "int", Value = 24, SetsValue = true });
        strong.Variables.Add(new VariableSave { Name = "IsBold", Type = "bool", Value = true, SetsValue = true });
        styleCategory.States.Add(strong);
        text.Categories.Add(styleCategory);

        GumProjectSave project = TestProjectBuilder.BuildProject(
            screens: new List<ScreenSave> { screen }, components: new List<ComponentSave>(components), standards: new[] { text });
        ObjectFinder.Self.GumProjectSave = project;

        int calls = 0;
        Gum.Expressions.GumExpressionService.Initialize();
        try
        {
            FontReferenceCollector collector = new FontReferenceCollector(instance =>
            {
                calls++;
                return ObjectFinder.Self.GetElementSave(instance);
            });
            Dictionary<string, BmfcSave> fonts = collector.Collect(project, new[] { screen });
            resolveCalls = calls;
            return fonts;
        }
        finally
        {
            GumRuntime.ElementSaveExtensions.ClearRegistrations();
        }
    }

    public void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
    }
}
