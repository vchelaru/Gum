using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Plugins.TreeView;
using Gum.Managers;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The Avalonia element tree control: its rows follow the node model, and pointer and key input
/// reach the shared selection model the way they do in the WPF tree.
/// </summary>
public class ElementTreeViewTests
{
    [AvaloniaFact]
    public void EveryCatalogIcon_ResolvesToLinkedArtwork()
    {
        List<string> missing = TreeIconCatalog.Icons.Values
            .Where(definition => !AssetLoader.Exists(AvaloniaTreeIcons.GetResourceUri(definition)))
            .Select(definition => definition.RelativePath)
            .ToList();

        missing.ShouldBeEmpty(string.Join(", ", missing));
    }

    [AvaloniaFact]
    public void ExpandingANode_ShowsItsChildren_AndCollapsingHidesThem()
    {
        (Window window, AvaloniaGumTreeView tree, GumTreeNode screens, GumTreeNode first, GumTreeNode second) = CreateTree();

        screens.IsExpanded = true;
        Dispatcher.UIThread.RunJobs();
        tree.VisibleNodes.ShouldBe(new[] { screens, first, second });

        screens.IsExpanded = false;
        Dispatcher.UIThread.RunJobs();
        tree.VisibleNodes.ShouldBe(new[] { screens });

        window.Close();
    }

    [AvaloniaFact]
    public void AddingAChildToAnExpandedNode_AddsItsRow()
    {
        (Window window, AvaloniaGumTreeView tree, GumTreeNode screens, GumTreeNode first, GumTreeNode second) = CreateTree();
        screens.IsExpanded = true;
        Dispatcher.UIThread.RunJobs();

        GumTreeNode third = new GumTreeNode("Third");
        screens.Nodes.Add(third);
        Dispatcher.UIThread.RunJobs();

        tree.VisibleNodes.ShouldBe(new[] { screens, first, second, third });
        window.Close();
    }

    [AvaloniaFact]
    public void ClickingARow_SelectsItsNode()
    {
        (Window window, AvaloniaGumTreeView tree, GumTreeNode screens, GumTreeNode first, _) = CreateTree();
        screens.IsExpanded = true;
        Dispatcher.UIThread.RunJobs();

        Click(window, tree, first, RawInputModifiers.None);

        tree.Selection.SelectedNode.ShouldBe(first);
        first.IsSelected.ShouldBeTrue();
        window.Close();
    }

    [AvaloniaFact]
    public void CtrlClick_AddsToTheSelection()
    {
        (Window window, AvaloniaGumTreeView tree, GumTreeNode screens, GumTreeNode first, GumTreeNode second) = CreateTree();
        screens.IsExpanded = true;
        Dispatcher.UIThread.RunJobs();

        Click(window, tree, first, RawInputModifiers.None);
        Click(window, tree, second, RawInputModifiers.Control);

        tree.Selection.SelectedNodes.ShouldBe(new[] { first, second });
        window.Close();
    }

    [AvaloniaFact]
    public void DownArrow_MovesTheSelectionToTheNextVisibleNode()
    {
        (Window window, AvaloniaGumTreeView tree, GumTreeNode screens, GumTreeNode first, GumTreeNode second) = CreateTree();
        screens.IsExpanded = true;
        Dispatcher.UIThread.RunJobs();
        Click(window, tree, first, RawInputModifiers.None);

        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        Dispatcher.UIThread.RunJobs();

        tree.Selection.SelectedNode.ShouldBe(second);
        window.Close();
    }

    [AvaloniaFact]
    public void ClickingTheExpander_TogglesTheNode_WithoutSelectingIt()
    {
        (Window window, AvaloniaGumTreeView tree, GumTreeNode screens, _, _) = CreateTree();
        TreeRowView row = RowFor(tree, screens);
        Point point = row.Expander.TranslatePoint(new Point(row.Expander.Bounds.Width / 2, row.Expander.Bounds.Height / 2), window)!.Value;

        window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        screens.IsExpanded.ShouldBeTrue();
        tree.Selection.SelectedNode.ShouldBeNull();
        window.Close();
    }

