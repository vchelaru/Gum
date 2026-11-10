using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.Forms.Controls;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// What a ComboBox drop-down looks like each time it opens, closes and opens again, as a text snapshot of
/// the popup (size, position, parent, layout values, rows, selection, scroll, focus) recorded from the
/// code before the open path was changed to allocate less (#7870). A reopened drop-down must also equal
/// a snapshot of one opened for the first time with the same items, so state cannot leak across opens.
/// Boundary: ComboBox open/close + ListBox layout; Forms only, no input pump.
/// </summary>
public class ComboBoxReopenCharacterizationTests : BaseTestClass
{
    private static ComboBox CreateComboBox(int itemCount, bool addToRoot = true)
    {
        ComboBox comboBox = new();
        comboBox.X = 20;
        comboBox.Y = 30;
        if (addToRoot)
        {
            comboBox.AddToRoot();
        }
        for (int i = 0; i < itemCount; i++)
        {
            comboBox.Items!.Add("Item " + i);
        }
        return comboBox;
    }

    private static string Snapshot(ComboBox comboBox)
    {
        StringBuilder text = new();
        ListBox listBox = comboBox.ListBox;
        GraphicalUiElement visual = listBox.Visual;
        GraphicalUiElement comboVisual = comboBox.Visual;

        text.AppendLine($"combo open={comboBox.IsDropDownOpen} text='{comboBox.Text}' selected={comboBox.SelectedIndex} " +
            $"rect={Rect(comboVisual)}");
        text.AppendLine($"list visible={listBox.IsVisible} parent={ParentDescription(visual)} rect={Rect(visual)} " +
            $"units={visual.XUnits}/{visual.YUnits}/{visual.WidthUnits}/{visual.HeightUnits} " +
            $"values={visual.X},{visual.Y},{visual.Width},{visual.Height} z={visual.Z}");
        text.AppendLine($"list focused={listBox.IsFocused} itemsFocus={listBox.DoListItemsHaveFocus} " +
            $"selected={listBox.SelectedIndex} scroll={listBox.VerticalScrollBarValue:0.##}/{listBox.VerticalScrollBarMaximum:0.##}");
        int outside = 0;
        for (int i = 0; i < listBox.ListBoxItems.Count; i++)
        {
            ListBoxItem row = listBox.ListBoxItems[i];
            if (row.Visual.AbsoluteBottom <= visual.AbsoluteTop || row.Visual.AbsoluteTop >= visual.AbsoluteBottom)
            {
                outside++;
                continue;
            }
            text.AppendLine($"  row {i} '{RowText(row)}' rect={Rect(row.Visual)} selected={row.IsSelected} " +
                $"highlighted={row.IsHighlighted} enabled={row.IsEnabled}");
        }
        text.AppendLine($"  rows outside the list: {outside}");
        return text.ToString().Replace("\r\n", "\n").TrimEnd();
    }

    private static string Rect(GraphicalUiElement element) =>
        $"({element.AbsoluteLeft:0.##},{element.AbsoluteTop:0.##},{element.GetAbsoluteWidth():0.##},{element.GetAbsoluteHeight():0.##})";

    private static string ParentDescription(GraphicalUiElement element)
    {
        GraphicalUiElement? parent = element.Parent as GraphicalUiElement;
        if (parent == null)
        {
            return "none";
        }
        return parent == Gum.Forms.Controls.FrameworkElement.PopupRoot ? "PopupRoot"
            : parent == Gum.Forms.Controls.FrameworkElement.ModalRoot ? "ModalRoot"
            : parent.Name ?? parent.GetType().Name;
    }

    private static string RowText(ListBoxItem row)
    {
        List<IText> texts = new();
        Collect(row.Visual, texts);
        return string.Join("|", texts.Select(t => t.RawText));
    }

    private static void Collect(GraphicalUiElement element, List<IText> into)
    {
        if (element.RenderableComponent is IText text)
        {
            into.Add(text);
        }
        if (element.Children != null)
        {
            foreach (GraphicalUiElement child in element.Children)
            {
                Collect(child, into);
            }
        }
    }

    // The same ComboBox, built fresh and opened once, as the reference a reopened one must equal.
    private static string FreshOpenSnapshot(int itemCount, System.Action<ComboBox>? arrange = null)
    {
        ComboBox fresh = CreateComboBox(itemCount);
        arrange?.Invoke(fresh);
        fresh.IsDropDownOpen = true;
        string snapshot = Snapshot(fresh);
        fresh.IsDropDownOpen = false;
        fresh.RemoveFromRoot();
        return snapshot;
    }

