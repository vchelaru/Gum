using Microsoft.Xna.Framework.Input;
using Gum.Forms.Controls;
using MonoGameGum.Input;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class KeyComboTests : BaseTestClass
{
    [Fact]
    public void IsComboPushed_ShouldReturnTrue_OnPushed()
    {
        KeyCombo sut = new ();
        sut.PushedKey = Microsoft.Xna.Framework.Input.Keys.Space;

        var mockKeyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        mockKeyboard
            .Setup(k => k.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Space))
            .Returns(true);

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        bool result = sut.IsComboPushed();
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsComboPushed_ShouldReturnTrue_OnKeyRepeat()
    {
        KeyCombo sut = new ();
        sut.PushedKey = Microsoft.Xna.Framework.Input.Keys.Space;
        sut.IsTriggeredOnRepeat = true;

        var mockKeyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        mockKeyboard
            .Setup(k => k.KeysTyped)
            .Returns(new List<Keys> { Keys.Space } );

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        bool result = sut.IsComboPushed();
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsComboPushed_ShouldReturnFalse_IfKeyWasntPushedWithNoRepeat()
    {
        KeyCombo sut = new();
        sut.PushedKey = Keys.Space;
        sut.IsTriggeredOnRepeat = false;

        var mockKeyboard = new Mock<IInputReceiverKeyboardMonoGame>();

        // It wasn't pushed this frame...
        mockKeyboard
            .Setup(k => k.KeyPushed(Keys.Space))
            .Returns(false);

        // ... but it was repeated
        mockKeyboard
            .Setup(k => k.KeyTyped(Keys.Space))
            .Returns(true);

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        bool result = sut.IsComboPushed();
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsComboPushed_ShouldReturnFalse_IfHeldKeyIsNotDown()
    {
        KeyCombo sut = new();
        sut.PushedKey = Keys.Space;
        sut.HeldKey = Keys.LeftControl; 

        var mockKeyboard = new Mock<IInputReceiverKeyboardMonoGame>();

        mockKeyboard
            .Setup(k => k.KeyDown(Keys.LeftControl))
            .Returns(false); // Held key is not down

        mockKeyboard
            .Setup(k => k.KeyPushed(Keys.Space))
            .Returns(true); // Pushed key is down

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        bool result = sut.IsComboPushed();
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsComboReleased_ShouldReturnTrue_IfKeyIsReleased()
    {
        KeyCombo sut = new();
        sut.PushedKey = Keys.Space;

        var mockKeyboard = new Mock<IInputReceiverKeyboardMonoGame>();

        mockKeyboard
            .Setup(k => k.KeyReleased(Keys.Space))
            .Returns(true); // Key was released

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

        var mockKeyboard = new Mock<IInputReceiverKeyboardMonoGame>();

        mockKeyboard
            .Setup(k => k.KeyReleased(Keys.Space))
            .Returns(true); // Main key was released
        mockKeyboard
            .Setup(k => k.KeyDown(Keys.LeftControl))
            .Returns(true); // Held key is down

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

        var mockKeyboard = new Mock<IInputReceiverKeyboardMonoGame>();

        mockKeyboard
            .Setup(k => k.KeyDown(Keys.Space))
            .Returns(true); // Main key was released
        mockKeyboard
            .Setup(k => k.KeyReleased(Keys.LeftControl))
            .Returns(true); // Held key is down

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        bool result = sut.IsComboReleased();
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsComboDown_ShouldReturnTrue_IfHeld()
    {
        KeyCombo sut = new();
        sut.PushedKey = Keys.Space;

        var mockKeyboard = new Mock<IInputReceiverKeyboardMonoGame>();

        mockKeyboard
            .Setup(k => k.KeyDown(Keys.Space))
            .Returns(true); // Key is down

        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        bool result = sut.IsComboDown();
        result.ShouldBeTrue();
    }

}
