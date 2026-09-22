using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;

namespace Gum.Bundle.Tests;

/// <summary>
/// Coverage for <see cref="FontReferenceCollector"/>'s branch enumeration: a font property driven
/// by a conditional (ternary) <c>VariableReferences</c> row has every branch pregenerated, not just
/// the one the condition currently selects (#4042). Each property that feeds
/// <see cref="BmfcSave.GetFontCacheFileNameFor"/> bakes its own atlas, so each needs enumerating -
/// the dropshadow pair and <c>UseCustomFont</c> were the ones missing (#4936).
/// </summary>
public class FontReferenceCollectorConditionalReferenceTests : IDisposable
{
    private const string Font = "Arial";
    private const int FontSize = 18;

    public FontReferenceCollectorConditionalReferenceTests()
    {
        StandardElementsManager.Self.Initialize();
    }

    [Fact]
    public void Collect_generates_both_branches_when_HasDropshadow_comes_from_a_conditional_reference()
    {
        const float blur = 4f;

        ComponentSave panel = BuildPanelWithText(out StateSave state);
        SetVariable(state, "IsHighContrast", false, "bool");
        SetVariable(state, "Label.HasDropshadow", false, "bool");
        SetVariable(state, "Label.DropshadowBlur", blur, "float");
        AddVariableReference(state, "Label", "HasDropshadow = IsHighContrast ? true : false");

        Dictionary<string, BmfcSave> fonts = Collect(panel);

        fonts.Keys.ShouldBe(new[]
        {
            CacheKey(hasDropshadow: false, blur: 0f),
            CacheKey(hasDropshadow: true, blur: blur),
        }, ignoreOrder: true);
    }

    [Fact]
    public void Collect_generates_both_branches_when_DropshadowBlur_comes_from_a_conditional_reference()
    {
        const float thinBlur = 2f;
        const float thickBlur = 8f;

        ComponentSave panel = BuildPanelWithText(out StateSave state);
        SetVariable(state, "IsHighContrast", false, "bool");
        SetVariable(state, "Label.HasDropshadow", true, "bool");
        SetVariable(state, "Label.DropshadowBlur", thinBlur, "float");
        AddVariableReference(state, "Label", "DropshadowBlur = IsHighContrast ? 8 : 2");

        Dictionary<string, BmfcSave> fonts = Collect(panel);

        fonts.Keys.ShouldBe(new[]
        {
            CacheKey(hasDropshadow: true, blur: thinBlur),
            CacheKey(hasDropshadow: true, blur: thickBlur),
        }, ignoreOrder: true);
    }

    [Fact]
    public void Collect_generates_both_identities_when_UseCustomFont_comes_from_a_conditional_reference()
    {
        // UseCustomFont swaps which of Font / CustomFontFile is the font's identity, so its
        // branches produce two entirely different atlases rather than two variants of one.
        const string customTtf = "Fonts/dogica.ttf";

        ComponentSave panel = BuildPanelWithText(out StateSave state);
        SetVariable(state, "IsPixelUi", false, "bool");
        SetVariable(state, "Label.UseCustomFont", false, "bool");
        SetVariable(state, "Label.CustomFontFile", customTtf, "string");
        AddVariableReference(state, "Label", "UseCustomFont = IsPixelUi ? true : false");

        Dictionary<string, BmfcSave> fonts = Collect(panel);

        fonts.Keys.ShouldBe(new[]
        {
            CacheKey(hasDropshadow: false, blur: 0f),
            BmfcSave.GetFontCacheFileNameFor(
                fontSize: FontSize, fontName: Font, outline: 0, useFontSmoothing: true,
                isItalic: false, isBold: false, fontFilePath: customTtf),
        }, ignoreOrder: true);
    }

    private static ComponentSave BuildPanelWithText(out StateSave state)
    {
        ComponentSave panel = TestProjectBuilder.BuildComponent("Panel");
        state = panel.DefaultState;

        panel.Instances.Add(new InstanceSave { Name = "Label", BaseType = "Text", ParentContainer = panel });

        SetVariable(state, "Label.Font", Font, "string");
        SetVariable(state, "Label.FontSize", FontSize, "int");
        return panel;
    }

    private static void SetVariable(StateSave state, string name, object value, string type)
    {
        state.Variables.Add(new VariableSave { SetsValue = true, Name = name, Value = value, Type = type });
    }

    private static void AddVariableReference(StateSave state, string instanceName, string reference)
    {
        VariableListSave<string> variableReferences = new VariableListSave<string>
        {
            Name = instanceName + ".VariableReferences",
            Type = "string"
        };
        variableReferences.ValueAsIList.Add(reference);
        state.VariableLists.Add(variableReferences);
    }

    private static string CacheKey(bool hasDropshadow, float blur) =>
        BmfcSave.GetFontCacheFileNameFor(
            fontSize: FontSize, fontName: Font, outline: 0, useFontSmoothing: true,
            isItalic: false, isBold: false, fontFilePath: null,
            hasDropshadow: hasDropshadow, dropshadowBlur: blur);

    /// <summary>
    /// Runs the collector with Roslyn expression evaluation wired up, then unwires it - the
    /// registration is process-wide static state that would otherwise leak into other tests.
    /// </summary>
    private static Dictionary<string, BmfcSave> Collect(ComponentSave component)
    {
        // ApplyVariableReferences resolves each assigned variable's root through the project's
        // standard elements, so Text has to be present even though only the component is collected.
        GumProjectSave project = TestProjectBuilder.BuildProject(
            components: new[] { component },
            standards: new[] { TestProjectBuilder.BuildStandard("Text") });
        ObjectFinder.Self.GumProjectSave = project;

        Gum.Expressions.GumExpressionService.Initialize();
        try
        {
            FontReferenceCollector collector = new FontReferenceCollector(
                instance => ObjectFinder.Self.GetElementSave(instance));
            return collector.Collect(project, new[] { (ElementSave)component });
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
