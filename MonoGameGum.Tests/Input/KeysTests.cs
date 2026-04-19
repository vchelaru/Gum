using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;
using GumKeys = Gum.Forms.Input.Keys;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace MonoGameGum.Tests.Input;

public class KeysTests : BaseTestClass
{
    [Fact]
    public void Keys_EveryGumKey_HasMatchingXnaKey()
    {
        // Every Gum key must have an XNA counterpart with the same numeric value.
        // This enforces the commitment that Gum.Forms.Input.Keys mirrors XNA Keys exactly —
        // no Gum-only keys allowed.
        Array xnaValues = Enum.GetValues(typeof(XnaKeys));
        HashSet<int> xnaValueSet = new HashSet<int>();
        Dictionary<int, string> xnaNamesByValue = new Dictionary<int, string>();
        foreach (object xnaValueObject in xnaValues)
        {
            int xnaValue = (int)xnaValueObject;
            xnaValueSet.Add(xnaValue);
            xnaNamesByValue[xnaValue] = xnaValueObject.ToString()!;
        }

        foreach (GumKeys gumKey in Enum.GetValues(typeof(GumKeys)))
        {
            int gumValue = (int)gumKey;
            string gumName = gumKey.ToString();

            xnaValueSet.ShouldContain(
                gumValue,
                customMessage: $"Gum key {gumName} (value {gumValue}) has no XNA counterpart.");

            string xnaName = xnaNamesByValue[gumValue];
            xnaName.ShouldBe(
                gumName,
                customMessage: $"Gum key {gumName} (value {gumValue}) matches XNA value but " +
                    $"the XNA name is {xnaName} — names must be identical.");
        }
    }

    [Fact]
    public void Keys_EveryXnaKey_HasMatchingGumKey()
    {
        // Every XNA key must exist in Gum's enum with the same name AND value.
        // Belt-and-suspenders completeness check. Runs via reflection in both directions
        // to catch any typo or omission in the hand-authored Gum enum.
        Array xnaValues = Enum.GetValues(typeof(XnaKeys));

        Dictionary<string, int> gumNamesToValues = new Dictionary<string, int>();
        foreach (GumKeys gumKey in Enum.GetValues(typeof(GumKeys)))
        {
            gumNamesToValues[gumKey.ToString()] = (int)gumKey;
        }

        foreach (object xnaKeyObject in xnaValues)
        {
            string xnaName = xnaKeyObject.ToString()!;
            int xnaValue = (int)xnaKeyObject;

            gumNamesToValues.ShouldContainKey(
                xnaName,
                customMessage: $"XNA key {xnaName} (value {xnaValue}) has no matching Gum key.");

            gumNamesToValues[xnaName].ShouldBe(
                xnaValue,
                customMessage: $"Gum key {xnaName} has value {gumNamesToValues[xnaName]} " +
                    $"but XNA key {xnaName} has value {xnaValue} — values must match.");
        }
    }

    [Fact]
    public void Keys_ValueForEnter_Matches13()
    {
        // Hardcoded critical values to catch any accidental renumbering during authoring.
        // These values are baked into user KeyCombo serialization and may never change.
        ((int)GumKeys.Enter).ShouldBe(13);
        ((int)GumKeys.Back).ShouldBe(8);
        ((int)GumKeys.Tab).ShouldBe(9);
        ((int)GumKeys.Escape).ShouldBe(27);
        ((int)GumKeys.Space).ShouldBe(32);
        ((int)GumKeys.A).ShouldBe(65);
        ((int)GumKeys.Z).ShouldBe(90);
        ((int)GumKeys.Left).ShouldBe(37);
        ((int)GumKeys.Up).ShouldBe(38);
        ((int)GumKeys.Right).ShouldBe(39);
        ((int)GumKeys.Down).ShouldBe(40);
        ((int)GumKeys.F1).ShouldBe(112);
        ((int)GumKeys.F12).ShouldBe(123);
        ((int)GumKeys.OemTilde).ShouldBe(192);
    }
}
