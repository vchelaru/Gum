using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Gum.GueDeriving;
using Moq;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Splash-damage sweep for #6643: where a ComboBox drop-down is scrolled when it opens, across the
/// ways the selection and the items can change while it is closed or open.
/// Boundary: ComboBox open/close + ListBox SelectedIndex/ScrollIndexIntoView/visibility/layout.
/// "In view" is judged from the rows' absolute rectangles against the clip container, which is
/// what the user sees, never from the scroll value formula under test.
/// </summary>
public class ComboBoxScrollSplashTests : BaseTestClass
{
    private const int ManyItems = 30;

    #region ComboBox: selection set while closed

    [Fact]
    public void Open_NoSelection_ShouldShowTop()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);

        OpenScrollable(comboBox);

        comboBox.ListBox.VerticalScrollBarValue.ShouldBe(0);
        IsRowInView(comboBox.ListBox, 0).ShouldBeTrue();
    }

    [Fact]
    public void Open_SelectedIndexSetBeforeAddToRoot_ShouldShowSelectedItem()
    {
        ComboBox comboBox = new();
        for (int i = 0; i < ManyItems; i++)
        {
            comboBox.Items!.Add("Item " + i);
        }
        comboBox.SelectedIndex = 0;
        comboBox.AddToRoot();

        OpenScrollable(comboBox);

        IsRowInView(comboBox.ListBox, 0).ShouldBeTrue();
        comboBox.ListBox.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void Open_SelectedIndexSetWhileClosed_ShouldShowSelectedItem()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);

        comboBox.SelectedIndex = 0;
        OpenScrollable(comboBox);

        IsRowInView(comboBox.ListBox, 0).ShouldBeTrue();
        comboBox.ListBox.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void Open_SelectedIndexSetWhileClosedToLast_ShouldShowSelectedItem()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);

        comboBox.SelectedIndex = ManyItems - 1;
        OpenScrollable(comboBox);

        IsRowInView(comboBox.ListBox, ManyItems - 1).ShouldBeTrue();
    }

    [Fact]
    public void Open_SelectedIndexSetWhileClosedToMiddle_ShouldShowSelectedItem()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);

        comboBox.SelectedIndex = 15;
        OpenScrollable(comboBox);

        IsRowInView(comboBox.ListBox, 15).ShouldBeTrue();
    }

    [Fact]
    public void Open_SelectedIndexSetWhileClosedThenChanged_ShouldShowLatestSelection()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);

        comboBox.SelectedIndex = ManyItems - 1;
        comboBox.SelectedIndex = 2;
        OpenScrollable(comboBox);

        IsRowInView(comboBox.ListBox, 2).ShouldBeTrue();
        comboBox.ListBox.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void Open_SelectedIndexSetWhileClosedToMinusOne_ShouldShowTop()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);
        comboBox.SelectedIndex = ManyItems - 1;

        comboBox.SelectedIndex = -1;
        OpenScrollable(comboBox);

        comboBox.ListBox.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void Open_SelectedIndexOutOfRangeWhileClosed_ShouldThrowAndLeaveTopShown()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);

        Should.Throw<IndexOutOfRangeException>(() => comboBox.SelectedIndex = ManyItems);
        OpenScrollable(comboBox);

        comboBox.SelectedIndex.ShouldBe(-1);
        comboBox.ListBox.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void Open_SelectedObjectSetWhileClosed_ShouldShowSelectedItem()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);

        comboBox.SelectedObject = "Item 20";
        OpenScrollable(comboBox);

        IsRowInView(comboBox.ListBox, 20).ShouldBeTrue();
    }

    [Fact]
    public void Open_SelectedIndexSetWhileClosedWithVisualTemplate_ShouldShowSelectedItem()
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        comboBox.VisualTemplate = new Gum.Forms.VisualTemplate(() => new ListBoxItem().Visual);
        for (int i = 0; i < ManyItems; i++)
        {
            comboBox.Items!.Add("Item " + i);
        }

        comboBox.SelectedIndex = 0;
        OpenScrollable(comboBox);

        IsRowInView(comboBox.ListBox, 0).ShouldBeTrue();
        comboBox.ListBox.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void Open_FewItemsSelectedWhileClosed_ShouldNotScroll()
    {
        ComboBox comboBox = CreateComboBox(3);

        comboBox.SelectedIndex = 2;
        comboBox.IsDropDownOpen = true;

        comboBox.ListBox.VerticalScrollBarValue.ShouldBe(0);
        IsRowInView(comboBox.ListBox, 2).ShouldBeTrue();
    }

    [Fact]
    public void Click_OpenAfterSelectedIndexSetWhileClosed_ShouldShowSelectedItem()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);
        comboBox.SelectedIndex = 0;

        PushOn(comboBox.Visual);

        comboBox.IsDropDownOpen.ShouldBeTrue();
        IsRowInView(comboBox.ListBox, 0).ShouldBeTrue();
        comboBox.ListBox.VerticalScrollBarValue.ShouldBe(0);
    }

    #endregion

    #region ComboBox: items change while closed

    [Fact]
    public void Open_ItemsAddedAfterSelectionWhileClosed_ShouldShowSelectedItem()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);
        comboBox.SelectedIndex = ManyItems - 1;

        for (int i = 0; i < 10; i++)
        {
            comboBox.Items!.Add("Extra " + i);
        }
        OpenScrollable(comboBox);

        IsRowInView(comboBox.ListBox, ManyItems - 1).ShouldBeTrue();
    }

    [Fact]
    public void Open_ItemInsertedBeforeSelectionWhileClosed_ShouldShowSelectedItem()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);
        comboBox.SelectedIndex = 25;

        comboBox.Items!.Insert(0, "Inserted");
        OpenScrollable(comboBox);

        comboBox.SelectedObject.ShouldBe("Item 25");
        IsRowInView(comboBox.ListBox, comboBox.SelectedIndex).ShouldBeTrue();
    }

    [Fact]
    public void Open_ItemRemovedBeforeSelectionWhileClosed_ShouldShowSelectedItem()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);
        comboBox.SelectedIndex = 25;

        comboBox.Items!.RemoveAt(0);
        OpenScrollable(comboBox);

        comboBox.SelectedObject.ShouldBe("Item 25");
        IsRowInView(comboBox.ListBox, comboBox.SelectedIndex).ShouldBeTrue();
    }

    [Fact]
    public void Open_SelectedItemRemovedWhileClosed_ShouldShowTopAndNotThrow()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);
        comboBox.SelectedIndex = ManyItems - 1;

        comboBox.Items!.RemoveAt(ManyItems - 1);
        OpenScrollable(comboBox);

        comboBox.SelectedIndex.ShouldBe(-1);
        comboBox.ListBox.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void Open_ItemsClearedAndRepopulatedWhileClosed_ShouldShowTop()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);
        comboBox.SelectedIndex = ManyItems - 1;

        comboBox.Items!.Clear();
        for (int i = 0; i < ManyItems; i++)
        {
            comboBox.Items.Add("New " + i);
        }
        OpenScrollable(comboBox);

        comboBox.SelectedIndex.ShouldBe(-1);
        comboBox.ListBox.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void Open_ItemsReplacedByCollectionThenSelectedWhileClosed_ShouldShowSelectedItem()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);
        comboBox.SelectedIndex = ManyItems - 1;

        comboBox.Items = CreateCollection("Bound ", ManyItems);
        comboBox.SelectedIndex = 0;
        OpenScrollable(comboBox);

        IsRowInView(comboBox.ListBox, 0).ShouldBeTrue();
        comboBox.ListBox.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void Open_ItemsReplacedByCollectionWhileClosed_ShouldShowTop()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);
        comboBox.SelectedIndex = ManyItems - 1;

        comboBox.Items = CreateCollection("Bound ", ManyItems);
        OpenScrollable(comboBox);

        comboBox.ListBox.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void Open_UnevenItemHeightsSelectedWhileClosed_ShouldShowSelectedItem()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);
        SetRowHeights(comboBox.ListBox, index => index % 2 == 0 ? 20 : 60);

        comboBox.SelectedIndex = 0;
        OpenScrollable(comboBox);
        IsRowInView(comboBox.ListBox, 0).ShouldBeTrue();
        comboBox.IsDropDownOpen = false;

        comboBox.SelectedIndex = ManyItems - 1;
        comboBox.IsDropDownOpen = true;

        IsRowInView(comboBox.ListBox, ManyItems - 1).ShouldBeTrue();
    }

    [Fact]
    public void Open_TallItemsSelectedWhileClosed_ShouldShowSelectedItem()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);
        SetRowHeights(comboBox.ListBox, index => 60);

        comboBox.SelectedIndex = 0;
        OpenScrollable(comboBox);

        IsRowInView(comboBox.ListBox, 0).ShouldBeTrue();
        comboBox.ListBox.VerticalScrollBarValue.ShouldBe(0);
    }

    #endregion

    #region ComboBox: open, close and reopen

    [Fact]
    public void Open_SelectedIndexSetWhileOpen_ShouldShowSelectedItem()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);
        OpenScrollable(comboBox);

        comboBox.SelectedIndex = 25;

        IsRowInView(comboBox.ListBox, 25).ShouldBeTrue();
    }

    [Fact]
    public void Open_ItemsAddedWhileOpen_ShouldKeepSelectedItemInView()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);
        OpenScrollable(comboBox);
        comboBox.SelectedIndex = 20;

        comboBox.Items!.Add("Extra");

        IsRowInView(comboBox.ListBox, 20).ShouldBeTrue();
    }

    [Fact]
    public void Open_TwiceInARow_ShouldKeepScrollPositionAndOnePopup()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);
        OpenScrollable(comboBox);
        comboBox.ListBox.VerticalScrollBarValue = 50;

        comboBox.IsDropDownOpen = true;

        comboBox.ListBox.VerticalScrollBarValue.ShouldBe(50);
        FrameworkElement.PopupRoot.Children.Count(x => x == comboBox.ListBox.Visual).ShouldBe(1);
    }

    [Fact]
    public void Reopen_NothingChanged_ShouldPreserveScrollPosition()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);
        OpenScrollable(comboBox);
        comboBox.ListBox.VerticalScrollBarValue = 60;

        comboBox.IsDropDownOpen = false;
        comboBox.IsDropDownOpen = true;

        comboBox.ListBox.VerticalScrollBarValue.ShouldBe(60);
    }

    [Fact]
    public void Reopen_SelectionMovedWhileClosed_ShouldShowNewSelection()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);
        OpenScrollable(comboBox);
        comboBox.IsDropDownOpen = false;

        comboBox.SelectedIndex = ManyItems - 1;
        comboBox.IsDropDownOpen = true;

        IsRowInView(comboBox.ListBox, ManyItems - 1).ShouldBeTrue();
    }

    [Fact]
    public void Reopen_SelectionMovedWhileOpen_ShouldKeepSelectionInView()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);
        OpenScrollable(comboBox);
        comboBox.SelectedIndex = ManyItems - 1;
        double scrollWhenClosed = comboBox.ListBox.VerticalScrollBarValue;
        comboBox.IsDropDownOpen = false;

        comboBox.IsDropDownOpen = true;

        comboBox.ListBox.VerticalScrollBarValue.ShouldBe(scrollWhenClosed);
        IsRowInView(comboBox.ListBox, ManyItems - 1).ShouldBeTrue();
    }

    [Fact]
    public void Reopen_SelectedByMouseAfterScrolling_ShouldPreserveScrollAndShowSelection()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);
        OpenScrollable(comboBox);
        comboBox.ListBox.VerticalScrollBarValue = 60;
        int pushedIndex = Enumerable.Range(0, ManyItems).First(i => IsRowInView(comboBox.ListBox, i));

        PushOn(comboBox.ListBox.ListBoxItems[pushedIndex].Visual);

        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.SelectedIndex.ShouldBe(pushedIndex);
        comboBox.IsDropDownOpen = true;
        comboBox.ListBox.VerticalScrollBarValue.ShouldBe(60);
        IsRowInView(comboBox.ListBox, pushedIndex).ShouldBeTrue();
    }

    [Fact]
    public void Reopen_ManyTimesWithChangingSelection_ShouldAlwaysShowSelection()
    {
        ComboBox comboBox = CreateComboBox(ManyItems);
        int[] selections = { 29, 0, 14, 29, 1, 28 };

        foreach (int selection in selections)
        {
            comboBox.SelectedIndex = selection;
            comboBox.IsDropDownOpen = true;
            IsRowInView(comboBox.ListBox, selection).ShouldBeTrue($"selection {selection}");
            comboBox.IsDropDownOpen = false;
        }
    }

    #endregion

    #region ListBox standalone

    [Fact]
    public void ListBox_SelectedIndexSetWhenLaidOut_ShouldScrollSelectedItemIntoView()
    {
        ListBox listBox = CreateLaidOutListBox(50);

        listBox.SelectedIndex = 40;

        IsRowInView(listBox, 40).ShouldBeTrue();
        listBox.VerticalScrollBarValue.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ListBox_SelectedIndexSetBackToTopWhenLaidOut_ShouldScrollToTop()
    {
        ListBox listBox = CreateLaidOutListBox(50);
        listBox.SelectedIndex = 40;

        listBox.SelectedIndex = 0;

        IsRowInView(listBox, 0).ShouldBeTrue();
        listBox.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void ListBox_ScrollIndexIntoViewBringIntoViewAlreadyVisible_ShouldNotScroll()
    {
        ListBox listBox = CreateLaidOutListBox(50);

        listBox.ScrollIndexIntoView(0);

        listBox.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void ListBox_ScrollIndexIntoViewTop_ShouldAlignRowWithTopOfView()
    {
        ListBox listBox = CreateLaidOutListBox(50);

        listBox.ScrollIndexIntoView(20, ScrollIntoViewStyle.Top);

        float rowTop = listBox.ListBoxItems[20].Visual.GetAbsoluteY();
        rowTop.ShouldBe(listBox.ClipContainer.GetAbsoluteY(), tolerance: 1);
    }

    [Fact]
    public void ListBox_ScrollIndexIntoViewBottom_ShouldAlignRowWithBottomOfView()
    {
        ListBox listBox = CreateLaidOutListBox(50);

        listBox.ScrollIndexIntoView(20, ScrollIntoViewStyle.Bottom);

        GraphicalUiElement row = listBox.ListBoxItems[20].Visual;
        float rowBottom = row.GetAbsoluteY() + row.AbsoluteHeight;
        float clipBottom = listBox.ClipContainer.GetAbsoluteY() + listBox.ClipContainer.AbsoluteHeight;
        rowBottom.ShouldBe(clipBottom, tolerance: 1);
    }

    [Fact]
    public void ListBox_ScrollIndexIntoViewCenter_ShouldCenterRowInView()
    {
        ListBox listBox = CreateLaidOutListBox(50);

        listBox.ScrollIndexIntoView(20, ScrollIntoViewStyle.Center);

        GraphicalUiElement row = listBox.ListBoxItems[20].Visual;
        float rowCenter = row.GetAbsoluteY() + row.AbsoluteHeight / 2;
        float clipCenter = listBox.ClipContainer.GetAbsoluteY() + listBox.ClipContainer.AbsoluteHeight / 2;
        rowCenter.ShouldBe(clipCenter, tolerance: 1);
    }

    [Fact]
    public void ListBox_SelectedIndexSetInHiddenParent_ShouldNotJumpToBottom()
    {
        ContainerRuntime parent = new();
        parent.AddToRoot();
        parent.Visible = false;
        ListBox listBox = new();
        parent.Children.Add(listBox.Visual);
        listBox.Visual.Height = 200;
        for (int i = 0; i < 50; i++)
        {
            listBox.Items!.Add("Item " + i);
        }

        listBox.SelectedIndex = 0;

        listBox.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void ListBox_HiddenFromStartLastItemSelectedThenShown_ShouldShowSelectedItem()
    {
        (ContainerRuntime parent, ListBox listBox) = CreateListBoxInHiddenParent(50);

        listBox.SelectedIndex = 49;
        parent.Visible = true;
        parent.UpdateLayout();

        IsRowInView(listBox, 49).ShouldBeTrue();
    }

    [Fact]
    public void ListBox_LaidOutThenHiddenSelectionChangedThenShown_ShouldShowSelectedItem()
    {
        ListBox listBox = CreateLaidOutListBox(50);
        listBox.SelectedIndex = 40;
        listBox.IsVisible = false;

        listBox.SelectedIndex = 0;
        listBox.IsVisible = true;

        IsRowInView(listBox, 0).ShouldBeTrue();
        listBox.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void ListBox_SelectedIndexSetInHiddenParentThenParentShown_ShouldShowSelectedItem()
    {
        (ContainerRuntime parent, ListBox listBox) = CreateListBoxInHiddenParent(50);
        listBox.SelectedIndex = 40;

        parent.Visible = true;
        parent.UpdateLayout();

        IsRowInView(listBox, 40).ShouldBeTrue();
    }

    [Fact]
    public void ListBox_HiddenScrollRequestsThenShown_ShouldApplyLatestRequest()
    {
        (ContainerRuntime parent, ListBox listBox) = CreateListBoxInHiddenParent(50);
        listBox.SelectedIndex = 40;

        listBox.ScrollIndexIntoView(5);
        parent.Visible = true;
        parent.UpdateLayout();

        IsRowInView(listBox, 5).ShouldBeTrue();
    }

    [Fact]
    public void ListBox_HiddenSelectedItemRemovedThenShown_ShouldNotScrollOrThrow()
    {
        (ContainerRuntime parent, ListBox listBox) = CreateListBoxInHiddenParent(50);
        listBox.SelectedIndex = 49;
        listBox.Items!.RemoveAt(49);

        parent.Visible = true;
        parent.UpdateLayout();

        listBox.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void ListBox_HiddenSelectionClearedThenShown_ShouldNotScroll()
    {
        (ContainerRuntime parent, ListBox listBox) = CreateListBoxInHiddenParent(50);
        listBox.SelectedIndex = 49;
        listBox.SelectedIndex = -1;

        parent.Visible = true;
        parent.UpdateLayout();

        listBox.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void ComboBox_InHiddenParentSelectionSetThenParentShownThenOpened_ShouldShowSelectedItem()
    {
        ContainerRuntime parent = new();
        parent.AddToRoot();
        parent.Visible = false;
        ComboBox comboBox = new();
        parent.Children.Add(comboBox.Visual);
        for (int i = 0; i < ManyItems; i++)
        {
            comboBox.Items!.Add("Item " + i);
        }
        comboBox.SelectedIndex = 20;

        parent.Visible = true;
        parent.UpdateLayout();
        OpenScrollable(comboBox);

        IsRowInView(comboBox.ListBox, 20).ShouldBeTrue();
    }

    #endregion

    #region ScrollViewer value after layout changes

    [Fact]
    public void ListBox_ItemsRemovedSoContentFits_ShouldResetScrollValueToZero()
    {
        ListBox listBox = CreateLaidOutListBox(50);
        listBox.VerticalScrollBarValue = listBox.VerticalScrollBarMaximum;

        while (listBox.Items!.Count > 2)
        {
            listBox.Items.RemoveAt(listBox.Items.Count - 1);
        }

        listBox.VerticalScrollBarMaximum.ShouldBe(0);
        listBox.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void ListBox_ItemsAddedAtEnd_ShouldKeepScrollValue()
    {
        ListBox listBox = CreateLaidOutListBox(50);
        listBox.VerticalScrollBarValue = 100;

        listBox.Items!.Add("Item 50");

        listBox.VerticalScrollBarValue.ShouldBe(100);
    }

    [Fact]
    public void ListBox_HeightGrownSoContentFits_ShouldResetScrollValueToZero()
    {
        ListBox listBox = CreateLaidOutListBox(50);
        listBox.VerticalScrollBarValue = listBox.VerticalScrollBarMaximum;

        listBox.Visual.Height = 10000;

        listBox.VerticalScrollBarMaximum.ShouldBe(0);
        listBox.VerticalScrollBarValue.ShouldBe(0);
    }

    #endregion

    #region Helpers

    private static ComboBox CreateComboBox(int itemCount)
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        for (int i = 0; i < itemCount; i++)
        {
            comboBox.Items!.Add("Item " + i);
        }
        return comboBox;
    }

    private static ObservableCollection<string> CreateCollection(string prefix, int count)
    {
        ObservableCollection<string> collection = new();
        for (int i = 0; i < count; i++)
        {
            collection.Add(prefix + i);
        }
        return collection;
    }

    private static (ContainerRuntime Parent, ListBox ListBox) CreateListBoxInHiddenParent(int itemCount)
    {
        ContainerRuntime parent = new();
        parent.AddToRoot();
        parent.Visible = false;
        ListBox listBox = new();
        parent.Children.Add(listBox.Visual);
        listBox.Visual.Height = 200;
        for (int i = 0; i < itemCount; i++)
        {
            listBox.Items!.Add("Item " + i);
        }
        return (parent, listBox);
    }

    private static ListBox CreateLaidOutListBox(int itemCount)
    {
        ListBox listBox = new();
        listBox.Visual.Height = 200;
        listBox.AddToRoot();
        for (int i = 0; i < itemCount; i++)
        {
            listBox.Items!.Add("Item " + i);
        }
        listBox.VerticalScrollBarMaximum.ShouldBeGreaterThan(0, "the test needs scrollable content");
        return listBox;
    }

    /// <summary>
    /// Opens the drop-down and asserts it has more rows than fit, so a pass cannot be vacuous.
    /// </summary>
    private static void OpenScrollable(ComboBox comboBox)
    {
        comboBox.IsDropDownOpen = true;
        comboBox.IsDropDownOpen.ShouldBeTrue();
        comboBox.ListBox.VerticalScrollBarMaximum.ShouldBeGreaterThan(0,
            "the drop-down must have more rows than it shows");
    }

    private static void SetRowHeights(ListBox listBox, Func<int, float> heightForIndex)
    {
        for (int i = 0; i < listBox.ListBoxItems.Count; i++)
        {
            GraphicalUiElement row = listBox.ListBoxItems[i].Visual;
            row.HeightUnits = DimensionUnitType.Absolute;
            row.Height = heightForIndex(i);
        }
    }

    /// <summary>
    /// True when the row's absolute rectangle lies inside the list box's clip container.
    /// </summary>
    private static bool IsRowInView(ListBox listBox, int index)
    {
        GraphicalUiElement row = listBox.ListBoxItems[index].Visual;
        GraphicalUiElement clip = listBox.ClipContainer;
        float clipTop = clip.GetAbsoluteY();
        float clipBottom = clipTop + clip.AbsoluteHeight;
        float rowTop = row.GetAbsoluteY();
        float rowBottom = rowTop + row.AbsoluteHeight;
        return clip.AbsoluteHeight > 0 && rowTop >= clipTop - 0.5f && rowBottom <= clipBottom + 0.5f;
    }

    private static void PushOn(InteractiveGue visualOver)
    {
        float x = visualOver.GetAbsoluteX() + visualOver.AbsoluteWidth / 2;
        float y = visualOver.GetAbsoluteY() + visualOver.AbsoluteHeight / 2;

        Mock<ICursor> cursor = new();
        cursor.Setup(c => c.PrimaryPush).Returns(true);
        cursor.Setup(c => c.LastInputDevice).Returns(InputDevice.Mouse);
        cursor.Setup(c => c.WindowPushed).Returns(visualOver);
        cursor.Setup(c => c.VisualOver).Returns(visualOver);
        cursor.Setup(c => c.X).Returns((int)x);
        cursor.Setup(c => c.Y).Returns((int)y);
        cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(x);
        cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(y);
        Gum.Forms.FormsUtilities.SetCursor(cursor.Object);

        Gum.GumService.Default.Update(new GameTime());
    }

    private static void PushOn(GraphicalUiElement visualOver) => PushOn((InteractiveGue)visualOver);

    #endregion
}
