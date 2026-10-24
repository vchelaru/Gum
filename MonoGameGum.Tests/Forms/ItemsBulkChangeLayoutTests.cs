using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Bulk Items changes on ListBox, ItemsControl and MenuItem lay out once rather than once per row, so the
/// layout work grows linearly with the item count (#6705).
/// </summary>
public class ItemsBulkChangeLayoutTests : BaseTestClass
{
    #region Helpers

    private sealed class RangeCollection : ObservableCollection<object>
    {
        public void AddRange(IList<object> toAdd)
        {
            int start = Count;
            foreach (object item in toAdd)
            {
                Items.Add(item);
            }
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, (System.Collections.IList)toAdd, start));
        }
    }

    private static List<object> Strings(int count)
    {
        List<object> list = new List<object>();
        for (int i = 0; i < count; i++)
        {
            list.Add("Item " + i);
        }
        return list;
    }

    private static int CountLayouts(System.Action action)
    {
        int before = GraphicalUiElement.UpdateLayoutCallCount;
        action();
        return GraphicalUiElement.UpdateLayoutCallCount - before;
    }

    private static ListBox CreateListBoxInRoot()
    {
        ListBox listBox = new();
        Gum.GumService.Default.Root.Children!.Add(listBox.Visual);
        return listBox;
    }

    private static MenuItem CreateMenuItemInRoot()
    {
        ContainerRuntime container = new();
        container.Width = 400;
        container.WidthUnits = DimensionUnitType.Absolute;
        container.Height = 40;
        container.HeightUnits = DimensionUnitType.Absolute;
        Gum.GumService.Default.Root.Children!.Add(container);

        Menu menu = new();
        container.Children!.Add(menu.Visual);
        MenuItem file = new() { Header = "File" };
        menu.Items!.Add(file);
        file.Items!.Add("First");
        container.UpdateLayout();
        return file;
    }

    private static List<GraphicalUiElement> PopupRowVisuals(MenuItem owner)
    {
        List<GraphicalUiElement> rows = new List<GraphicalUiElement>();
        foreach (GraphicalUiElement popup in FrameworkElement.PopupRoot.Children!)
        {
            if (popup is InteractiveGue gue && gue.FormsControlAsObject is ScrollViewer scrollViewer)
            {
                foreach (GraphicalUiElement row in scrollViewer.InnerPanel.Children!)
                {
                    if ((row as InteractiveGue)?.FormsControlAsObject is MenuItem rowItem && rowItem.ParentMenuItem == owner)
                    {
                        rows.Add(row);
                    }
                }
            }
        }
        return rows;
    }

    #endregion

    [Fact]
    public void ListBox_ItemsAssigned_LayoutWorkGrowsLinearly()
    {
        ListBox small = CreateListBoxInRoot();
        int smallCount = CountLayouts(() => small.Items = new ObservableCollection<object>(Strings(50)));

        ListBox large = CreateListBoxInRoot();
        int largeCount = CountLayouts(() => large.Items = new ObservableCollection<object>(Strings(100)));

        ((double)largeCount / smallCount).ShouldBeLessThan(3, $"layouts: {smallCount} at n, {largeCount} at 2n");
    }

    [Fact]
    public void ListBox_MultiItemAdd_LayoutWorkGrowsLinearly_AndRowsStack()
    {
        ListBox small = CreateListBoxInRoot();
        RangeCollection smallItems = new();
        small.Items = smallItems;
        int smallCount = CountLayouts(() => smallItems.AddRange(Strings(50)));

        ListBox large = CreateListBoxInRoot();
        RangeCollection largeItems = new();
        large.Items = largeItems;
        int largeCount = CountLayouts(() => largeItems.AddRange(Strings(100)));

        ((double)largeCount / smallCount).ShouldBeLessThan(3, $"layouts: {smallCount} at n, {largeCount} at 2n");
        large.ListBoxItems.Count.ShouldBe(100);
        float rowHeight = large.ListBoxItems[0].Visual.AbsoluteHeight;
        rowHeight.ShouldBeGreaterThan(0);
        for (int i = 1; i < 100; i++)
        {
            large.ListBoxItems[i].Visual.GetAbsoluteTop().ShouldBe(
                large.ListBoxItems[i - 1].Visual.GetAbsoluteBottom(), 0.01f);
        }
    }

    [Fact]
    public void MenuItem_OpenWithManyItems_LayoutWorkGrowsLinearly_AndPopupFitsRows()
    {
        MenuItem small = CreateMenuItemInRoot();
        foreach (object item in Strings(49))
        {
            small.Items!.Add(item);
        }
        int smallCount = CountLayouts(() => small.IsSelected = true);
        small.IsSelected = false;

        MenuItem large = CreateMenuItemInRoot();
        foreach (object item in Strings(99))
        {
            large.Items!.Add(item);
        }
        int largeCount = CountLayouts(() => large.IsSelected = true);

        ((double)largeCount / smallCount).ShouldBeLessThan(3, $"layouts: {smallCount} at n, {largeCount} at 2n");

        List<GraphicalUiElement> rows = PopupRowVisuals(large);
        rows.Count.ShouldBe(100);
        rows[0].AbsoluteHeight.ShouldBeGreaterThan(0);
        for (int i = 1; i < rows.Count; i++)
        {
            rows[i].GetAbsoluteTop().ShouldBe(rows[i - 1].GetAbsoluteBottom(), 0.01f);
        }
        GraphicalUiElement popupPanel = (GraphicalUiElement)rows[0].Parent!;
        popupPanel.AbsoluteHeight.ShouldBe(
            rows[rows.Count - 1].GetAbsoluteBottom() - rows[0].GetAbsoluteTop(), 0.01f);
    }

    [Fact]
    public void MenuItem_MultiItemAddWhileOpen_LayoutWorkGrowsLinearly_AndRowsStack()
    {
        MenuItem small = CreateMenuItemInRoot();
        RangeCollection smallItems = new() { "First" };
        small.Items = smallItems;
        small.IsSelected = true;
        int smallCount = CountLayouts(() => smallItems.AddRange(Strings(50)));
        small.IsSelected = false;

        MenuItem large = CreateMenuItemInRoot();
        RangeCollection largeItems = new() { "First" };
        large.Items = largeItems;
        large.IsSelected = true;
        int largeCount = CountLayouts(() => largeItems.AddRange(Strings(100)));

        ((double)largeCount / smallCount).ShouldBeLessThan(3, $"layouts: {smallCount} at n, {largeCount} at 2n");

        List<GraphicalUiElement> rows = PopupRowVisuals(large);
        rows.Count.ShouldBe(101);
        for (int i = 1; i < rows.Count; i++)
        {
            rows[i].GetAbsoluteTop().ShouldBe(rows[i - 1].GetAbsoluteBottom(), 0.01f);
        }
    }
}
