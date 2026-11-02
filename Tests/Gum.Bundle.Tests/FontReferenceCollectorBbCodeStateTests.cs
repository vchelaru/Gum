using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;

namespace Gum.Bundle.Tests;

/// <summary>
/// Coverage for <see cref="FontReferenceCollector"/> baking the fonts a <c>[state=Name]</c> BBCode tag
/// produces (#7204). The renderer expands the tag into the state's font variables while it draws, so a
/// font set only by that state is invisible to a scan that reads the Text value alone.
/// </summary>
/// <remarks>
/// The expected key sets are the ones the renderer asks for when it draws the same text
/// (<c>TextRuntimeBbCodeStateFontTests</c> in MonoGameGum.Tests pins the identical list), including the
/// in-between fonts the stack passes through while a state's variables open and close one at a time.
/// </remarks>
public class FontReferenceCollectorBbCodeStateTests : IDisposable
{
    private const string Font = "Arial";
    private const int BaseSize = 18;

    public FontReferenceCollectorBbCodeStateTests()
    {
        StandardElementsManager.Self.Initialize();
    }

    [Fact]
    public void Collect_bakes_the_fonts_a_state_tag_changes()
    {
        StandardElementSave text = BuildTextStandard();
        AddBigState(text.States, "Big");
        ScreenSave screen = ScreenWithText("[state=Big]Hello[/state]");

        Dictionary<string, BmfcSave> fonts = Collect(screen, text);

        fonts.Keys.ShouldBe(BigStateKeys(), ignoreOrder: true);
    }

    [Fact]
    public void Collect_matches_the_state_tag_name_ignoring_case()
    {
        StandardElementSave text = BuildTextStandard();
        AddBigState(text.States, "Big");
        ScreenSave screen = ScreenWithText("[STATE=Big]Hello[/STATE]");

        Collect(screen, text).Keys.ShouldBe(BigStateKeys(), ignoreOrder: true);
    }

    [Fact]
    public void Collect_bakes_a_state_that_is_in_a_category()
    {
        StandardElementSave text = BuildTextStandard();
        StateSaveCategory category = new StateSaveCategory { Name = "Style" };
        AddBigState(category.States, "Big");
        text.Categories.Add(category);
        ScreenSave screen = ScreenWithText("[state=Big]Hello[/state]");

        Collect(screen, text).Keys.ShouldBe(BigStateKeys(), ignoreOrder: true);
    }

    [Fact]
    public void Collect_bakes_a_state_defined_on_a_component_whose_base_is_Text()
    {
        StandardElementSave text = BuildTextStandard();
        ComponentSave fancy = TestProjectBuilder.BuildComponent("FancyText");
        fancy.BaseType = "Text";
        AddBigState(fancy.States, "Big");

        ScreenSave screen = TestProjectBuilder.BuildScreen("Menu");
        screen.Instances.Add(new InstanceSave { Name = "Label", BaseType = "FancyText", ParentContainer = screen });
        SetVariable(screen.DefaultState, "Label.Text", "[state=Big]Hello[/state]", "string");

        Collect(screen, text, fancy).Keys.ShouldBe(BigStateKeys(), ignoreOrder: true);
    }

    [Fact]
    public void Collect_bakes_a_state_inherited_from_the_base_of_a_component_whose_base_is_Text()
    {
        StandardElementSave text = BuildTextStandard();
        AddBigState(text.States, "Big");
        ComponentSave fancy = TestProjectBuilder.BuildComponent("FancyText");
        fancy.BaseType = "Text";

        ScreenSave screen = TestProjectBuilder.BuildScreen("Menu");
        screen.Instances.Add(new InstanceSave { Name = "Label", BaseType = "FancyText", ParentContainer = screen });
        SetVariable(screen.DefaultState, "Label.Text", "[state=Big]Hello[/state]", "string");

        Collect(screen, text, fancy).Keys.ShouldBe(BigStateKeys(), ignoreOrder: true);
    }

    [Fact]
    public void Collect_bakes_a_state_tag_on_a_Text_inside_a_component_instance()
    {
        StandardElementSave text = BuildTextStandard();
        AddBigState(text.States, "Big");
        ComponentSave button = TestProjectBuilder.BuildComponent("Button");
        button.Instances.Add(new InstanceSave { Name = "Label", BaseType = "Text", ParentContainer = button });
        SetVariable(button.DefaultState, "Label.Text", "[state=Big]go[/state]", "string");

        ScreenSave screen = TestProjectBuilder.BuildScreen("Menu");
        screen.Instances.Add(new InstanceSave { Name = "Start", BaseType = "Button", ParentContainer = screen });

        // Only the screen is collected, so the Button's own states don't mask the nested path.
        Collect(screen, text, button).Keys.ShouldBe(BigStateKeys(), ignoreOrder: true);
    }

    [Fact]
    public void Collect_bakes_a_state_tag_nested_inside_a_font_tag()
    {
        StandardElementSave text = BuildTextStandard();
        StateSave small = AddState(text.States, "Small");
        SetVariable(small, "FontSize", 10, "int");
        ScreenSave screen = ScreenWithText("[IsBold=true]a[state=Small]b[/state][/IsBold]");

        Collect(screen, text).Keys.ShouldBe(
            new[] { Key(BaseSize), Key(BaseSize, bold: true), Key(10, bold: true) }, ignoreOrder: true);
    }

