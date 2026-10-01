using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Plugins.TreeView;
using Gum.Avalonia.Services;
using Gum.Managers;
using Gum.Services;
using InputLibrary;
using Moq;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// #5555: on macOS Ctrl+left-click is a right-click; on Windows and Linux it keeps its own meaning.
/// </summary>
public class SecondaryClickTests
{
    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtrlClick_OnAControlWithAContextMenu_OpensTheMenuOnlyOnMacOS(bool isMacOS)
    {
        using IDisposable hook = SecondaryClickHook.Install(OperatingSystemOf(isMacOS));
        ContextMenu menu = new ContextMenu { Items = { new MenuItem { Header = "Item" } } };
        int clicks = 0;
        int rightPresses = 0;
        Button button = new Button { Content = "Target", Width = 100, Height = 40, ContextMenu = menu };
        button.Click += (_, _) => clicks++;
        button.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
        {
            if (e.GetCurrentPoint(button).Properties.IsRightButtonPressed)
            {
                rightPresses++;
            }
        }, RoutingStrategies.Bubble, handledEventsToo: true);
        Window window = new Window { Width = 300, Height = 300, Content = new global::Avalonia.Controls.Canvas { Children = { button } } };
        window.Show();
        window.UpdateLayout();

        Point point = new Point(50, 20);
        window.MouseDown(point, MouseButton.Left, RawInputModifiers.Control);
        window.MouseUp(point, MouseButton.Left, RawInputModifiers.Control);
        Dispatcher.UIThread.RunJobs();

