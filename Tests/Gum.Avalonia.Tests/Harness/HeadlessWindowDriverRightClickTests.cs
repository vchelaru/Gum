using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Shouldly;

namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// <see cref="HeadlessWindowDriver.RightClick"/> returns with the menu open however the menu got
/// there: a press that landed elsewhere is repeated, and a menu opened by later work is waited for
/// by the real clock, so a loaded machine cannot make a test read a menu that is not open yet.
/// </summary>
public class HeadlessWindowDriverRightClickTests
{
    [AvaloniaFact]
    public void RightClick_OnAControlWhoseMenuOpensFromAThreadPoolContinuation_FindsTheMenu()
    {
        ContextMenu menu = NewMenu();
        Border target = NewTarget(menu);
        // The request is taken and the menu opens a second later from another thread, as the
        // canvas's menu does once its frame is presented.
        target.AddHandler(Control.ContextRequestedEvent, (_, e) =>
        {
            e.Handled = true;
            Task.Delay(1200).ContinueWith(_ => Dispatcher.UIThread.Post(() => menu.Open(target)));
        }, global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
        using HeadlessWindowDriver driver = Host(target);

        driver.RightClick(target);

        driver.ContextMenuHeaders().ShouldBe(new[] { "First" });
    }

    [AvaloniaFact]
    public void RightClick_WhenThePressLandsOnSomethingCoveringTheControl_IsRepeatedOnceItClears()
    {
        Border target = NewTarget(NewMenu());
        Border cover = new Border { Background = Brushes.Transparent };
        Grid content = new Grid();
        content.Children.Add(target);
        content.Children.Add(cover);
        // The cover stands for a scene that is not what the layout says yet: the first presses
        // land on it and raise a request on something else.
        Task.Delay(800).ContinueWith(_ => Dispatcher.UIThread.Post(() => content.Children.Remove(cover)));
        using HeadlessWindowDriver driver = Host(content);

        driver.RightClick(target);

        driver.ContextMenuHeaders().ShouldBe(new[] { "First" });
    }

    [AvaloniaFact]
    public void RightClick_OnAControlWithNoMenu_ReturnsAtOnceAndLeavesNoMenuOpen()
    {
        Border target = new Border { Background = Brushes.Transparent };
        using HeadlessWindowDriver driver = Host(target);
        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

        driver.RightClick(target);

        driver.OpenContextMenu.ShouldBeNull();
        watch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(2));
    }

    private static ContextMenu NewMenu() => new ContextMenu { ItemsSource = new[] { new MenuItem { Header = "First" } } };

    private static Border NewTarget(ContextMenu menu) =>
        new Border { Background = Brushes.Gray, ContextMenu = menu };

    private static HeadlessWindowDriver Host(Control content) =>
        new HeadlessWindowDriver(content, 300, 200, "HeadlessWindowDriverRightClickTests");
}
