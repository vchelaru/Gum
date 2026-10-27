using System.Collections.Generic;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Moq;
using MonoGameGum.Tests.Input;
using Shouldly;
using Xunit;
using GamePad = Gum.Input.GamePad;

namespace MonoGameGum.Tests.Forms;

/// <summary>Drives one gamepad through the focused input receiver, the way a game frame does.</summary>
internal sealed class PadHarness
{
    private readonly GamePad _pad = new();

    public PadHarness() => FrameworkElement.GamePadsForUiControl.Add(_pad);

    public void Frame(Buttons buttons = 0)
    {
        _pad.Activity(new GamePadState(Vector2.Zero, Vector2.Zero, 0, 0, buttons), 0);
        InteractiveGue.CurrentInputReceiver?.OnFocusUpdate();
    }

    /// <summary>Pushes for one frame, then releases for one frame.</summary>
    public void Tap(Buttons buttons)
    {
        Frame(buttons);
        Frame();
    }

    /// <summary>Holds for the given frames, like a human press, then releases.</summary>
    public void Hold(Buttons buttons, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            Frame(buttons);
        }
        Frame();
    }
}

/// <summary>Records the last visual state a control applied.</summary>
internal sealed class StateLog
{
    public string? Last { get; private set; }

    public StateLog(FrameworkElement element)
    {
        string[] names =
        {
            FrameworkElement.EnabledStateName, FrameworkElement.HighlightedStateName,
            FrameworkElement.PushedStateName, FrameworkElement.DisabledStateName,
            FrameworkElement.DisabledFocusedStateName, FrameworkElement.FocusedStateName,
            FrameworkElement.HighlightedFocusedStateName,
        };
        foreach (string name in names)
        {
            string captured = name;
            element.GetState(captured)!.Apply = () => Last = captured;
        }
    }
}

