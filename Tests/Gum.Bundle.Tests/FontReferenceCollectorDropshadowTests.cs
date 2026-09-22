using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;

namespace Gum.Bundle.Tests;

/// <summary>
/// Coverage for <see cref="FontReferenceCollector"/>'s handling of dropshadow (#4929). The shadow is
/// baked into the atlas as a blurred silhouette variant, so a dropshadow Text needs a different file
/// than the same font without one - reflected by the <c>_ds{blur}</c> suffix in
/// <see cref="BmfcSave.FontCacheFileName"/>.
/// </summary>
public class FontReferenceCollectorDropshadowTests : IDisposable
{
    public FontReferenceCollectorDropshadowTests()
    {
        StandardElementsManager.Self.Initialize();
    }

    [Fact]
    public void Collect_generates_the_dropshadow_font_for_a_Text_with_a_dropshadow()
    {
        const string font = "Arial";
        const int fontSize = 18;
        const float blur = 4f;

        StandardElementSave textStandard = TestProjectBuilder.BuildStandard("Text");
        StateSave defaultState = textStandard.DefaultState;
        defaultState.Variables.Add(new VariableSave { SetsValue = true, Name = "Font", Value = font, IsFont = true });
        defaultState.Variables.Add(new VariableSave { SetsValue = true, Name = "FontSize", Value = fontSize });
        defaultState.Variables.Add(new VariableSave { SetsValue = true, Name = "HasDropshadow", Value = true });
        defaultState.Variables.Add(new VariableSave { SetsValue = true, Name = "DropshadowBlur", Value = blur });

        GumProjectSave project = TestProjectBuilder.BuildProject(standards: new[] { textStandard });
        ObjectFinder.Self.GumProjectSave = project;

        FontReferenceCollector collector = new FontReferenceCollector(
            instance => ObjectFinder.Self.GetElementSave(instance));
        Dictionary<string, BmfcSave> fonts = collector.Collect(project, new[] { textStandard });

        string expectedKey = BmfcSave.GetFontCacheFileNameFor(
            fontSize: fontSize, fontName: font, outline: 0, useFontSmoothing: true,
            isItalic: false, isBold: false, fontFilePath: null,
            hasDropshadow: true, dropshadowBlur: blur);

        fonts.Keys.ShouldBe(new[] { expectedKey });
        fonts[expectedKey].HasDropshadow.ShouldBeTrue();
        fonts[expectedKey].DropshadowBlur.ShouldBe(blur);
    }

    public void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
    }
}
