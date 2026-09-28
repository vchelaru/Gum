using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Gum.Avalonia.Shell;
using Shouldly;

namespace Gum.Avalonia.Tests;

public class BackgroundWindowPlacementTests
{
    [AvaloniaFact]
    public void Apply_WithoutPlacingBeforeShowing_LeavesThePositionUntilTheWindowOpens()
    {
        // macOS collapses a window to its minimum size when it is first shown at a position it
        // has to pull back onto a screen (#5467), so there the window moves only once it is open.
        PixelPoint startPosition = new PixelPoint(100, 100);
        Window window = new Window { Width = 1280, Height = 720, Position = startPosition };
        try
        {
            BackgroundWindowPlacement.Apply(window, placeBeforeShowing: false);

            window.Position.ShouldBe(startPosition);

            window.Show();
            Dispatcher.UIThread.RunJobs();

            window.Position.ShouldBe(BackgroundWindowPlacement.Position);
            window.ShowActivated.ShouldBeFalse();
            window.WindowStartupLocation.ShouldBe(WindowStartupLocation.Manual);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Apply_PlacingBeforeShowing_MovesTheWindowBeforeItOpens()
    {
        Window window = new Window { Width = 1280, Height = 720, Position = new PixelPoint(100, 100) };
        try
        {
            BackgroundWindowPlacement.Apply(window, placeBeforeShowing: true);

            window.Position.ShouldBe(BackgroundWindowPlacement.Position);

            window.Show();
            Dispatcher.UIThread.RunJobs();

            window.Position.ShouldBe(BackgroundWindowPlacement.Position);
        }
        finally
        {
            window.Close();
        }
    }
}
