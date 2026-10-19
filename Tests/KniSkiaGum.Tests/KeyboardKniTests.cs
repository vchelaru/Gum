using Gum.Input;
using Shouldly;
using System;
using System.Linq;
using XnaKeyboardState = Microsoft.Xna.Framework.Input.KeyboardState;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;
using GumKeys = Gum.Forms.Input.Keys;

namespace KniSkiaGum.Tests;

/// <summary>
/// Tests for the KNI keyboard: it casts KNI's <c>Keys</c> straight to Gum's, reads the device once
/// per frame, and leaves edge detection and repeat to <see cref="PolledKeyboard"/>.
/// </summary>
public class KeyboardKniTests
{
    [Fact]
    public void Activity_ReportsEveryGumKeyDown_WhenKniReportsTheSameNumberedKey()
    {
        // Gum's Keys enum promises the same names and values as KNI's. This holds the promise to every
        // key the keyboard supports, so a key added to either enum without the other fails here.
        GumKeys[] gumKeys = Enum.GetValues<GumKeys>().Where(key => key != GumKeys.None).ToArray();

        foreach (GumKeys gumKey in gumKeys)
        {
            Keyboard keyboard = new Keyboard { ReadKeyboard = () => new XnaKeyboardState((XnaKeys)(int)gumKey) };

            keyboard.Activity(0);

            keyboard.KeyDown(gumKey).ShouldBeTrue($"{gumKey} should be down");
        }
    }

    [Fact]
    public void Activity_ReportsOnlyTheKeysKniHasDown()
    {
        Keyboard keyboard = new Keyboard { ReadKeyboard = () => new XnaKeyboardState(XnaKeys.Left) };

        keyboard.Activity(0);

        keyboard.KeyDown(GumKeys.Left).ShouldBeTrue();
        keyboard.KeyDown(GumKeys.Right).ShouldBeFalse();
    }

    [Fact]
    public void Activity_ReadsKniOncePerFrame()
    {
        int reads = 0;
        Keyboard keyboard = new Keyboard
        {
            ReadKeyboard = () =>
            {
                reads++;
                return new XnaKeyboardState();
            }
        };

        keyboard.Activity(0);
        keyboard.Activity(0.1);

        reads.ShouldBe(2);
    }

    [Fact]
    public void Activity_DerivesPushedAndReleasedFromKniDownState()
    {
        XnaKeyboardState state = new XnaKeyboardState(XnaKeys.A);
        Keyboard keyboard = new Keyboard { ReadKeyboard = () => state };

        keyboard.Activity(0);
        keyboard.KeyPushed(GumKeys.A).ShouldBeTrue();

        state = new XnaKeyboardState();
        keyboard.Activity(0.1);
        keyboard.KeyReleased(GumKeys.A).ShouldBeTrue();
    }
}
