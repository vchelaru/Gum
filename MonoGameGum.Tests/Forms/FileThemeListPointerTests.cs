using Gum.Automation;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Pointer behavior of ListBox, ScrollViewer, ComboBox and Menu built from each packaged theme's files:
/// item selection, wheel scrolling, drop-down open/select/close, menu open and hover navigation.
/// </summary>
public class FileThemeListPointerTests : FileThemeInputTestBase
{
    private ListBox PlaceListBox(string theme, int count = 40)
    {
        ListBox listBox = Place<ListBox>(LoadTheme(theme), "ListBox");
        listBox.Height = 200;
        listBox.Width = 250;
        for (int i = 0; i < count; i++)
        {
            listBox.Items!.Add("Item " + i);
        }
        Driver.Step(3);
        return listBox;
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ClickItem_ListBox_SelectsThatItem(string theme)
    {
        ListBox listBox = PlaceListBox(theme);

        ClickCenter(listBox.ListBoxItems[2].Visual);

        listBox.SelectedIndex.ShouldBe(2, theme);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void RightClickItem_ListBox_DoesNotSelect(string theme)
    {
        ListBox listBox = PlaceListBox(theme);

        ClickCenter(listBox.ListBoxItems[2].Visual, MouseButton.Secondary);

        listBox.SelectedIndex.ShouldBe(-1, theme);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void HoverItem_ListBox_HighlightsAndClearsOnLeave(string theme)
    {
        ListBox listBox = PlaceListBox(theme);
        GraphicalUiElement item = listBox.ListBoxItems[1].Visual;

        Driver.MoveTo(CenterX(item), CenterY(item));
        AppliedState(item, "ListBoxItemCategory").ShouldBe("Highlighted", theme);

        Driver.MoveTo(OffX, OffY);
        AppliedState(item, "ListBoxItemCategory").ShouldBe("Enabled", theme);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ClickItemBelowTheClip_ListBox_DoesNotSelectIt(string theme)
    {
        ListBox listBox = PlaceListBox(theme);
        GraphicalUiElement clipped = listBox.ListBoxItems[15].Visual;
        int y = CenterY(clipped);
        y.ShouldBeGreaterThan((int)(listBox.Visual.AbsoluteTop + listBox.Visual.AbsoluteHeight), theme + " test setup: item should lie below the list");
        if (y >= (int)GraphicalUiElement.CanvasHeight)
        {
            return;
        }

        Driver.Click(CenterX(clipped), y);

        listBox.SelectedIndex.ShouldBe(-1, theme);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void Wheel_ListBox_ScrollsDownThenUp(string theme)
    {
        ListBox listBox = PlaceListBox(theme);
        GraphicalUiElement item = listBox.ListBoxItems[1].Visual;
        Driver.MoveTo(CenterX(item), CenterY(item));

        Driver.Cursor.Scroll(-3);
        Driver.Step();
        double down = listBox.VerticalScrollBarValue;
        down.ShouldBeGreaterThan(0, theme + " wheel down");

        Driver.Cursor.Scroll(3);
        Driver.Step();
        listBox.VerticalScrollBarValue.ShouldBeLessThan(down, theme + " wheel up");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ClickItemAfterWheelScroll_ListBox_SelectsTheItemUnderTheCursor(string theme)
    {
        ListBox listBox = PlaceListBox(theme);
        GraphicalUiElement first = listBox.ListBoxItems[1].Visual;
        Driver.MoveTo(CenterX(first), CenterY(first));
        Driver.Cursor.Scroll(-3);
        Driver.Step();

        int x = CenterX(first);
        int y = (int)(listBox.Visual.AbsoluteTop + 100);
        Driver.Click(x, y);

        int expected = -1;
        for (int i = 0; i < listBox.ListBoxItems.Count; i++)
        {
            GraphicalUiElement visual = listBox.ListBoxItems[i].Visual;
            if (y >= visual.AbsoluteTop && y < visual.AbsoluteTop + visual.AbsoluteHeight && visual.AbsoluteVisible)
            {
                expected = i;
                break;
            }
        }
        listBox.SelectedIndex.ShouldBe(expected, theme);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void Wheel_ScrollViewer_ScrollsContent(string theme)
    {
        ScrollViewer viewer = Place<ScrollViewer>(LoadTheme(theme), "ScrollViewer");
        viewer.Width = 200;
        viewer.Height = 100;
        for (int i = 0; i < 20; i++)
        {
            Label label = new Label();
            label.Text = "Row " + i;
            viewer.AddChild(label);
        }
        Driver.Step(3);

        Driver.MoveTo(CenterX(viewer.Visual), CenterY(viewer.Visual));
        Driver.Cursor.Scroll(-3);
        Driver.Step();

        viewer.VerticalScrollBarValue.ShouldBeGreaterThan(0, theme);
    }

    private ComboBox PlaceComboBox(string theme)
    {
        ComboBox combo = Place<ComboBox>(LoadTheme(theme), "ComboBox");
        for (int i = 0; i < 6; i++)
        {
            combo.Items!.Add("Choice " + i);
        }
        Driver.Step(3);
        return combo;
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void Click_ComboBox_OpensTheDropDown(string theme)
    {
        ComboBox combo = PlaceComboBox(theme);
        ClickCenter(combo.Visual);
        combo.IsDropDownOpen.ShouldBeTrue(theme);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void RightClick_ComboBox_DoesNotOpen(string theme)
    {
        ComboBox combo = PlaceComboBox(theme);
        ClickCenter(combo.Visual, MouseButton.Secondary);
        combo.IsDropDownOpen.ShouldBeFalse(theme);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ClickDropDownItem_ComboBox_SelectsAndCloses(string theme)
    {
        ComboBox combo = PlaceComboBox(theme);
        ClickCenter(combo.Visual);
        ListBox dropDown = combo.ListBox!;
        Driver.Step(2);

        ClickCenter(dropDown.ListBoxItems[2].Visual);

        combo.SelectedIndex.ShouldBe(2, theme);
        combo.IsDropDownOpen.ShouldBeFalse(theme + " drop down stayed open after selecting");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ClickAway_ComboBox_ClosesWithoutChangingSelection(string theme)
    {
        ComboBox combo = PlaceComboBox(theme);
        ClickCenter(combo.Visual);
        combo.IsDropDownOpen.ShouldBeTrue(theme);

        Driver.Click(OffX, OffY);

        combo.IsDropDownOpen.ShouldBeFalse(theme);
        combo.SelectedIndex.ShouldBe(-1, theme);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ClickTwice_ComboBox_OpensThenCloses(string theme)
    {
        ComboBox combo = PlaceComboBox(theme);
        ClickCenter(combo.Visual);
        ClickCenter(combo.Visual);
        combo.IsDropDownOpen.ShouldBeFalse(theme);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void Disabled_ComboBox_IgnoresClick(string theme)
    {
        ComboBox combo = PlaceComboBox(theme);
        combo.IsEnabled = false;
        Driver.Step();
        ClickCenter(combo.Visual);
        combo.IsDropDownOpen.ShouldBeFalse(theme);
    }

    private Menu PlaceMenu(string theme)
    {
        Menu menu = Place<Menu>(LoadTheme(theme), "Menu");
        for (int i = 0; i < 3; i++)
        {
            MenuItem top = new MenuItem();
            top.Header = "Top " + i;
            menu.Items!.Add(top);
            for (int j = 0; j < 2; j++)
            {
                MenuItem sub = new MenuItem();
                sub.Header = $"Sub {i}.{j}";
                top.Items!.Add(sub);
            }
        }
        Driver.Step(3);
        return menu;
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void Click_MenuItem_OpensItsSubmenu(string theme)
    {
        Menu menu = PlaceMenu(theme);
        MenuItem top = menu.MenuItems[0];

        ClickCenter(top.Visual);

        top.IsPopupVisible.ShouldBeTrue(theme);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void HoverOtherMenuItem_AfterOpening_SwitchesSubmenu(string theme)
    {
        Menu menu = PlaceMenu(theme);
        ClickCenter(menu.MenuItems[0].Visual);

        Driver.MoveTo(CenterX(menu.MenuItems[1].Visual), CenterY(menu.MenuItems[1].Visual));

        menu.MenuItems[1].IsPopupVisible.ShouldBeTrue(theme + " second submenu did not open on hover");
        menu.MenuItems[0].IsPopupVisible.ShouldBeFalse(theme + " first submenu stayed open");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ClickSubmenuItem_Menu_RaisesClickedAndCloses(string theme)
    {
        Menu menu = PlaceMenu(theme);
        MenuItem top = menu.MenuItems[0];
        MenuItem sub = (MenuItem)top.Items![0]!;
        int clicked = 0;
        sub.Clicked += (_, _) => clicked++;
        ClickCenter(top.Visual);

        ClickCenter(sub.Visual);

        clicked.ShouldBe(1, theme);
        top.IsPopupVisible.ShouldBeFalse(theme + " submenu stayed open after choosing an item");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ClickAway_OpenMenu_ClosesTheSubmenu(string theme)
    {
        Menu menu = PlaceMenu(theme);
        ClickCenter(menu.MenuItems[0].Visual);

        Driver.Click(OffX, OffY);

        menu.MenuItems[0].IsPopupVisible.ShouldBeFalse(theme);
    }
}
