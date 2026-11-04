using System.Linq;
using Gum.DataTypes;
using Gum.Plugins.FontsPlugin;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ProjectServices.FontGeneration;
using Gum.Services;
using Gum.SettingsWindow;
using Moq;
using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests.SettingsWindow;

/// <summary>The rows of the Project > Fonts page: which settings get a row, their labels, and the OS rule.</summary>
public class FontsRowsTests
{
    private static FontsViewModel CreateViewModel()
    {
        FontsViewModel viewModel = new FontsViewModel();
        viewModel.SetFrom(new GumProjectSave());
        return viewModel;
    }

    private static IOperatingSystemInfo Os(bool isWindows, string name) =>
        Mock.Of<IOperatingSystemInfo>(info => info.IsWindows == isWindows && info.DisplayName == name);

    private static InstanceMember Row(FontsRows rows, string name) =>
        rows.BuildCategories().SelectMany(section => section.Members).Single(member => member.Name == name);

    [Fact]
    public void Sections_AreCharacterRangesThenGeneration_WithTheSettingsAsRows()
    {
        FontsRows rows = new FontsRows(CreateViewModel(), Os(true, "Windows"));

        var sections = rows.BuildCategories();

        sections.Select(section => section.Name).ShouldBe(new[] { "Character ranges", "Generation" });
        sections[0].Members.Select(member => member.Name).ShouldBe(new[] { nameof(FontsViewModel.UseFontCharacterFile), "FontRanges" });
        sections[1].Members.Select(member => member.Name).ShouldBe(new[]
        {
            nameof(FontsViewModel.FontGenerator),
            nameof(FontsViewModel.FontSpacingHorizontal),
            nameof(FontsViewModel.FontSpacingVertical),
            nameof(FontsViewModel.AutoSizeFontOutputs),
        });
        sections[1].Members.Select(member => member.DisplayName).ShouldBe(new[]
        {
            "Font Generator", "Font Spacing Horizontal", "Font Spacing Vertical", "Auto-Size Font Outputs",
        });
        Row(rows, nameof(FontsViewModel.FontGenerator)).CustomOptions
            .ShouldBe(new object[] { FontGeneratorType.BmFont, FontGeneratorType.KernSmith });
        sections.SelectMany(section => section.Members).ShouldAllBe(member => !member.SupportsMakeDefault);
    }

    [Fact]
    public void EveryRow_HasAHint_AndTheSharedRowShape()
    {
        FontsRows rows = new FontsRows(CreateViewModel(), Os(true, "Windows"));

        InstanceMember[] members = rows.BuildCategories().SelectMany(section => section.Members).ToArray();

        members.ShouldAllBe(member => !string.IsNullOrEmpty(member.DetailText) && member.IsSpaciousRow);
        Row(rows, "FontRanges").PreferredDisplayer.ShouldBe(typeof(GumDisplayers.FontRanges));
    }

    [Fact]
    public void EachRow_ReadsAndWritesTheViewModelSetting()
    {
        FontsViewModel viewModel = CreateViewModel();
        FontsRows rows = new FontsRows(viewModel, Os(true, "Windows"));

        Row(rows, nameof(FontsViewModel.FontSpacingHorizontal)).Value = 4;
        Row(rows, nameof(FontsViewModel.FontSpacingVertical)).Value = 5;
        Row(rows, nameof(FontsViewModel.AutoSizeFontOutputs)).Value = true;
        Row(rows, nameof(FontsViewModel.UseFontCharacterFile)).Value = true;
        Row(rows, nameof(FontsViewModel.FontGenerator)).Value = FontGeneratorType.KernSmith;

        (viewModel.FontSpacingHorizontal, viewModel.FontSpacingVertical).ShouldBe((4, 5));
        viewModel.AutoSizeFontOutputs.ShouldBeTrue();
        viewModel.UseFontCharacterFile.ShouldBeTrue();
        viewModel.FontGenerator.ShouldBe(FontGeneratorType.KernSmith);
        Row(rows, nameof(FontsViewModel.FontSpacingHorizontal)).Value.ShouldBe(4);
        Row(rows, "FontRanges").Value.ShouldBeSameAs(viewModel);
    }

    [Theory]
    [InlineData(true, "Windows", false, "The program that bakes the font atlases.")]
    [InlineData(false, "macOS", true, "KernSmith is used on macOS")]
    [InlineData(false, "Linux", true, "KernSmith is used on Linux")]
    public void FontGenerator_IsReadOnlyAndNamesTheOs_WhereBmFontCannotRun(bool isWindows, string osName, bool expectedReadOnly, string expectedDetail)
    {
        FontsRows rows = new FontsRows(CreateViewModel(), Os(isWindows, osName));

        InstanceMember member = Row(rows, nameof(FontsViewModel.FontGenerator));

        member.IsReadOnly.ShouldBe(expectedReadOnly);
        member.DetailText.ShouldBe(expectedDetail);
    }

    [Fact]
    public void Reloaded_IsRaisedWhenTheViewModelIsReloaded()
    {
        FontsViewModel viewModel = CreateViewModel();
        FontsRows rows = new FontsRows(viewModel, Os(true, "Windows"));
        int raised = 0;
        rows.Reloaded += () => raised++;

        viewModel.NotifyReloaded();

        raised.ShouldBe(1);
    }

    [Fact]
    public void SearchTerms_IncludeTheLabelsAndHints()
    {
        string[] terms = new FontsRows(CreateViewModel(), Os(true, "Windows")).SearchTerms().ToArray();

        terms.ShouldContain("Font Spacing Horizontal");
        terms.ShouldContain("Ranges per font");
        terms.ShouldContain("Font character file");
    }
}