    [Fact]
    public void Open_FourItems_PlacesPopupBelowComboBoxInPopupRoot()
    {
        ComboBox comboBox = CreateComboBox(4);

        comboBox.IsDropDownOpen = true;

        Snapshot(comboBox).ShouldBe(Expected.OpenFourItems);
    }

    [Fact]
    public void Close_FourItems_RestoresLayoutValuesAndHidesPopup()
    {
        ComboBox comboBox = CreateComboBox(4);
        comboBox.IsDropDownOpen = true;

        comboBox.IsDropDownOpen = false;

        Snapshot(comboBox).ShouldBe(Expected.ClosedFourItems);
    }

    [Fact]
    public void Reopen_ThreeCycles_EqualsFirstOpen()
    {
        ComboBox comboBox = CreateComboBox(4);
        comboBox.IsDropDownOpen = true;
        string first = Snapshot(comboBox);

        for (int i = 0; i < 3; i++)
        {
            comboBox.IsDropDownOpen = false;
            Snapshot(comboBox).ShouldBe(Expected.ClosedFourItems);
            comboBox.IsDropDownOpen = true;
            Snapshot(comboBox).ShouldBe(first);
        }
    }

    [Fact]
    public void Reopen_ItemsChangedWhileClosed_ShowsNewItemsInOrder()
    {
        ComboBox comboBox = CreateComboBox(4);
        comboBox.IsDropDownOpen = true;
        comboBox.IsDropDownOpen = false;

        comboBox.Items!.Add("Added");
        comboBox.Items.RemoveAt(0);
        comboBox.Items.Insert(1, "Inserted");
        comboBox.IsDropDownOpen = true;

        string snapshot = Snapshot(comboBox);
        snapshot.ShouldBe(Expected.ReopenAfterItemsChangedClosed);
        snapshot.ShouldBe(FreshOpenSnapshot(0, fresh =>
        {
            foreach (string item in new[] { "Item 1", "Inserted", "Item 2", "Item 3", "Added" })
            {
                fresh.Items!.Add(item);
            }
        }));
    }

    [Fact]
    public void Open_ItemsChangedWhileOpen_RowsFollowTheItems()
    {
        ComboBox comboBox = CreateComboBox(4);
        comboBox.IsDropDownOpen = true;

        comboBox.Items!.Add("Late");
        comboBox.Items.RemoveAt(1);

        Snapshot(comboBox).ShouldBe(Expected.ItemsChangedWhileOpen);

        comboBox.IsDropDownOpen = false;
        comboBox.IsDropDownOpen = true;
        Snapshot(comboBox).ShouldBe(Expected.ItemsChangedWhileOpenThenReopened);
    }

    [Fact]
    public void Reopen_SelectedIndexChangedWhileClosed_HighlightsAndScrollsToIt()
    {
        ComboBox comboBox = CreateComboBox(30);
        comboBox.IsDropDownOpen = true;
        comboBox.IsDropDownOpen = false;

        comboBox.SelectedIndex = 27;
        comboBox.IsDropDownOpen = true;

        string snapshot = Snapshot(comboBox);
        snapshot.ShouldBe(Expected.ReopenSelectedFar);
        snapshot.ShouldBe(FreshOpenSnapshot(30, fresh => fresh.SelectedIndex = 27));
    }

    [Fact]
    public void Reopen_SelectedIndexMovesBetweenOpens_ScrollFollowsEachTime()
    {
        ComboBox comboBox = CreateComboBox(30);
        List<string> snapshots = new();

        foreach (int index in new[] { 27, 2, 15, -1, 29 })
        {
            comboBox.IsDropDownOpen = false;
            comboBox.SelectedIndex = index;
            comboBox.IsDropDownOpen = true;
            snapshots.Add($"{index}: scroll={comboBox.ListBox.VerticalScrollBarValue:0.##} " +
                $"selected={comboBox.ListBox.SelectedIndex} rect={Rect(comboBox.ListBox.Visual)}");
        }

        string.Join("\n", snapshots).ShouldBe(Expected.SelectedMovesBetweenOpens);
    }

