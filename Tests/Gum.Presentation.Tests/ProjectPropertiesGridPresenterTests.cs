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
        viewModel.SetFrom(autoSave: true, new GumProjectSave());
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

    [Fact]
    public void Reloaded_RebuildsTheRows_FromTheSameViewModel()
    {
        ProjectPropertiesViewModel viewModel = new ProjectPropertiesViewModel();
        viewModel.SetFrom(autoSave: true, new GumProjectSave());
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
