using Gum.Forms.Controls;
using Gum.Input;
using Gum.Managers;
using Gum.Wireframe;
using Moq;
using Shouldly;
using Xunit;
using Keys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Keyboard stepping at item level follows the list's own axis: Left/Right step a horizontal list,
/// the same as the gamepad d-pad does; Up/Down keep stepping every list (#6972).
/// </summary>
public class ListBoxKeyboardAxisTests : BaseTestClass
{
    private static ListBox CreateList(bool horizontal, int selectedIndex = 1)
    {
        ListBox listBox = new();
        listBox.AddToRoot();
        for (int i = 0; i < 4; i++)
        {
            listBox.Items!.Add("Item " + i);
        }
        if (horizontal)
        {
            listBox.InnerPanel.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        }
        listBox.SelectedIndex = selectedIndex;
        listBox.IsFocused = true;
        listBox.DoListItemsHaveFocus = true;
        listBox.ListBoxItems[selectedIndex].IsFocused = true;
        return listBox;
    }

    private static void TypeKey(Keys key)
    {
        Mock<IInputReceiverKeyboardMonoGame> keyboard = new();
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyTyped(key)).Returns(true);
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);
    }

    [Fact]
    public void Right_HorizontalList_StepsForward()
    {
        ListBox listBox = CreateList(horizontal: true);
        TypeKey(Keys.Right);

        listBox.OnFocusUpdate();

        listBox.SelectedIndex.ShouldBe(2);
        listBox.ListBoxItems[2].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void Left_HorizontalList_StepsBack()
    {
        ListBox listBox = CreateList(horizontal: true);
        TypeKey(Keys.Left);

        listBox.OnFocusUpdate();

        listBox.SelectedIndex.ShouldBe(0);
    }

    [Fact]
    public void Right_HorizontalList_AtLastItem_StaysOnLastItem()
    {
        ListBox listBox = CreateList(horizontal: true, selectedIndex: 3);
        TypeKey(Keys.Right);

        listBox.OnFocusUpdate();

        listBox.SelectedIndex.ShouldBe(3);
    }

    [Fact]
    public void Right_HorizontalList_SkipsDisabledItem()
    {
        ListBox listBox = CreateList(horizontal: true);
        listBox.ListBoxItems[2].IsEnabled = false;
        TypeKey(Keys.Right);

        listBox.OnFocusUpdate();

        listBox.SelectedIndex.ShouldBe(3);
    }

    [Fact]
    public void Down_HorizontalList_StillSteps()
    {
        ListBox listBox = CreateList(horizontal: true);
        TypeKey(Keys.Down);

        listBox.OnFocusUpdate();

        listBox.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void Down_VerticalList_StepsForward()
    {
        ListBox listBox = CreateList(horizontal: false);
        TypeKey(Keys.Down);

        listBox.OnFocusUpdate();

        listBox.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void Right_VerticalList_DoesNotStep()
    {
        ListBox listBox = CreateList(horizontal: false);
        TypeKey(Keys.Right);

        listBox.OnFocusUpdate();

        listBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void Right_HorizontalList_RightKeyComboConfigured_LeavesTheListInsteadOfStepping()
    {
        ListBox listBox = CreateList(horizontal: true);
        FrameworkElement.RightKeyCombos.Add(new KeyCombo { PushedKey = Keys.Right });
        TypeKey(Keys.Right);

        listBox.OnFocusUpdate();

        listBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void Left_HorizontalList_AtFirstItem_ClampsWithoutWrapping()
    {
        ListBox listBox = CreateList(horizontal: true, selectedIndex: 0);
        TypeKey(Keys.Left);

        listBox.OnFocusUpdate();

        listBox.SelectedIndex.ShouldBe(0);
    }

    [Fact]
    public void Right_HorizontalList_RaisesSelectionChangedOncePerStep()
    {
        ListBox listBox = CreateList(horizontal: true);
        int count = 0;
        listBox.SelectionChanged += (_, _) => count++;
        TypeKey(Keys.Right);

        listBox.OnFocusUpdate();

        count.ShouldBe(1);
    }

    [Fact]
    public void Right_WrappingHorizontalList_StepsToNeighborRow()
    {
        ListBox listBox = CreateList(horizontal: true, selectedIndex: 0);
        listBox.InnerPanel.WrapsChildren = true;
        listBox.InnerPanel.StackSpacing = 0;
        foreach (ListBoxItem item in listBox.ListBoxItems)
        {
            item.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            item.Visual.Width = 100;
            item.Visual.Height = 20;
        }
        listBox.InnerPanel.Width = 200;
        listBox.InnerPanel.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        listBox.InnerPanel.UpdateLayout();
        TypeKey(Keys.Right);

        listBox.OnFocusUpdate();

        listBox.SelectedIndex.ShouldBe(1);
        listBox.ListBoxItems[1].IsFocused.ShouldBeTrue();
    }
}
