using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;

namespace Gum.Bundle.Tests;

/// <summary>
/// Coverage for the two places <see cref="FontReferenceCollector"/> used to resolve a single value
/// where several can render: a Text whose font comes through a <c>StyleCategoryState</c> assignment,
/// and a conditional (ternary) reference reached through a nested component instance (#6018).
/// </summary>
public class FontReferenceCollectorStyleAndNestedTests : IDisposable
{
    private const string Font = "Arial";
    private const int DefaultSize = 18;

    public FontReferenceCollectorStyleAndNestedTests()
    {
        StandardElementsManager.Self.Initialize();
    }

    [Fact]
    public void Collect_uses_the_style_state_font_for_a_Text_instance_in_the_collected_element()
    {
        const int strongSize = 24;

        StandardElementSave text = BuildTextStandardWithStrongStyle(strongSize);
        ComponentSave panel = TestProjectBuilder.BuildComponent("Panel");
        AddStyledLabel(panel, "Label");

        Dictionary<string, BmfcSave> fonts = Collect(new[] { panel }, text, panel);

        fonts.Keys.ShouldBe(new[] { StrongKey(strongSize) });
    }

    [Fact]
    public void Collect_uses_the_style_state_font_for_a_Text_inside_a_nested_component()
    {
        const int strongSize = 24;

        StandardElementSave text = BuildTextStandardWithStrongStyle(strongSize);
        ComponentSave button = TestProjectBuilder.BuildComponent("Button");
        AddStyledLabel(button, "Label");
        ScreenSave screen = TestProjectBuilder.BuildScreen("MainMenu");
        screen.Instances.Add(new InstanceSave { Name = "StartButton", BaseType = "Button", ParentContainer = screen });

        // Only the screen is collected, so the Button's own states don't mask the nested path.
        Dictionary<string, BmfcSave> fonts = Collect(new[] { button }, text, screen);

        fonts.Keys.ShouldBe(new[] { StrongKey(strongSize) });
    }

    [Fact]
    public void Collect_uses_the_style_state_assigned_through_an_exposed_variable_on_a_nested_component()
    {
        const int strongSize = 24;

        StandardElementSave text = BuildTextStandardWithStrongStyle(strongSize);
        ComponentSave button = TestProjectBuilder.BuildComponent("Button");
        button.Instances.Add(new InstanceSave { Name = "Label", BaseType = "Text", ParentContainer = button });
        SetVariable(button.DefaultState, "Label.StyleCategoryState", "", "StyleCategory");
        button.DefaultState.Variables.Find(v => v.Name == "Label.StyleCategoryState")!.ExposedAsName = "Style";
        button.DefaultState.Variables.Find(v => v.Name == "Label.StyleCategoryState")!.SetsValue = false;

        ScreenSave screen = TestProjectBuilder.BuildScreen("MainMenu");
        screen.Instances.Add(new InstanceSave { Name = "StartButton", BaseType = "Button", ParentContainer = screen });
        SetVariable(screen.DefaultState, "StartButton.Style", "Strong", "StyleCategory");

        Dictionary<string, BmfcSave> fonts = Collect(new[] { button }, text, screen);

        fonts.Keys.ShouldBe(new[] { StrongKey(strongSize) });
    }

    [Fact]
    public void Collect_uses_the_style_state_assigned_by_a_non_default_state()
    {
        const int strongSize = 24;

        StandardElementSave text = BuildTextStandardWithStrongStyle(strongSize);
        ComponentSave panel = TestProjectBuilder.BuildComponent("Panel");
        panel.Instances.Add(new InstanceSave { Name = "Label", BaseType = "Text", ParentContainer = panel });
        StateSaveCategory panelCategory = new StateSaveCategory { Name = "PanelCategory" };
        StateSave emphasized = new StateSave { Name = "Emphasized", ParentContainer = panel };
        SetVariable(emphasized, "Label.StyleCategoryState", "Strong", "StyleCategory");
        panelCategory.States.Add(emphasized);
        panel.Categories.Add(panelCategory);

        Dictionary<string, BmfcSave> fonts = Collect(new[] { panel }, text, panel);

        fonts.Keys.ShouldBe(new[] { PlainKey(DefaultSize), StrongKey(strongSize) }, ignoreOrder: true);
    }

    [Fact]
    public void Collect_generates_both_branches_when_a_conditional_reference_drives_an_exposed_font_variable()
    {
        const int smallSize = 12;
        const int bigSize = 30;

        StandardElementSave text = BuildTextStandard();
        ComponentSave button = TestProjectBuilder.BuildComponent("Button");
        button.Instances.Add(new InstanceSave { Name = "Label", BaseType = "Text", ParentContainer = button });
        SetVariable(button.DefaultState, "Label.Font", Font, "string");
        SetVariable(button.DefaultState, "Label.FontSize", DefaultSize, "int");
        button.DefaultState.Variables.Find(v => v.Name == "Label.FontSize")!.ExposedAsName = "TextSize";

        ScreenSave screen = TestProjectBuilder.BuildScreen("MainMenu");
        screen.Instances.Add(new InstanceSave { Name = "StartButton", BaseType = "Button", ParentContainer = screen });
        SetVariable(screen.DefaultState, "IsBig", false, "bool");
        SetVariable(screen.DefaultState, "StartButton.TextSize", smallSize, "int");
        VariableListSave<string> references = new VariableListSave<string>
        {
            Name = "StartButton.VariableReferences",
            Type = "string"
        };
        references.ValueAsIList.Add($"TextSize = IsBig ? {bigSize} : {smallSize}");
        screen.DefaultState.VariableLists.Add(references);

        Dictionary<string, BmfcSave> fonts = Collect(new[] { button }, text, screen);

        fonts.Keys.ShouldBe(new[] { PlainKey(smallSize), PlainKey(bigSize) }, ignoreOrder: true);
        screen.DefaultState.Variables.Find(v => v.Name == "StartButton.TextSize")!.Value.ShouldBe(smallSize);
        screen.DefaultState.Variables.Count.ShouldBe(2);
    }

