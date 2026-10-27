using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.Tests.Input;
using Moq;
using Shouldly;
using Xunit;
using Keys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// With item-level gamepad focus, the axis a list does not use for itself leaves the list the way it does
/// from the list top level, like a Slider perpendicular axis (#6938). The own axis of the list, A, B and
/// the top level are unchanged. A ComboBox drop-down keeps stepping on both axes.
/// </summary>
public class ListBoxPerpendicularExitTests : BaseTestClass
{
    public enum Input { DPad, Stick }

    private sealed class Layout
    {
        public Panel Panel = null!;
        public ListBox List = null!;
        public Button Left = null!;
        public Button Right = null!;
        public Button Above = null!;
        public Button Below = null!;
    }

    private static Layout CreateLayout(bool horizontal, GamepadNavigationMode? mode, int itemCount = 4)
    {
        Panel panel = new();
        panel.AddToRoot();
        panel.GamepadNavigationMode = mode;

        Layout layout = new() { Panel = panel };

        layout.Left = AddButton(panel, 0, 200);
        layout.Above = AddButton(panel, 300, 0);

        ListBox listBox = new();
        panel.AddChild(listBox);
        listBox.X = 300;
        listBox.Y = 200;
        listBox.Width = horizontal ? 300 : 100;
        listBox.Height = horizontal ? 60 : 150;
        for (int i = 0; i < itemCount; i++)
        {
            listBox.Items!.Add("Item " + i);
        }
        if (horizontal)
        {
            listBox.InnerPanel.ChildrenLayout = ChildrenLayout.LeftToRightStack;
            foreach (ListBoxItem item in listBox.ListBoxItems)
            {
                item.Visual.WidthUnits = DimensionUnitType.Absolute;
                item.Visual.Width = 60;
            }
        }
        layout.List = listBox;

        layout.Below = AddButton(panel, 300, 500);
        layout.Right = AddButton(panel, 700, 200);
        return layout;
    }

    private static Button AddButton(Panel panel, float x, float y)
    {
        Button button = new();
        panel.AddChild(button);
        button.X = x;
        button.Y = y;
        return button;
    }

    private static void EnterItems(ListBox listBox, int selectedIndex)
    {
        listBox.SelectedIndex = selectedIndex;
        listBox.IsFocused = true;
        listBox.DoListItemsHaveFocus = true;
        if (selectedIndex > -1)
        {
            listBox.ListBoxItems[selectedIndex].IsFocused = true;
        }
    }

    private static void Press(IInputReceiver receiver, Input input, Buttons dpad, Vector2 stick)
    {
        Gum.Input.GamePad pad = new();
        GamePadState state = input == Input.DPad
            ? new GamePadState(Vector2.Zero, Vector2.Zero, 0, 0, dpad)
            : new GamePadState(stick, Vector2.Zero, 0, 0, 0);
        pad.Activity(state, 0);
        FrameworkElement.GamePadsForUiControl.Clear();
        FrameworkElement.GamePadsForUiControl.Add(pad);
        receiver.OnFocusUpdate();
    }

    private static void PressLeft(IInputReceiver r, Input i) => Press(r, i, Buttons.DPadLeft, new Vector2(-1, 0));
    private static void PressRight(IInputReceiver r, Input i) => Press(r, i, Buttons.DPadRight, new Vector2(1, 0));
    private static void PressUp(IInputReceiver r, Input i) => Press(r, i, Buttons.DPadUp, new Vector2(0, 1));
    private static void PressDown(IInputReceiver r, Input i) => Press(r, i, Buttons.DPadDown, new Vector2(0, -1));

    private static void ShouldHaveLeftTheList(ListBox listBox, int selectedIndex)
    {
        listBox.IsFocused.ShouldBeFalse();
        listBox.DoListItemsHaveFocus.ShouldBeFalse();
        listBox.ListBoxItems.ShouldAllBe(item => !item.IsFocused);
        listBox.SelectedIndex.ShouldBe(selectedIndex);
    }

    #region Spatial: the perpendicular axis leaves

