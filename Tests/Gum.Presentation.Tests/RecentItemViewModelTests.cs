using System.Collections.Generic;
using Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin.ViewModels;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for RecentItemViewModel, relocated out of Gum.csproj into the
/// headless Gum.Presentation assembly (ADR-0005, #3754). Its WPF-typed FavoriteImage (BitmapImage)
/// property was removed - the View now converts the plain IsFavorite bool to an icon via a
/// WPF-only FavoriteToImageSourceConverter that stays behind in the Gum tool project.
/// </summary>
public class RecentItemViewModelTests
{
    [Fact]
    public void StrippedName_ReturnsFileNameWithoutPath_WhenFullPathIsSet()
    {
        RecentItemViewModel item = new() { FullPath = "c:\\Projects\\MyGame\\MyGame.gumx" };

        item.StrippedName.ShouldBe("MyGame.gumx");
    }

    [Fact]
    public void StrippedName_ReturnsEmpty_WhenFullPathIsEmpty()
    {
        RecentItemViewModel item = new();

        item.StrippedName.ShouldBe("");
    }

    [Fact]
    public void IsFavorite_Set_RaisesPropertyChanged()
    {
        RecentItemViewModel item = new();
        List<string?> changedProperties = new();
        item.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        item.IsFavorite = true;

        item.IsFavorite.ShouldBeTrue();
        changedProperties.ShouldContain(nameof(RecentItemViewModel.IsFavorite));
    }

    [Fact]
    public void FullPath_Set_RaisesPropertyChanged_AndStrippedName()
    {
        RecentItemViewModel item = new();
        List<string?> changedProperties = new();
        item.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        item.FullPath = "c:\\Projects\\MyGame\\MyGame.gumx";

        changedProperties.ShouldContain(nameof(RecentItemViewModel.FullPath));
        changedProperties.ShouldContain(nameof(RecentItemViewModel.StrippedName));
    }
}
