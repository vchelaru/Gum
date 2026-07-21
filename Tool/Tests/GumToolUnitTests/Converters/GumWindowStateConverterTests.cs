using Gum.Converters;
using Gum.ViewModels;
using Shouldly;
using WpfWindowState = System.Windows.WindowState;

namespace GumToolUnitTests.Converters;

/// <summary>
/// Pins <see cref="GumWindowStateConverter"/>'s boundary conversion between the headless
/// <see cref="GumWindowState"/> (VM side, ADR-0004) and the WPF <see cref="WpfWindowState"/>
/// (<c>Window.WindowState</c> binding side), part of #3856.
/// </summary>
public class GumWindowStateConverterTests
{
    private readonly GumWindowStateConverter _sut = new();

    [Theory]
    [InlineData(GumWindowState.Normal, WpfWindowState.Normal)]
    [InlineData(GumWindowState.Minimized, WpfWindowState.Minimized)]
    [InlineData(GumWindowState.Maximized, WpfWindowState.Maximized)]
    public void Convert_ShouldMapGumWindowStateToMatchingWpfWindowState(GumWindowState gumState, WpfWindowState expected)
    {
        object? result = _sut.Convert(gumState, typeof(WpfWindowState), null, null!);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(WpfWindowState.Normal, GumWindowState.Normal)]
    [InlineData(WpfWindowState.Minimized, GumWindowState.Minimized)]
    [InlineData(WpfWindowState.Maximized, GumWindowState.Maximized)]
    public void ConvertBack_ShouldMapWpfWindowStateToMatchingGumWindowState(WpfWindowState wpfState, GumWindowState expected)
    {
        object? result = _sut.ConvertBack(wpfState, typeof(GumWindowState), null, null!);

        result.ShouldBe(expected);
    }
}