    [Theory]
    [InlineData(Input.DPad)]
    [InlineData(Input.Stick)]
    public void VerticalList_Spatial_RightLeavesToTheRight(Input input)
    {
        Layout layout = CreateLayout(horizontal: false, GamepadNavigationMode.Spatial);
        EnterItems(layout.List, 2);

        PressRight(layout.List, input);

        layout.Right.IsFocused.ShouldBeTrue();
        ShouldHaveLeftTheList(layout.List, 2);
    }

    [Theory]
    [InlineData(Input.DPad)]
    [InlineData(Input.Stick)]
    public void VerticalList_Spatial_LeftLeavesToTheLeft(Input input)
    {
        Layout layout = CreateLayout(horizontal: false, GamepadNavigationMode.Spatial);
        EnterItems(layout.List, 2);

        PressLeft(layout.List, input);

        layout.Left.IsFocused.ShouldBeTrue();
        ShouldHaveLeftTheList(layout.List, 2);
    }

    [Theory]
    [InlineData(Input.DPad)]
    [InlineData(Input.Stick)]
    public void HorizontalList_Spatial_UpLeavesUpward(Input input)
    {
        Layout layout = CreateLayout(horizontal: true, GamepadNavigationMode.Spatial);
        EnterItems(layout.List, 1);

        PressUp(layout.List, input);

        layout.Above.IsFocused.ShouldBeTrue();
        ShouldHaveLeftTheList(layout.List, 1);
    }

    [Theory]
    [InlineData(Input.DPad)]
    [InlineData(Input.Stick)]
    public void HorizontalList_Spatial_DownLeavesDownward(Input input)
    {
        Layout layout = CreateLayout(horizontal: true, GamepadNavigationMode.Spatial);
        EnterItems(layout.List, 1);

        PressDown(layout.List, input);

        layout.Below.IsFocused.ShouldBeTrue();
        ShouldHaveLeftTheList(layout.List, 1);
    }

    [Fact]
    public void VerticalList_Spatial_StickPushedMostlyRightLeaves()
    {
        Layout layout = CreateLayout(horizontal: false, GamepadNavigationMode.Spatial);
        EnterItems(layout.List, 2);

        Press(layout.List, Input.Stick, 0, new Vector2(0.9f, 0.3f));

        layout.Right.IsFocused.ShouldBeTrue();
        layout.List.SelectedIndex.ShouldBe(2);
    }

    #endregion

    #region TabOrder: the perpendicular axis does what the top level does

    [Theory]
    [InlineData(Input.DPad)]
    [InlineData(Input.Stick)]
    public void VerticalList_TabOrder_RightTabsToTheNextControl(Input input)
    {
        Layout layout = CreateLayout(horizontal: false, mode: null);
        EnterItems(layout.List, 2);

        PressRight(layout.List, input);

        layout.Below.IsFocused.ShouldBeTrue();
        ShouldHaveLeftTheList(layout.List, 2);
    }

    [Theory]
    [InlineData(Input.DPad)]
    [InlineData(Input.Stick)]
    public void VerticalList_TabOrder_LeftTabsToThePreviousControl(Input input)
    {
        Layout layout = CreateLayout(horizontal: false, mode: null);
        EnterItems(layout.List, 2);

        PressLeft(layout.List, input);

        layout.Above.IsFocused.ShouldBeTrue();
        ShouldHaveLeftTheList(layout.List, 2);
    }

    [Theory]
    [InlineData(Input.DPad)]
    [InlineData(Input.Stick)]
    public void HorizontalList_TabOrder_DownTabsToTheNextControl(Input input)
    {
        Layout layout = CreateLayout(horizontal: true, mode: null);
        EnterItems(layout.List, 1);

        PressDown(layout.List, input);

        layout.Below.IsFocused.ShouldBeTrue();
        ShouldHaveLeftTheList(layout.List, 1);
    }

    [Theory]
    [InlineData(Input.DPad)]
    [InlineData(Input.Stick)]
    public void HorizontalList_TabOrder_UpTabsToThePreviousControl(Input input)
    {
        Layout layout = CreateLayout(horizontal: true, mode: null);
        EnterItems(layout.List, 1);

        PressUp(layout.List, input);

        layout.Above.IsFocused.ShouldBeTrue();
        ShouldHaveLeftTheList(layout.List, 1);
    }

