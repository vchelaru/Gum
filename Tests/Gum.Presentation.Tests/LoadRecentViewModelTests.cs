using System.Linq;
using Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin.ViewModels;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for LoadRecentViewModel, relocated out of Gum.csproj into the
/// headless Gum.Presentation assembly (ADR-0005, #3754). Its three WPF Visibility properties were
/// converted to bool (ADR-0004) - the View now applies BoolToVisibilityConverter.
/// </summary>
public class LoadRecentViewModelTests
{
    [Fact]
    public void IsSearchButtonVisible_True_WhenSearchBoxTextIsNotEmpty()
    {
        LoadRecentViewModel viewModel = new() { SearchBoxText = "abc" };

        viewModel.IsSearchButtonVisible.ShouldBeTrue();
    }

    [Fact]
    public void IsSearchButtonVisible_False_WhenSearchBoxTextIsEmpty()
    {
        LoadRecentViewModel viewModel = new() { SearchBoxText = "" };

        viewModel.IsSearchButtonVisible.ShouldBeFalse();
    }

    [Fact]
    public void IsTipsVisible_IsAlwaysFalse()
    {
        LoadRecentViewModel viewModel = new();

        viewModel.IsTipsVisible.ShouldBeFalse();
    }

    [Fact]
    public void IsSearchPlaceholderVisible_True_WhenNotFocusedAndSearchBoxTextIsEmpty()
    {
        LoadRecentViewModel viewModel = new() { IsSearchBoxFocused = false, SearchBoxText = "" };

        viewModel.IsSearchPlaceholderVisible.ShouldBeTrue();
    }

    [Fact]
    public void IsSearchPlaceholderVisible_False_WhenFocused()
    {
        LoadRecentViewModel viewModel = new() { IsSearchBoxFocused = true, SearchBoxText = "" };

        viewModel.IsSearchPlaceholderVisible.ShouldBeFalse();
    }

    [Fact]
    public void IsSearchPlaceholderVisible_False_WhenSearchBoxTextIsNotEmpty()
    {
        LoadRecentViewModel viewModel = new() { IsSearchBoxFocused = false, SearchBoxText = "abc" };

        viewModel.IsSearchPlaceholderVisible.ShouldBeFalse();
    }

    [Fact]
    public void CanExecuteAffirmative_False_WhenNoItemSelected()
    {
        LoadRecentViewModel viewModel = new();

        viewModel.CanExecuteAffirmative().ShouldBeFalse();
    }

    [Fact]
    public void CanExecuteAffirmative_True_WhenItemSelected()
    {
        LoadRecentViewModel viewModel = new();
        viewModel.AllItems.Add(new RecentItemViewModel { FullPath = "c:\\a.gumx" });
        viewModel.RefreshFilteredItems();

        viewModel.CanExecuteAffirmative().ShouldBeTrue();
    }

    [Fact]
    public void RefreshFilteredItems_OrdersFavoritesBeforeNonFavorites()
    {
        LoadRecentViewModel viewModel = new();
        RecentItemViewModel nonFavorite = new() { FullPath = "c:\\a.gumx", IsFavorite = false };
        RecentItemViewModel favorite = new() { FullPath = "c:\\b.gumx", IsFavorite = true };
        viewModel.AllItems.Add(nonFavorite);
        viewModel.AllItems.Add(favorite);

        viewModel.RefreshFilteredItems();

        viewModel.FilteredItems.ShouldBe(new[] { favorite, nonFavorite });
    }

    [Fact]
    public void RefreshFilteredItems_FiltersBySearchBoxText()
    {
        LoadRecentViewModel viewModel = new();
        RecentItemViewModel matching = new() { FullPath = "c:\\projects\\MyGame.gumx" };
        RecentItemViewModel nonMatching = new() { FullPath = "c:\\projects\\OtherGame.gumx" };
        viewModel.AllItems.Add(matching);
        viewModel.AllItems.Add(nonMatching);
        viewModel.SearchBoxText = "MyGame";

        viewModel.RefreshFilteredItems();

        viewModel.FilteredItems.ShouldBe(new[] { matching });
    }

    [Fact]
    public void RefreshFilteredItems_RetainsPreviousSelection_WhenStillPresentAfterFilter()
    {
        LoadRecentViewModel viewModel = new();
        RecentItemViewModel first = new() { FullPath = "c:\\a.gumx" };
        RecentItemViewModel second = new() { FullPath = "c:\\b.gumx" };
        viewModel.AllItems.Add(first);
        viewModel.AllItems.Add(second);
        viewModel.RefreshFilteredItems();
        viewModel.SelectedItem = second;

        viewModel.RefreshFilteredItems();

        viewModel.SelectedItem.ShouldBe(viewModel.FilteredItems.Single(item => item.FullPath == second.FullPath));
    }

    [Fact]
    public void SearchBoxText_Set_TriggersRefreshFilteredItems()
    {
        LoadRecentViewModel viewModel = new();
        viewModel.AllItems.Add(new RecentItemViewModel { FullPath = "c:\\projects\\MyGame.gumx" });
        viewModel.AllItems.Add(new RecentItemViewModel { FullPath = "c:\\projects\\OtherGame.gumx" });
        viewModel.RefreshFilteredItems();

        viewModel.SearchBoxText = "MyGame";

        viewModel.FilteredItems.Count.ShouldBe(1);
    }
}
