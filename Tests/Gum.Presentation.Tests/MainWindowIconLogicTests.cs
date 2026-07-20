using Gum.Dialogs;
using Gum.ViewModels;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization tests for <see cref="MainWindowIconLogic"/>, pinning the icon-per-theme mapping
/// relocated from <c>MainWindowViewModel.Receive(ThemeChangedMessage)</c> (#3856).
/// </summary>
public class MainWindowIconLogicTests
{
    [Fact]
    public void GetIconSource_ReturnsLightIcon_WhenModeIsLight()
    {
        string iconSource = MainWindowIconLogic.GetIconSource(ThemeMode.Light);

        iconSource.ShouldBe("pack://application:,,,/GumLogo64Light.png");
    }

    [Fact]
    public void GetIconSource_ReturnsDarkIcon_WhenModeIsDark()
    {
        string iconSource = MainWindowIconLogic.GetIconSource(ThemeMode.Dark);

        iconSource.ShouldBe("pack://application:,,,/GumLogo64.png");
    }

    [Fact]
    public void GetIconSource_ReturnsDarkIcon_WhenModeIsSystem()
    {
        string iconSource = MainWindowIconLogic.GetIconSource(ThemeMode.System);

        iconSource.ShouldBe("pack://application:,,,/GumLogo64.png");
    }
}
