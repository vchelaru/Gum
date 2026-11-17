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

    [Fact]
    public void IsComboPushed_ExcludedKeyDown_ReturnsFalse()
    {
        KeyCombo sut = new() { PushedKey = Keys.Tab, ExcludedHeldKeys = new[] { Keys.LeftShift } };
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeyPushed(Keys.Tab)).Returns(true);
        keyboard.Setup(k => k.KeyDown(Keys.LeftShift)).Returns(true);

        sut.IsComboPushed().ShouldBeFalse();
    }

    [Fact]
    public void IsComboPushed_ExcludedKeyNotDown_ReturnsTrue()
    {
        KeyCombo sut = new() { PushedKey = Keys.Tab, ExcludedHeldKeys = new[] { Keys.LeftShift } };
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeyPushed(Keys.Tab)).Returns(true);

        sut.IsComboPushed().ShouldBeTrue();
    }

    [Fact]
    public void IsComboPushed_SecondExcludedKeyDown_ReturnsFalse()
    {
        KeyCombo sut = new() { PushedKey = Keys.Tab, ExcludedHeldKeys = new[] { Keys.LeftShift, Keys.RightAlt } };
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeyPushed(Keys.Tab)).Returns(true);
        keyboard.Setup(k => k.KeyDown(Keys.RightAlt)).Returns(true);

        sut.IsComboPushed().ShouldBeFalse();
    }

    [Fact]
    public void IsComboPushed_EmptyExcludedList_BehavesLikeNoExclusion()
    {
        KeyCombo sut = new() { PushedKey = Keys.Tab, ExcludedHeldKeys = new Keys[0] };
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeyPushed(Keys.Tab)).Returns(true);
        keyboard.Setup(k => k.KeyDown(Keys.LeftShift)).Returns(true);

        sut.IsComboPushed().ShouldBeTrue();
    }

    [Fact]
    public void IsComboPushed_NoExclusionAndOtherKeyHeld_StillMatches()
    {
        KeyCombo sut = new() { PushedKey = Keys.Tab };
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeyPushed(Keys.Tab)).Returns(true);
        keyboard.Setup(k => k.KeyDown(Keys.LeftShift)).Returns(true);

        sut.IsComboPushed().ShouldBeTrue();
    }

    [Fact]
    public void IsComboPushed_HeldKeyDownAndExcludedKeyDown_ReturnsFalse()
    {
        KeyCombo sut = new() { PushedKey = Keys.Tab, HeldKey = Keys.LeftControl, ExcludedHeldKeys = new[] { Keys.LeftShift } };
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeyPushed(Keys.Tab)).Returns(true);
        keyboard.Setup(k => k.KeyDown(Keys.LeftControl)).Returns(true);
        keyboard.Setup(k => k.KeyDown(Keys.LeftShift)).Returns(true);

        sut.IsComboPushed().ShouldBeFalse();
    }

    [Fact]
    public void IsComboPushed_HeldKeyDownAndExcludedKeyUp_ReturnsTrue()
    {
        KeyCombo sut = new() { PushedKey = Keys.Tab, HeldKey = Keys.LeftControl, ExcludedHeldKeys = new[] { Keys.LeftShift } };
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeyPushed(Keys.Tab)).Returns(true);
        keyboard.Setup(k => k.KeyDown(Keys.LeftControl)).Returns(true);

        sut.IsComboPushed().ShouldBeTrue();
    }

    [Fact]
    public void IsComboPushed_RepeatTypedWithExcludedKeyDown_ReturnsFalse()
    {
        KeyCombo sut = new() { PushedKey = Keys.Tab, IsTriggeredOnRepeat = true, ExcludedHeldKeys = new[] { Keys.LeftShift } };
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeysTyped).Returns(new List<Keys> { Keys.Tab });
        keyboard.Setup(k => k.KeyDown(Keys.LeftShift)).Returns(true);

        sut.IsComboPushed().ShouldBeFalse();
    }

    [Fact]
    public void IsComboPushed_RepeatTypedWithExcludedKeyUp_ReturnsTrue()
    {
        KeyCombo sut = new() { PushedKey = Keys.Tab, IsTriggeredOnRepeat = true, ExcludedHeldKeys = new[] { Keys.LeftShift } };
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeysTyped).Returns(new List<Keys> { Keys.Tab });

        sut.IsComboPushed().ShouldBeTrue();
    }

    [Fact]
    public void IsComboDown_ExcludedKeyDown_ReturnsFalse()
    {
        KeyCombo sut = new() { PushedKey = Keys.Space, ExcludedHeldKeys = new[] { Keys.LeftControl } };
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeyDown(Keys.Space)).Returns(true);
        keyboard.Setup(k => k.KeyDown(Keys.LeftControl)).Returns(true);

        sut.IsComboDown().ShouldBeFalse();
    }

    [Fact]
    public void IsComboDown_ExcludedKeyNotDown_ReturnsTrue()
    {
        KeyCombo sut = new() { PushedKey = Keys.Space, ExcludedHeldKeys = new[] { Keys.LeftControl } };
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeyDown(Keys.Space)).Returns(true);

        sut.IsComboDown().ShouldBeTrue();
    }

    [Fact]
    public void IsComboReleased_ExcludedKeyDown_ReturnsFalse()
    {
        KeyCombo sut = new() { PushedKey = Keys.Space, ExcludedHeldKeys = new[] { Keys.LeftControl } };
        Mock<IInputReceiverKeyboard> keyboard = AddKeyboard();
        keyboard.Setup(k => k.KeyReleased(Keys.Space)).Returns(true);
        keyboard.Setup(k => k.KeyDown(Keys.LeftControl)).Returns(true);

        sut.IsComboReleased().ShouldBeFalse();
    }

    [Fact]
    public void IsComboReleased_ExcludedKeyNotDown_ReturnsTrue()
    {
        KeyCombo sut = new() { PushedKey = Keys.Space, ExcludedHeldKeys = new[] { Keys.LeftControl } };
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
}
