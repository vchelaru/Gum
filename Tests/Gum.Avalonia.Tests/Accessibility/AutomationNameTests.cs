using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Dialogs;
using Gum.Avalonia.Dialogs.Views;
using Gum.Avalonia.Panels;
using Gum.Avalonia.Plugins.EditorTab;
using Gum.Avalonia.Plugins.TreeView;
using Gum.Avalonia.Shell;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.Accessibility;

/// <summary>
/// What UI Automation reports for the head's controls: a screen reader announces the name, so it
/// must be the visible text, never a type name or access-key markup (#5591).
/// </summary>
public class AutomationNameTests
{
    private static readonly Regex TypeNameLike = new Regex(@"^[A-Za-z_][\w`]*(\.[\w`]+)+$");

    private static List<(Control Control, string Name)> NamesIn(Control root)
    {
        List<(Control, string)> result = new List<(Control, string)>();
        foreach (Control control in root.GetSelfAndVisualDescendants().OfType<Control>())
        {
            string name = ControlAutomationPeer.CreatePeerForElement(control).GetName();
            result.Add((control, name));
        }
        return result;
    }

    private static void ShouldHaveNoBadNames(Control root)
    {
        List<string> bad = NamesIn(root)
            .Where(item => TypeNameLike.IsMatch(item.Name) || item.Name.StartsWith("_"))
            .Select(item => $"{item.Control.GetType().Name} -> {item.Name}")
            .ToList();
        bad.ShouldBeEmpty(string.Join("; ", bad));
    }

    [AvaloniaFact]
    public void PanelTabs_AreNamedByTheirTitle()
    {
        AvaloniaTabManager tabs = ActivatorUtilities.CreateInstance<AvaloniaTabManager>(TestAppBuilder.Services);
        MainPanelView view = new MainPanelView(tabs);
        Window window = new Window { Content = view, Width = 1000, Height = 700 };
        window.Show();
        tabs.AddControl(new TextBlock(), "Variables", TabLocation.RightTop);
        tabs.AddControl(new TextBlock(), "Output", TabLocation.RightBottom);
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        List<string> tabNames = NamesIn(view).Where(item => item.Control is TabItem).Select(item => item.Name).ToList();

        tabNames.ShouldBe(new[] { "Variables", "Output" }, ignoreOrder: true);
        ShouldHaveNoBadNames(view);
        window.Close();
    }

    [AvaloniaFact]
    public void EditorToolbarPreviewButton_IsNamedPreview()
    {
        EditorToolbar toolbar = new EditorToolbar();
        Window window = new Window { Content = toolbar, Width = 800, Height = 100 };
        window.Show();

        ControlAutomationPeer.CreatePeerForElement(toolbar.PreviewButton).GetName().ShouldBe("Preview in runtime");
        ShouldHaveNoBadNames(toolbar);
        window.Close();
    }

    [AvaloniaFact]
    public void OutputClearButton_IsNamedByItsTooltip()
    {
        OutputView view = new OutputView();
        Window window = new Window { Content = view, Width = 400, Height = 300 };
        window.Show();

        NamesIn(view).Select(item => item.Name).ShouldContain("Clear Output");
        ShouldHaveNoBadNames(view);
        window.Close();
    }

    [AvaloniaFact]
    public void ToolPanels_HaveNoTypeNamesOrAccessKeyMarkupAsNames()
    {
        Control[] views =
        {
            (Control)new AvaloniaElementTreeView().Content, new ErrorsView(), new UndosView(), new AlignmentView(),
            new BehaviorsView(), new HotkeyView(), new FileWatchView(), new PerformanceView(),
        };
        foreach (Control view in views)
        {
            Window window = new Window { Content = view, Width = 500, Height = 400 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            ShouldHaveNoBadNames(view);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void AccessKeyDialogButtons_DropTheUnderscore()
    {
        DeleteOptionsDialogViewModel viewModel = new DeleteOptionsDialogViewModel { Title = "Delete?", Message = "Delete Button?" };
        DialogViewRegistry registry = TestAppBuilder.Services.GetRequiredService<DialogViewRegistry>();
        Control content = registry.CreateView(viewModel);
        DialogWindow window = new DialogWindow(viewModel, content);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        List<string> names = NamesIn(window).Select(item => item.Name).ToList();

        names.ShouldContain("Yes");
        names.ShouldContain("No");
        ShouldHaveNoBadNames(window);
        window.Close();
    }

    [AvaloniaFact]
    public void TreeRows_AreInAutomationOrderMatchingTheirVisualOrder()
    {
        AvaloniaGumTreeView tree = new AvaloniaGumTreeView();
        GumTreeNode screens = new GumTreeNode("Screens");
        screens.Nodes.Add(new GumTreeNode("Alpha"));
        screens.Nodes.Add(new GumTreeNode("Gamma"));
        screens.IsExpanded = true;
        tree.Nodes.Add(screens);
        Window window = new Window { Width = 300, Height = 400, Content = tree };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        // Added after the tree first loaded, between the two existing rows.
        screens.Nodes.Insert(1, new GumTreeNode("Beta"));
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        string[] rowNames = { "Screens", "Alpha", "Beta", "Gamma" };
        List<string> automationOrder = new List<string>();
        void Walk(AutomationPeer peer)
        {
            string name = peer.GetName();
            if (rowNames.Contains(name) && !automationOrder.Contains(name))
            {
                automationOrder.Add(name);
            }
            foreach (AutomationPeer child in peer.GetChildren())
            {
                Walk(child);
            }
        }
        Walk(ControlAutomationPeer.CreatePeerForElement(tree));

        automationOrder.ShouldBe(rowNames);
        window.Close();
    }
}
