using Gum.Forms.Controls;
using Gum.Wireframe;
using Gum.Input;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Keys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Tests for <see cref="KeyCombo"/> predicates. Mocks are configured on the
/// <see cref="IInputReceiverKeyboard"/> base interface (via <c>Mock.As</c>) because
/// <see cref="KeyCombo"/> now calls the shared <see cref="Keys"/>-typed overloads rather
/// than the MonoGame-specific XNA-typed ones on <see cref="IInputReceiverKeyboardMonoGame"/>.
/// </summary>
public class KeyComboTests : BaseTestClass
{
    private static Mock<IInputReceiverKeyboardMonoGame> CreateKeyboardMock()
    {
        return new Mock<IInputReceiverKeyboardMonoGame>();
    }

    [Fact]
    public void IsComboDown_BothKeysDown_ReturnsTrue()
    {
        KeyCombo sut = new();
        sut.PushedKey = Keys.Space;
        sut.HeldKey = Keys.LeftControl;

        Mock<IInputReceiverKeyboardMonoGame> mockKeyboard = CreateKeyboardMock();
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyDown(Keys.Space)).Returns(true);
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyDown(Keys.LeftControl)).Returns(true);

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        bool result = sut.IsComboDown();
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsComboDown_ShouldReturnTrue_IfHeld()
    {
        KeyCombo sut = new();
        sut.PushedKey = Keys.Space;

        Mock<IInputReceiverKeyboardMonoGame> mockKeyboard = CreateKeyboardMock();
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyDown(Keys.Space)).Returns(true);

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        bool result = sut.IsComboDown();
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsComboPushed_HeldKeyDownAndPushedKeyPressed_ReturnsTrue()
    {
        KeyCombo sut = new();
        sut.PushedKey = Keys.Space;
        sut.HeldKey = Keys.LeftControl;

        Mock<IInputReceiverKeyboardMonoGame> mockKeyboard = CreateKeyboardMock();
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyDown(Keys.LeftControl)).Returns(true);
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyPushed(Keys.Space)).Returns(true);

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        bool result = sut.IsComboPushed();
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsComboPushed_HeldKeyUp_ReturnsFalse()
    {
        KeyCombo sut = new();
        sut.PushedKey = Keys.Space;
        sut.HeldKey = Keys.LeftControl;

        Mock<IInputReceiverKeyboardMonoGame> mockKeyboard = CreateKeyboardMock();
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyDown(Keys.LeftControl)).Returns(false);
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyPushed(Keys.Space)).Returns(true);

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        bool result = sut.IsComboPushed();
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsComboPushed_NoHeldKeyAndPushedKeyPressed_ReturnsTrue()
    {
        KeyCombo sut = new();
        sut.PushedKey = Keys.Space;

        Mock<IInputReceiverKeyboardMonoGame> mockKeyboard = CreateKeyboardMock();
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyPushed(Keys.Space)).Returns(true);

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        bool result = sut.IsComboPushed();
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsComboPushed_ShouldReturnFalse_IfHeldKeyIsNotDown()
    {
        KeyCombo sut = new();
        sut.PushedKey = Keys.Space;
        sut.HeldKey = Keys.LeftControl;

        Mock<IInputReceiverKeyboardMonoGame> mockKeyboard = CreateKeyboardMock();
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyDown(Keys.LeftControl)).Returns(false);
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyPushed(Keys.Space)).Returns(true);

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        bool result = sut.IsComboPushed();
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsComboPushed_ShouldReturnFalse_IfKeyWasntPushedWithNoRepeat()
    {
        KeyCombo sut = new();
        sut.PushedKey = Keys.Space;
        sut.IsTriggeredOnRepeat = false;

        Mock<IInputReceiverKeyboardMonoGame> mockKeyboard = CreateKeyboardMock();
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyPushed(Keys.Space)).Returns(false);
        // The key was repeated this frame, but IsTriggeredOnRepeat is false
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeysTyped).Returns(new List<Keys> { Keys.Space });

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        bool result = sut.IsComboPushed();
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsComboPushed_ShouldReturnTrue_OnKeyRepeat()
    {
        KeyCombo sut = new();
        sut.PushedKey = Keys.Space;
        sut.IsTriggeredOnRepeat = true;

        Mock<IInputReceiverKeyboardMonoGame> mockKeyboard = CreateKeyboardMock();
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeysTyped).Returns(new List<Keys> { Keys.Space });

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        bool result = sut.IsComboPushed();
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsComboPushed_ShouldReturnTrue_OnPushed()
    {
        KeyCombo sut = new();
        sut.PushedKey = Keys.Space;

        Mock<IInputReceiverKeyboardMonoGame> mockKeyboard = CreateKeyboardMock();
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyPushed(Keys.Space)).Returns(true);

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        bool result = sut.IsComboPushed();
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsComboReleased_HeldKeyReleasedWhilePushedKeyDown_ReturnsTrue()
    {
        KeyCombo sut = new();
        sut.PushedKey = Keys.Space;
        sut.HeldKey = Keys.LeftControl;

        Mock<IInputReceiverKeyboardMonoGame> mockKeyboard = CreateKeyboardMock();
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyReleased(Keys.LeftControl)).Returns(true);
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyDown(Keys.Space)).Returns(true);

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        bool result = sut.IsComboReleased();
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsComboReleased_ShouldReturnTrue_IfHeldIsTrueAndReleasedMainKey()
    {
        KeyCombo sut = new();
        sut.PushedKey = Keys.Space;
        sut.HeldKey = Keys.LeftControl;

        Mock<IInputReceiverKeyboardMonoGame> mockKeyboard = CreateKeyboardMock();
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyReleased(Keys.Space)).Returns(true);
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyDown(Keys.LeftControl)).Returns(true);

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        bool result = sut.IsComboReleased();
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsComboReleased_ShouldReturnTrue_IfKeyIsReleased()
    {
        KeyCombo sut = new();
        sut.PushedKey = Keys.Space;

        Mock<IInputReceiverKeyboardMonoGame> mockKeyboard = CreateKeyboardMock();
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyReleased(Keys.Space)).Returns(true);

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        bool result = sut.IsComboReleased();
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsComboReleased_ShouldReturnTrue_IfMainIsTrueAndReleasedHeld()
    {
        KeyCombo sut = new();
        sut.PushedKey = Keys.Space;
        sut.HeldKey = Keys.LeftControl;

        Mock<IInputReceiverKeyboardMonoGame> mockKeyboard = CreateKeyboardMock();
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyDown(Keys.Space)).Returns(true);
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyReleased(Keys.LeftControl)).Returns(true);

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        bool result = sut.IsComboReleased();
        result.ShouldBeTrue();
    }

    private static Mock<IInputReceiverKeyboard> AddKeyboard()
    {
        Mock<IInputReceiverKeyboardMonoGame> mockKeyboard = CreateKeyboardMock();
        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);
        return mockKeyboard.As<IInputReceiverKeyboard>();
    }

    private static Button CreateFocusedButtonWithClickCombos(out Func<int> getClicks)
    {
        Button button = new Button();
        button.AddToRoot();
        button.IsFocused = true;
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        getClicks = () => clicks;
        FrameworkElement.ClickCombos.Clear();
        FrameworkElement.ClickCombos.Add(new KeyCombo { PushedKey = Keys.Enter });
        FrameworkElement.ClickCombos.Add(new KeyCombo { PushedKey = Keys.Space });
        return button;
    }

    [Theory]
    [InlineData(Keys.LeftShift)]
    [InlineData(Keys.RightShift)]
    [InlineData(Keys.LeftControl)]
    [InlineData(Keys.RightControl)]
    [InlineData(Keys.LeftAlt)]
    [InlineData(Keys.RightAlt)]
    public void IsComboPushed_NoHeldKeyAndModifierDown_ReturnsFalse(Keys modifier)
    {
        KeyCombo sut = new() { PushedKey = Keys.Tab };
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeyPushed(Keys.Tab)).Returns(true);
        keyboard.Setup(k => k.KeyDown(modifier)).Returns(true);

        sut.IsComboPushed().ShouldBeFalse();
    }

    [Fact]
    public void IsComboPushed_NoHeldKeyAndNonModifierDown_ReturnsTrue()
    {
        KeyCombo sut = new() { PushedKey = Keys.Tab };
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeyPushed(Keys.Tab)).Returns(true);
        keyboard.Setup(k => k.KeyDown(Keys.A)).Returns(true);

        sut.IsComboPushed().ShouldBeTrue();
    }

    [Fact]
    public void IsComboPushed_NoHeldKeyAndPushedKeyIsTheModifier_ReturnsTrue()
    {
        KeyCombo sut = new() { PushedKey = Keys.LeftShift };
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeyPushed(Keys.LeftShift)).Returns(true);
        keyboard.Setup(k => k.KeyDown(Keys.LeftShift)).Returns(true);

        sut.IsComboPushed().ShouldBeTrue();
    }

    [Fact]
    public void IsComboPushed_HeldKeyDownAndOtherModifierDown_StillReturnsTrue()
    {
        KeyCombo sut = new() { PushedKey = Keys.C, HeldKey = Keys.LeftControl };
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeyPushed(Keys.C)).Returns(true);
        keyboard.Setup(k => k.KeyDown(Keys.LeftControl)).Returns(true);
        keyboard.Setup(k => k.KeyDown(Keys.LeftShift)).Returns(true);

        sut.IsComboPushed().ShouldBeTrue();
    }

    [Fact]
    public void IsComboPushed_RepeatTypedNoHeldKeyAndModifierDown_ReturnsFalse()
    {
        KeyCombo sut = new() { PushedKey = Keys.Tab, IsTriggeredOnRepeat = true };
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeysTyped).Returns(new List<Keys> { Keys.Tab });
        keyboard.Setup(k => k.KeyDown(Keys.LeftShift)).Returns(true);

        sut.IsComboPushed().ShouldBeFalse();
    }

    [Fact]
    public void IsComboPushed_RepeatTypedNoHeldKeyAndNoModifier_ReturnsTrue()
    {
        KeyCombo sut = new() { PushedKey = Keys.Tab, IsTriggeredOnRepeat = true };
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeysTyped).Returns(new List<Keys> { Keys.Tab });

        sut.IsComboPushed().ShouldBeTrue();
    }

    [Fact]
    public void IsComboDown_NoHeldKeyAndModifierDown_ReturnsFalse()
    {
        KeyCombo sut = new() { PushedKey = Keys.Space };
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeyDown(Keys.Space)).Returns(true);
        keyboard.Setup(k => k.KeyDown(Keys.LeftControl)).Returns(true);

        sut.IsComboDown().ShouldBeFalse();
    }

    [Fact]
    public void IsComboReleased_NoHeldKeyAndModifierDown_ReturnsFalse()
    {
        KeyCombo sut = new() { PushedKey = Keys.Space };
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeyReleased(Keys.Space)).Returns(true);
        keyboard.Setup(k => k.KeyDown(Keys.LeftControl)).Returns(true);

        sut.IsComboReleased().ShouldBeFalse();
    }

    [Fact]
    public void IsComboReleased_NoHeldKeyAndNoModifierDown_ReturnsTrue()
    {
        KeyCombo sut = new() { PushedKey = Keys.Space };
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeyReleased(Keys.Space)).Returns(true);

        sut.IsComboReleased().ShouldBeTrue();
    }

    [Fact]
    public void DefaultTabKeyCombos_DoNotMatchTabWithAnyModifierHeld()
    {
        foreach (Keys modifier in new[] { Keys.LeftShift, Keys.RightShift, Keys.LeftControl, Keys.RightControl, Keys.LeftAlt, Keys.RightAlt })
        {
            FrameworkElement.KeyboardsForUiControl.Clear();
            Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
            keyboard.Setup(k => k.KeyPushed(Keys.Tab)).Returns(true);
            keyboard.Setup(k => k.KeyDown(modifier)).Returns(true);

            FrameworkElement.TabKeyCombos.Any(combo => combo.IsComboPushed()).ShouldBeFalse(modifier.ToString());
        }
    }

    [Theory]
    [InlineData(Keys.Enter)]
    [InlineData(Keys.Space)]
    public void ClickCombos_PlainKey_Clicks(Keys key)
    {
        Button button = CreateFocusedButtonWithClickCombos(out Func<int> getClicks);
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeyPushed(key)).Returns(true);

        button.OnFocusUpdate();

        getClicks().ShouldBe(1);
    }

    [Theory]
    [InlineData(Keys.Enter, Keys.LeftControl)]
    [InlineData(Keys.Enter, Keys.RightAlt)]
    [InlineData(Keys.Space, Keys.LeftShift)]
    [InlineData(Keys.Space, Keys.RightControl)]
    public void ClickCombos_KeyWithModifierHeld_DoesNotClick(Keys key, Keys modifier)
    {
        Button button = CreateFocusedButtonWithClickCombos(out Func<int> getClicks);
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeyPushed(key)).Returns(true);
        keyboard.Setup(k => k.KeyDown(modifier)).Returns(true);

        button.OnFocusUpdate();

        getClicks().ShouldBe(0);
    }

    [Fact]
    public void ClickCombos_ExplicitCtrlHeldKey_ClicksOnCtrlEnter()
    {
        Button button = CreateFocusedButtonWithClickCombos(out Func<int> getClicks);
        FrameworkElement.ClickCombos.Clear();
        FrameworkElement.ClickCombos.Add(new KeyCombo { PushedKey = Keys.Enter, HeldKey = Keys.LeftControl });
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeyPushed(Keys.Enter)).Returns(true);
        keyboard.Setup(k => k.KeyDown(Keys.LeftControl)).Returns(true);

        button.OnFocusUpdate();

        getClicks().ShouldBe(1);
    }
}
