using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.Plugins.PropertiesWindowPlugin;
using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests;

/// <summary>
/// The Project > General page: the single pixel texture's four nullable bounds map to one "Custom bounds"
/// toggle without changing what the project saves.
/// </summary>
public class ProjectGeneralRowsTests
{
    private readonly GumProjectSave _project = new GumProjectSave();
    private readonly ProjectPropertiesViewModel _viewModel = new ProjectPropertiesViewModel();
    private readonly ProjectGeneralRows _sut;

    public ProjectGeneralRowsTests()
    {
        _viewModel.SetFrom(_project);
        _sut = new ProjectGeneralRows(_viewModel);
    }

    private (int?, int?, int?, int?) Saved()
    {
        _viewModel.ApplyToModelObjects();
        return (_project.SinglePixelTextureLeft, _project.SinglePixelTextureTop, _project.SinglePixelTextureRight, _project.SinglePixelTextureBottom);
    }

    private void Load(int? left, int? top, int? right, int? bottom)
    {
        _project.SinglePixelTextureLeft = left;
        _project.SinglePixelTextureTop = top;
        _project.SinglePixelTextureRight = right;
        _project.SinglePixelTextureBottom = bottom;
        _viewModel.SetFrom(_project);
    }

    private InstanceMember? Member(string name) =>
        _sut.BuildCategories().SelectMany(category => category.Members).FirstOrDefault(member => member.Name == name);

    [Fact]
    public void UseCustomBounds_WithAllFourNull_IsOff()
    {
        _sut.UseCustomBounds.ShouldBeFalse();
    }

    [Theory]
    [InlineData(1, null, null, null)]
    [InlineData(null, 2, null, null)]
    [InlineData(null, null, 3, null)]
    [InlineData(null, null, null, 4)]
    public void UseCustomBounds_WithAnyBoundSet_IsOn(int? left, int? top, int? right, int? bottom)
    {
        Load(left, top, right, bottom);

        _sut.UseCustomBounds.ShouldBeTrue();
    }

    [Fact]
    public void TurningOn_FromAllNull_SavesTheOnePixelWhiteTextureBounds()
    {
        _sut.UseCustomBounds = true;

        _sut.UseCustomBounds.ShouldBeTrue();
        Saved().ShouldBe((0, 0, 1, 1));
    }

    [Fact]
    public void TurningOn_WhenSomeBoundsAreAlreadySet_ChangesNothing()
    {
        Load(5, null, 9, null);

        _sut.UseCustomBounds = true;

        Saved().ShouldBe((5, null, 9, null));
    }

    [Fact]
    public void TurningOff_SetsAllFourBackToNull_SoTheFileSavesAsBefore()
    {
        Load(1, 2, 3, 4);

        _sut.UseCustomBounds = false;

        _sut.UseCustomBounds.ShouldBeFalse();
        Saved().ShouldBe((null, null, null, null));
    }

    [Fact]
    public void ProjectWithAllBoundsNull_RoundTripsUnchanged()
    {
        Saved().ShouldBe((null, null, null, null));
    }

    [Fact]
    public void ProjectWithBoundsSet_RoundTripsUnchanged()
    {
        Load(1, 2, 3, 4);

        Saved().ShouldBe((1, 2, 3, 4));
    }

    [Fact]
    public void BuildCategories_ShowsTheFourBoundsOnlyWhileTheToggleIsOn()
    {
        string[] bounds = { "SinglePixelTextureLeft", "SinglePixelTextureTop", "SinglePixelTextureRight", "SinglePixelTextureBottom" };
        bounds.ShouldAllBe(name => Member(name) == null);
        Member("UseCustomBounds").ShouldNotBeNull();

        _sut.UseCustomBounds = true;

        bounds.ShouldAllBe(name => Member(name) != null);
    }

    [Fact]
    public void ToggleRow_WritesThroughTheToggle_AndTheFieldsWriteTheirBounds()
    {
        Member("UseCustomBounds")!.Value = true;
        Member("SinglePixelTextureRight")!.Value = 40;

        Saved().ShouldBe((0, 0, 40, 1));
    }

    [Fact]
    public void TurningTheToggle_AsksTheViewToRebuild()
    {
        int rebuilds = 0;
        _sut.Reloaded += () => rebuilds++;

        _sut.UseCustomBounds = true;
        _sut.UseCustomBounds = false;

        rebuilds.ShouldBe(2);
    }

    [Fact]
    public void BuildCategories_GroupsRowsBySection_WithAHintUnderEveryRow()
    {
        _sut.UseCustomBounds = true;

        List<MemberCategory> sections = _sut.BuildCategories();

        sections.Select(section => section.Name).ShouldBe(new[] { "Rendering", "Values and names", "Single pixel texture" });
        sections.SelectMany(section => section.Members).ShouldAllBe(member => !string.IsNullOrEmpty(member.DetailText));
        sections.SelectMany(section => section.Members).ShouldAllBe(member => member.IsSpaciousRow);
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
