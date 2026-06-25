using Gum.Input;
using Shouldly;
using WinFormsKeys = System.Windows.Forms.Keys;
using WinFormsKeyEventArgs = System.Windows.Forms.KeyEventArgs;

namespace GumToolUnitTests.Input;

public class KeysExtensionsTests : BaseTestClass
{
    [Fact]
    public void ToGumKey_Arrows_MapToMatchingGumKey()
    {
        WinFormsKeys.Up.ToGumKey().ShouldBe(GumKey.Up);
        WinFormsKeys.Down.ToGumKey().ShouldBe(GumKey.Down);
        WinFormsKeys.Left.ToGumKey().ShouldBe(GumKey.Left);
        WinFormsKeys.Right.ToGumKey().ShouldBe(GumKey.Right);
    }

    [Fact]
    public void ToGumKey_BoundNonArrowKeys_Map()
    {
        // The mapping covers every key Gum binds (any key code that is a defined GumKey), not only the
        // arrows, so the keydown handlers' keys (copy/delete/go-to-definition/etc.) resolve too.
        WinFormsKeys.C.ToGumKey().ShouldBe(GumKey.C);
        WinFormsKeys.Delete.ToGumKey().ShouldBe(GumKey.Delete);
        WinFormsKeys.F12.ToGumKey().ShouldBe(GumKey.F12);
    }

    [Fact]
    public void ToGumKey_IgnoresModifierFlags()
    {
        // The boundary passes modifier state separately, so ToGumKey strips Shift/Ctrl/Alt and
        // maps only the key code.
        (WinFormsKeys.Shift | WinFormsKeys.Up).ToGumKey().ShouldBe(GumKey.Up);
        (WinFormsKeys.Control | WinFormsKeys.Left).ToGumKey().ShouldBe(GumKey.Left);
    }

    [Fact]
    public void ToGumKey_UnmappedKey_ReturnsNull()
    {
        // A key code that is not a defined GumKey maps to null.
        WinFormsKeys.A.ToGumKey().ShouldBeNull();
        WinFormsKeys.Enter.ToGumKey().ShouldBeNull();
    }

    [Fact]
    public void ToGumKeyEventArgs_WinForms_ExtractsKeyAndModifiers()
    {
        GumKeyEventArgs neutral = new WinFormsKeyEventArgs(WinFormsKeys.X | WinFormsKeys.Control).ToGumKeyEventArgs();

        neutral.Key.ShouldBe(GumKey.X);
        neutral.IsCtrlDown.ShouldBeTrue();
        neutral.IsShiftDown.ShouldBeFalse();
        neutral.IsAltDown.ShouldBeFalse();
    }

    [Fact]
    public void ToWinFormsKeyEventArgs_RoundTrip_PreservesKeyCodeAndModifiers()
    {
        // boundary -> neutral -> impl reconstruction must reproduce the same WinForms key event the
        // matching logic originally consumed (key code + modifier flags).
        WinFormsKeyEventArgs original = new WinFormsKeyEventArgs(WinFormsKeys.C | WinFormsKeys.Control | WinFormsKeys.Shift);

        WinFormsKeyEventArgs roundTripped = original.ToGumKeyEventArgs().ToWinFormsKeyEventArgs();

        roundTripped.KeyCode.ShouldBe(WinFormsKeys.C);
        roundTripped.Modifiers.ShouldBe(WinFormsKeys.Control | WinFormsKeys.Shift);
    }
}
