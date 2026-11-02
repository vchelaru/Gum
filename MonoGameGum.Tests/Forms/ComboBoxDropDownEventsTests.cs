using System.Collections.Generic;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using Moq;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// DropDownOpened and DropDownClosed each fire exactly once per open and per close, whichever route
/// opens or closes the drop-down (#7078). Each test also asserts IsDropDownOpen, which pins the
/// open/close behavior the events ride on.
/// </summary>
public class ComboBoxDropDownEventsTests : BaseTestClass
{
    #region Helpers

    private sealed class EventLog
    {
        public int Opened { get; private set; }
        public int Closed { get; private set; }
        public object? OpenedSender { get; private set; }
        public object? ClosedSender { get; private set; }

        public EventLog(ComboBox comboBox)
        {
            comboBox.DropDownOpened += (sender, _) => { Opened++; OpenedSender = sender; };
            comboBox.DropDownClosed += (sender, _) => { Closed++; ClosedSender = sender; };
        }
    }

    private static ComboBox CreateComboBox() => ComboBoxListBoxGamepadTests.CreateComboBox();

    private static (ContainerRuntime Container, ComboBox ComboBox) CreateInContainer()
    {
        ContainerRuntime container = new();
        Gum.GumService.Default.Root.Children!.Add(container);
        ComboBox comboBox = new();
        container.Children!.Add(comboBox.Visual);
        comboBox.Items!.Add("Apple");
        comboBox.Items.Add("Banana");
        return (container, comboBox);
    }

    private static void ShouldHaveRaised(EventLog log, int opened, int closed)
    {
        log.Opened.ShouldBe(opened);
        log.Closed.ShouldBe(closed);
    }

    #endregion

    #region Open

    [Fact]
    public void Open_FromCode_RaisesOpenedOnce()
    {
        ComboBox comboBox = CreateComboBox();
        EventLog log = new(comboBox);

        comboBox.IsDropDownOpen = true;

        comboBox.IsDropDownOpen.ShouldBeTrue();
        ShouldHaveRaised(log, opened: 1, closed: 0);
        log.OpenedSender.ShouldBe(comboBox);
    }

    [Fact]
    public void Open_ByMousePush_RaisesOpenedOnce()
    {
        ComboBox comboBox = CreateComboBox();
        EventLog log = new(comboBox);

        ComboBoxPopupFollowTests.PushOn(comboBox.Visual);

        comboBox.IsDropDownOpen.ShouldBeTrue();
        ShouldHaveRaised(log, opened: 1, closed: 0);
    }

    [Fact]
    public void Open_ByGamepad_RaisesOpenedOnce()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsFocused = true;
        EventLog log = new(comboBox);
        PadHarness pad = new();

        pad.Tap(Buttons.A);

