using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.Plugins.PropertiesWindowPlugin;
using Gum.SettingsWindow;
using Shouldly;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests;

/// <summary>The Project > Localization page's layout and that every row reads and writes the setting it always did.</summary>
public class LocalizationRowsTests
{
    private readonly ProjectPropertiesViewModel _viewModel = new ProjectPropertiesViewModel();
    private readonly LocalizationRows _sut;

    public LocalizationRowsTests()
    {
        _viewModel.SetFrom(new GumProjectSave());
        _sut = new LocalizationRows(_viewModel);
    }

    private InstanceMember Row(string name) =>
        _sut.BuildCategories().SelectMany(section => section.Members).Single(member => member.Name == name);

    [Fact]
    public void BuildCategories_WithoutLanguages_ShowsTheFilesAndTheShowToggle_AndNoLanguageChoice()
    {
        List<MemberCategory> sections = _sut.BuildCategories();

        sections.Select(section => section.Name).ShouldBe(new[] { "Files", "Language" });
        sections[0].Members.Select(member => member.Name).ShouldBe(new[] { "LocalizationFiles" });
        sections[1].Members.Select(member => member.Name).ShouldBe(new[] { "ShowLocalization" });
    }

    [Fact]
    public void BuildCategories_WithLanguages_OffersThemInTheLanguageChoice()
    {
        _viewModel.AvailableLanguages = new[] { "English", "French" };

        List<MemberCategory> sections = _sut.BuildCategories();

        sections[1].Members.Select(member => member.Name).ShouldBe(new[] { "LanguageName", "ShowLocalization" });
        InstanceMember language = sections[1].Members[0];
        language.DisplayName.ShouldBe("Language");
        language.CustomOptions!.Cast<string>().ShouldBe(new[] { "English", "French" });
    }

    [Fact]
    public void BuildCategories_GivesEveryRowAHintAndTheSharedRowShape()
    {
        _viewModel.AvailableLanguages = new[] { "English" };
        IEnumerable<InstanceMember> rows = _sut.BuildCategories().SelectMany(section => section.Members);

        rows.ShouldAllBe(member => !string.IsNullOrEmpty(member.DetailText));
        rows.ShouldAllBe(member => member.IsSpaciousRow);
        rows.ShouldAllBe(member => member.FirstGridLength == SettingsRows.LabelColumnWidth);
    }

    [Fact]
    public void FilesRow_IsTheFileListEditor_WithTheLocalizationFileFilter()
    {
        InstanceMember files = Row("LocalizationFiles");

        files.DisplayName.ShouldBe("Localization Files");
        files.PreferredDisplayer.ShouldBe(typeof(StandardDisplayers.MultiFile));
        files.PropertiesToSetOnDisplayer["Filter"].ShouldBe("Localization Files|*.csv;*.resx|All Files|*.*");
    }

    [Fact]
    public void FilesRow_ReadsAndWritesTheProjectsFileList()
    {
        _viewModel.LocalizationFiles = new List<string> { "A.resx" };
        InstanceMember files = Row("LocalizationFiles");

        ((IEnumerable<string>)files.Value!).ShouldBe(new[] { "A.resx" });
        files.Value = new List<string> { "A.resx", "B.resx" };

        _viewModel.LocalizationFiles.ShouldBe(new[] { "A.resx", "B.resx" });
    }

    [Fact]
    public void LanguageRow_ReadsAndWritesTheLanguageName()
    {
        _viewModel.AvailableLanguages = new[] { "English", "French" };
        _viewModel.LanguageName = "English";
        InstanceMember language = Row("LanguageName");

        language.Value.ShouldBe("English");
        language.Value = "French";

        _viewModel.LanguageName.ShouldBe("French");
    }

    [Fact]
    public void ShowLocalizationRow_ReadsAndWritesTheSetting()
    {
        InstanceMember show = Row("ShowLocalization");
        show.DisplayName.ShouldBe("Show Localization");

        show.Value = false;

        _viewModel.ShowLocalization.ShouldBeFalse();
        Row("ShowLocalization").Value.ShouldBe(false);
    }

    [Fact]
    public void SearchTerms_AreTheLabelAndHintOfEveryRow()
    {
        string[] terms = _sut.SearchTerms().ToArray();

        terms.ShouldContain("Localization Files");
        terms.ShouldContain("Show Localization");
        terms.Length.ShouldBe(4);
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