    [Fact]
    public void Collect_prefers_a_value_set_on_the_nested_Text_over_the_style_state_assigned_above()
    {
        const int explicitSize = 20;
        const int strongSize = 24;

        StandardElementSave text = BuildTextStandardWithStrongStyle(strongSize);
        ComponentSave button = TestProjectBuilder.BuildComponent("Button");
        button.Instances.Add(new InstanceSave { Name = "Label", BaseType = "Text", ParentContainer = button });
        SetVariable(button.DefaultState, "Label.FontSize", explicitSize, "int");
        SetVariable(button.DefaultState, "Label.StyleCategoryState", "", "StyleCategory");
        VariableSave exposedStyle = button.DefaultState.Variables.Find(v => v.Name == "Label.StyleCategoryState")!;
        exposedStyle.ExposedAsName = "Style";
        exposedStyle.SetsValue = false;

        ScreenSave screen = TestProjectBuilder.BuildScreen("MainMenu");
        screen.Instances.Add(new InstanceSave { Name = "StartButton", BaseType = "Button", ParentContainer = screen });
        SetVariable(screen.DefaultState, "StartButton.Style", "Strong", "StyleCategory");

        Dictionary<string, BmfcSave> fonts = Collect(new[] { button }, text, screen);

        // The explicit size wins; the state still contributes the boldness the Button doesn't set.
        fonts.Keys.ShouldBe(new[] { StrongKey(explicitSize) });
    }

    /// <summary>
    /// A Text standard whose default state supplies the font and size, and a StyleCategory category
    /// with a "Strong" state that overrides size and boldness.
    /// </summary>
    private static StandardElementSave BuildTextStandardWithStrongStyle(int strongSize)
    {
        StandardElementSave text = BuildTextStandard();

        StateSaveCategory styleCategory = new StateSaveCategory { Name = "StyleCategory" };
        StateSave strong = new StateSave { Name = "Strong" };
        strong.Variables.Add(new VariableSave { Name = "FontSize", Type = "int", Value = strongSize, SetsValue = true });
        strong.Variables.Add(new VariableSave { Name = "IsBold", Type = "bool", Value = true, SetsValue = true });
        styleCategory.States.Add(strong);
        text.Categories.Add(styleCategory);
        return text;
    }

    private static StandardElementSave BuildTextStandard()
    {
        StandardElementSave text = TestProjectBuilder.BuildStandard("Text");
        SetVariable(text.DefaultState, "Font", Font, "string");
        SetVariable(text.DefaultState, "FontSize", DefaultSize, "int");
        return text;
    }

    private static void AddStyledLabel(ElementSave element, string instanceName)
    {
        element.Instances.Add(new InstanceSave { Name = instanceName, BaseType = "Text", ParentContainer = element });
        SetVariable(element.DefaultState, instanceName + ".StyleCategoryState", "Strong", "StyleCategory");
    }

    private static void SetVariable(StateSave state, string name, object value, string type)
    {
        state.Variables.Add(new VariableSave { SetsValue = true, Name = name, Value = value, Type = type });
    }

    private static string StrongKey(int size) =>
        BmfcSave.GetFontCacheFileNameFor(
            fontSize: size, fontName: Font, outline: 0, useFontSmoothing: true,
            isItalic: false, isBold: true, fontFilePath: null);

    private static string PlainKey(int size) =>
        BmfcSave.GetFontCacheFileNameFor(
            fontSize: size, fontName: Font, outline: 0, useFontSmoothing: true,
            isItalic: false, isBold: false, fontFilePath: null);

    /// <summary>
    /// Builds a project from <paramref name="components"/> and <paramref name="text"/>, then collects
    /// only <paramref name="collected"/> with expression evaluation wired up (unwired afterwards - the
    /// registration is process-wide static state).
    /// </summary>
    private static Dictionary<string, BmfcSave> Collect(ComponentSave[] components, StandardElementSave text, ElementSave collected)
    {
        List<ComponentSave> allComponents = new List<ComponentSave>(components);
        List<ScreenSave> screens = new List<ScreenSave>();
        if (collected is ScreenSave screen)
        {
            screens.Add(screen);
        }
        else if (!allComponents.Contains((ComponentSave)collected))
        {
            allComponents.Add((ComponentSave)collected);
        }

        GumProjectSave project = TestProjectBuilder.BuildProject(
            screens: screens, components: allComponents, standards: new[] { text });
        ObjectFinder.Self.GumProjectSave = project;

        Gum.Expressions.GumExpressionService.Initialize();
        try
        {
            FontReferenceCollector collector = new FontReferenceCollector(
                instance => ObjectFinder.Self.GetElementSave(instance));
            return collector.Collect(project, new[] { collected });
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
