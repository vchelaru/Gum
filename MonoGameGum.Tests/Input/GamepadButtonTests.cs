using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;
using GumGamepadButton = Gum.Input.GamepadButton;
using XnaButtons = Microsoft.Xna.Framework.Input.Buttons;

namespace MonoGameGum.Tests.Input;

public class GamepadButtonTests : BaseTestClass
{
    [Fact]
    public void GamepadButton_EveryGumButtonNamePresentInXna_HasMatchingValue()
    {
        // The GUM003 analyzer migrates user code from XNA Buttons to Gum.Input.GamepadButton
        // by same-name rewrite (Buttons.A -> GamepadButton.A), and the MonoGame input driver
        // maps GamePadState to GamepadButton by name. This locks the name/value parity across
        // Gum.Input.GamepadButton and Microsoft.Xna.Framework.Input.Buttons that both rely on.
        //
        // Note: Gum.Input.GamepadButton has a few extras (LeftGrip, RightGrip) with
        // no XNA counterpart — they're skipped here because they'd never satisfy
        // the alignment check by definition. The cast for those values produces
        // an undefined XNA Buttons that MonoGame's switches don't handle (returns
        // false silently). Acceptable, since those buttons aren't surfaced by
        // standard XNA input anyway.
        Dictionary<string, int> xnaNamesToValues = new Dictionary<string, int>();
        foreach (XnaButtons xnaButton in Enum.GetValues(typeof(XnaButtons)))
        {
            xnaNamesToValues[xnaButton.ToString()] = (int)xnaButton;
        }

        foreach (GumGamepadButton gumButton in Enum.GetValues(typeof(GumGamepadButton)))
        {
            string gumName = gumButton.ToString();
            int gumValue = (int)gumButton;

            if (!xnaNamesToValues.TryGetValue(gumName, out int xnaValue))
            {
                continue;
            }

            xnaValue.ShouldBe(
                gumValue,
                customMessage: $"Gum button {gumName} has value {gumValue} but XNA " +
                    $"button {gumName} has value {xnaValue} — values must match.");
        }
    }

    [Fact]
    public void GamepadButton_ValuesForCommonButtons_MatchXnaValues()
    {
        // Hardcoded critical values for the buttons Forms code routes through the
        // IGamePad interface (FrameworkElement, ListBox, ComboBox, ButtonBase, etc.).
        // These cannot change without breaking the cast-based dispatch.
        ((int)GumGamepadButton.A).ShouldBe((int)XnaButtons.A);
        ((int)GumGamepadButton.B).ShouldBe((int)XnaButtons.B);
        ((int)GumGamepadButton.X).ShouldBe((int)XnaButtons.X);
        ((int)GumGamepadButton.Y).ShouldBe((int)XnaButtons.Y);
        ((int)GumGamepadButton.Back).ShouldBe((int)XnaButtons.Back);
        ((int)GumGamepadButton.Start).ShouldBe((int)XnaButtons.Start);
        ((int)GumGamepadButton.DPadUp).ShouldBe((int)XnaButtons.DPadUp);
        ((int)GumGamepadButton.DPadDown).ShouldBe((int)XnaButtons.DPadDown);
        ((int)GumGamepadButton.DPadLeft).ShouldBe((int)XnaButtons.DPadLeft);
        ((int)GumGamepadButton.DPadRight).ShouldBe((int)XnaButtons.DPadRight);
        ((int)GumGamepadButton.LeftShoulder).ShouldBe((int)XnaButtons.LeftShoulder);
        ((int)GumGamepadButton.RightShoulder).ShouldBe((int)XnaButtons.RightShoulder);
        ((int)GumGamepadButton.LeftTrigger).ShouldBe((int)XnaButtons.LeftTrigger);
        ((int)GumGamepadButton.RightTrigger).ShouldBe((int)XnaButtons.RightTrigger);
        ((int)GumGamepadButton.LeftStick).ShouldBe((int)XnaButtons.LeftStick);
        ((int)GumGamepadButton.RightStick).ShouldBe((int)XnaButtons.RightStick);
    }
}
