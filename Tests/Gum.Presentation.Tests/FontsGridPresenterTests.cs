using System.Linq;
using Gum.DataTypes;
using Gum.Plugins.FontsPlugin;
using Gum.Services;
using Moq;
using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests;

/// <summary>The Generation rows of the Fonts tab: which members get a row, their names, and the OS rule.</summary>
public class FontsGridPresenterTests
{
    [Fact]
    public void Bind_ShowsOnlyTheGenerationSettings_InOneCategoryWithTheirNames()
    {
        FontsViewModel viewModel = new FontsViewModel();
        viewModel.SetFrom(new GumProjectSave());
        ModelBackedGrid grid = new ModelBackedGrid();
        FontsGridPresenter presenter = new FontsGridPresenter(grid);

        presenter.Bind(viewModel);

        MemberCategory category = grid.Categories.Where(candidate => candidate.Members.Count > 0).ShouldHaveSingleItem();
        category.Name.ShouldBe("Generation");
        category.Members.Select(member => member.Name).ShouldBe(new[]
        {
            nameof(FontsViewModel.FontGenerator),
            nameof(FontsViewModel.FontSpacingHorizontal),
            nameof(FontsViewModel.FontSpacingVertical),
            nameof(FontsViewModel.AutoSizeFontOutputs),
        });
        category.Members.Select(member => member.DisplayName).ShouldBe(new[]
        {
            "Font Generator", "Font Spacing Horizontal", "Font Spacing Vertical", "Auto-Size Font Outputs",
        });
        grid.GetInstanceMember(nameof(FontsViewModel.FontGenerator))!.CustomOptions
            .ShouldBe(new object[] { FontGeneratorType.BmFont, FontGeneratorType.KernSmith });
        grid.Categories.SelectMany(candidate => candidate.Members).ShouldAllBe(member => !member.SupportsMakeDefault);
    }

    [Theory]
    [InlineData(true, "Windows", false, null)]
    [InlineData(false, "macOS", true, "KernSmith is used on macOS")]
    [InlineData(false, "Linux", true, "KernSmith is used on Linux")]
    public void FontGenerator_IsReadOnlyAndNamesTheOs_WhereBmFontCannotRun(bool isWindows, string osName, bool expectedReadOnly, string? expectedDetail)
    {
        FontsViewModel viewModel = new FontsViewModel();
        viewModel.SetFrom(new GumProjectSave());
        ModelBackedGrid grid = new ModelBackedGrid();
        IOperatingSystemInfo os = Mock.Of<IOperatingSystemInfo>(info => info.IsWindows == isWindows && info.DisplayName == osName);
        FontsGridPresenter presenter = new FontsGridPresenter(grid, os);

        presenter.Bind(viewModel);

        InstanceMember member = grid.GetInstanceMember(nameof(FontsViewModel.FontGenerator))!;
        member.IsReadOnly.ShouldBe(expectedReadOnly);
        member.DetailText.ShouldBe(expectedDetail);
    }

    [Fact]
    public void Reloaded_RebuildsTheRows_FromTheSameViewModel()
    {
        FontsViewModel viewModel = new FontsViewModel();
        viewModel.SetFrom(new GumProjectSave());
        ModelBackedGrid grid = new ModelBackedGrid();
        FontsGridPresenter presenter = new FontsGridPresenter(grid);
        presenter.Bind(viewModel);
        InstanceMember before = grid.GetInstanceMember(nameof(FontsViewModel.FontSpacingVertical))!;

        viewModel.NotifyReloaded();

        grid.GetInstanceMember(nameof(FontsViewModel.FontSpacingVertical)).ShouldNotBeSameAs(before);
    }
}
