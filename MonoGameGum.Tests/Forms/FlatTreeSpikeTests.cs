using Gum.Forms;
using Gum.Forms.Controls;
using Shouldly;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Throwaway spike for #6148: can a tree be a flat ListBox whose Items are the visible nodes,
/// with expand/collapse inserting and removing rows? Not product code.
/// </summary>
public class FlatTreeSpikeTests : BaseTestClass
{
    class Node
    {
        public string Header { get; }
        public int Depth { get; set; }
        public bool IsExpanded { get; set; }
        public List<Node> Children { get; } = new();
        public Node(string header) { Header = header; }
        public Node Add(Node child) { child.Depth = Depth + 1; Children.Add(child); return child; }
        public override string ToString() => Header;
    }

    class FlatTree
    {
        public ListBox ListBox { get; } = new();
        public ObservableCollection<Node> Rows { get; } = new();

        public FlatTree(params Node[] roots)
        {
            ListBox.DisplayMemberPath = nameof(Node.Header);
            ListBox.Items = Rows;
            foreach (Node root in roots) Rows.Add(root);
        }

        public void SetExpanded(Node node, bool expanded)
        {
            if (node.IsExpanded == expanded) return;
            node.IsExpanded = expanded;
            int index = Rows.IndexOf(node);
            if (index < 0) return; // hidden under a collapsed ancestor
            Gum.Wireframe.GraphicalUiElement.RunWithAllLayoutSuspended(() =>
            {
                if (expanded)
                {
                    int at = index + 1;
                    foreach (Node visible in Flatten(node.Children)) Rows.Insert(at++, visible);
                }
                else
                {
                    int count = Flatten(node.Children).Count();
                    for (int i = 0; i < count; i++) Rows.RemoveAt(index + 1);
                }
            }, ListBox.Visual);
        }

        static IEnumerable<Node> Flatten(IEnumerable<Node> nodes)
        {
            foreach (Node n in nodes)
            {
                yield return n;
                if (n.IsExpanded) foreach (Node d in Flatten(n.Children)) yield return d;
            }
        }
    }

    static (FlatTree tree, Node a, Node a1, Node a2, Node a2x, Node b) Build()
    {
        Node a = new("A"), b = new("B");
        Node a1 = a.Add(new Node("A1"));
        Node a2 = a.Add(new Node("A2"));
        Node a2x = a2.Add(new Node("A2x"));
        return (new FlatTree(a, b), a, a1, a2, a2x, b);
    }

    [Fact]
    public void Expand_ShouldInsertDescendantsAfterParent_AndKeepListBoxItemsInSync()
    {
        var (tree, a, a1, a2, a2x, b) = Build();
        a2.IsExpanded = true; // pre-expanded child must flatten recursively when A opens

        tree.SetExpanded(a, true);

        tree.Rows.Select(n => n.Header).ShouldBe(new[] { "A", "A1", "A2", "A2x", "B" });
        tree.ListBox.ListBoxItems.Count.ShouldBe(5);
        tree.ListBox.ListBoxItems.Select(i => i.ToString()).Count().ShouldBe(5);
    }

    [Fact]
    public void Collapse_ShouldKeepSiblingRowInstances()
    {
        var (tree, a, a1, a2, a2x, b) = Build();
        tree.SetExpanded(a, true);
        ListBoxItem bRowBefore = tree.ListBox.ListBoxItems[3];

        tree.SetExpanded(a, false);

        tree.ListBox.ListBoxItems.Count.ShouldBe(2);
        tree.ListBox.ListBoxItems[1].ShouldBeSameAs(bRowBefore);
    }

    [Fact]
    public void Expand_AboveSelectedRow_ShouldKeepSelectionOnSameNode()
    {
        var (tree, a, a1, a2, a2x, b) = Build();
        tree.ListBox.SelectedObject = b;

        tree.SetExpanded(a, true);

        tree.ListBox.SelectedObject.ShouldBeSameAs(b);
        tree.ListBox.SelectedIndex.ShouldBe(3);
    }

    [Fact]
    public void Collapse_WithSelectedDescendant_ShouldReportDeselection()
    {
        var (tree, a, a1, a2, a2x, b) = Build();
        tree.SetExpanded(a, true);
        tree.ListBox.SelectedObject = a1;
        int selectionChangedCount = 0;
        object? removed = null;
        tree.ListBox.SelectionChanged += (_, args) =>
        {
            selectionChangedCount++;
            removed = args.RemovedItems.Cast<object>().FirstOrDefault();
        };

        tree.SetExpanded(a, false);

        selectionChangedCount.ShouldBeGreaterThan(0);
        removed.ShouldBeSameAs(a1);
        tree.ListBox.SelectedObject.ShouldBeNull();
    }

    [Fact]
    public void VisualTemplate_ShouldIndentRowByNodeDepth()
    {
        var (tree, a, a1, a2, a2x, b) = Build();
        tree.ListBox.VisualTemplate = new VisualTemplate((object? o, bool createForms) =>
        {
            Node node = (Node)o!;
            // Forms object must exist: ListBox does not wrap a Forms-less template visual (see next test).
            var visual = (Gum.Forms.DefaultVisuals.V3.ListBoxItemVisual)new ListBoxItem().Visual;
            visual.TextInstance.X = 4 + node.Depth * 16;
            return visual;
        });
        a2.IsExpanded = true;
        tree.SetExpanded(a, true);

        var indents = tree.ListBox.ListBoxItems
            .Select(i => ((Gum.Forms.DefaultVisuals.V3.ListBoxItemVisual)i.Visual).TextInstance.X).ToArray();

        indents.ShouldBe(new float[] { 4, 20, 20, 36, 4 });
        tree.ListBox.ListBoxItems.Count.ShouldBe(5);
    }

    [Fact]
    public void VisualTemplate_WithoutFormsObject_AsDocumented_RecordsRowCount()
    {
        var (tree, a, a1, a2, a2x, b) = Build();
        tree.ListBox.VisualTemplate = new VisualTemplate((object? o, bool createForms) =>
            new Gum.Forms.DefaultVisuals.V3.ListBoxItemVisual(true, createForms));
        tree.SetExpanded(a, true);

        // Spike result: rows=4, listBoxItems=0, panelChildren=4 (documented Forms-less template leaves ListBoxItems empty).
        tree.Rows.Count.ShouldBe(4);
    }

    [Fact]
    public void Expand_OneThousandChildren_ShouldFinish_AndLogTime()
    {
        Node root = new("root");
        for (int i = 0; i < 1000; i++) root.Add(new Node("c" + i));
        FlatTree tree = new(root);

        var watch = System.Diagnostics.Stopwatch.StartNew();
        tree.SetExpanded(root, true);
        long expandMs = watch.ElapsedMilliseconds;
        watch.Restart();
        tree.SetExpanded(root, false);
        long collapseMs = watch.ElapsedMilliseconds;

        // Spike result (headless, layout suspended): expand 1000 = 67ms, collapse 1000 = 13ms; unsuspended 882ms / 3896ms.
        expandMs.ShouldBeLessThan(5000);
        collapseMs.ShouldBeLessThan(5000);
        tree.ListBox.ListBoxItems.Count.ShouldBe(1);
    }
}
