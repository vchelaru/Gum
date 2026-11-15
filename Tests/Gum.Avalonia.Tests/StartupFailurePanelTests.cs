using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Gum.Avalonia.Diagnostics;
using Gum.Avalonia.Shell;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The startup failure screen is a normal closable window: selectable text of bounded width, plus
/// Copy and Close buttons.
/// </summary>
public class StartupFailurePanelTests
{
    [AvaloniaFact]
    public void Panel_ShowsSelectableTextWithBoundedWidth()
    {
        StartupFailurePanel panel = new StartupFailurePanel(new InvalidOperationException("boom"), () => { }, _ => { });

        panel.Message.ShouldBeOfType<SelectableTextBlock>();
        panel.Message.Text!.ShouldContain("boom");
        panel.Message.MaxWidth.ShouldBeLessThan(double.PositiveInfinity);
    }

    [AvaloniaFact]
    public void CopyAndCloseButtons_InvokeTheirActions_WithTheFailureText()
    {
        string? copied = null;
        bool closed = false;
        StartupFailurePanel panel = new StartupFailurePanel(
            new InvalidOperationException("boom"), () => closed = true, text => copied = text);

        panel.CopyButton.Command!.Execute(null);
        panel.CloseButton.Command!.Execute(null);

        copied.ShouldNotBeNull().ShouldContain("boom");
        closed.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void CloseOnAReportedWindow_ClosesTheMainWindow()
    {
        Window window = new Window { Content = new TextBlock { Text = "panels" } };
        bool windowClosed = false;
        window.Closed += (_, _) => windowClosed = true;
        StartupFailureReporter reporter = new StartupFailureReporter(window, new StringWriter());
        window.Show();

        reporter.Report(new InvalidOperationException("boom"));
        window.Content.ShouldBeOfType<StartupFailurePanel>().CloseButton.Command!.Execute(null);

        windowClosed.ShouldBeTrue();
    }
}
