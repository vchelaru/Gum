using Gum.Forms.Controls;
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

    private static void PushEscape()
    {
        Mock<IInputReceiverKeyboardMonoGame> keyboard = new();
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyPushed(Keys.Escape)).Returns(true);
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);
    }

    // The receiver chain InteractiveGue runs each frame: preview (stops when handled), then update.
    private static bool RunFrame()
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
}
