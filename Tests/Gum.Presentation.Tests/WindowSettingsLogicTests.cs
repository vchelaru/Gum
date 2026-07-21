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
}