    [Fact]
    public void Open_DisabledItem_RowStaysDisabledAcrossReopen()
    {
        ComboBox comboBox = CreateComboBox(4);
        comboBox.IsDropDownOpen = true;
        comboBox.ListBox.ListBoxItems[2].IsEnabled = false;
        comboBox.IsDropDownOpen = false;

        comboBox.IsDropDownOpen = true;

        Snapshot(comboBox).ShouldBe(Expected.DisabledItem);
    }

    [Fact]
    public void Open_TallDropDown_ShrinksToRootAndReopensTheSame()
    {
        ComboBox comboBox = CreateComboBox(60);
        comboBox.IsDropDownOpen = true;
        string first = Snapshot(comboBox);

        comboBox.IsDropDownOpen = false;
        comboBox.IsDropDownOpen = true;

        Snapshot(comboBox).ShouldBe(first);
        first.ShouldBe(Expected.TallDropDown);
    }

    [Fact]
    public void Open_ComboBoxMovedWhileClosed_PopupFollowsTheNewPosition()
    {
        ComboBox comboBox = CreateComboBox(4);
        comboBox.IsDropDownOpen = true;
        comboBox.IsDropDownOpen = false;

        comboBox.X = 140;
        comboBox.Y = 90;
        comboBox.Width = 180;
        comboBox.IsDropDownOpen = true;

        Snapshot(comboBox).ShouldBe(Expected.MovedWhileClosed);
    }

    [Fact]
    public void Open_NearBottomOfScreen_PopupIsKeptInsideTheCanvas()
    {
        ComboBox comboBox = CreateComboBox(8);
        comboBox.Y = 560;
        comboBox.IsDropDownOpen = true;
        string first = Snapshot(comboBox);

        comboBox.IsDropDownOpen = false;
        comboBox.IsDropDownOpen = true;

        Snapshot(comboBox).ShouldBe(first);
        first.ShouldBe(Expected.NearBottom);
    }

    [Fact]
    public void Reopen_NestedListBoxStateAfterRowPushed_IsPreserved()
    {
        ComboBox comboBox = CreateComboBox(6);
        comboBox.IsDropDownOpen = true;
        comboBox.ListBox.SelectedIndex = 4;
        comboBox.IsDropDownOpen = false;

        comboBox.Text.ShouldBe("Item 4");
        comboBox.IsDropDownOpen = true;

        Snapshot(comboBox).ShouldBe(Expected.AfterSelectingRow);
    }

    [Fact]
    public void TwoComboBoxes_OpenedAlternately_EachKeepsItsOwnPopup()
    {
        ComboBox first = CreateComboBox(3);
        ComboBox second = CreateComboBox(5);
        second.Y = 120;

        for (int i = 0; i < 3; i++)
        {
            first.IsDropDownOpen = true;
            first.IsDropDownOpen = false;
            second.IsDropDownOpen = true;
            second.IsDropDownOpen = false;
        }
        first.IsDropDownOpen = true;

        (Snapshot(first) + Snapshot(second)).ShouldBe(Expected.TwoComboBoxes);
    }

    [Fact]
    public void Open_Disabled_PopupDoesNotTakeFocus()
    {
        ComboBox comboBox = CreateComboBox(4);
        comboBox.IsDropDownOpen = true;
        comboBox.IsDropDownOpen = false;

        comboBox.IsEnabled = false;
        comboBox.IsDropDownOpen = true;

        Snapshot(comboBox).ShouldBe(Expected.OpenedWhileDisabled);
    }

    [Fact]
    public void Open_ParentChangedHandlerThrowsOnce_ListBoxIsNotLeftSuspendedAndNextOpenWorks()
    {
        ComboBox comboBox = CreateComboBox(4);
        comboBox.IsDropDownOpen = true;
        comboBox.IsDropDownOpen = false;
        GraphicalUiElement listVisual = comboBox.ListBox.Visual;
        bool thrown = false;
        void ThrowOnceWhenDetachedFromComboBox(object? sender, GraphicalUiElement.ParentChangedEventArgs args)
        {
            if (!thrown && args.OldValue == comboBox.Visual && args.NewValue == null)
            {
                thrown = true;
                throw new System.InvalidOperationException("handler failed");
            }
        }
        listVisual.ParentChanged += ThrowOnceWhenDetachedFromComboBox;

        Should.Throw<System.InvalidOperationException>(() => comboBox.IsDropDownOpen = true);

        thrown.ShouldBeTrue();
        listVisual.IsLayoutSuspended.ShouldBeFalse();
        listVisual.ParentChanged -= ThrowOnceWhenDetachedFromComboBox;
        comboBox.IsDropDownOpen = false;
        comboBox.IsDropDownOpen = true;
        Snapshot(comboBox).ShouldBe(Expected.OpenFourItems);
    }

