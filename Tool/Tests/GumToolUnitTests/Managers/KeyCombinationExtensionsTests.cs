using Gum.Input;
using Gum.Managers;
using Shouldly;

namespace GumToolUnitTests.Managers;

public class KeyCombinationExtensionsTests : BaseTestClass
{
    [Fact]
    public void IsPressed_GumKeyEventArgs_KeyAndModifiersMatch_ReturnsTrue()
    {
        var combo = KeyCombination.Ctrl(GumKey.Z);
        var args = new GumKeyEventArgs { Key = GumKey.Z, IsCtrlDown = true };

        combo.IsPressed(args).ShouldBeTrue();
    }

    [Fact]
    public void IsPressed_GumKeyEventArgs_KeyMatchesButRequiredModifierMissing_ReturnsFalse()
    {
        var combo = KeyCombination.Ctrl(GumKey.Z);
        var args = new GumKeyEventArgs { Key = GumKey.Z, IsCtrlDown = false };

        combo.IsPressed(args).ShouldBeFalse();
    }

    [Fact]
    public void IsPressed_GumKeyEventArgs_ModifiersMatchButKeyDiffers_ReturnsFalse()
    {
        var combo = KeyCombination.Ctrl(GumKey.Z);
        var args = new GumKeyEventArgs { Key = GumKey.Y, IsCtrlDown = true };

        combo.IsPressed(args).ShouldBeFalse();
    }

    [Fact]
    public void IsPressed_GumKeyEventArgs_NoKeyOnCombination_MatchesOnModifiersAlone()
    {
        // e.g. KeyCombination.Alt() with no key - used for "is Alt held" checks.
        var combo = KeyCombination.Alt();
        var args = new GumKeyEventArgs { Key = GumKey.Up, IsAltDown = true };

        combo.IsPressed(args).ShouldBeTrue();
    }

    [Fact]
    public void IsPressed_GumKeyEventArgs_KeyedCombinationWithUnrequestedModifierDown_ReturnsFalse()
    {
        // Windows reports AltGr as Ctrl+Alt, so a Ctrl shortcut that ignored the extra Alt would
        // swallow every AltGr character a non-US layout types (AltGr+E is the Euro sign).
        KeyCombination combo = KeyCombination.Ctrl(GumKey.E);
        GumKeyEventArgs args = new() { Key = GumKey.E, IsCtrlDown = true, IsAltDown = true };

        combo.IsPressed(args).ShouldBeFalse();
    }

    [Fact]
    public void IsPressed_GumKeyEventArgs_NoKeyOnCombinationWithExtraModifierDown_StillMatches()
    {
        // A modifier-only combination qualifies a mouse gesture rather than naming a shortcut, so
        // holding something else alongside it must not stop it matching.
        KeyCombination combo = KeyCombination.Shift();
        GumKeyEventArgs args = new() { Key = GumKey.Up, IsShiftDown = true, IsCtrlDown = true };

        combo.IsPressed(args).ShouldBeTrue();
    }
}
