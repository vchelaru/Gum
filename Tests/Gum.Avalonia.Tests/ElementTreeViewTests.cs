using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
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
    public void LosingCaptureAfterAnExpanderPress_DoesNotSwallowTheNextClick()
    {
        // No release follows a lost capture (the window deactivates mid-press, say), so the
        // "this release came from the expander" note must not survive to the next click.
        (Window window, AvaloniaGumTreeView tree, GumTreeNode screens, GumTreeNode first, _) = CreateTree();
        IPointer? pointer = null;
        window.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer, RoutingStrategies.Tunnel);
        TreeRowView row = RowFor(tree, screens);
        Point point = row.Expander.TranslatePoint(new Point(row.Expander.Bounds.Width / 2, row.Expander.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        screens.IsExpanded.ShouldBeTrue();

        // Releasing the capture raises PointerCaptureLost on the expander and every ancestor, the tree included.
        pointer!.Capture(null);
        Click(window, tree, first, RawInputModifiers.None);

        tree.Selection.SelectedNode.ShouldBe(first);
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

    /// <summary>
    /// #4882 follow-up: inserting a row above the current scroll position (e.g. adding an instance
    /// to a container scrolled out of view above) used to leave the scroll offset untouched, so
    /// every already-visible row silently shifted down by one - eventually pushing whatever the user
    /// was clicking off screen. The row insertion must shift the scroll offset by the same amount so
    /// the already-visible rows stay exactly where they were.
    /// </summary>
    [AvaloniaFact]
    public void InsertingARowAboveTheViewport_KeepsAlreadyVisibleRowsAtTheSameScreenPosition()
    {
        AvaloniaGumTreeView tree = new AvaloniaGumTreeView();
        tree.Selection.IsSelectingOnPush = false;

        GumTreeNode root = new GumTreeNode("Root");
        List<GumTreeNode> children = new List<GumTreeNode>();
        for (int i = 0; i < 20; i++)
        {
            GumTreeNode child = new GumTreeNode($"Child{i}");
            children.Add(child);
            root.Nodes.Add(child);
        }
        tree.Nodes.Add(root);
        root.IsExpanded = true;

        // Short enough that only a handful of the 20 children fit at once.
        Window window = new Window { Width = 300, Height = 120, Content = tree };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        ScrollViewer scrollViewer = window.GetVisualDescendants().OfType<ScrollViewer>().Single();
        double rowHeight = RowFor(tree, children[0]).Bounds.Height;

        // Scroll so Child10 sits at the very top of the viewport, with Child0-9 (and Root) above it,
        // scrolled out of view.
        scrollViewer.Offset = new Vector(0, rowHeight * 10);
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        double anchorYBefore = RowFor(tree, children[10]).TranslatePoint(new Point(0, 0), window)!.Value.Y;

        // Simulates adding an instance to a container that's scrolled above the viewport: a new
        // sibling row lands before every currently-visible row.
        GumTreeNode inserted = new GumTreeNode("Inserted");
        root.Nodes.Insert(0, inserted);
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        double anchorYAfter = RowFor(tree, children[10]).TranslatePoint(new Point(0, 0), window)!.Value.Y;

        anchorYAfter.ShouldBe(anchorYBefore);
        scrollViewer.Offset.Y.ShouldBe(rowHeight * 11);
        window.Close();
    }

    /// <summary>
    /// #4906: dragging near the top/bottom edge auto-scrolls the tree (<c>AutoScrollWhileDragging</c>)
    /// and, in the same pointer-move tick, asks which row is now under the pointer - before the
    /// virtualizing panel's next layout pass has realized rows at their new positions. Hit-testing the
    /// realized visuals at that moment sees stale containers, so the drop target (and with it the drag
    /// cursor and the drop indicator) flickered between found and not-found every tick. <c>NodeAt</c>
    /// must answer correctly from the scroll offset alone, without waiting for a layout pass.
    /// </summary>
    [AvaloniaFact]
    public void NodeAt_ImmediatelyAfterScrollOffsetChanges_FindsTheRowAtTheNewOffset_WithoutALayoutPass()
    {
        AvaloniaGumTreeView tree = new AvaloniaGumTreeView();
        tree.Selection.IsSelectingOnPush = false;

        GumTreeNode root = new GumTreeNode("Root");
        List<GumTreeNode> children = new List<GumTreeNode>();
        for (int i = 0; i < 20; i++)
        {
            GumTreeNode child = new GumTreeNode($"Child{i}");
            children.Add(child);
            root.Nodes.Add(child);
        }
        tree.Nodes.Add(root);
        root.IsExpanded = true;

        // Short enough that only a handful of the 20 children are realized at once.
        Window window = new Window { Width = 300, Height = 120, Content = tree };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        double rowHeight = RowFor(tree, children[0]).Bounds.Height;

        // Scroll ten rows down - deliberately not following this with RunJobs()/UpdateLayout(), the
        // same as HandleDragOver calling GetDropAt right after AutoScrollWhileDragging moves the
        // offset within one DragOver tick. Root is row 0, so Child9 (row 10) lands at the top.
        ScrollViewer scrollViewer = window.GetVisualDescendants().OfType<ScrollViewer>().Single();
        scrollViewer.Offset = new Vector(0, rowHeight * 10);

        GumTreeNode? found = tree.NodeAt(new Point(10, rowHeight / 2));

        found.ShouldBe(children[9]);
        window.Close();
    }

    private static TreeRowView RowFor(AvaloniaGumTreeView tree, GumTreeNode node)
    {
        List<TreeRowView> rows = tree.GetVisualDescendants().OfType<TreeRowView>().ToList();
        List<TreeRowView> matches = rows.Where(view => view.Row?.Node == node).ToList();
        // A missing row is a CI-only flake (#4858); say which stage went wrong rather than just "no match".
        matches.Count.ShouldBe(1,
            $"Row for {node.Text}: {matches.Count} matches, {rows.Count} rows realized, " +
            $"{tree.VisibleNodes.Count} visible nodes, IsMeasureValid {tree.IsMeasureValid}, bounds {tree.Bounds}");
        return matches[0];
    }

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
        // The rows are built on a posted dispatcher job and realized on the layout pass after it;
        // run the layout synchronously too so a row lookup never depends on a render job having run.
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return (window, tree, screens, first, second);
    }

    private static void Click(Window window, AvaloniaGumTreeView tree, GumTreeNode node, RawInputModifiers modifiers)
    {
        TreeRowView row = RowFor(tree, node);
        Point point = row.TranslatePoint(new Point(row.Bounds.Width / 2, row.Bounds.Height / 2), window)!.Value;

        window.MouseDown(point, MouseButton.Left, modifiers);
        window.MouseUp(point, MouseButton.Left, modifiers);
        Dispatcher.UIThread.RunJobs();
    }
}
