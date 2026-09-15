using Gum.Input;
using Gum.Managers;
using Shouldly;

namespace Gum.Presentation.Tests.Managers;

public class KeyCombinationFormatterTests
{
    [Theory]
    [InlineData(KeyDisplayStyle.Windows, "Ctrl+Shift+Z")]
    [InlineData(KeyDisplayStyle.MacOS, "⇧⌘Z")]
    public void Format_PrimaryAndShiftWithKey_NamesThePlatformModifiers(KeyDisplayStyle style, string expected)
    {
        KeyCombination combo = new KeyCombination { Key = GumKey.Z, IsCtrlDown = true, IsShiftDown = true };
        KeyCombinationFormatter formatter = new KeyCombinationFormatter(style);

        formatter.Format(combo).ShouldBe(expected);
    }

    [Theory]
    [InlineData(KeyDisplayStyle.Windows, "Alt+Up")]
    [InlineData(KeyDisplayStyle.MacOS, "⌥↑")]
    public void Format_AltWithArrow_UsesThePlatformArrowName(KeyDisplayStyle style, string expected)
    {
        KeyCombination combo = KeyCombination.Alt(GumKey.Up);
        KeyCombinationFormatter formatter = new KeyCombinationFormatter(style);

        formatter.Format(combo).ShouldBe(expected);
    }

    [Theory]
    [InlineData(KeyDisplayStyle.Windows, "Shift")]
    [InlineData(KeyDisplayStyle.MacOS, "⇧")]
    public void Format_ModifierOnly_ShowsJustTheModifier(KeyDisplayStyle style, string expected)
    {
        KeyCombination combo = KeyCombination.Shift();
        KeyCombinationFormatter formatter = new KeyCombinationFormatter(style);

        formatter.Format(combo).ShouldBe(expected);
    }

    [Theory]
    [InlineData(KeyDisplayStyle.Windows, "Ctrl+Numpad +")]
    [InlineData(KeyDisplayStyle.MacOS, "⌘Numpad +")]
    public void Format_NumpadAdd_UsesAReadableKeyName(KeyDisplayStyle style, string expected)
    {
        KeyCombination combo = KeyCombination.Ctrl(GumKey.Add);
        KeyCombinationFormatter formatter = new KeyCombinationFormatter(style);

        formatter.Format(combo).ShouldBe(expected);
    }

    [Fact]
    public void Format_KeyOnly_ShowsTheKey()
    {
        KeyCombination combo = KeyCombination.Pressed(GumKey.F12);
        KeyCombinationFormatter formatter = new KeyCombinationFormatter(KeyDisplayStyle.MacOS);

        formatter.Format(combo).ShouldBe("F12");
    }
}