        menu.IsOpen.ShouldBe(isMacOS);
        rightPresses.ShouldBe(isMacOS ? 1 : 0);
        clicks.ShouldBe(isMacOS ? 0 : 1);
        menu.Close();
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtrlClick_OnATreeRow_RightClicksOnMacOS_AndAddsToTheSelectionElsewhere(bool isMacOS)
    {
        using IDisposable hook = SecondaryClickHook.Install(OperatingSystemOf(isMacOS));
        AvaloniaGumTreeView tree = new AvaloniaGumTreeView();
        tree.Selection.IsSelectingOnPush = false;
        GumTreeNode screens = new GumTreeNode("Screens");
        GumTreeNode first = new GumTreeNode("First");
        GumTreeNode second = new GumTreeNode("Second");
        screens.Nodes.Add(first);
        screens.Nodes.Add(second);
        tree.Nodes.Add(screens);
        screens.IsExpanded = true;
        Window window = new Window { Width = 300, Height = 400, Content = tree };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        int menuRequests = 0;
        tree.ContextMenuRequested += () => menuRequests++;

        Click(window, tree, first, RawInputModifiers.None);
        Click(window, tree, second, RawInputModifiers.Control);

        menuRequests.ShouldBe(isMacOS ? 1 : 0);
        tree.Selection.SelectedNodes.ShouldBe(isMacOS ? new[] { second } : new[] { first, second });
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtrlPress_OnTheCanvas_HoldsTheRightButtonOnMacOS_UntilReleased(bool isMacOS)
    {
        IOperatingSystemInfo operatingSystem = OperatingSystemOf(isMacOS);
        using IDisposable hook = SecondaryClickHook.Install(operatingSystem);
        Border control = new Border { Width = 100, Height = 80, Background = global::Avalonia.Media.Brushes.Red };
        Window window = new Window { Width = 300, Height = 300, Content = new global::Avalonia.Controls.Canvas { Children = { control } } };
        window.Show();
        window.UpdateLayout();
        AvaloniaInputHostAdapter adapter = new AvaloniaInputHostAdapter(control, operatingSystem);

        window.MouseDown(new Point(10, 10), MouseButton.Left, RawInputModifiers.Control);
        HostPointerState pressed = adapter.GetPointerState();
        // Letting go of Ctrl mid-press does not turn the press back into a left press.
        window.MouseMove(new Point(12, 12), RawInputModifiers.LeftMouseButton);
        HostPointerState moved = adapter.GetPointerState();
        window.MouseUp(new Point(12, 12), MouseButton.Left, RawInputModifiers.None);
        HostPointerState released = adapter.GetPointerState();

        pressed.IsRightDown.ShouldBe(isMacOS);
        pressed.IsLeftDown.ShouldBe(!isMacOS);
        moved.IsRightDown.ShouldBe(isMacOS);
        moved.IsLeftDown.ShouldBe(!isMacOS);
        released.IsRightDown.ShouldBeFalse();
        released.IsLeftDown.ShouldBeFalse();
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void DoubleCtrlClick_OnATreeRow_IsTwoRightClicksOnMacOS_NotADoubleTap(bool isMacOS)
    {
        using IDisposable hook = SecondaryClickHook.Install(OperatingSystemOf(isMacOS));
        AvaloniaGumTreeView tree = new AvaloniaGumTreeView();
        tree.Selection.IsSelectingOnPush = false;
        GumTreeNode screens = new GumTreeNode("Screens");
        screens.Nodes.Add(new GumTreeNode("First"));
        tree.Nodes.Add(screens);
        Window window = new Window { Width = 300, Height = 400, Content = tree };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        int menuRequests = 0;
        int taps = 0;
        int doubleTaps = 0;
        tree.ContextMenuRequested += () => menuRequests++;
        tree.AddHandler(InputElement.TappedEvent, (_, _) => taps++);
        tree.AddHandler(InputElement.DoubleTappedEvent, (_, _) => doubleTaps++);

        Click(window, tree, screens, RawInputModifiers.Control);
        Click(window, tree, screens, RawInputModifiers.Control);

        // Elsewhere the second click is a double click, which toggles the row's expansion.
        doubleTaps.ShouldBe(isMacOS ? 0 : 1);
        taps.ShouldBe(isMacOS ? 0 : 1);
        screens.IsExpanded.ShouldBe(!isMacOS);
        menuRequests.ShouldBe(isMacOS ? 2 : 0);
        window.Close();
    }

    [AvaloniaFact]
    public void AppInputHooks_InstallEachHookOnce()
    {
        Mock<ICanvasRedrawScheduler> scheduler = new Mock<ICanvasRedrawScheduler>();
        using IDisposable hooks = AppInputHooks.Install(scheduler.Object, OperatingSystemOf(isMacOS: true));
        int rightPresses = 0;
        Border target = new Border { Width = 100, Height = 40, Background = global::Avalonia.Media.Brushes.Red };
        target.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
        {
            if (e.GetCurrentPoint(target).Properties.IsRightButtonPressed)
            {
                rightPresses++;
            }
        }, RoutingStrategies.Bubble, handledEventsToo: true);
        Window window = new Window { Width = 300, Height = 300, Content = new global::Avalonia.Controls.Canvas { Children = { target } } };
        window.Show();
        window.UpdateLayout();

        window.MouseMove(new Point(50, 20));
        scheduler.Verify(s => s.RequestRedraw(), Times.Once());
        window.MouseDown(new Point(50, 20), MouseButton.Left, RawInputModifiers.Control);
        window.MouseUp(new Point(50, 20), MouseButton.Left, RawInputModifiers.Control);

        rightPresses.ShouldBe(1);
        window.Close();
    }

    private static IOperatingSystemInfo OperatingSystemOf(bool isMacOS) =>
        Mock.Of<IOperatingSystemInfo>(o => o.IsMacOS == isMacOS && o.IsWindows == !isMacOS);

    private static void Click(Window window, AvaloniaGumTreeView tree, GumTreeNode node, RawInputModifiers modifiers)
    {
        TreeRowView row = tree.GetVisualDescendants().OfType<TreeRowView>().Single(view => view.Row?.Node == node);
        Point point = row.TranslatePoint(new Point(row.Bounds.Width / 2, row.Bounds.Height / 2), window)!.Value;

        window.MouseDown(point, MouseButton.Left, modifiers);
        window.MouseUp(point, MouseButton.Left, modifiers);
        Dispatcher.UIThread.RunJobs();
    }
}
