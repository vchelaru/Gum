using Gum.Forms.Controls;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System.Collections.Generic;
using Keys = Gum.Forms.Input.Keys;

namespace RaylibGum.Tests.Forms;

/// <summary>
/// End-to-end Raylib keyboard coverage for <see cref="Slider"/>. Proves the full
/// chain from a mocked <see cref="IInputReceiverKeyboard"/> through
/// <c>Slider.OnFocusUpdate</c> → <c>KeyTyped(Keys.Right)</c> advances
/// <c>Value</c> by <c>SmallChange</c> on Raylib.
/// </summary>
public class SliderTests : BaseTestClass
{
    [Fact]
    public void Slider_HandleKeyDown_RaisesKeyDownEvent()
    {
        // Covers Bucket A Site 5 guard flip: Slider.HandleKeyDown body was previously
        // #if MONOGAME && !FRB, so base.RaiseKeyDown never fired on Raylib. After
        // widening to #if !FRB (with the alias now pointing at Gum.Forms.Input.Keys),
        // the KeyEventArgs.Key type lines up with the shared Gum key space and the
        // event fires.
        Slider slider = new Slider();

        slider.Visual.ShouldNotBeNull();
        slider.AddToRoot();

        Keys? receivedKey = null;
        slider.KeyDown += (_, args) => receivedKey = args.Key;

        slider.HandleKeyDown(Keys.Right, isShiftDown: false, isAltDown: false, isCtrlDown: false);

        receivedKey.ShouldBe(Keys.Right);
    }

    [Fact]
    public void Slider_KeysTypedIteration_InvokesHandleKeyDownWithGumKeys()
    {
        // Covers Bucket A Site 4 guard removal: the `#if !RAYLIB` wrapping the
        // IInputReceiverKeyboardMonoGame cast is gone. DoKeyboardAction now iterates
        // KeysTyped as ints (Gum key space) and dispatches to HandleKeyDown on every
        // platform. This proves the iteration path runs on Raylib.
        Slider slider = new Slider();

        slider.Visual.ShouldNotBeNull();
        slider.AddToRoot();

        Keys? receivedKey = null;
        slider.KeyDown += (_, args) => receivedKey = args.Key;

        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard>();
        keyboard.Setup(k => k.KeysTyped).Returns(new List<Keys> { Keys.Right });
        keyboard.Setup(k => k.GetStringTyped()).Returns("");

        slider.DoKeyboardAction(keyboard.Object);

        receivedKey.ShouldBe(Keys.Right);
    }

    [Fact]
    public void Slider_RightArrowPressed_IncreasesValue()
    {
        Slider slider = new Slider();

        slider.Visual.ShouldNotBeNull();
        slider.AddToRoot();

        slider.Minimum = 0;
        slider.Maximum = 100;
        slider.SmallChange = 5;
        slider.Value = 50;

        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard>();
        keyboard.Setup(k => k.KeyTyped(Keys.Right)).Returns(true);
        keyboard.Setup(k => k.KeysTyped).Returns(new List<Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        slider.OnFocusUpdate();

        slider.Value.ShouldBe(55);
    }
}
