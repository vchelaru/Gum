using EditorTabPlugin_XNA.ExtensionMethods;
using Gum.Input;
using Shouldly;
using WinFormsKeys = System.Windows.Forms.Keys;

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
        // Only the keys the wireframe nudge path binds are mapped; everything else is null.
        WinFormsKeys.A.ToGumKey().ShouldBeNull();
        WinFormsKeys.Enter.ToGumKey().ShouldBeNull();
    }
}
