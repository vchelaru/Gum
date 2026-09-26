using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Dialogs;
using Gum.Plugins;
using Gum.Services;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The Manage Plugins dialog is laid out as the WPF <c>PluginsDialogView</c>: a "Plugins" tab and a
/// "Folder scan" tab, each scrolling inside the window, the scan unwrapped in a monospace font, and
/// Copy Scan on the dialog's own button row.
/// </summary>
public class PluginsDialogViewTests
{
    [AvaloniaFact]
    public void PluginsDialog_TabsTheListAndTheScan_WithCopyScanOnTheButtonRow()
    {
        string folder = @"C:\Users\someone\AppData\Local\Programs\Gum\a\deeply\nested\install\folder\that\is\long\Plugins";
        PluginScanReport report = new PluginScanReport(folder, FolderExists: true, Files: Array.Empty<PluginFileScan>());
        Mock<IPluginManager> pluginManager = new Mock<IPluginManager>();
        pluginManager.Setup(manager => manager.GetAllPluginSummaries()).Returns(Enumerable.Range(0, 40)
            .Select(i => new PluginSummary($"Plugin{i:00}", $"Plugin{i:00}", IsEnabled: true, HasFailureDetails: false, PluginHandle: new object()))
            .ToList());
        pluginManager.Setup(manager => manager.GetPluginScanReport()).Returns(report);
        Mock<IClipboardService> clipboard = new Mock<IClipboardService>();
        PluginsDialogViewModel viewModel = new PluginsDialogViewModel(Mock.Of<IDialogService>(), pluginManager.Object, clipboard.Object);

        Control view = TestAppBuilder.Services.GetRequiredService<DialogViewRegistry>().CreateView(viewModel);
        DialogWindow window = new DialogWindow(viewModel, view) { MaxHeight = 400 };
        window.Show();
        Layout(window);

        // Two tabs, as WPF; on a short window the plugin list scrolls inside its tab.
        TabControl tabs = window.GetVisualDescendants().OfType<TabControl>().Single();
        tabs.Items.OfType<TabItem>().Select(tab => tab.Header).ShouldBe(new object[] { "Plugins", "Folder scan" });
        window.Bounds.Height.ShouldBeLessThanOrEqualTo(400);
        ScrollViewer pluginScroller = window.GetVisualDescendants().OfType<ScrollViewer>()
            .Single(scroller => scroller.GetVisualDescendants().OfType<CheckBox>().Any());
        pluginScroller.Extent.Height.ShouldBeGreaterThan(pluginScroller.Viewport.Height);
        Button close = window.GetVisualDescendants().OfType<Button>().Single(button => button.Name == DialogWindow.AffirmativeButtonName);
        ShouldBeInsideWindow(close, window);

        // Clicking the Folder scan tab shows the scan: monospace and unwrapped, so a long path
        // scrolls sideways instead of breaking mid-path.
        TabItem scanTab = tabs.Items.OfType<TabItem>().Single(tab => (string?)tab.Header == "Folder scan");
        Size sizeBeforeSwitch = window.Bounds.Size;
        Click(window, scanTab);
        window.Bounds.Size.ShouldBe(sizeBeforeSwitch, "switching tabs should not resize the dialog");
        TextBox scan = window.GetVisualDescendants().OfType<TextBox>().Single(box => box.IsEffectivelyVisible);
        scan.Text.ShouldBe(viewModel.Diagnostics);
        scan.TextWrapping.ShouldBe(TextWrapping.NoWrap);
        scan.FontFamily.FamilyNames.ShouldContain("monospace");
        scan.GetVisualDescendants().OfType<ScrollViewer>().ShouldContain(scroller => scroller.Extent.Width > scroller.Viewport.Width);

        // Copy Scan sits on the button row beside Close, and copies the scan.
        Button copy = window.GetVisualDescendants().OfType<Button>().Single(button => button.GetVisualDescendants().OfType<TextBlock>().Any(text => text.Text == "Copy Scan"));
        Math.Abs(CenterOf(copy, window).Y - CenterOf(close, window).Y).ShouldBeLessThan(2);
        Click(window, copy);
        clipboard.Verify(service => service.SetText(viewModel.Diagnostics), Times.Once);
        window.Close();
    }

    private static void Layout(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    private static Point CenterOf(Control control, Window window) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

    private static void Click(Window window, Control control)
    {
        Point point = CenterOf(control, window);
        window.MouseMove(point);
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Layout(window);
    }

    private static void ShouldBeInsideWindow(Control control, Window window)
    {
        Point origin = control.TranslatePoint(new Point(0, 0), window)!.Value;
        control.Bounds.Height.ShouldBeGreaterThan(0);
        origin.Y.ShouldBeGreaterThanOrEqualTo(0);
        (origin.Y + control.Bounds.Height).ShouldBeLessThanOrEqualTo(window.Bounds.Height);
    }
}
