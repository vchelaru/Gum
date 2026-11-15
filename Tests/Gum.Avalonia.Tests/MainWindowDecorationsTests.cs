using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Gum.Avalonia.Shell;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Where the main window draws into its title bar (Windows, macOS), Avalonia 12 draws the caption
/// buttons itself: the window must keep full decorations, and the drawn title text and full-screen
/// button must stay hidden so only minimize, maximize and close show beside the title row.
/// </summary>
public class MainWindowDecorationsTests
{
    [AvaloniaFact]
    public void MainWindow_DrawingIntoTheTitleBar_KeepsFullDecorationsAndTheCaptionButtonsTheme()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows() || OperatingSystem.IsMacOS(), "Linux keeps the system title bar");
        MainWindow window = TestAppBuilder.Services.GetRequiredService<MainWindow>();

        window.ExtendClientAreaToDecorationsHint.ShouldBeTrue();
        window.WindowDecorations.ShouldBe(WindowDecorations.Full);
        ControlTheme? theme = window.WindowDecorationsTheme;
        theme.ShouldNotBeNull("the caption buttons theme was not built, so the drawn title text and full-screen button would show");
        theme.TargetType.ShouldBe(typeof(WindowDrawnDecorations));
        theme.BasedOn.ShouldNotBeNull();
        theme.Children.Count.ShouldBe(2);
    }
}