    [Fact]
    public void Open_ListBoxSuspendedByCaller_StaysSuspendedThroughOpenAndClose()
    {
        ComboBox comboBox = CreateComboBox(4);
        comboBox.IsDropDownOpen = true;
        comboBox.IsDropDownOpen = false;
        GraphicalUiElement listVisual = comboBox.ListBox.Visual;
        listVisual.SuspendLayout();

        comboBox.IsDropDownOpen = true;
        bool suspendedWhileOpen = listVisual.IsLayoutSuspended;
        comboBox.IsDropDownOpen = false;

        suspendedWhileOpen.ShouldBeTrue();
        listVisual.IsLayoutSuspended.ShouldBeTrue();
    }

    private static class Expected
    {
        public const string OpenFourItems = """
combo open=True text='' selected=-1 rect=(20,30,256,24)
list visible=True parent=PopupRoot rect=(20,54,256,128) units=PixelsFromSmall/PixelsFromSmall/Absolute/Absolute values=20,54,256,128 z=∞
list focused=True itemsFocus=True selected=-1 scroll=0/0
  row 0 'Item 0' rect=(22,56,252,21) selected=False highlighted=False enabled=True
  row 1 'Item 1' rect=(22,77,252,21) selected=False highlighted=False enabled=True
  row 2 'Item 2' rect=(22,98,252,21) selected=False highlighted=False enabled=True
  row 3 'Item 3' rect=(22,119,252,21) selected=False highlighted=False enabled=True
  rows outside the list: 0
""";
        public const string ClosedFourItems = """
combo open=False text='' selected=-1 rect=(20,30,256,24)
list visible=False parent=none rect=(0,24,1056,128) units=PixelsFromSmall/PixelsFromSmall/RelativeToContainer/Absolute values=0,24,0,128 z=∞
list focused=False itemsFocus=False selected=-1 scroll=0/0
  row 0 'Item 0' rect=(2,26,252,21) selected=False highlighted=False enabled=True
  row 1 'Item 1' rect=(2,47,252,21) selected=False highlighted=False enabled=True
  row 2 'Item 2' rect=(2,68,252,21) selected=False highlighted=False enabled=True
  row 3 'Item 3' rect=(2,89,252,21) selected=False highlighted=False enabled=True
  rows outside the list: 0
""";
        public const string ReopenAfterItemsChangedClosed = """
combo open=True text='' selected=-1 rect=(20,30,256,24)
list visible=True parent=PopupRoot rect=(20,54,256,128) units=PixelsFromSmall/PixelsFromSmall/Absolute/Absolute values=20,54,256,128 z=∞
list focused=True itemsFocus=True selected=-1 scroll=0/0
  row 0 'Item 1' rect=(22,56,252,21) selected=False highlighted=False enabled=True
  row 1 'Inserted' rect=(22,77,252,21) selected=False highlighted=False enabled=True
  row 2 'Item 2' rect=(22,98,252,21) selected=False highlighted=False enabled=True
  row 3 'Item 3' rect=(22,119,252,21) selected=False highlighted=False enabled=True
  row 4 'Added' rect=(22,140,252,21) selected=False highlighted=False enabled=True
  rows outside the list: 0
""";
        public const string ItemsChangedWhileOpen = """
combo open=True text='' selected=-1 rect=(20,30,256,24)
list visible=True parent=PopupRoot rect=(20,54,256,128) units=PixelsFromSmall/PixelsFromSmall/Absolute/Absolute values=20,54,256,128 z=∞
list focused=True itemsFocus=True selected=-1 scroll=0/0
  row 0 'Item 0' rect=(22,56,252,21) selected=False highlighted=False enabled=True
  row 1 'Item 2' rect=(22,77,252,21) selected=False highlighted=False enabled=True
  row 2 'Item 3' rect=(22,98,252,21) selected=False highlighted=False enabled=True
  row 3 'Late' rect=(22,119,252,21) selected=False highlighted=False enabled=True
  rows outside the list: 0
""";
        public const string ItemsChangedWhileOpenThenReopened = """
combo open=True text='' selected=-1 rect=(20,30,256,24)
list visible=True parent=PopupRoot rect=(20,54,256,128) units=PixelsFromSmall/PixelsFromSmall/Absolute/Absolute values=20,54,256,128 z=∞
list focused=True itemsFocus=True selected=-1 scroll=0/0
  row 0 'Item 0' rect=(22,56,252,21) selected=False highlighted=False enabled=True
  row 1 'Item 2' rect=(22,77,252,21) selected=False highlighted=False enabled=True
  row 2 'Item 3' rect=(22,98,252,21) selected=False highlighted=False enabled=True
  row 3 'Late' rect=(22,119,252,21) selected=False highlighted=False enabled=True
  rows outside the list: 0
""";
        public const string ReopenSelectedFar = """
combo open=True text='Item 27' selected=27 rect=(20,30,256,24)
list visible=True parent=PopupRoot rect=(20,54,256,128) units=PixelsFromSmall/PixelsFromSmall/Absolute/Absolute values=20,54,256,128 z=∞
list focused=True itemsFocus=True selected=27 scroll=464/506
  row 22 'Item 22' rect=(22,54,228,21) selected=False highlighted=False enabled=True
  row 23 'Item 23' rect=(22,75,228,21) selected=False highlighted=False enabled=True
  row 24 'Item 24' rect=(22,96,228,21) selected=False highlighted=False enabled=True
  row 25 'Item 25' rect=(22,117,228,21) selected=False highlighted=False enabled=True
  row 26 'Item 26' rect=(22,138,228,21) selected=False highlighted=False enabled=True
  row 27 'Item 27' rect=(22,159,228,21) selected=True highlighted=False enabled=True
  row 28 'Item 28' rect=(22,180,228,21) selected=False highlighted=False enabled=True
  rows outside the list: 23
""";
        public const string SelectedMovesBetweenOpens = """
27: scroll=464 selected=27 rect=(20,54,256,128)
2: scroll=42 selected=2 rect=(20,54,256,128)
15: scroll=212 selected=15 rect=(20,54,256,128)
-1: scroll=212 selected=-1 rect=(20,54,256,128)
29: scroll=506 selected=29 rect=(20,54,256,128)
""";
        public const string DisabledItem = """
combo open=True text='' selected=-1 rect=(20,30,256,24)
list visible=True parent=PopupRoot rect=(20,54,256,128) units=PixelsFromSmall/PixelsFromSmall/Absolute/Absolute values=20,54,256,128 z=∞
list focused=True itemsFocus=True selected=-1 scroll=0/0
  row 0 'Item 0' rect=(22,56,252,21) selected=False highlighted=False enabled=True
  row 1 'Item 1' rect=(22,77,252,21) selected=False highlighted=False enabled=True
  row 2 'Item 2' rect=(22,98,252,21) selected=False highlighted=False enabled=False
  row 3 'Item 3' rect=(22,119,252,21) selected=False highlighted=False enabled=True
  rows outside the list: 0
""";
        public const string TallDropDown = """
combo open=True text='' selected=-1 rect=(20,30,256,24)
list visible=True parent=PopupRoot rect=(20,54,256,128) units=PixelsFromSmall/PixelsFromSmall/Absolute/Absolute values=20,54,256,128 z=∞
list focused=True itemsFocus=True selected=-1 scroll=0/1136
  row 0 'Item 0' rect=(22,56,228,21) selected=False highlighted=False enabled=True
  row 1 'Item 1' rect=(22,77,228,21) selected=False highlighted=False enabled=True
  row 2 'Item 2' rect=(22,98,228,21) selected=False highlighted=False enabled=True
  row 3 'Item 3' rect=(22,119,228,21) selected=False highlighted=False enabled=True
  row 4 'Item 4' rect=(22,140,228,21) selected=False highlighted=False enabled=True
  row 5 'Item 5' rect=(22,161,228,21) selected=False highlighted=False enabled=True
  rows outside the list: 54
""";
        public const string MovedWhileClosed = """
combo open=True text='' selected=-1 rect=(140,90,180,24)
list visible=True parent=PopupRoot rect=(140,114,180,128) units=PixelsFromSmall/PixelsFromSmall/Absolute/Absolute values=140,114,180,128 z=∞
list focused=True itemsFocus=True selected=-1 scroll=0/0
  row 0 'Item 0' rect=(142,116,176,21) selected=False highlighted=False enabled=True
  row 1 'Item 1' rect=(142,137,176,21) selected=False highlighted=False enabled=True
  row 2 'Item 2' rect=(142,158,176,21) selected=False highlighted=False enabled=True
  row 3 'Item 3' rect=(142,179,176,21) selected=False highlighted=False enabled=True
  rows outside the list: 0
""";
        public const string NearBottom = """
combo open=True text='' selected=-1 rect=(20,560,256,24)
list visible=True parent=PopupRoot rect=(20,472,256,128) units=PixelsFromSmall/PixelsFromSmall/Absolute/Absolute values=20,472,256,128 z=∞
list focused=True itemsFocus=True selected=-1 scroll=0/44
  row 0 'Item 0' rect=(22,474,228,21) selected=False highlighted=False enabled=True
  row 1 'Item 1' rect=(22,495,228,21) selected=False highlighted=False enabled=True
  row 2 'Item 2' rect=(22,516,228,21) selected=False highlighted=False enabled=True
  row 3 'Item 3' rect=(22,537,228,21) selected=False highlighted=False enabled=True
  row 4 'Item 4' rect=(22,558,228,21) selected=False highlighted=False enabled=True
  row 5 'Item 5' rect=(22,579,228,21) selected=False highlighted=False enabled=True
  rows outside the list: 2
""";
        public const string AfterSelectingRow = """
combo open=True text='Item 4' selected=4 rect=(20,30,256,24)
list visible=True parent=PopupRoot rect=(20,54,256,128) units=PixelsFromSmall/PixelsFromSmall/Absolute/Absolute values=20,54,256,128 z=∞
list focused=True itemsFocus=True selected=4 scroll=0/2
  row 0 'Item 0' rect=(22,56,228,21) selected=False highlighted=False enabled=True
  row 1 'Item 1' rect=(22,77,228,21) selected=False highlighted=False enabled=True
  row 2 'Item 2' rect=(22,98,228,21) selected=False highlighted=False enabled=True
  row 3 'Item 3' rect=(22,119,228,21) selected=False highlighted=False enabled=True
  row 4 'Item 4' rect=(22,140,228,21) selected=True highlighted=False enabled=True
  row 5 'Item 5' rect=(22,161,228,21) selected=False highlighted=False enabled=True
  rows outside the list: 0
""";
        public const string TwoComboBoxes = """
combo open=True text='' selected=-1 rect=(20,30,256,24)
list visible=True parent=PopupRoot rect=(20,54,256,128) units=PixelsFromSmall/PixelsFromSmall/Absolute/Absolute values=20,54,256,128 z=∞
list focused=True itemsFocus=True selected=-1 scroll=0/0
  row 0 'Item 0' rect=(22,56,252,21) selected=False highlighted=False enabled=True
  row 1 'Item 1' rect=(22,77,252,21) selected=False highlighted=False enabled=True
  row 2 'Item 2' rect=(22,98,252,21) selected=False highlighted=False enabled=True
  rows outside the list: 0combo open=False text='' selected=-1 rect=(20,120,256,24)
list visible=False parent=none rect=(0,24,1056,128) units=PixelsFromSmall/PixelsFromSmall/RelativeToContainer/Absolute values=0,24,0,128 z=∞
list focused=False itemsFocus=False selected=-1 scroll=0/0
  row 0 'Item 0' rect=(2,26,252,21) selected=False highlighted=False enabled=True
  row 1 'Item 1' rect=(2,47,252,21) selected=False highlighted=False enabled=True
  row 2 'Item 2' rect=(2,68,252,21) selected=False highlighted=False enabled=True
  row 3 'Item 3' rect=(2,89,252,21) selected=False highlighted=False enabled=True
  row 4 'Item 4' rect=(2,110,252,21) selected=False highlighted=False enabled=True
  rows outside the list: 0
""";
        public const string OpenedWhileDisabled = """
combo open=True text='' selected=-1 rect=(20,30,256,24)
list visible=True parent=PopupRoot rect=(20,54,256,128) units=PixelsFromSmall/PixelsFromSmall/Absolute/Absolute values=20,54,256,128 z=∞
list focused=False itemsFocus=False selected=-1 scroll=0/0
  row 0 'Item 0' rect=(22,56,252,21) selected=False highlighted=False enabled=True
  row 1 'Item 1' rect=(22,77,252,21) selected=False highlighted=False enabled=True
  row 2 'Item 2' rect=(22,98,252,21) selected=False highlighted=False enabled=True
  row 3 'Item 3' rect=(22,119,252,21) selected=False highlighted=False enabled=True
  rows outside the list: 0
""";
    }
}