    [Fact]
    public void VerticalList_TabOrder_RightDisabledForNavigation_DoesNothing()
    {
        Layout layout = CreateLayout(horizontal: false, mode: null);
        layout.List.IsUsingLeftAndRightGamepadDirectionsForNavigation = false;
        EnterItems(layout.List, 2);

        PressRight(layout.List, Input.DPad);

        layout.List.DoListItemsHaveFocus.ShouldBeTrue();
        layout.List.SelectedIndex.ShouldBe(2);
        layout.Below.IsFocused.ShouldBeFalse();
    }

    #endregion

    #region Explicit overrides

    [Fact]
    public void VerticalList_ExplicitRightOverride_WinsOverSpatialScoring()
    {
        Layout layout = CreateLayout(horizontal: false, GamepadNavigationMode.Spatial);
        Button far = AddButton(layout.Panel, 900, 900);
        layout.List.SpatialNavigationRight = far;
        EnterItems(layout.List, 2);

        PressRight(layout.List, Input.DPad);

        far.IsFocused.ShouldBeTrue();
        layout.Right.IsFocused.ShouldBeFalse();
        ShouldHaveLeftTheList(layout.List, 2);
    }

    [Fact]
    public void VerticalList_SelfReferencingRightOverride_StaysInTheList()
    {
        Layout layout = CreateLayout(horizontal: false, GamepadNavigationMode.Spatial);
        layout.List.SpatialNavigationRight = layout.List;
        EnterItems(layout.List, 2);

        PressRight(layout.List, Input.DPad);

        layout.List.DoListItemsHaveFocus.ShouldBeTrue();
        layout.List.IsFocused.ShouldBeTrue();
        layout.List.SelectedIndex.ShouldBe(2);
    }

    #endregion

    #region The own axis, A, B and top level are unchanged

    [Theory]
    [InlineData(Input.DPad)]
    [InlineData(Input.Stick)]
    public void VerticalList_UpAndDownStillStep(Input input)
    {
        Layout layout = CreateLayout(horizontal: false, GamepadNavigationMode.Spatial);
        EnterItems(layout.List, 1);

        PressDown(layout.List, input);
        layout.List.SelectedIndex.ShouldBe(2);
        layout.List.DoListItemsHaveFocus.ShouldBeTrue();

        PressUp(layout.List, input);
        PressUp(layout.List, input);
        layout.List.SelectedIndex.ShouldBe(0);
        layout.List.ListBoxItems[0].IsFocused.ShouldBeTrue();
        layout.Above.IsFocused.ShouldBeFalse();
    }

    [Theory]
    [InlineData(Input.DPad)]
    [InlineData(Input.Stick)]
    public void HorizontalList_LeftAndRightStillStep(Input input)
    {
        Layout layout = CreateLayout(horizontal: true, GamepadNavigationMode.Spatial);
        EnterItems(layout.List, 1);

        PressRight(layout.List, input);
        layout.List.SelectedIndex.ShouldBe(2);

        PressLeft(layout.List, input);
        PressLeft(layout.List, input);
        layout.List.SelectedIndex.ShouldBe(0);
        layout.List.DoListItemsHaveFocus.ShouldBeTrue();
    }

