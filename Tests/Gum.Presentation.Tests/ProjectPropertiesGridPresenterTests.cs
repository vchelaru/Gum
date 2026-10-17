using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.Plugins.PropertiesWindowPlugin;
using Shouldly;
using WpfDataUi;
using WpfDataUi.DataTypes;
using WpfDataUi.EventArguments;

namespace Gum.Presentation.Tests;

/// <summary>
/// The Project Properties grid layout both heads share: the WPF tab's grouping, naming, editors and
/// hidden members, and the rebuild and read-only rules the presenter applies.
/// </summary>
public class ProjectPropertiesGridPresenterTests
{
    [Fact]
    public void Bind_GroupsNamesAndTrimsTheRows_AsTheWpfGrid()
    {
        ProjectPropertiesViewModel viewModel = new ProjectPropertiesViewModel();
        viewModel.SetFrom(new GumProjectSave());
        ModelBackedGrid grid = new ModelBackedGrid();
        ProjectPropertiesGridPresenter presenter = new ProjectPropertiesGridPresenter(grid);

        presenter.Bind(viewModel);

        string[] categories = grid.Categories.Select(category => category.Name).ToArray();
        categories.ShouldContain("Guides");
        categories.ShouldContain("Single Pixel Texture");
        categories.ShouldNotContain("Font Generation");
        InstanceMember Member(string name) => grid.GetInstanceMember(name).ShouldNotBeNull();
        Member(nameof(ProjectPropertiesViewModel.ShowOutlines)).Category.ShouldNotBeNull().Name.ShouldBe("Guides");
        Member(nameof(ProjectPropertiesViewModel.TextureFilter)).CustomOptions.ShouldBe(new object[] { TextureFilter.Point, TextureFilter.Linear });
        Member(nameof(ProjectPropertiesViewModel.RestrictFileNamesForAndroid)).DisplayName.ShouldBe("Restrict File Names For Android");
        Member(nameof(ProjectPropertiesViewModel.LocalizationFiles)).PreferredDisplayer.ShouldBe(typeof(StandardDisplayers.MultiFile));
        Member(nameof(ProjectPropertiesViewModel.LocalizationFiles)).PropertiesToSetOnDisplayer["Filter"].ShouldBe("Localization Files|*.csv;*.resx|All Files|*.*");
        Member(nameof(ProjectPropertiesViewModel.SinglePixelTextureFile)).PreferredDisplayer.ShouldBe(typeof(StandardDisplayers.FileSelection));
        grid.Categories.SelectMany(category => category.Members).ShouldAllBe(member => !member.SupportsMakeDefault);
        foreach (string hidden in new[]
                 {
                     nameof(ProjectPropertiesViewModel.IsUpdatingFromModel),
                     nameof(ProjectPropertiesViewModel.LanguageIndex),
                     nameof(ProjectPropertiesViewModel.AvailableLanguages),
                     // No languages are loaded, so there is no language to pick.
                     nameof(ProjectPropertiesViewModel.LanguageName),
                     // Removed setting that was never saved or applied (#5383).
                     "RenderTextCharacterByCharacter",
                 })
        {
            grid.GetInstanceMember(hidden).ShouldBeNull(hidden);
        }
    }