    [Fact]
    public void Collect_ignores_a_state_tag_naming_a_state_that_does_not_exist()
    {
        StandardElementSave text = BuildTextStandard();
        ScreenSave screen = ScreenWithText("[state=Missing]Hello[/state]");

        Collect(screen, text).Keys.ShouldBe(new[] { Key(BaseSize) });
    }

    [Fact]
    public void Collect_ignores_state_variables_that_have_no_per_run_meaning_or_are_not_set()
    {
        StandardElementSave text = BuildTextStandard();
        StateSave state = AddState(text.States, "Odd");
        SetVariable(state, "Width", 500f, "float");
        SetVariable(state, "Text", "[state=Odd]x[/state]", "string");
        state.Variables.Add(new VariableSave { Name = "FontSize", Type = "int", Value = 99, SetsValue = false });
        ScreenSave screen = ScreenWithText("[state=Odd]Hello[/state]");

        Collect(screen, text).Keys.ShouldBe(new[] { Key(BaseSize) });
    }

    [Fact]
    public void Collect_does_not_loop_when_a_state_refers_to_itself_through_its_Text_value()
    {
        StandardElementSave text = BuildTextStandard();
        StateSave state = AddState(text.States, "Self");
        SetVariable(state, "FontSize", 30, "int");
        SetVariable(state, "Text", "[state=Self]again[/state]", "string");
        ScreenSave screen = ScreenWithText("[state=Self]Hello[/state]");

        Collect(screen, text).Keys.ShouldBe(new[] { Key(BaseSize), Key(30) }, ignoreOrder: true);
    }

    [Fact]
    public void Collect_leaves_a_text_value_without_a_state_tag_unchanged()
    {
        StandardElementSave text = BuildTextStandard();
        AddBigState(text.States, "Big");
        ScreenSave screen = ScreenWithText("Hello [FontSize=30]world[/FontSize]");

        Collect(screen, text).Keys.ShouldBe(new[] { Key(BaseSize), Key(30) }, ignoreOrder: true);
    }

    [Fact]
    public void Collect_does_not_bake_run_fonts_for_a_Text_with_a_prebaked_fnt_base()
    {
        // At draw time a styled run on a custom-.fnt base resolves to the same pre-baked file
        // (UseCustomFont stays on, so the run's file is "<name>.fnt" whatever the size or style tag says),
        // so there is nothing to generate. Pins that the missing run fonts (#7204) are not a gap.
        StandardElementSave text = BuildTextStandard();
        SetVariable(text.DefaultState, "UseCustomFont", true, "bool");
        SetVariable(text.DefaultState, "CustomFontFile", "Fonts/dogica.fnt", "string");
        ScreenSave screen = ScreenWithText("[FontSize=30]big[/FontSize] [IsBold=true]bold[/IsBold]");

        Collect(screen, text).ShouldBeEmpty();
    }

    [Fact]
    public void Collect_skips_the_runs_where_a_state_has_UseCustomFont_on_but_not_the_one_after_it_closes()
    {
        // While UseCustomFont is on over a run the renderer loads a pre-baked "<Font>.fnt", so nothing is
        // generated for those runs. The flag's close tag comes first, and the run it leaves behind
        // (Pixelated, flag off again) is a real font the renderer asks for.
        StandardElementSave text = BuildTextStandard();
        StateSave state = AddState(text.States, "Pixel");
        SetVariable(state, "UseCustomFont", true, "bool");
        SetVariable(state, "Font", "Pixelated", "string");
        ScreenSave screen = ScreenWithText("[state=Pixel]Hello[/state]");

        Collect(screen, text).Keys.ShouldBe(
            new[] { Key(BaseSize), Key(BaseSize, name: "Pixelated") }, ignoreOrder: true);
    }

    private static StateSave AddBigState(List<StateSave> states, string name)
    {
        StateSave state = AddState(states, name);
        SetVariable(state, "Font", "Verdana", "string");
        SetVariable(state, "FontSize", 40, "int");
        return state;
    }

    private static StateSave AddState(List<StateSave> states, string name)
    {
        StateSave state = new StateSave { Name = name };
        states.Add(state);
        return state;
    }

    /// <summary>
    /// What the renderer asks for while drawing <c>[state=Big]Hello[/state]</c>: the base font, then each
    /// font the stack passes through as Big's Font and FontSize open (Verdana 18, Verdana 40) and close
    /// in turn (Arial 40, back to Arial 18).
    /// </summary>
    private static string[] BigStateKeys() => new[]
    {
        Key(BaseSize),
        Key(BaseSize, name: "Verdana"),
        Key(40, name: "Verdana"),
        Key(40),
    };

    private static StandardElementSave BuildTextStandard()
    {
        StandardElementSave text = TestProjectBuilder.BuildStandard("Text");
        SetVariable(text.DefaultState, "Font", Font, "string");
        SetVariable(text.DefaultState, "FontSize", BaseSize, "int");
        return text;
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

    private static string Key(int size, string name = Font, bool bold = false) =>
        BmfcSave.GetFontCacheFileNameFor(
            fontSize: size, fontName: name, outline: 0, useFontSmoothing: true,
            isItalic: false, isBold: bold, fontFilePath: null);

    private static Dictionary<string, BmfcSave> Collect(ScreenSave screen, StandardElementSave text,
        params ComponentSave[] components)
    {
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