/// <summary>
/// The three reported bugs: #6931 (ComboBox stays Pushed after a pick with A), #6932 (a drop-down opened
/// without the gamepad ignores the gamepad), #6933 (gamepad stepping lands on disabled items).
/// </summary>
public class ComboBoxListBoxGamepadTests : BaseTestClass
{
    internal static ComboBox CreateComboBox(int itemCount = 4)
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        for (int i = 0; i < itemCount; i++)
        {
            comboBox.Items!.Add("Item " + i);
        }
        return comboBox;
    }

    internal static ListBox CreateListBox(int itemCount = 4)
    {
        ListBox listBox = new();
        listBox.AddToRoot();
        for (int i = 0; i < itemCount; i++)
        {
            listBox.Items!.Add("Item " + i);
        }
        return listBox;
    }

    #region 6931: Pushed state after picking with A

    [Fact]
    public void PickWithA_AHeldThenReleased_ComboBoxReturnsToFocused()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.SelectedIndex = 0;
        comboBox.IsFocused = true;
        StateLog log = new(comboBox);
        PadHarness pad = new();

        pad.Hold(Buttons.A, 3);
        pad.Tap(Buttons.DPadDown);
        pad.Hold(Buttons.A, 8);

        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.SelectedIndex.ShouldBe(1);
        comboBox.IsFocused.ShouldBeTrue();
        log.Last.ShouldBe(FrameworkElement.FocusedStateName);
    }

    [Fact]
    public void PickWithA_SingleFrameTap_ComboBoxReturnsToFocused()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.SelectedIndex = 0;
        comboBox.IsFocused = true;
        StateLog log = new(comboBox);
        PadHarness pad = new();

        pad.Tap(Buttons.A);
        pad.Tap(Buttons.DPadDown);
        pad.Tap(Buttons.A);

        comboBox.IsDropDownOpen.ShouldBeFalse();
        log.Last.ShouldBe(FrameworkElement.FocusedStateName);
    }

    [Fact]
    public void CloseWithB_ComboBoxIsFocused()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsFocused = true;
        StateLog log = new(comboBox);
        PadHarness pad = new();

        pad.Tap(Buttons.A);
        pad.Hold(Buttons.B, 4);

        comboBox.IsDropDownOpen.ShouldBeFalse();
        log.Last.ShouldBe(FrameworkElement.FocusedStateName);
    }

    #endregion

    #region 6932: drop-down opened without the gamepad

    [Fact]
    public void OpenedFromCode_ComboBoxFocused_DownThenA_SelectsAndCloses()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.SelectedIndex = 0;
        comboBox.IsFocused = true;
        PadHarness pad = new();

        comboBox.IsDropDownOpen = true;
        pad.Tap(Buttons.DPadDown);
        comboBox.SelectedIndex.ShouldBe(1);
        pad.Tap(Buttons.A);

        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.SelectedIndex.ShouldBe(1);
        comboBox.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void OpenedFromCode_NothingFocused_DownMovesSelection()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.SelectedIndex = 0;
        InteractiveGue.CurrentInputReceiver = null;
        PadHarness pad = new();

        comboBox.IsDropDownOpen = true;
        pad.Tap(Buttons.DPadDown);

        comboBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void OpenedFromCode_NothingFocused_AClosesAndLeavesNothingFocused()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.SelectedIndex = 0;
        InteractiveGue.CurrentInputReceiver = null;
        PadHarness pad = new();

        comboBox.IsDropDownOpen = true;
        pad.Tap(Buttons.DPadDown);
        pad.Tap(Buttons.A);

        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.SelectedIndex.ShouldBe(1);
        InteractiveGue.CurrentInputReceiver.ShouldNotBe(comboBox.ListBox);
    }

    [Fact]
    public void OpenedFromCode_OtherButtonFocused_GamepadDoesNotReachCoveredControls()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.SelectedIndex = 0;
        Button save = new();
        save.AddToRoot();
        Button cancel = new();
        cancel.AddToRoot();
        int clicks = 0;
        save.Click += (_, _) => clicks++;
        cancel.Click += (_, _) => clicks++;
        save.IsFocused = true;
        PadHarness pad = new();

        comboBox.IsDropDownOpen = true;
        pad.Tap(Buttons.DPadDown);
        pad.Tap(Buttons.A);

        clicks.ShouldBe(0);
        cancel.IsFocused.ShouldBeFalse();
        comboBox.SelectedIndex.ShouldBe(1);
        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void OpenedFromCode_OtherButtonFocused_FocusReturnsToItAfterPick()
    {
        ComboBox comboBox = CreateComboBox();
        Button save = new();
        save.AddToRoot();
        save.IsFocused = true;
        PadHarness pad = new();

        comboBox.IsDropDownOpen = true;
        pad.Tap(Buttons.A);

        comboBox.IsDropDownOpen.ShouldBeFalse();
        save.IsFocused.ShouldBeTrue();
        InteractiveGue.CurrentInputReceiver.ShouldBe(save);
    }

    [Fact]
    public void OpenedFromCode_BCloses()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.SelectedIndex = 0;
        comboBox.IsFocused = true;
        PadHarness pad = new();

        comboBox.IsDropDownOpen = true;
        pad.Tap(Buttons.B);

        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void OpenedWithMouse_OtherButtonFocused_GamepadOperatesDropDown()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.SelectedIndex = 0;
        Button save = new();
        save.AddToRoot();
        Button cancel = new();
        cancel.AddToRoot();
        int clicks = 0;
        cancel.Click += (_, _) => clicks++;
        save.IsFocused = true;
        PadHarness pad = new();

        PushOn(comboBox.Visual);
        comboBox.IsDropDownOpen.ShouldBeTrue();
        pad.Tap(Buttons.DPadDown);
        pad.Tap(Buttons.A);

        clicks.ShouldBe(0);
        comboBox.SelectedIndex.ShouldBe(1);
        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void OpenedFromCode_KeyboardDown_MovesSelection()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.SelectedIndex = 0;
        comboBox.IsFocused = true;
        Mock<IInputReceiverKeyboardMonoGame> keyboard = new();
        keyboard.As<IInputReceiverKeyboard>()
            .Setup(k => k.KeyTyped(Gum.Forms.Input.Keys.Down)).Returns(true);
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        comboBox.IsDropDownOpen = true;
        InteractiveGue.CurrentInputReceiver!.OnFocusUpdate();

        comboBox.SelectedIndex.ShouldBe(1);
    }

    #endregion

    #region 6933: disabled items

    private static void Disable(ListBox listBox, params int[] rows)
    {
        foreach (int row in rows)
        {
            listBox.ListBoxItems[row].IsEnabled = false;
        }
    }

    [Fact]
    public void ListBox_DownSkipsDisabledItem()
    {
        ListBox listBox = CreateListBox();
        Disable(listBox, 2);
        listBox.SelectedIndex = 0;
        listBox.IsFocused = true;
        listBox.DoListItemsHaveFocus = true;
        PadHarness pad = new();

        pad.Tap(Buttons.DPadDown);
        listBox.SelectedIndex.ShouldBe(1);
        pad.Tap(Buttons.DPadDown);
        listBox.SelectedIndex.ShouldBe(3);
        listBox.ListBoxItems[3].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void ListBox_UpSkipsDisabledItem()
    {
        ListBox listBox = CreateListBox();
        Disable(listBox, 2);
        listBox.SelectedIndex = 3;
        listBox.IsFocused = true;
        listBox.DoListItemsHaveFocus = true;
        PadHarness pad = new();

        pad.Tap(Buttons.DPadUp);

        listBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void ListBox_DisabledItemsAtEnd_DownStaysOnLastEnabled()
    {
        ListBox listBox = CreateListBox();
        Disable(listBox, 2, 3);
        listBox.SelectedIndex = 1;
        listBox.IsFocused = true;
        listBox.DoListItemsHaveFocus = true;
        PadHarness pad = new();

        pad.Tap(Buttons.DPadDown);
        pad.Tap(Buttons.DPadDown);

        listBox.SelectedIndex.ShouldBe(1);
        listBox.ListBoxItems[1].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void ListBox_ConfirmAfterSteppingOverDisabled_DoesNotSelectDisabled()
    {
        ListBox listBox = CreateListBox();
        Disable(listBox, 2);
        listBox.SelectedIndex = 1;
        listBox.IsFocused = true;
        listBox.DoListItemsHaveFocus = true;
        PadHarness pad = new();

        pad.Tap(Buttons.DPadDown);
        pad.Tap(Buttons.A);

        listBox.SelectedIndex.ShouldBe(3);
    }

    [Fact]
    public void ListBox_KeyboardDownSkipsDisabledItem()
    {
        ListBox listBox = CreateListBox();
        Disable(listBox, 2);
        listBox.SelectedIndex = 1;
        listBox.IsFocused = true;
        listBox.DoListItemsHaveFocus = true;
        Mock<IInputReceiverKeyboardMonoGame> keyboard = new();
        keyboard.As<IInputReceiverKeyboard>()
            .Setup(k => k.KeyTyped(Gum.Forms.Input.Keys.Down)).Returns(true);
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        listBox.OnFocusUpdate();

        listBox.SelectedIndex.ShouldBe(3);
    }

    [Fact]
    public void ComboBoxOpenedWithA_DownSkipsDisabledItem()
    {
        ComboBox comboBox = CreateComboBox();
        Disable(comboBox.ListBox, 2);
        comboBox.SelectedIndex = 0;
        comboBox.IsFocused = true;
        PadHarness pad = new();

        pad.Tap(Buttons.A);
        pad.Tap(Buttons.DPadDown);
        pad.Tap(Buttons.DPadDown);
        pad.Tap(Buttons.A);

        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.SelectedIndex.ShouldBe(3);
    }

    [Fact]
    public void ComboBoxFocusedWhileOpen_DownSkipsDisabledItem()
    {
        ComboBox comboBox = CreateComboBox();
        Disable(comboBox.ListBox, 2);
        comboBox.SelectedIndex = 1;
        comboBox.IsDropDownOpen = true;
        comboBox.IsFocused = true;
        PadHarness pad = new();

        pad.Tap(Buttons.DPadDown);

        comboBox.SelectedIndex.ShouldBe(3);
    }

    [Fact]
    public void ComboBoxFocusedWhileOpen_UpSkipsDisabledItem()
    {
        ComboBox comboBox = CreateComboBox();
        Disable(comboBox.ListBox, 2);
        comboBox.SelectedIndex = 3;
        comboBox.IsDropDownOpen = true;
        comboBox.IsFocused = true;
        PadHarness pad = new();

        pad.Tap(Buttons.DPadUp);

        comboBox.SelectedIndex.ShouldBe(1);
    }

    #endregion

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
}
