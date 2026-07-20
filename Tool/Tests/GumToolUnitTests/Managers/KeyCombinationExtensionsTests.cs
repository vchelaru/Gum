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
}