    [Fact]
    public void VerticalList_StepAtEnd_StaysOnTheLastRow()
    {
        Layout layout = CreateLayout(horizontal: false, GamepadNavigationMode.Spatial);
        EnterItems(layout.List, 3);

        PressDown(layout.List, Input.DPad);

        layout.List.SelectedIndex.ShouldBe(3);
        layout.List.DoListItemsHaveFocus.ShouldBeTrue();
        layout.Below.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void VerticalList_LeftAndDownHeldTogether_OwnAxisWinsAndTheListStaysFocused()
    {
        Layout layout = CreateLayout(horizontal: false, GamepadNavigationMode.Spatial);
        EnterItems(layout.List, 1);

        Press(layout.List, Input.DPad, Buttons.DPadLeft | Buttons.DPadDown, Vector2.Zero);

        layout.List.SelectedIndex.ShouldBe(2);
        layout.List.DoListItemsHaveFocus.ShouldBeTrue();
        layout.Left.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void VerticalList_BLeavesItemLevelButKeepsTheListFocused()
    {
        Layout layout = CreateLayout(horizontal: false, GamepadNavigationMode.Spatial);
        EnterItems(layout.List, 2);

        Press(layout.List, Input.DPad, Buttons.B, Vector2.Zero);

        layout.List.DoListItemsHaveFocus.ShouldBeFalse();
        layout.List.IsFocused.ShouldBeTrue();
        layout.List.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void VerticalList_APushesTheFocusedRow()
    {
        Layout layout = CreateLayout(horizontal: false, GamepadNavigationMode.Spatial);
        EnterItems(layout.List, 2);
        object? pushed = null;
        layout.List.ItemPushed += (sender, _) => pushed = layout.List.SelectedObject;

        Press(layout.List, Input.DPad, Buttons.A, Vector2.Zero);

        pushed.ShouldBe("Item 2");
        layout.List.DoListItemsHaveFocus.ShouldBeFalse();
    }

    [Fact]
    public void VerticalList_TopLevel_AEntersItemsAndRightStillNavigates()
    {
        Layout layout = CreateLayout(horizontal: false, GamepadNavigationMode.Spatial);
        layout.List.SelectedIndex = 2;
        layout.List.IsFocused = true;

        Press(layout.List, Input.DPad, Buttons.A, Vector2.Zero);
        layout.List.DoListItemsHaveFocus.ShouldBeTrue();

        layout.List.DoListItemsHaveFocus = false;
        PressRight(layout.List, Input.DPad);

        layout.Right.IsFocused.ShouldBeTrue();
        layout.List.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void WrappingList_RightStillStepsToTheNeighborColumn()
    {
        Layout layout = CreateLayout(horizontal: false, GamepadNavigationMode.Spatial, itemCount: 6);
        layout.List.InnerPanel.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        layout.List.InnerPanel.WrapsChildren = true;
        layout.List.InnerPanel.StackSpacing = 0;
        foreach (ListBoxItem item in layout.List.ListBoxItems)
        {
            item.Visual.WidthUnits = DimensionUnitType.Absolute;
            item.Visual.Width = 100;
            item.Visual.Height = 20;
        }
        layout.List.InnerPanel.Width = 200;
        layout.List.InnerPanel.WidthUnits = DimensionUnitType.Absolute;
        layout.List.InnerPanel.UpdateLayout();
        EnterItems(layout.List, 0);

        PressRight(layout.List, Input.DPad);

        layout.List.SelectedIndex.ShouldBe(1);
        layout.List.DoListItemsHaveFocus.ShouldBeTrue();
        layout.Right.IsFocused.ShouldBeFalse();
    }

    #endregion

    #region Edge cases

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void VerticalList_FewItems_RightStillLeaves(int itemCount)
    {
        Layout layout = CreateLayout(horizontal: false, GamepadNavigationMode.Spatial, itemCount);
        layout.List.IsFocused = true;
        layout.List.DoListItemsHaveFocus = true;
        if (itemCount > 0)
        {
            layout.List.SelectedIndex = 0;
            layout.List.ListBoxItems[0].IsFocused = true;
        }

        PressRight(layout.List, Input.DPad);

        layout.Right.IsFocused.ShouldBeTrue();
        layout.List.DoListItemsHaveFocus.ShouldBeFalse();
        layout.List.SelectedIndex.ShouldBe(itemCount > 0 ? 0 : -1);
    }

    [Fact]
    public void VerticalList_DisabledNeighborRows_RightStillLeavesAndKeepsSelection()
    {
        Layout layout = CreateLayout(horizontal: false, GamepadNavigationMode.Spatial);
        layout.List.ListBoxItems[1].IsEnabled = false;
        layout.List.ListBoxItems[3].IsEnabled = false;
        EnterItems(layout.List, 2);

        PressRight(layout.List, Input.DPad);

        layout.Right.IsFocused.ShouldBeTrue();
        layout.List.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void VerticalList_NothingToTheRight_StaysInTheList()
    {
        Layout layout = CreateLayout(horizontal: false, GamepadNavigationMode.Spatial);
        layout.Right.IsVisible = false;
        EnterItems(layout.List, 2);

        PressRight(layout.List, Input.DPad);

        layout.List.DoListItemsHaveFocus.ShouldBeTrue();
        layout.List.IsFocused.ShouldBeTrue();
        layout.List.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void DisabledList_RightDoesNotLeave()
    {
        Layout layout = CreateLayout(horizontal: false, GamepadNavigationMode.Spatial);
        EnterItems(layout.List, 2);
        layout.List.IsEnabled = false;

        PressRight(layout.List, Input.DPad);

        layout.Right.IsFocused.ShouldBeFalse();
        layout.List.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void VerticalList_ReenteringAfterLeaving_StepsFromTheKeptSelection()
    {
        Layout layout = CreateLayout(horizontal: false, GamepadNavigationMode.Spatial);
        EnterItems(layout.List, 2);
        PressRight(layout.List, Input.DPad);
        PressLeft(layout.Right, Input.DPad);
        layout.List.IsFocused.ShouldBeTrue();

        Press(layout.List, Input.DPad, Buttons.A, Vector2.Zero);
        PressDown(layout.List, Input.DPad);

        layout.List.SelectedIndex.ShouldBe(3);
    }

    #endregion

    #region Keyboard

    private static void AddKeyboard(Keys typed)
    {
        Mock<IInputReceiverKeyboardMonoGame> keyboard = new();
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyTyped(typed)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyPushed(typed)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyDown(typed)).Returns(true);
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);
    }

    [Theory]
    [InlineData(Keys.Left)]
    [InlineData(Keys.Right)]
    public void VerticalList_KeyboardLeftRightArrowsDoNotStep(Keys key)
    {
        Layout layout = CreateLayout(horizontal: false, GamepadNavigationMode.Spatial);
        EnterItems(layout.List, 2);
        AddKeyboard(key);

        layout.List.OnFocusUpdate();

        layout.List.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void VerticalList_KeyboardDownStillSteps()
    {
        Layout layout = CreateLayout(horizontal: false, GamepadNavigationMode.Spatial);
        EnterItems(layout.List, 1);
        AddKeyboard(Keys.Down);

        layout.List.OnFocusUpdate();

        layout.List.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void VerticalList_KeyboardRightComboConfigured_Leaves()
    {
        Layout layout = CreateLayout(horizontal: false, GamepadNavigationMode.Spatial);
        FrameworkElement.RightKeyCombos.Add(new KeyCombo { PushedKey = Keys.Right });
        EnterItems(layout.List, 2);
        AddKeyboard(Keys.Right);

        layout.List.OnFocusUpdate();

        layout.Right.IsFocused.ShouldBeTrue();
        layout.List.SelectedIndex.ShouldBe(2);
    }

    #endregion

    #region ComboBox drop-down keeps stepping on both axes

    [Theory]
    [InlineData(Buttons.DPadRight, 2)]
    [InlineData(Buttons.DPadLeft, 0)]
    public void ComboBoxDropDown_LeftAndRightStillStepAndTheDropDownStaysOpen(Buttons button, int expected)
    {
        Panel panel = new();
        panel.AddToRoot();
        panel.GamepadNavigationMode = GamepadNavigationMode.Spatial;
        ComboBox comboBox = new();
        panel.AddChild(comboBox);
        comboBox.X = 100;
        comboBox.Y = 100;
        for (int i = 0; i < 4; i++)
        {
            comboBox.Items!.Add("Item " + i);
        }
        Button beside = AddButton(panel, 600, 100);
        comboBox.SelectedIndex = 1;
        comboBox.IsDropDownOpen = true;
        InteractiveGue.CurrentInputReceiver.ShouldBe(comboBox.ListBox);

        Press(InteractiveGue.CurrentInputReceiver!, Input.DPad, button, Vector2.Zero);

        comboBox.IsDropDownOpen.ShouldBeTrue();
        comboBox.SelectedIndex.ShouldBe(expected);
        beside.IsFocused.ShouldBeFalse();
    }

    #endregion
}