    [Theory]
    [InlineData(ProjectPropertiesSection.General, new[]
    {
        nameof(ProjectPropertiesViewModel.TextureFilter),
        nameof(ProjectPropertiesViewModel.RestrictToUnitValues),
        nameof(ProjectPropertiesViewModel.RestrictFileNamesForAndroid),
        nameof(ProjectPropertiesViewModel.SinglePixelTextureFile),
        nameof(ProjectPropertiesViewModel.SinglePixelTextureLeft),
        nameof(ProjectPropertiesViewModel.SinglePixelTextureTop),
        nameof(ProjectPropertiesViewModel.SinglePixelTextureRight),
        nameof(ProjectPropertiesViewModel.SinglePixelTextureBottom),
    })]
    [InlineData(ProjectPropertiesSection.Canvas, new[]
    {
        nameof(ProjectPropertiesViewModel.CanvasWidth),
        nameof(ProjectPropertiesViewModel.CanvasHeight),
        nameof(ProjectPropertiesViewModel.ShowOutlines),
        nameof(ProjectPropertiesViewModel.ShowCanvasOutline),
        nameof(ProjectPropertiesViewModel.ShowCheckerBackground),
    })]
    [InlineData(ProjectPropertiesSection.Localization, new[]
    {
        nameof(ProjectPropertiesViewModel.ShowLocalization),
        nameof(ProjectPropertiesViewModel.LocalizationFiles),
    })]
    public void Bind_WithASection_ShowsOnlyThatSectionsRows(ProjectPropertiesSection section, string[] expectedRows)
    {
        ProjectPropertiesViewModel viewModel = new ProjectPropertiesViewModel();
        viewModel.SetFrom(new GumProjectSave());
        ModelBackedGrid grid = new ModelBackedGrid();
        ProjectPropertiesGridPresenter presenter = new ProjectPropertiesGridPresenter(grid, section);

        presenter.Bind(viewModel);

        string[] shown = grid.Categories.SelectMany(category => category.Members).Select(member => member.Name).ToArray();
        shown.OrderBy(name => name).ToArray().ShouldBe(expectedRows.OrderBy(name => name).ToArray());
    }

    [Fact]
    public void Bind_WithASection_DropsCategoriesThatEndUpEmpty()
    {
        ProjectPropertiesViewModel viewModel = new ProjectPropertiesViewModel();
        viewModel.SetFrom(new GumProjectSave());
        ModelBackedGrid grid = new ModelBackedGrid();
        ProjectPropertiesGridPresenter presenter = new ProjectPropertiesGridPresenter(grid, ProjectPropertiesSection.Localization);

        presenter.Bind(viewModel);

        grid.Categories.ShouldAllBe(category => category.Members.Count > 0);
        grid.Categories.Select(category => category.Name).ShouldNotContain("Guides");
        grid.Categories.Select(category => category.Name).ShouldNotContain("Single Pixel Texture");
    }

    [Theory]
    [InlineData(ProjectPropertiesSection.General, "General")]
    [InlineData(ProjectPropertiesSection.Canvas, "Canvas Size")]
    [InlineData(ProjectPropertiesSection.Localization, "Localization")]
    public void Bind_WithASection_NamesTheUncategorizedRowsAfterThePage(ProjectPropertiesSection section, string expectedName)
    {
        ProjectPropertiesViewModel viewModel = new ProjectPropertiesViewModel();
        viewModel.SetFrom(new GumProjectSave());
        ModelBackedGrid grid = new ModelBackedGrid();
        ProjectPropertiesGridPresenter presenter = new ProjectPropertiesGridPresenter(grid, section);

        presenter.Bind(viewModel);

        string[] names = grid.Categories.Select(category => category.Name).ToArray();
        names.ShouldContain(expectedName);
        names.ShouldNotContain("Uncategorized");
    }

    [Fact]
    public void Bind_WithoutASection_NeverShowsAutoSave()
    {
        // Auto save is an application setting, on the Application > General page.
        ProjectPropertiesViewModel viewModel = new ProjectPropertiesViewModel();
        viewModel.SetFrom(new GumProjectSave());
        ModelBackedGrid grid = new ModelBackedGrid();
        ProjectPropertiesGridPresenter presenter = new ProjectPropertiesGridPresenter(grid);

        presenter.Bind(viewModel);

        grid.GetInstanceMember("AutoSave").ShouldBeNull();
    }

    [Fact]
    public void Reloaded_RebuildsTheRows_FromTheSameViewModel()
    {
        ProjectPropertiesViewModel viewModel = new ProjectPropertiesViewModel();
        viewModel.SetFrom(new GumProjectSave());
        ModelBackedGrid grid = new ModelBackedGrid();
        ProjectPropertiesGridPresenter presenter = new ProjectPropertiesGridPresenter(grid);
        presenter.Bind(viewModel);
        InstanceMember before = grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.CanvasWidth))!;

        viewModel.NotifyReloaded();

        InstanceMember after = grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.CanvasWidth))!;
        after.ShouldNotBeSameAs(before);
        after.Category.ShouldNotBeNull().Name.ShouldBe(before.Category.ShouldNotBeNull().Name);
    }
}
