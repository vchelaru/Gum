using Gum.Forms.Controls;
using System.Collections.Generic;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using Shouldly;
using Xunit;
using Keys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>Escape closes an open drop-down without changing the selection, and is left alone otherwise (#6972).</summary>
public class ComboBoxEscapeTests : BaseTestClass
{
    private static ComboBox CreateComboBox()
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        for (int i = 0; i < 4; i++)
        {
            comboBox.Items!.Add("Item " + i);
        }
        comboBox.SelectedIndex = 1;
        comboBox.IsFocused = true;
        return comboBox;
    }

    internal static void PushEscape()
    {
        Mock<IInputReceiverKeyboardMonoGame> keyboard = new();
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyPushed(Keys.Escape)).Returns(true);
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);
    }

    // The receiver chain InteractiveGue runs each frame: preview (stops when handled), then update.
    internal static bool RunFrame()
    {
        IInputReceiver receiver = InteractiveGue.CurrentInputReceiver!;
        RoutedEventArgs args = new();
        receiver.OnFocusUpdatePreview(args);
        if (!args.Handled)
        {
            receiver.OnFocusUpdate();
        }
        return args.Handled;
    }

    [Fact]
    public void Escape_OpenedFromCode_ClosesWithoutChangingSelection()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsDropDownOpen = true;
        PushEscape();

        bool handled = RunFrame();

        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.SelectedIndex.ShouldBe(1);
        handled.ShouldBeTrue();
    }

    [Fact]
    public void Escape_OpenedByClick_ClosesAndFocusReturnsToComboBox()
    {
        ComboBox comboBox = CreateComboBox();
        ComboBoxListBoxGamepadTests.PushOn(comboBox.Visual);
        comboBox.IsDropDownOpen.ShouldBeTrue();
        PushEscape();

        RunFrame();

        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.SelectedIndex.ShouldBe(1);
        comboBox.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void Escape_ComboBoxFocusedWhileOpen_Closes()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsDropDownOpen = true;
        comboBox.IsFocused = true;
        InteractiveGue.CurrentInputReceiver.ShouldBe(comboBox);
        PushEscape();

        bool handled = RunFrame();

        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.SelectedIndex.ShouldBe(1);
        handled.ShouldBeTrue();
    }

    [Fact]
    public void Escape_DropDownClosed_IsNotConsumedAndDoesNothing()
    {
        ComboBox comboBox = CreateComboBox();
        PushEscape();

        bool handled = RunFrame();

        handled.ShouldBeFalse();
        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void Escape_PlainListBoxItemLevel_StillLeavesItemLevel()
    {
        ListBox listBox = new();
        listBox.AddToRoot();
        listBox.Items!.Add("a");
        listBox.IsFocused = true;
        listBox.DoListItemsHaveFocus = true;
        PushEscape();

        bool handled = RunFrame();

        handled.ShouldBeTrue();
        listBox.SelectedIndex.ShouldBe(-1);
    }

    // Previews run outermost first, as InteractiveGue does each frame.
    private static bool RunFrameThroughParents()
    {
        IInputReceiver receiver = InteractiveGue.CurrentInputReceiver!;
        List<IInputReceiver> chain = new() { receiver };
        for (IInputReceiver? parent = receiver.ParentInputReceiver; parent != null; parent = parent.ParentInputReceiver)
        {
            chain.Insert(0, parent);
        }
        foreach (IInputReceiver link in chain)
        {
            RoutedEventArgs args = new();
            link.OnFocusUpdatePreview(args);
            if (args.Handled)
            {
                return true;
            }
        }
        receiver.OnFocusUpdate();
        return false;
    }

    [Fact]
    public void Escape_ComboBoxFocusedWhileOpenInsideScrollViewer_ClosesDropDown()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.AddToRoot();
        ComboBox comboBox = new();
        scrollViewer.AddChild(comboBox);
        comboBox.Items!.Add("a");
        comboBox.Items.Add("b");
        comboBox.SelectedIndex = 1;
        comboBox.IsDropDownOpen = true;
        comboBox.IsFocused = true;
        InteractiveGue.CurrentInputReceiver.ShouldBe(comboBox);
        PushEscape();

        RunFrameThroughParents();

        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.SelectedIndex.ShouldBe(1);
        // The ComboBox consumed the key, so the ScrollViewer did not take focus.
        comboBox.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void Escape_ComboBoxFocusedInsideScrollViewerDropDownClosed_StillLeavesScrollViewerItems()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.AddToRoot();
        ComboBox comboBox = new();
        scrollViewer.AddChild(comboBox);
        comboBox.IsFocused = true;
        PushEscape();

        bool handled = RunFrameThroughParents();

        handled.ShouldBeTrue();
    }

    [Fact]
    public void Escape_DisabledWhileOpen_ClosedByDisableThenEscapeDoesNothing()
    {
        ComboBox comboBox = CreateComboBox();
        comboBox.IsDropDownOpen = true;
        comboBox.IsEnabled = false;
        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.AllowFocusWhenDisabled = true;
        comboBox.IsFocused = true;
        PushEscape();

        bool handled = RunFrame();

        handled.ShouldBeFalse();
        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void MouseOpened_FocusIsOnThePopupList()
    {
        ComboBox comboBox = CreateComboBox();
        ComboBoxListBoxGamepadTests.PushOn(comboBox.Visual);

        comboBox.IsDropDownOpen.ShouldBeTrue();
        InteractiveGue.CurrentInputReceiver.ShouldBe(comboBox.ListBox);
    }
}
