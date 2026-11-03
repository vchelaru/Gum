using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.Plugins.PropertiesWindowPlugin;
using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests;

/// <summary>The Project > Canvas page's layout: sections, one row per setting, a hint under every row.</summary>
public class CanvasRowsTests
{
    private readonly ProjectPropertiesViewModel _viewModel = new ProjectPropertiesViewModel();
    private readonly CanvasRows _sut;

    public CanvasRowsTests()
    {
        _viewModel.SetFrom(new GumProjectSave());
        _sut = new CanvasRows(_viewModel);
    }

    [Fact]
    public void BuildCategories_GroupsTheFiveSettingsIntoCanvasSizeAndGuides()
    {
        List<MemberCategory> sections = _sut.BuildCategories();

        sections.Select(section => section.Name).ShouldBe(new[] { "Canvas size", "Guides" });
        sections[0].Members.Select(member => member.Name).ShouldBe(new[] { "CanvasWidth", "CanvasHeight" });
        sections[1].Members.Select(member => member.Name).ShouldBe(new[] { "ShowOutlines", "ShowCanvasOutline", "ShowCheckerBackground" });
    }

    [Fact]
    public void BuildCategories_GivesEveryRowAHintAndTheSharedRowShape()
    {
        IEnumerable<InstanceMember> rows = _sut.BuildCategories().SelectMany(section => section.Members);

        rows.ShouldAllBe(member => !string.IsNullOrEmpty(member.DetailText));
        rows.ShouldAllBe(member => member.IsSpaciousRow);
        rows.ShouldAllBe(member => member.FirstGridLength == Gum.SettingsWindow.SettingsRows.LabelColumnWidth);
    }

    [Fact]
    public void SearchTerms_AreTheLabelAndHintOfEveryRow()
    {
        string[] terms = _sut.SearchTerms().ToArray();

        terms.ShouldContain("Canvas Width");
        terms.ShouldContain("Show Checker Background");
        terms.Length.ShouldBe(10);
    }

    [Fact]
    public void Reloaded_IsRaisedWhenTheViewModelIsRefilledFromTheProject()
    {
        int rebuilds = 0;
        _sut.Reloaded += () => rebuilds++;

        _viewModel.NotifyReloaded();

        rebuilds.ShouldBe(1);
    }
}
