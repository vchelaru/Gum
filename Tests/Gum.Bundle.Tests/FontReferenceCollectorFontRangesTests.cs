using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;

namespace Gum.Bundle.Tests;

/// <summary>
/// Coverage for how <see cref="FontReferenceCollector"/> picks <see cref="BmfcSave.Ranges"/>: through
/// <see cref="GumProjectSave.GetFontRanges"/>, so a family with its own entry bakes that range and every
/// other family gets the project-wide one (#4656).
/// </summary>
public class FontReferenceCollectorFontRangesTests : IDisposable
{
    public FontReferenceCollectorFontRangesTests()
    {
        StandardElementsManager.Self.Initialize();
    }

    [Fact]
    public void Collect_bakes_each_familys_own_range_and_the_global_range_for_the_rest()
    {
        const string globalRanges = "32-126";
        const string iconRanges = "57344-57400";

        StandardElementSave textStandard = TestProjectBuilder.BuildStandard("Text");
        textStandard.DefaultState.Variables.Add(new VariableSave { SetsValue = true, Name = "Font", Value = "Arial", IsFont = true });
        textStandard.DefaultState.Variables.Add(new VariableSave { SetsValue = true, Name = "FontSize", Value = 18 });

        StandardElementSave iconStandard = TestProjectBuilder.BuildStandard("Sprite");
        iconStandard.DefaultState.Variables.Add(new VariableSave { SetsValue = true, Name = "Font", Value = "IconFont", IsFont = true });
        iconStandard.DefaultState.Variables.Add(new VariableSave { SetsValue = true, Name = "FontSize", Value = 18 });

        GumProjectSave project = TestProjectBuilder.BuildProject(standards: new[] { textStandard, iconStandard });
        project.FontRanges = globalRanges;
        project.FontRangeEntries.Add(new FontRangeEntry { FontFamily = "IconFont", Ranges = iconRanges });
        ObjectFinder.Self.GumProjectSave = project;

        FontReferenceCollector collector = new FontReferenceCollector(
            instance => ObjectFinder.Self.GetElementSave(instance));
        Dictionary<string, BmfcSave> fonts = collector.Collect(project, new[] { textStandard, iconStandard });

        fonts.Values.Single(f => f.FontName == "Arial").Ranges.ShouldBe(globalRanges);
        fonts.Values.Single(f => f.FontName == "IconFont").Ranges.ShouldBe(iconRanges);
    }

    public void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
    }
}
