using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Collection changes that ObservableCollection never raises (multi-item Remove/Move, Reset with
/// items left) and the user's own SuspendLayout around bulk Items changes (#6710, #6711, #6712).
/// </summary>
public class ItemsControlCollectionChangeTests : BaseTestClass
{
    #region Helpers

    /// <summary>
    /// A collection that changes its contents silently and then raises whatever event the test
    /// gives it, the way a custom INotifyCollectionChanged collection (or a range API) does.
    /// </summary>
    private sealed class RaisingCollection : ObservableCollection<object>
    {
        public RaisingCollection(IEnumerable<object> items) : base(items) { }

        public void RemoveRange(int index, int count)
        {
            List<object> removed = new List<object>();
            for (int i = 0; i < count; i++)
            {
                removed.Add(Items[index]);
                Items.RemoveAt(index);
            }
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove, (IList)removed, index));
        }

        public void InsertRange(int index, IList<object> toAdd)
        {
            for (int i = 0; i < toAdd.Count; i++)
            {
                Items.Insert(index + i, toAdd[i]);
            }
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, (IList)toAdd.ToList(), index));
        }

        public void MoveRange(int oldIndex, int count, int newIndex)
        {
            List<object> moved = new List<object>();
            for (int i = 0; i < count; i++)
            {
                moved.Add(Items[oldIndex]);
                Items.RemoveAt(oldIndex);
            }
            for (int i = 0; i < count; i++)
            {
                Items.Insert(newIndex + i, moved[i]);
            }
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Move, (IList)moved, newIndex, oldIndex));
        }

        public void ReplaceRange(int index, IList<object> newItems)
        {
            List<object> old = new List<object>();
            for (int i = 0; i < newItems.Count; i++)
            {
                old.Add(Items[index + i]);
                Items[index + i] = newItems[i];
            }
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Replace, (IList)newItems.ToList(), (IList)old, index));
        }

        public void SetAllAndReset(IEnumerable<object> newContents)
        {
            Items.Clear();
            foreach (object item in newContents)
            {
                Items.Add(item);
            }
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    private static ListBox CreateListBoxInRoot()
    {
        ListBox listBox = new();
        Gum.GumService.Default.Root.Children!.Add(listBox.Visual);
        return listBox;
    }

    private static List<object?> RowData(ListBox listBox) =>
        listBox.ListBoxItems.Select(row => row.DataObject).ToList();

    private static List<object?> PanelRowData(ListBox listBox) =>
        listBox.InnerPanel.Children!
            .Select(child => ((child as InteractiveGue)?.FormsControlAsObject as ListBoxItem)?.DataObject)
            .ToList();

    #endregion

    #region Remove (#6710)

    [Fact]
    public void MultiItemRemove_RemovesEveryRemovedItemsRow()
    {
        ListBox listBox = CreateListBoxInRoot();
        RaisingCollection items = new(new object[] { "A", "B", "C", "D" });
        listBox.Items = items;

        items.RemoveRange(1, 2);

        RowData(listBox).ShouldBe(new object[] { "A", "D" });
        PanelRowData(listBox).ShouldBe(new object[] { "A", "D" });
    }

    [Fact]
    public void MultiItemRemove_AfterDecoration_RemovesTheRemovedRowsAndKeepsTheDecoration()
    {
        ListBox listBox = CreateListBoxInRoot();
        RaisingCollection items = new(new object[] { "A", "B", "C", "D" });
        listBox.Items = items;
        ContainerRuntime separator = new() { Name = "Separator" };
        listBox.InsertDecorationAfter("A", separator);

        items.RemoveRange(1, 2);

        RowData(listBox).ShouldBe(new object[] { "A", "D" });
        listBox.InnerPanel.Children!.Count.ShouldBe(3);
        listBox.InnerPanel.Children[1].ShouldBeSameAs(separator);
    }

    [Fact]
    public void MultiItemRemove_OfSelectedRow_ClearsSelection()
    {
        ListBox listBox = CreateListBoxInRoot();
        RaisingCollection items = new(new object[] { "A", "B", "C", "D" });
        listBox.Items = items;
        listBox.SelectedObject = "C";

        items.RemoveRange(1, 2);

        listBox.SelectedObject.ShouldBeNull();
        listBox.SelectedItems.Count.ShouldBe(0);
    }

    #endregion

    #region Reset (#6711)

    [Fact]
    public void Reset_WithItemsLeft_RebuildsRowsFromCurrentItems()
    {
        ListBox listBox = CreateListBoxInRoot();
        RaisingCollection items = new(new object[] { "A", "B", "C" });
        listBox.Items = items;

        items.SetAllAndReset(new object[] { "C", "B", "A", "Z" });

        RowData(listBox).ShouldBe(new object[] { "C", "B", "A", "Z" });
        PanelRowData(listBox).ShouldBe(new object[] { "C", "B", "A", "Z" });
    }

    [Fact]
    public void Reset_WithItemsLeft_RowsStack()
    {
        ListBox listBox = CreateListBoxInRoot();
        RaisingCollection items = new(new object[] { "A", "B" });
        listBox.Items = items;

        items.SetAllAndReset(new object[] { "B", "A", "C" });

        var rows = listBox.ListBoxItems;
        rows[1].Visual.AbsoluteTop.ShouldBeGreaterThan(rows[0].Visual.AbsoluteTop);
        rows[2].Visual.AbsoluteTop.ShouldBeGreaterThan(rows[1].Visual.AbsoluteTop);
    }

    [Fact]
    public void Reset_WithListBoxItemsLeft_ShowsThoseRowsInTheNewOrder()
    {
        ListBox listBox = CreateListBoxInRoot();
        ListBoxItem first = new();
        ListBoxItem second = new();
        RaisingCollection items = new(new object[] { first, second });
        listBox.Items = items;

        items.SetAllAndReset(new object[] { second, first });

        listBox.ListBoxItems.ShouldBe(new[] { second, first });
        listBox.InnerPanel.Children!.ShouldBe(new[] { second.Visual, first.Visual });
        listBox.Items.Count.ShouldBe(2);
    }

    [Fact]
    public void Reset_FromClear_LeavesNoRows()
    {
        ListBox listBox = CreateListBoxInRoot();
        ObservableCollection<object> items = new(new object[] { "A", "B" });
        listBox.Items = items;

        items.Clear();

        listBox.ListBoxItems.Count.ShouldBe(0);
        listBox.InnerPanel.Children!.Count.ShouldBe(0);
    }

    [Fact]
    public void ItemsReassigned_ShowsOnlyTheNewCollectionsRows()
    {
        ListBox listBox = CreateListBoxInRoot();
        listBox.Items = new ObservableCollection<object>(new object[] { "A", "B" });

        listBox.Items = new ObservableCollection<object>(new object[] { "X" });

        RowData(listBox).ShouldBe(new object[] { "X" });
        PanelRowData(listBox).ShouldBe(new object[] { "X" });
    }

    #endregion

    #region Add, Move, Replace (same single-index check)

    [Fact]
    public void MultiItemInsert_InMiddle_PlacesRowsInOrder()
    {
        ListBox listBox = CreateListBoxInRoot();
        RaisingCollection items = new(new object[] { "A", "D" });
        listBox.Items = items;

        items.InsertRange(1, new object[] { "B", "C" });

        RowData(listBox).ShouldBe(new object[] { "A", "B", "C", "D" });
        PanelRowData(listBox).ShouldBe(new object[] { "A", "B", "C", "D" });
        listBox.Items.Count.ShouldBe(4);
    }

    [Fact]
    public void MultiItemMove_MovesEveryMovedRow()
    {
        ListBox listBox = CreateListBoxInRoot();
        RaisingCollection items = new(new object[] { "A", "B", "C", "D" });
        listBox.Items = items;

        items.MoveRange(oldIndex: 0, count: 2, newIndex: 2);

        RowData(listBox).ShouldBe(new object[] { "C", "D", "A", "B" });
        PanelRowData(listBox).ShouldBe(new object[] { "C", "D", "A", "B" });
        listBox.Items.Count.ShouldBe(4);
    }

    [Fact]
    public void SingleItemMove_MovesTheRowAndKeepsSelection()
    {
        ListBox listBox = CreateListBoxInRoot();
        ObservableCollection<object> items = new(new object[] { "A", "B", "C" });
        listBox.Items = items;
        listBox.SelectedObject = "A";

        items.Move(0, 2);

        RowData(listBox).ShouldBe(new object[] { "B", "C", "A" });
        PanelRowData(listBox).ShouldBe(new object[] { "B", "C", "A" });
        listBox.SelectedObject.ShouldBe("A");
    }

    [Fact]
    public void Replace_ByIndexer_ShowsTheNewItem()
    {
        ListBox listBox = CreateListBoxInRoot();
        ObservableCollection<object> items = new(new object[] { "A", "B", "C" });
        listBox.Items = items;

        items[1] = "X";

        RowData(listBox).ShouldBe(new object[] { "A", "X", "C" });
        PanelRowData(listBox).ShouldBe(new object[] { "A", "X", "C" });
        listBox.Items.Count.ShouldBe(3);
    }

    [Fact]
    public void MultiItemReplace_ShowsEveryNewItem()
    {
        ListBox listBox = CreateListBoxInRoot();
        RaisingCollection items = new(new object[] { "A", "B", "C", "D" });
        listBox.Items = items;

        items.ReplaceRange(1, new object[] { "X", "Y" });

        RowData(listBox).ShouldBe(new object[] { "A", "X", "Y", "D" });
        PanelRowData(listBox).ShouldBe(new object[] { "A", "X", "Y", "D" });
        listBox.Items.Count.ShouldBe(4);
    }

    [Fact]
    public void Replace_OfSelectedItem_ClearsSelection()
    {
        ListBox listBox = CreateListBoxInRoot();
        ObservableCollection<object> items = new(new object[] { "A", "B", "C" });
        listBox.Items = items;
        listBox.SelectedObject = "B";

        items[1] = "X";

        listBox.SelectedObject.ShouldBeNull();
    }

    #endregion

    #region Layout outside InnerPanel after a bulk change

    /// <summary>
    /// A ListBox sized to its rows, stacked above a sibling in a parent that sizes to its children,
    /// so a row change has to move work outside InnerPanel (the ListBox, the sibling, the parent).
    /// </summary>
    private static (ListBox listBox, ContainerRuntime parent, ContainerRuntime sibling) CreateContentSizedListBoxInStack(
        RaisingCollection items)
    {
        ContainerRuntime parent = new();
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        parent.Height = 0;
        parent.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        Gum.GumService.Default.Root.Children!.Add(parent);

        ListBox listBox = new();
        for (GraphicalUiElement? element = listBox.InnerPanel; element != null; element = element.Parent as GraphicalUiElement)
        {
            element.Height = 0;
            element.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            if (element == listBox.Visual)
            {
                break;
            }
        }
        parent.Children!.Add(listBox.Visual);

        ContainerRuntime sibling = new() { Height = 10, HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute };
        parent.Children.Add(sibling);

        listBox.Items = items;
        return (listBox, parent, sibling);
    }

    public static IEnumerable<object[]> BulkChanges()
    {
        yield return new object[] { "Reset" };
        yield return new object[] { "MultiRemove" };
        yield return new object[] { "MultiInsert" };
        yield return new object[] { "Replace" };
        yield return new object[] { "MultiMove" };
        yield return new object[] { "Add" };
    }

    [Theory]
    [MemberData(nameof(BulkChanges))]
    public void BulkChange_OnContentSizedListBoxInStack_LaysOutTheListBoxAndItsSiblings(string change)
    {
        RaisingCollection items = new(new object[] { "A", "B", "C" });
        var (listBox, parent, sibling) = CreateContentSizedListBoxInStack(items);
        float heightBefore = listBox.Visual.GetAbsoluteHeight();
        heightBefore.ShouldBeGreaterThan(0);

        switch (change)
        {
            case "Reset": items.SetAllAndReset(new object[] { "A", "B", "C", "D", "E" }); break;
            case "MultiRemove": items.RemoveRange(0, 2); break;
            case "MultiInsert": items.InsertRange(1, new object[] { "X", "Y" }); break;
            case "Replace": items.ReplaceRange(0, new object[] { "X", "Y" }); break;
            case "MultiMove": items.MoveRange(oldIndex: 0, count: 2, newIndex: 1); break;
            case "Add": items.Add("D"); break;
        }

        float listBoxHeight = listBox.Visual.GetAbsoluteHeight();
        float siblingTop = sibling.AbsoluteTop;
        float parentHeight = parent.GetAbsoluteHeight();
        float lastRowBottom = listBox.ListBoxItems[^1].Visual.AbsoluteBottom;

        // What a full layout from scratch produces is the reference: deferred work left unflushed
        // anywhere in the tree shows up as a difference.
        parent.UpdateLayout();

        listBox.Visual.GetAbsoluteHeight().ShouldBe(listBoxHeight);
        sibling.AbsoluteTop.ShouldBe(siblingTop);
        parent.GetAbsoluteHeight().ShouldBe(parentHeight);
        listBox.ListBoxItems[^1].Visual.AbsoluteBottom.ShouldBe(lastRowBottom);
        sibling.AbsoluteTop.ShouldBe(listBox.Visual.AbsoluteBottom);
        if (change is "Reset" or "MultiRemove" or "MultiInsert" or "Add")
        {
            listBoxHeight.ShouldNotBe(heightBefore);
        }
    }

    #endregion

    #region User SuspendLayout (#6712)

    [Fact]
    public void ItemsAssigned_InsideUserSuspendLayout_KeepsTheVisualSuspended_UntilUserResumes()
    {
        ListBox listBox = CreateListBoxInRoot();
        listBox.Visual.SuspendLayout();

        listBox.Items = new ObservableCollection<object>(new object[] { "A", "B", "C" });

        listBox.Visual.IsLayoutSuspended.ShouldBeTrue();

        listBox.Visual.ResumeLayout();

        listBox.Visual.IsLayoutSuspended.ShouldBeFalse();
        var rows = listBox.ListBoxItems;
        rows[1].Visual.AbsoluteTop.ShouldBeGreaterThan(rows[0].Visual.AbsoluteTop);
        rows[2].Visual.AbsoluteTop.ShouldBeGreaterThan(rows[1].Visual.AbsoluteTop);
    }

    [Fact]
    public void ItemsAssigned_InsideUserRecursiveSuspendLayout_KeepsEverythingSuspended_UntilUserResumes()
    {
        ListBox listBox = CreateListBoxInRoot();
        listBox.Visual.SuspendLayout(recursive: true);

        listBox.Items = new ObservableCollection<object>(new object[] { "A", "B", "C" });

        listBox.Visual.IsLayoutSuspended.ShouldBeTrue();
        listBox.InnerPanel.IsLayoutSuspended.ShouldBeTrue();

        listBox.Visual.ResumeLayout(recursive: true);

        var rows = listBox.ListBoxItems;
        rows[1].Visual.AbsoluteTop.ShouldBeGreaterThan(rows[0].Visual.AbsoluteTop);
        rows[2].Visual.AbsoluteTop.ShouldBeGreaterThan(rows[1].Visual.AbsoluteTop);
    }

    [Fact]
    public void ItemAdded_InsideUserRecursiveSuspendLayout_KeepsInnerPanelSuspended()
    {
        ListBox listBox = CreateListBoxInRoot();
        listBox.Items!.Add("A");
        listBox.Visual.SuspendLayout(recursive: true);

        listBox.Items.Add("B");

        listBox.InnerPanel.IsLayoutSuspended.ShouldBeTrue();

        listBox.Visual.ResumeLayout(recursive: true);

        var rows = listBox.ListBoxItems;
        rows[1].Visual.AbsoluteTop.ShouldBeGreaterThan(rows[0].Visual.AbsoluteTop);
    }

    [Fact]
    public void ItemsAssigned_WithUserSuspendedInnerPanel_LaysOutTheRestAndKeepsThePanelSuspended()
    {
        ListBox listBox = CreateListBoxInRoot();
        listBox.InnerPanel.SuspendLayout();

        listBox.Items = new ObservableCollection<object>(new object[] { "A", "B", "C" });

        listBox.Visual.IsLayoutSuspended.ShouldBeFalse();
        listBox.InnerPanel.IsLayoutSuspended.ShouldBeTrue();

        listBox.InnerPanel.ResumeLayout();

        var rows = listBox.ListBoxItems;
        rows[1].Visual.AbsoluteTop.ShouldBeGreaterThan(rows[0].Visual.AbsoluteTop);
        rows[2].Visual.AbsoluteTop.ShouldBeGreaterThan(rows[1].Visual.AbsoluteTop);
    }

    [Fact]
    public void FrameworkElementTemplateSet_InsideUserSuspendLayout_KeepsTheVisualSuspended()
    {
        ListBox listBox = CreateListBoxInRoot();
        listBox.Items = new ObservableCollection<object>(new object[] { "A", "B" });
        listBox.Visual.SuspendLayout();

        listBox.FrameworkElementTemplate = new Gum.Forms.FrameworkElementTemplate(typeof(ListBoxItem));

        listBox.Visual.IsLayoutSuspended.ShouldBeTrue();
        listBox.Visual.ResumeLayout();
        var rows = listBox.ListBoxItems;
        rows[1].Visual.AbsoluteTop.ShouldBeGreaterThan(rows[0].Visual.AbsoluteTop);
    }

    #endregion
}
