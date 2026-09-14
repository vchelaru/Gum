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
        categories.ShouldContain("Font Generation");
        InstanceMember Member(string name) => grid.GetInstanceMember(name).ShouldNotBeNull();
        Member(nameof(ProjectPropertiesViewModel.ShowOutlines)).Category.Name.ShouldBe("Guides");
        Member(nameof(ProjectPropertiesViewModel.FontGenerator)).CustomOptions.ShouldBe(new object[] { FontGeneratorType.BmFont, FontGeneratorType.KernSmith });
        Member(nameof(ProjectPropertiesViewModel.TextureFilter)).CustomOptions.ShouldBe(new object[] { TextureFilter.Point, TextureFilter.Linear });
        Member(nameof(ProjectPropertiesViewModel.UseFontCharacterFile)).DisplayName.ShouldBe("Use Font Character File (.gumfcs)");
        Member(nameof(ProjectPropertiesViewModel.AutoSizeFontOutputs)).DisplayName.ShouldBe("Auto-Size Font Outputs");
        Member(nameof(ProjectPropertiesViewModel.RestrictFileNamesForAndroid)).DisplayName.ShouldBe("Restrict File Names For Android");
        Member(nameof(ProjectPropertiesViewModel.LocalizationFiles)).PreferredDisplayer.ShouldBe(typeof(StandardDisplayers.MultiFile));
        Member(nameof(ProjectPropertiesViewModel.LocalizationFiles)).PropertiesToSetOnDisplayer["Filter"].ShouldBe("Localization Files|*.csv;*.resx|All Files|*.*");
        Member(nameof(ProjectPropertiesViewModel.SinglePixelTextureFile)).PreferredDisplayer.ShouldBe(typeof(StandardDisplayers.FileSelection));
        grid.Categories.SelectMany(category => category.Members).ShouldAllBe(member => !member.SupportsMakeDefault);
        foreach (string hidden in new[]
                 {
                     nameof(ProjectPropertiesViewModel.IsUpdatingFromModel),
                     nameof(ProjectPropertiesViewModel.LanguageIndex),
                     nameof(ProjectPropertiesViewModel.IsFontRangesReadOnly),
                     nameof(ProjectPropertiesViewModel.AvailableLanguages),
                     // No languages are loaded, so there is no language to pick.
                     nameof(ProjectPropertiesViewModel.LanguageName),
                 })
        {
            grid.GetInstanceMember(hidden).ShouldBeNull(hidden);
        }
    }

    [Fact]
    public void FontRanges_IsReadOnly_WhileAFontCharacterFileIsUsed()
    {
        ProjectPropertiesViewModel viewModel = new ProjectPropertiesViewModel();
        viewModel.SetFrom(autoSave: true, new GumProjectSave());
        ModelBackedGrid grid = new ModelBackedGrid();
        ProjectPropertiesGridPresenter presenter = new ProjectPropertiesGridPresenter(grid);
        presenter.Bind(viewModel);
        grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.FontRanges))!.IsReadOnly.ShouldBeFalse();

        viewModel.UseFontCharacterFile = true;

        grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.FontRanges))!.IsReadOnly.ShouldBeTrue();
        grid.RefreshCount.ShouldBeGreaterThan(0);
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
        after.Category.Name.ShouldBe(before.Category.Name);
    }

    /// <summary>An <see cref="IDataUiGrid"/> over a bare model, as either head's grid is.</summary>
    private sealed class ModelBackedGrid : IDataUiGrid
    {
        public DataUiGridModel Model { get; } = new DataUiGridModel();
        public int RefreshCount { get; private set; }
        public object? Instance { get => Model.Instance; set => Model.Instance = value; }
        public bool IsEnabled { get; set; } = true;
        public BulkObservableCollection<MemberCategory> Categories => Model.Categories;
        public event Action<string, PropertyChangedArgs>? PropertyChange
        {
            add => Model.PropertyChange += value;
            remove => Model.PropertyChange -= value;
        }
        public void SetCategories(IList<MemberCategory> newCategories) => Model.SetCategories(newCategories);
        public void SetMultipleCategoryLists(List<List<MemberCategory>> listOfCategoryLists) => Model.SetMultipleCategoryLists(listOfCategoryLists);
        public void ApplyMemberFilter(Func<InstanceMember, bool>? isMatch) => Model.ApplyMemberFilter(isMatch);
        public InstanceMember? GetInstanceMember(string memberName) => Model.GetInstanceMember(memberName);
        public void InsertSpacesInCamelCaseMemberNames() => Model.InsertSpacesInCamelCaseMemberNames();
        public void Refresh() => RefreshCount++;
    }
}
