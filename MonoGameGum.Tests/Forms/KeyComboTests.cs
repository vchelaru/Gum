using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.Input;
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
        sut.PushedKey = Microsoft.Xna.Framework.Input.Keys.Space;
        sut.HeldKey = Microsoft.Xna.Framework.Input.Keys.LeftControl;

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
        sut.PushedKey = Microsoft.Xna.Framework.Input.Keys.Space;

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
        sut.PushedKey = Microsoft.Xna.Framework.Input.Keys.Space;
        sut.HeldKey = Microsoft.Xna.Framework.Input.Keys.LeftControl;

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
        sut.PushedKey = Microsoft.Xna.Framework.Input.Keys.Space;
        sut.HeldKey = Microsoft.Xna.Framework.Input.Keys.LeftControl;

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
        sut.PushedKey = Microsoft.Xna.Framework.Input.Keys.Space;

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
        sut.PushedKey = Microsoft.Xna.Framework.Input.Keys.Space;
        sut.HeldKey = Microsoft.Xna.Framework.Input.Keys.LeftControl;

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
        sut.PushedKey = Microsoft.Xna.Framework.Input.Keys.Space;
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
        sut.PushedKey = Microsoft.Xna.Framework.Input.Keys.Space;
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
        sut.PushedKey = Microsoft.Xna.Framework.Input.Keys.Space;

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
        sut.PushedKey = Microsoft.Xna.Framework.Input.Keys.Space;
        sut.HeldKey = Microsoft.Xna.Framework.Input.Keys.LeftControl;

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
        sut.PushedKey = Microsoft.Xna.Framework.Input.Keys.Space;
        sut.HeldKey = Microsoft.Xna.Framework.Input.Keys.LeftControl;

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
        sut.PushedKey = Microsoft.Xna.Framework.Input.Keys.Space;

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
        sut.PushedKey = Microsoft.Xna.Framework.Input.Keys.Space;
        sut.HeldKey = Microsoft.Xna.Framework.Input.Keys.LeftControl;

        Mock<IInputReceiverKeyboardMonoGame> mockKeyboard = CreateKeyboardMock();
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyDown(Keys.Space)).Returns(true);
        mockKeyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyReleased(Keys.LeftControl)).Returns(true);

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        bool result = sut.IsComboReleased();
        result.ShouldBeTrue();
    }
}