        comboBox.IsDropDownOpen.ShouldBeTrue();
        ShouldHaveRaised(log, opened: 1, closed: 0);
    }

    [Fact]
    public void Open_ByEnterKey_RaisesOpenedOnce()
    {
        ComboBox comboBox = CreateComboBox();
        EventLog log = new(comboBox);
        Mock<Gum.Input.IInputReceiverKeyboardMonoGame> keyboard = new();
        keyboard.As<IInputReceiverKeyboard>()
            .Setup(k => k.KeyPushed(Gum.Forms.Input.Keys.Enter)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>()
            .Setup(k => k.KeysTyped).Returns(new List<Gum.Forms.Input.Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        comboBox.OnFocusUpdate();

        comboBox.IsDropDownOpen.ShouldBeTrue();
        ShouldHaveRaised(log, opened: 1, closed: 0);
    }

    [Fact]
    public void Open_WhileAlreadyOpen_DoesNotRaiseAgain()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsDropDownOpen = true;
        EventLog log = new(comboBox);

        comboBox.IsDropDownOpen = true;

        ShouldHaveRaised(log, opened: 0, closed: 0);
    }

    #endregion

    #region Close

    [Fact]
    public void Close_FromCode_RaisesClosedOnce()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsDropDownOpen = true;
        EventLog log = new(comboBox);

        comboBox.IsDropDownOpen = false;

        comboBox.IsDropDownOpen.ShouldBeFalse();
        ShouldHaveRaised(log, opened: 0, closed: 1);
        log.ClosedSender.ShouldBe(comboBox);
    }

    [Fact]
    public void Close_WhileAlreadyClosed_DoesNotRaise()
    {
        ComboBox comboBox = CreateComboBox();
        EventLog log = new(comboBox);

        comboBox.IsDropDownOpen = false;

        ShouldHaveRaised(log, opened: 0, closed: 0);
    }

    [Fact]
    public void Close_ByPushingTheComboBoxAgain_RaisesClosedOnce()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsDropDownOpen = true;
        EventLog log = new(comboBox);

        comboBox.Visual.TryCallPush();

        comboBox.IsDropDownOpen.ShouldBeFalse();
        ShouldHaveRaised(log, opened: 0, closed: 1);
    }

    [Fact]
    public void Close_ByPickingAnItemWithTheMouse_RaisesClosedOnce()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsDropDownOpen = true;
        EventLog log = new(comboBox);

        ComboBoxPopupFollowTests.PushOn(comboBox.ListBox.ListBoxItems[2].Visual);

        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.SelectedIndex.ShouldBe(2);
        ShouldHaveRaised(log, opened: 0, closed: 1);
    }

    [Fact]
    public void Close_ByPickingWithGamepadA_RaisesClosedOnce()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsFocused = true;
        PadHarness pad = new();
        pad.Tap(Buttons.A);
        EventLog log = new(comboBox);

        pad.Tap(Buttons.DPadDown);
        pad.Tap(Buttons.A);

        comboBox.IsDropDownOpen.ShouldBeFalse();
        ShouldHaveRaised(log, opened: 0, closed: 1);
    }

    [Fact]
    public void Close_ByGamepadB_RaisesClosedOnce()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsFocused = true;
        PadHarness pad = new();
        pad.Tap(Buttons.A);
        EventLog log = new(comboBox);

        pad.Tap(Buttons.B);

        comboBox.IsDropDownOpen.ShouldBeFalse();
        ShouldHaveRaised(log, opened: 0, closed: 1);
    }

    [Fact]
    public void Close_ByEscape_RaisesClosedOnce()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsFocused = true;
        comboBox.IsDropDownOpen = true;
        EventLog log = new(comboBox);
        ComboBoxEscapeTests.PushEscape();

        ComboBoxEscapeTests.RunFrame();

        comboBox.IsDropDownOpen.ShouldBeFalse();
        ShouldHaveRaised(log, opened: 0, closed: 1);
    }

    [Fact]
    public void Close_ByClickingAway_RaisesClosedOnce()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsDropDownOpen = true;
        EventLog log = new(comboBox);

        ComboBoxPopupFollowTests.PushOn(null, frames: 2);

        comboBox.IsDropDownOpen.ShouldBeFalse();
        ShouldHaveRaised(log, opened: 0, closed: 1);
    }

    [Fact]
    public void Close_ByFocusMovingToAnotherControl_RaisesClosedOnce()
    {
        ComboBox comboBox = CreateComboBox();
        Button button = new();
        button.AddToRoot();
        comboBox.IsDropDownOpen = true;
        EventLog log = new(comboBox);

        button.IsFocused = true;

        comboBox.IsDropDownOpen.ShouldBeFalse();
        ShouldHaveRaised(log, opened: 0, closed: 1);
    }

    [Fact]
    public void Close_ByDisablingWhileOpen_RaisesClosedOnce()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsDropDownOpen = true;
        EventLog log = new(comboBox);

        comboBox.IsEnabled = false;

        comboBox.IsDropDownOpen.ShouldBeFalse();
        ShouldHaveRaised(log, opened: 0, closed: 1);
    }

    [Fact]
    public void Close_ByHidingAnAncestorWhileOpen_RaisesClosedOnce()
    {
        (ContainerRuntime container, ComboBox comboBox) = CreateInContainer();
        comboBox.IsDropDownOpen = true;
        EventLog log = new(comboBox);

        container.Visible = false;

        comboBox.IsDropDownOpen.ShouldBeFalse();
        ShouldHaveRaised(log, opened: 0, closed: 1);
    }

    [Fact]
    public void Close_ByRemovingFromRootWhileOpen_RaisesClosedOnce()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsDropDownOpen = true;
        EventLog log = new(comboBox);

        comboBox.RemoveFromRoot();

        comboBox.IsDropDownOpen.ShouldBeFalse();
        ShouldHaveRaised(log, opened: 0, closed: 1);
    }

    [Fact]
    public void Close_ByReplacingTheVisualWhileOpen_RaisesClosedOnce()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsDropDownOpen = true;
        EventLog log = new(comboBox);

        comboBox.Visual = null!;

        comboBox.IsDropDownOpen.ShouldBeFalse();
        ShouldHaveRaised(log, opened: 0, closed: 1);
    }

    [Fact]
    public void Close_ByReplacingTheListBoxWhileOpen_RaisesClosedOnce()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsDropDownOpen = true;
        EventLog log = new(comboBox);

        comboBox.ListBox = new ListBox();

        comboBox.IsDropDownOpen.ShouldBeFalse();
        ShouldHaveRaised(log, opened: 0, closed: 1);
    }

    #endregion

    #region Cycles

    [Fact]
    public void OpenCloseTwice_RaisesEachEventTwiceInOrder()
    {
        ComboBox comboBox = CreateComboBox();
        List<string> order = new();
        comboBox.DropDownOpened += (_, _) => order.Add("opened");
        comboBox.DropDownClosed += (_, _) => order.Add("closed");

        comboBox.IsDropDownOpen = true;
        comboBox.IsDropDownOpen = false;
        comboBox.IsDropDownOpen = true;
        comboBox.IsDropDownOpen = false;

        order.ShouldBe(new[] { "opened", "closed", "opened", "closed" });
    }

    [Fact]
    public void Open_DoesNotRaiseSelectionChanged()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.SelectedIndex = 1;
        int selectionChanged = 0;
        comboBox.SelectionChanged += (_, _) => selectionChanged++;

        comboBox.IsDropDownOpen = true;
        comboBox.IsDropDownOpen = false;

        selectionChanged.ShouldBe(0);
    }

    #endregion
}
