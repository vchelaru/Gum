using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;

namespace Gum.Bundle.Tests;

/// <summary>
/// Coverage for <see cref="FontReferenceCollector"/> baking the fonts a Text value's BBCode tags
/// resolve to (#7201). The renderers create a run's font while drawing, so a fresh checkout has none
/// of them until something displays the text.
/// </summary>
public class FontReferenceCollectorBbCodeTests : IDisposable
{
    private const string Font = "Arial";
    private const int BaseSize = 18;

    public FontReferenceCollectorBbCodeTests()
    {
        StandardElementsManager.Self.Initialize();
    }

    [Fact]
    public void Collect_bakes_the_size_of_a_FontSize_tag()
    {
        ScreenSave screen = ScreenWithText("[FontSize=30]big[/FontSize] small");

        Collect(screen).Keys.ShouldBe(new[] { Key(BaseSize), Key(30) }, ignoreOrder: true);
    }

    [Fact]
    public void Collect_bakes_each_combination_of_nested_tags()
    {
        ScreenSave screen = ScreenWithText("[IsBold=true]a[FontSize=30]b[/FontSize]c[/IsBold]");

        Collect(screen).Keys.ShouldBe(
            new[] { Key(BaseSize), Key(BaseSize, bold: true), Key(30, bold: true) }, ignoreOrder: true);
    }

    [Fact]
    public void Collect_bakes_font_outline_and_italic_tags()
    {
        ScreenSave screen = ScreenWithText("[Font=Verdana]a[/Font][OutlineThickness=2]b[/OutlineThickness][IsItalic=true]c[/IsItalic]");

        Collect(screen).Keys.ShouldBe(
            new[]
            {
                Key(BaseSize),
                Key(BaseSize, name: "Verdana"),
                Key(BaseSize, outline: 2),
                Key(BaseSize, italic: true),
            },
            ignoreOrder: true);
    }

    [Fact]
    public void Collect_bakes_a_dropshadow_font_for_a_HasDropshadow_tag()
    {
        ScreenSave screen = ScreenWithText("[HasDropshadow=true]shadow[/HasDropshadow]");

        Dictionary<string, BmfcSave> fonts = Collect(screen);

        fonts.Count.ShouldBe(2);
        fonts.Values.ShouldContain(f => f.HasDropshadow);
        fonts.Values.ShouldContain(f => !f.HasDropshadow);
    }

    [Fact]
    public void Collect_matches_tag_names_ignoring_case()
    {
        ScreenSave screen = ScreenWithText("[fontsize=30]big[/FONTSIZE]");

        Collect(screen).Keys.ShouldBe(new[] { Key(BaseSize), Key(30) }, ignoreOrder: true);
    }

    [Fact]
    public void Collect_ignores_a_tag_with_an_invalid_value()
    {
        ScreenSave screen = ScreenWithText("[FontSize=abc]text[/FontSize] [OutlineThickness=x]y[/OutlineThickness]");

        Collect(screen).Keys.ShouldBe(new[] { Key(BaseSize) });
    }

    [Fact]
    public void Collect_ignores_a_text_value_with_brackets_that_are_not_tags()
    {
        ScreenSave screen = ScreenWithText("array[3] and [unknown]x[/unknown]");

        Collect(screen).Keys.ShouldBe(new[] { Key(BaseSize) });
    }

    [Fact]
    public void Collect_scans_the_text_value_of_every_state()
    {
        ScreenSave screen = ScreenWithText("plain");
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        StateSave highlighted = new StateSave { Name = "Highlighted", ParentContainer = screen };
        highlighted.Variables.Add(new VariableSave
        {
            Name = "Label.Text", Type = "string", Value = "[FontSize=40]hi[/FontSize]", SetsValue = true
        });
        category.States.Add(highlighted);
        screen.Categories.Add(category);

        Collect(screen).Keys.ShouldBe(new[] { Key(BaseSize), Key(40) }, ignoreOrder: true);
    }

    [Fact]
    public void Collect_bakes_the_runs_of_a_Text_inside_a_component_instance()
    {
        ComponentSave button = TestProjectBuilder.BuildComponent("Button");
        button.Instances.Add(new InstanceSave { Name = "Label", BaseType = "Text", ParentContainer = button });
        SetVariable(button.DefaultState, "Label.Text", "[FontSize=30]go[/FontSize]", "string");

        ScreenSave screen = TestProjectBuilder.BuildScreen("Menu");
        screen.Instances.Add(new InstanceSave { Name = "Start", BaseType = "Button", ParentContainer = screen });

        // Only the screen is collected, so the Button's own states don't mask the nested path.
        Dictionary<string, BmfcSave> fonts = Collect(screen, button);

        fonts.Keys.ShouldBe(new[] { Key(BaseSize), Key(30) }, ignoreOrder: true);
    }

    private static ScreenSave ScreenWithText(string textValue)
    {
        ScreenSave screen = TestProjectBuilder.BuildScreen("Menu");
        screen.Instances.Add(new InstanceSave { Name = "Label", BaseType = "Text", ParentContainer = screen });
        SetVariable(screen.DefaultState, "Label.Text", textValue, "string");
        return screen;
    }

    private static void SetVariable(StateSave state, string name, object value, string type)
    {
        state.Variables.Add(new VariableSave { SetsValue = true, Name = name, Value = value, Type = type });
    }

    private static string Key(int size, string name = Font, bool bold = false, bool italic = false, int outline = 0) =>
        BmfcSave.GetFontCacheFileNameFor(
            fontSize: size, fontName: name, outline: outline, useFontSmoothing: true,
            isItalic: italic, isBold: bold, fontFilePath: null);

    private static Dictionary<string, BmfcSave> Collect(ScreenSave screen, params ComponentSave[] components)
    {
        StandardElementSave text = TestProjectBuilder.BuildStandard("Text");
        SetVariable(text.DefaultState, "Font", Font, "string");
        SetVariable(text.DefaultState, "FontSize", BaseSize, "int");

        GumProjectSave project = TestProjectBuilder.BuildProject(
            screens: new[] { screen }, components: components, standards: new[] { text });
        ObjectFinder.Self.GumProjectSave = project;

        FontReferenceCollector collector = new FontReferenceCollector(
            instance => ObjectFinder.Self.GetElementSave(instance));
        return collector.Collect(project, new[] { screen });
    }

    public void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
    }
}
