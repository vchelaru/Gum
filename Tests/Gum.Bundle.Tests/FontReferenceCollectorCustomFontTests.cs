using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;

namespace Gum.Bundle.Tests;

/// <summary>
/// Coverage for <see cref="FontReferenceCollector"/>'s handling of <c>UseCustomFont</c> (#4922).
/// A Text whose font comes from <c>CustomFontFile</c> keeps its <c>Font</c>/<c>FontSize</c>/style
/// variables populated - the tool's variable grid hides them rather than clearing them - so the
/// collector has to read the flag to know which of the two identities actually applies.
/// </summary>
public class FontReferenceCollectorCustomFontTests : IDisposable
{
    public FontReferenceCollectorCustomFontTests()
    {
        // RecursiveVariableFinder falls back to the standard elements' canonical defaults, the same
        // way GumProjectDependencyWalker primes them before collecting.
        StandardElementsManager.Self.Initialize();
    }

    [Fact]
    public void Collect_generates_nothing_for_a_Text_whose_custom_font_is_a_prebaked_fnt()
    {
        StandardElementSave textStandard = BuildTextStandard(
            useCustomFont: true,
            customFontFile: "Fonts/dogica.fnt",
            font: "Dogica Pixel",
            fontSize: 8);

        GumProjectSave project = TestProjectBuilder.BuildProject(standards: new[] { textStandard });
        ObjectFinder.Self.GumProjectSave = project;

        Dictionary<string, BmfcSave> fonts = Collect(project, textStandard);

        fonts.ShouldBeEmpty();
    }

    [Fact]
    public void Collect_generates_the_custom_ttf_rather_than_the_system_font_when_UseCustomFont_is_true()
    {
        const string customTtf = "Fonts/dogica.ttf";
        StandardElementSave textStandard = BuildTextStandard(
            useCustomFont: true,
            customFontFile: customTtf,
            font: "Dogica Pixel",
            fontSize: 8);

        GumProjectSave project = TestProjectBuilder.BuildProject(standards: new[] { textStandard });
        ObjectFinder.Self.GumProjectSave = project;

        Dictionary<string, BmfcSave> fonts = Collect(project, textStandard);

        BmfcSave only = fonts.Values.ShouldHaveSingleItem();
        only.FontFile.ShouldBe(customTtf);
        only.FontName.ShouldBe("dogica");
    }

    [Fact]
    public void Collect_generates_the_system_font_for_a_state_that_turns_UseCustomFont_back_off()
    {
        // Issue #4922's project shape, inverted so there is something to collect: UseCustomFont is
        // declared once on Default and the category states only override Font/FontSize, so reading
        // the flag off the state alone (instead of recursively) would miss it. "Condensed" opts back
        // out, and is the only state that should contribute a font.
        StandardElementSave textStandard = BuildTextStandard(
            useCustomFont: true,
            customFontFile: "Fonts/dogica.fnt",
            font: "Dogica Pixel",
            fontSize: 8);

        StateSave title = AddState(textStandard, "Title");
        title.Variables.Add(new VariableSave { SetsValue = true, Name = "Font", Value = "Goodbye Despair" });

        StateSave condensed = AddState(textStandard, "Condensed");
        condensed.Variables.Add(new VariableSave { SetsValue = true, Name = "UseCustomFont", Value = false });
        condensed.Variables.Add(new VariableSave { SetsValue = true, Name = "Font", Value = "Awesome 9" });
        condensed.Variables.Add(new VariableSave { SetsValue = true, Name = "FontSize", Value = 32 });

        GumProjectSave project = TestProjectBuilder.BuildProject(standards: new[] { textStandard });
        ObjectFinder.Self.GumProjectSave = project;

        Dictionary<string, BmfcSave> fonts = Collect(project, textStandard);

        string expectedKey = BmfcSave.GetFontCacheFileNameFor(
            fontSize: 32, fontName: "Awesome 9", outline: 0, useFontSmoothing: true,
            isItalic: false, isBold: false);

        fonts.Keys.ShouldBe(new[] { expectedKey });
    }

    [Fact]
    public void Collect_skips_the_system_font_of_a_custom_font_Text_nested_in_a_component()
    {
        StandardElementSave textStandard = BuildTextStandard(
            useCustomFont: true,
            customFontFile: "Fonts/dogica.fnt",
            font: "Dogica Pixel",
            fontSize: 8);

        ComponentSave label = TestProjectBuilder.BuildComponent("Label");
        InstanceSave innerText = new InstanceSave { Name = "TextInstance", BaseType = "Text", ParentContainer = label };
        label.Instances.Add(innerText);

        ComponentSave button = TestProjectBuilder.BuildComponent("Button");
        InstanceSave labelInstance = new InstanceSave { Name = "LabelInstance", BaseType = "Label", ParentContainer = button };
        button.Instances.Add(labelInstance);

        GumProjectSave project = TestProjectBuilder.BuildProject(
            components: new[] { label, button },
            standards: new[] { textStandard });
        ObjectFinder.Self.GumProjectSave = project;

        Dictionary<string, BmfcSave> fonts = Collect(project, label, button, textStandard);

        fonts.ShouldBeEmpty();
    }

    private static StateSave AddState(ElementSave element, string name)
    {
        StateSave state = new StateSave { Name = name, ParentContainer = element };
        element.States.Add(state);
        return state;
    }

    private static StandardElementSave BuildTextStandard(bool useCustomFont, string customFontFile,
        string font, int fontSize)
    {
        StandardElementSave textStandard = TestProjectBuilder.BuildStandard("Text");
        StateSave defaultState = textStandard.DefaultState;
        defaultState.Variables.Add(new VariableSave { SetsValue = true, Name = "UseCustomFont", Value = useCustomFont });
        defaultState.Variables.Add(new VariableSave { SetsValue = true, Name = "CustomFontFile", Value = customFontFile, IsFile = true });
        defaultState.Variables.Add(new VariableSave { SetsValue = true, Name = "Font", Value = font, IsFont = true });
        defaultState.Variables.Add(new VariableSave { SetsValue = true, Name = "FontSize", Value = fontSize });
        return textStandard;
    }

    private static Dictionary<string, BmfcSave> Collect(GumProjectSave project, params ElementSave[] elements)
    {
        FontReferenceCollector collector = new FontReferenceCollector(
            instance => ObjectFinder.Self.GetElementSave(instance));
        return collector.Collect(project, elements);
    }

    public void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
    }
}
