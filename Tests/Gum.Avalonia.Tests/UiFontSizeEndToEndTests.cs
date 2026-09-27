using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Gum.Avalonia.Plugins.EditorTab;
using Gum.Avalonia.Services;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Ctrl+Plus / Ctrl+Minus through the main window's app-wide key routing, resizing the head's real
/// editor toolbar and Project panel (the instances the running tool shows).
/// </summary>
public class UiFontSizeEndToEndTests
{
    [AvaloniaFact]
    public void CtrlPlus_GrowsTheToolbarAndCollapseButtons_WithoutClipping_AndCtrlMinusShrinksThemBack()
    {
        ToolStartup.EnsureInitialized();
        IServiceProvider services = TestAppBuilder.Services;
        IUiSettingsService uiSettings = services.GetRequiredService<IUiSettingsService>();
        double originalSize = uiSettings.BaseFontSize;
        EditorToolbar toolbar = services.GetRequiredService<PluginManager>().InitializedPlugins
            .OfType<AvaloniaEditorTabPlugin>().Single().Toolbar.ShouldNotBeNull();
        Control projectView = (Control)((AvaloniaTabManager)services.GetRequiredService<ITabManager>())
            .Left.Single(tab => tab.Title == "Project").Content;

        // Borrow both views into one window; the toolbar goes back into the editor tab afterwards.
        DockPanel editorTab = (DockPanel)toolbar.Parent!;
        int toolbarIndex = editorTab.Children.IndexOf(toolbar);
        editorTab.Children.Remove(toolbar);
        toolbar.DataContext = editorTab.DataContext;
        HeadlessWindowDriver.DetachFromHost(projectView);
        DockPanel host = new DockPanel();
        DockPanel.SetDock(toolbar, Dock.Top);
        host.Children.Add(toolbar);
        host.Children.Add(projectView);
        HeadlessWindowDriver? driver = null;
        IDisposable? fontSizeFollower = null;
        try
        {
            driver = new HeadlessWindowDriver(host, width: 900, height: 700, framesFolderName: "GumUiFontSize");
            TimelineRouting(driver.Window);
            fontSizeFollower = AppWideWindowInput.FollowBaseFontSize(driver.Window, services.GetRequiredService<IAppScaleProvider>());
            List<Button> collapseButtons = projectView.GetVisualDescendants().OfType<Button>()
                .Where(button => ToolTip.GetTip(button) is string tip && tip.StartsWith("Collapse")).ToList();
            collapseButtons.Count.ShouldBe(2);
            List<Button> toolbarButtons = toolbar.SizedButtons.ToList();
            Dictionary<Button, Size> before = toolbarButtons.Concat(collapseButtons).ToDictionary(button => button, button => button.Bounds.Size);
            driver.SaveFrame("step3-before");
            driver.Click(projectView.GetVisualDescendants().OfType<TextBox>().First());

            for (int i = 0; i < 6; i++)
            {
                driver.Press(Key.OemPlus, PhysicalKey.Equal, RawInputModifiers.Control);
            }

            uiSettings.BaseFontSize.ShouldBe(originalSize + 6);
            foreach (Button button in toolbarButtons)
            {
                button.Bounds.Width.ShouldBeGreaterThan(before[button].Width, "a toolbar +/- button widened");
                ShouldFitItsContent(button);
            }
            foreach (Button button in collapseButtons)
            {
                button.Bounds.Height.ShouldBeGreaterThan(before[button].Height, "a collapse button grew");
                ((Control)button.Content!).Bounds.Height.ShouldBeGreaterThan(16, "its icon grew");
                ShouldFitItsContent(button);
            }
            driver.SaveFrame("step3-after-ctrl-plus");

            for (int i = 0; i < 6; i++)
            {
                driver.Press(Key.OemMinus, PhysicalKey.Minus, RawInputModifiers.Control);
            }

            uiSettings.BaseFontSize.ShouldBe(originalSize);
            foreach ((Button button, Size size) in before)
            {
                button.Bounds.Size.ShouldBe(size, "back to the size it had before Ctrl+Plus");
            }
            driver.SaveFrame("step3-after-ctrl-minus");
        }
        finally
        {
            fontSizeFollower?.Dispose();
            uiSettings.BaseFontSize = originalSize;
            driver?.Dispose();
            host.Children.Clear();
            toolbar.ClearValue(StyledElement.DataContextProperty);
            editorTab.Children.Insert(toolbarIndex, toolbar);
        }
    }

    private static void TimelineRouting(Window window) => Animations.TimelineEndToEndTests.RouteAppWideHotkeys(window);

    // The content at its natural size fits inside the button's content area, so nothing is clipped.
    private static void ShouldFitItsContent(Button button)
    {
        ContentPresenter presenter = button.Presenter.ShouldNotBeNull();
        Size natural = presenter.Child switch
        {
            TextBlock text => Measure(new TextBlock { Text = text.Text, FontSize = text.FontSize, FontFamily = text.FontFamily, FontWeight = text.FontWeight }),
            Control other => new Size(other.Width, other.Height),
            _ => throw new ShouldAssertException("The button shows nothing."),
        };
        // The button's template is its presenter, so the room for content is inside its padding and border.
        Rect contentArea = new Rect(presenter.Bounds.Size).Deflate(presenter.Padding).Deflate(presenter.BorderThickness);
        natural.Width.ShouldBeLessThanOrEqualTo(contentArea.Width + 0.5, "the content fits the button's width");
        natural.Height.ShouldBeLessThanOrEqualTo(contentArea.Height + 0.5, "the content fits the button's height");
    }

    private static Size Measure(Control control)
    {
        control.Measure(Size.Infinity);
        return control.DesiredSize;
    }
}
