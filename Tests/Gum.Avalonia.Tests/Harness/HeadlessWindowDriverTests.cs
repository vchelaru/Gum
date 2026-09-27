using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Shouldly;

namespace Gum.Avalonia.Tests.Harness;

public class HeadlessWindowDriverTests
{
    [AvaloniaFact]
    public void AWindowOpenedWhileAnEarlierFrameIsUnrendered_HitTestsItsContent()
    {
        // The compositor sends one frame at a time: while an earlier frame waits for a render tick,
        // the new window's first frame is held back. The shared application makes that the normal
        // case between tests, since the last test's final frame may never have been rendered.
        Border earlier = new Border { Background = Brushes.Blue };
        Window earlierWindow = new Window { Content = earlier, Width = 100, Height = 100 };
        earlierWindow.Show();
        try
        {
            earlier.Background = Brushes.Green;
            Dispatcher.UIThread.RunJobs();

            using HeadlessWindowDriver driver = new HeadlessWindowDriver(new Border { Background = Brushes.Red }, width: 100, height: 100, framesFolderName: "GumDriverTests");

            driver.Window.InputHitTest(new global::Avalonia.Point(50, 50)).ShouldNotBeNull();
        }
        finally
        {
            earlierWindow.Close();
        }
    }
}
