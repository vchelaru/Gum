using Gum.Settings;
using Gum.ViewModels;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization tests for <see cref="WindowSettingsLogic.IsFirstLaunch"/>, pinning the
/// first-launch guard relocated from <c>MainWindowViewModel.LoadWindowSettings</c> (#3856).
/// </summary>
public class WindowSettingsLogicTests
{
    [Fact]
    public void IsFirstLaunch_ShouldReturnFalse_WhenSettingsAreFullyPopulated()
    {
        WindowSettings settings = new(Width: 1280, Height: 720, Top: 10, Left: 20, IsMaximized: false);

        bool result = WindowSettingsLogic.IsFirstLaunch(settings);

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsFirstLaunch_ShouldReturnTrue_WhenLeftAndTopAreNull()
    {
        WindowSettings settings = new(Width: 1280, Height: 720, Top: null, Left: null);

        bool result = WindowSettingsLogic.IsFirstLaunch(settings);

        result.ShouldBeTrue();
    }

    [Fact]
    public void IsFirstLaunch_ShouldReturnTrue_WhenWidthIsZero()
    {
        WindowSettings settings = new(Width: 0, Height: 720, Top: 10, Left: 20);

        bool result = WindowSettingsLogic.IsFirstLaunch(settings);

        result.ShouldBeTrue();
    }

    [Fact]
    public void IsFirstLaunch_ShouldReturnTrue_WhenHeightIsZero()
    {
        WindowSettings settings = new(Width: 1280, Height: 0, Top: 10, Left: 20);

        bool result = WindowSettingsLogic.IsFirstLaunch(settings);

        result.ShouldBeTrue();
    }

    [Fact]
    public void IsFirstLaunch_ShouldReturnTrue_WhenSizeIsTooSmallToBeUsable()
    {
        // The OS minimum track size, as reported in #4361 - a title-bar-only window.
        WindowSettings settings = new(Width: 159.2, Height: 27.2, Top: 0, Left: 0);

        bool result = WindowSettingsLogic.IsFirstLaunch(settings);

        result.ShouldBeTrue();
    }

    [Fact]
    public void WithUsableSize_ShouldPreserveSize_WhenSizeIsUsable()
    {
        WindowSettings settings = new(Width: 1000, Height: 800, Top: 10, Left: 20, IsMaximized: true);

        WindowSettings result = WindowSettingsLogic.WithUsableSize(settings);

        result.ShouldBe(settings);
    }

    [Fact]
    public void WithUsableSize_ShouldReplaceSizeWithDefault_WhenSizeIsTooSmallToBeUsable()
    {
        WindowSettings settings = new(Width: 159.2, Height: 27.2, Top: 10, Left: 20, IsMaximized: true);

        WindowSettings result = WindowSettingsLogic.WithUsableSize(settings);

        result.Width.ShouldBe(1280);
        result.Height.ShouldBe(720);
        result.Top.ShouldBe(10);
        result.Left.ShouldBe(20);
        result.IsMaximized.ShouldBeTrue();
    }
}
