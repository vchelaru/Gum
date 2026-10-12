using Avalonia.Automation.Peers;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Shell;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.Accessibility;

/// <summary>
/// What the main window hands every platform's accessibility bridge: the Avalonia automation peer
/// tree. On macOS the tree an Accessibility client sees is nearly empty (#5969) even though this
/// one is complete, so the gap is in Avalonia's native bridge or how it is queried, not in a
/// control that lacks a peer. These tests keep this side from regressing and give a macOS run
/// (Tools/MacPass, scenario "window-content-in-ax-tree") something to compare against.
/// </summary>
public class MainWindowAutomationTreeTests
{
    [AvaloniaFact]
    public void MainWindow_ConstructsAndShows_AndExposesNamedTabsButtonsAndTreeRows_ThroughItsPeerTree()
    {
        // This is the only test that shows the container's one MainWindow: a closed window cannot be
        // shown again, and one that stays open keeps the tab content parented to it, which breaks
        // the harnesses that host that content themselves.
        MainWindow window = TestAppBuilder.Services.GetRequiredService<MainWindow>();
        window.Show();
        window.IsVisible.ShouldBeTrue();
        window.Title.ShouldNotBeNullOrEmpty();
        // A shown canvas creates the process-wide KNI/SDL device on its first frame, and SDL on the
        // headless UI thread aborts the whole test host on macOS ("setting the main menu on a
        // non-main thread"). A hidden canvas skips its frames, and this walk does not need it.
        foreach (AvaloniaGraphicsDeviceControl canvas in window.GetVisualDescendants().OfType<AvaloniaGraphicsDeviceControl>())
        {
            canvas.IsVisible = false;
        }
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        List<AutomationPeer> peers = new List<AutomationPeer>();
        void Collect(AutomationPeer peer)
        {
            peers.Add(peer);
            foreach (AutomationPeer child in peer.GetChildren())
            {
                Collect(child);
            }
        }
        Collect(ControlAutomationPeer.CreatePeerForElement(window));

        string[] Names(AutomationControlType type) => peers
            .Where(peer => peer.IsControlElement() && peer.GetAutomationControlType() == type)
            .Select(peer => peer.GetName())
            .ToArray();

        Names(AutomationControlType.TabItem).ShouldContain("Project");
        Names(AutomationControlType.TabItem).ShouldContain("Variables");
        Names(AutomationControlType.TabItem).ShouldContain("Editor");
        Names(AutomationControlType.Button).ShouldContain("Preview in runtime");
        Names(AutomationControlType.Text).ShouldContain("Components");
        peers.Count(peer => peer.IsControlElement()).ShouldBeGreaterThan(50);
        window.Close();
    }
}
