using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Gum.Avalonia.Plugins.TreeView;
using Gum.Managers;
using Shouldly;

namespace Gum.Avalonia.Tests.Accessibility;

/// <summary>
/// macOS asks an element for its top-level element and frame by walking its parents (#5969), so
/// every tree row's parent chain has to end at the window, not at a detached template fragment.
/// </summary>
public class TreeRowAutomationParentTests
{
    [AvaloniaFact]
    public void TreeRows_ParentChainReachesTheWindowPeer_AfterTheTreeIsWalkedFromTheWindow()
    {
        AvaloniaGumTreeView tree = new AvaloniaGumTreeView();
        GumTreeNode screens = new GumTreeNode("Screens");
        screens.Nodes.Add(new GumTreeNode("Alpha"));
        screens.IsExpanded = true;
        tree.Nodes.Add(screens);
        Window window = new Window { Width = 300, Height = 400, Content = tree };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        // A bridge reads children top-down before it asks any element for its parent.
        AutomationPeer windowPeer = ControlAutomationPeer.CreatePeerForElement(window);
        List<AutomationPeer> rows = new List<AutomationPeer>();
        void Walk(AutomationPeer peer)
        {
            if (peer.GetName() is "Screens" or "Alpha")
            {
                rows.Add(peer);
            }
            foreach (AutomationPeer child in peer.GetChildren())
            {
                Walk(child);
            }
        }
        Walk(windowPeer);

        rows.Count.ShouldBe(2);
        foreach (AutomationPeer row in rows)
        {
            AutomationPeer top = row;
            for (AutomationPeer? parent = row.GetParent(); parent != null; parent = top.GetParent())
            {
                top = parent;
            }
            top.GetProvider<IRootProvider>().ShouldNotBeNull($"'{row.GetName()}' ends at {top.GetType().Name}");
        }
        window.Close();
    }
}