    [AvaloniaFact]
    public void RightClickingTheExpander_IsAClickOnTheRow()
    {
        (Window window, AvaloniaGumTreeView tree, GumTreeNode screens, _, _) = CreateTree();
        int requests = 0;
        tree.ContextMenuRequested += () => requests++;
        TreeRowView row = RowFor(tree, screens);
        Point point = row.Expander.TranslatePoint(new Point(row.Expander.Bounds.Width / 2, row.Expander.Bounds.Height / 2), window)!.Value;

        window.MouseDown(point, MouseButton.Right, RawInputModifiers.None);
        window.MouseUp(point, MouseButton.Right, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        screens.IsExpanded.ShouldBeFalse();
        tree.Selection.SelectedNode.ShouldBe(screens);
        requests.ShouldBe(1);
        window.Close();
    }

    [AvaloniaFact]
    public void RightClick_AnywhereOnTheRow_SelectsItsNode_AndRequestsTheMenu()
    {
        (Window window, AvaloniaGumTreeView tree, GumTreeNode screens, GumTreeNode first, _) = CreateTree();
        screens.IsExpanded = true;
        Dispatcher.UIThread.RunJobs();
        int requests = 0;
        tree.ContextMenuRequested += () => requests++;
        TreeRowView row = RowFor(tree, first);
        // Rows span the tree, so the empty space past the text belongs to the row too.
        row.Bounds.Width.ShouldBeGreaterThan(250);
        Point point = row.TranslatePoint(new Point(row.Bounds.Width - 4, row.Bounds.Height / 2), window)!.Value;

        window.MouseDown(point, MouseButton.Right, RawInputModifiers.None);
        window.MouseUp(point, MouseButton.Right, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        tree.Selection.SelectedNode.ShouldBe(first);
        requests.ShouldBe(1);
        window.Close();
    }

    [AvaloniaFact]
    public void SelectedRow_HighlightsFromItsIndent_ToTheRowsEdge()
    {
        (Window window, AvaloniaGumTreeView tree, GumTreeNode screens, GumTreeNode first, _) = CreateTree();
        screens.IsExpanded = true;
        Dispatcher.UIThread.RunJobs();
        Click(window, tree, first, RawInputModifiers.None);
        window.UpdateLayout();

        // As the WPF row: the box starts after the parent's indent, not at the tree's edge.
        TreeRowView row = RowFor(tree, first);
        Border highlight = row.Highlight;
        Point origin = highlight.TranslatePoint(new Point(0, 0), row)!.Value;
        origin.X.ShouldBe(TreeRowView.Indent);
        (origin.X + highlight.Bounds.Width).ShouldBe(row.Bounds.Width);
        highlight.BorderBrush.ShouldNotBeSameAs(Brushes.Transparent);
        RowFor(tree, screens).Highlight.BorderBrush.ShouldBeSameAs(Brushes.Transparent);
        window.Close();
    }

    private static TreeRowView RowFor(AvaloniaGumTreeView tree, GumTreeNode node) =>
        tree.GetVisualDescendants().OfType<TreeRowView>().Single(view => view.Row?.Node == node);

    private static (Window Window, AvaloniaGumTreeView Tree, GumTreeNode Screens, GumTreeNode First, GumTreeNode Second) CreateTree()
    {
        AvaloniaGumTreeView tree = new AvaloniaGumTreeView();
        tree.Selection.IsSelectingOnPush = false;

        GumTreeNode screens = new GumTreeNode("Screens");
        GumTreeNode first = new GumTreeNode("First");
        GumTreeNode second = new GumTreeNode("Second");
        screens.Nodes.Add(first);
        screens.Nodes.Add(second);
        tree.Nodes.Add(screens);

        Window window = new Window { Width = 300, Height = 400, Content = tree };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, tree, screens, first, second);
    }

    private static void Click(Window window, AvaloniaGumTreeView tree, GumTreeNode node, RawInputModifiers modifiers)
    {
        TreeRowView row = tree.GetVisualDescendants().OfType<TreeRowView>().Single(view => view.Row?.Node == node);
        Point point = row.TranslatePoint(new Point(row.Bounds.Width / 2, row.Bounds.Height / 2), window)!.Value;

        window.MouseDown(point, MouseButton.Left, modifiers);
        window.MouseUp(point, MouseButton.Left, modifiers);
        Dispatcher.UIThread.RunJobs();
    }
}
