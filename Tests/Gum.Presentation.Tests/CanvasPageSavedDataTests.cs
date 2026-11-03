using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.Plugins.PropertiesWindowPlugin;
using Shouldly;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests;

/// <summary>
/// What the Project > Canvas page saves: setting each of its rows writes the same project value as it
/// always has. Pins the data so the page's layout can change without changing the file.
/// </summary>
public class CanvasPageSavedDataTests
{
    private readonly GumProjectSave _project = new GumProjectSave();
    private readonly ProjectPropertiesViewModel _viewModel = new ProjectPropertiesViewModel();

    public CanvasPageSavedDataTests()
    {
        _viewModel.SetFrom(_project);
    }

    private InstanceMember Row(string name)
    {
        List<MemberCategory> sections = new CanvasRows(_viewModel).BuildCategories();
        return sections.SelectMany(section => section.Members).First(member => member.Name == name);
    }

    private GumProjectSave Saved()
    {
        _viewModel.ApplyToModelObjects();
        return _project;
    }

    [Fact]
    public void CanvasWidth_WhenSet_IsSavedAsTheDefaultCanvasWidth()
    {
        Row("CanvasWidth").Value = 1024;

        Saved().DefaultCanvasWidth.ShouldBe(1024);
    }

    [Fact]
    public void CanvasHeight_WhenSet_IsSavedAsTheDefaultCanvasHeight()
    {
        Row("CanvasHeight").Value = 768;

        Saved().DefaultCanvasHeight.ShouldBe(768);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShowOutlines_WhenSet_IsSaved(bool value)
    {
        _project.ShowOutlines = !value;
        _viewModel.SetFrom(_project);

        Row("ShowOutlines").Value = value;

        Saved().ShowOutlines.ShouldBe(value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShowCanvasOutline_WhenSet_IsSaved(bool value)
    {
        _project.ShowCanvasOutline = !value;
        _viewModel.SetFrom(_project);

        Row("ShowCanvasOutline").Value = value;

        Saved().ShowCanvasOutline.ShouldBe(value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShowCheckerBackground_WhenSet_IsSaved(bool value)
    {
        _project.ShowCheckerBackground = !value;
        _viewModel.SetFrom(_project);

        Row("ShowCheckerBackground").Value = value;

        Saved().ShowCheckerBackground.ShouldBe(value);
    }

    [Fact]
    public void RowsShowTheSavedValues()
    {
        _project.DefaultCanvasWidth = 320;
        _project.DefaultCanvasHeight = 240;
        _project.ShowOutlines = false;
        _project.ShowCanvasOutline = false;
        _project.ShowCheckerBackground = false;
        _viewModel.SetFrom(_project);

        Row("CanvasWidth").Value.ShouldBe(320);
        Row("CanvasHeight").Value.ShouldBe(240);
        Row("ShowOutlines").Value.ShouldBe(false);
        Row("ShowCanvasOutline").Value.ShouldBe(false);
        Row("ShowCheckerBackground").Value.ShouldBe(false);
    }

    [Fact]
    public void ChangingARow_RaisesThePropertyChangeTheProjectIsSavedFrom()
    {
        List<string?> changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        Row("CanvasWidth").Value = 500;

        changed.ShouldContain("CanvasWidth");
    }
}
