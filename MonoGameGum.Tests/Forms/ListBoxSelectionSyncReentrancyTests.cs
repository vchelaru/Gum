using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Xunit;

namespace MonoGameGum.Tests.Forms;

// ListBox keeps SelectedItems and each ListBoxItem's IsSelected in sync behind a reentrancy flag.
// A handler that throws while the flag is set (an item's Selected event, a SelectedItems
// CollectionChanged subscriber) must not leave it set, or selection stops syncing for good.
public class ListBoxSelectionSyncReentrancyTests : BaseTestClass
{
    private static ListBox CreateListBoxWithItems(string[] items)
    {
        ListBox listBox = new();
        listBox.SelectionMode = SelectionMode.Multiple;
        foreach (string item in items)
        {
            listBox.Items!.Add(item);
        }
        return listBox;
    }

    [Fact]
    public void SelectedItemsAdd_ShouldStillSyncIsSelected_AfterAnItemSelectedHandlerThrew()
    {
        ListBox listBox = CreateListBoxWithItems(new[] { "A", "B", "C" });
        EventHandler throwingHandler = (_, _) => throw new InvalidOperationException("handler failed");
        listBox.ListBoxItems[0].Selected += throwingHandler;
        Should.Throw<InvalidOperationException>(() => listBox.SelectedItems.Add("A"));
        listBox.ListBoxItems[0].Selected -= throwingHandler;

        listBox.SelectedItems.Add("C");

        listBox.ListBoxItems[2].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void SelectedObjectSet_ShouldStillSyncIsSelected_AfterAnItemSelectedHandlerThrew()
    {
        ListBox listBox = CreateListBoxWithItems(new[] { "A", "B", "C" });
        EventHandler throwingHandler = (_, _) => throw new InvalidOperationException("handler failed");
        listBox.ListBoxItems[0].Selected += throwingHandler;
        Should.Throw<InvalidOperationException>(() => listBox.SelectedObject = "A");
        listBox.ListBoxItems[0].Selected -= throwingHandler;

        listBox.SelectedItems.Add("C");

        listBox.ListBoxItems[2].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void SelectedItemsSubscriberThrowing_ShouldNotStopSelectionSync_WhenASelectedItemIsRemoved()
    {
        ObservableCollection<string> items = new() { "A", "B", "C" };
        ListBox listBox = new();
        listBox.SelectionMode = SelectionMode.Multiple;
        listBox.Items = items;
        listBox.SelectedItems.Add("A");
        NotifyCollectionChangedEventHandler throwingHandler =
            (_, _) => throw new InvalidOperationException("handler failed");
        ((INotifyCollectionChanged)listBox.SelectedItems).CollectionChanged += throwingHandler;
        Should.Throw<InvalidOperationException>(() => items.Remove("A"));
        ((INotifyCollectionChanged)listBox.SelectedItems).CollectionChanged -= throwingHandler;

        listBox.SelectedItems.Add("C");

        listBox.ListBoxItems[1].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void SelectedItemsSubscriberThrowing_ShouldNotStopSelectionSync_WhenItemsReset()
    {
        ObservableCollection<string> items = new() { "A", "B", "C" };
        ListBox listBox = new();
        listBox.SelectionMode = SelectionMode.Multiple;
        listBox.Items = items;
        listBox.SelectedItems.Add("A");
        NotifyCollectionChangedEventHandler throwingHandler =
            (_, _) => throw new InvalidOperationException("handler failed");
        ((INotifyCollectionChanged)listBox.SelectedItems).CollectionChanged += throwingHandler;
        Should.Throw<InvalidOperationException>(() => items.Clear());
        ((INotifyCollectionChanged)listBox.SelectedItems).CollectionChanged -= throwingHandler;
        items.Add("X");
        items.Add("Y");

        listBox.SelectedItems.Add("Y");

        listBox.ListBoxItems[1].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void ItemsCollectionChanged_ShouldStillReachListBox_AfterADragReorderSubscriberThrew()
    {
        ObservableCollection<string> items = new() { "A", "B", "C" };
        ListBox listBox = new();
        listBox.AddToRoot();
        listBox.Items = items;
        listBox.DragDropReorderMode = DragDropReorderMode.Immediate;
        InteractiveGue dragged = listBox.ListBoxItems[0].Visual;
        GraphicalUiElement target = listBox.ListBoxItems[1].Visual;
        Mock<ICursor> cursor = new();
        cursor.Setup(c => c.LastInputDevice).Returns(InputDevice.Mouse);
        cursor.Setup(c => c.X).Returns((int)(target.AbsoluteX + target.AbsoluteWidth / 2));
        cursor.Setup(c => c.Y).Returns((int)(target.AbsoluteY + target.AbsoluteHeight / 2));
        FrameworkElement.MainCursor = cursor.Object;
        NotifyCollectionChangedEventHandler throwingHandler =
            (_, _) => throw new InvalidOperationException("handler failed");
        items.CollectionChanged += throwingHandler;
        Should.Throw<InvalidOperationException>(() => dragged.TryCallDragging());
        items.CollectionChanged -= throwingHandler;

        items.Add("D");

        listBox.ListBoxItems.Count.ShouldBe(4);
    }
}
