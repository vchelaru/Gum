using Gum.Forms.Controls;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Keys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>Enter and Space activate a focused Button, CheckBox and RadioButton of every packaged theme.</summary>
public class ThemeButtonKeyboardActivationTests : FileThemeInputTestBase
{
    public static IEnumerable<object[]> ThemesAndKeys() =>
        RepresentativeThemes().SelectMany(theme => new[] { Keys.Enter, Keys.Space }.Select(key => new object[] { theme[0], key }));

    [Theory]
    [MemberData(nameof(ThemesAndKeys))]
    public void Button_EnterOrSpace_RaisesClickOnce(string theme, Keys key)
    {
        Button button = PlaceAlone<Button>(theme, "/ButtonStandard|/Button");
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        button.IsFocused = true;
        Driver.Step();

        Press(key);

        clicks.ShouldBe(1, $"{Label(theme)} {key}");
    }

    [Theory]
    [InlineData(Keys.Enter)]
    [InlineData(Keys.Space)]
    public void CodeBuiltButton_EnterOrSpace_RaisesClickOnce(Keys key)
    {
        Button button = new();
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        button.AddToRoot();
        button.IsFocused = true;
        Driver.Step();

        Press(key);

        clicks.ShouldBe(1, key.ToString());
    }

    [Theory]
    [MemberData(nameof(ThemesAndKeys))]
    public void CheckBox_EnterOrSpace_TogglesIsChecked(string theme, Keys key)
    {
        CheckBox checkBox = PlaceAlone<CheckBox>(theme, "/CheckBox");
        checkBox.IsFocused = true;
        Driver.Step();
        bool before = checkBox.IsChecked == true;

        Press(key);
        (checkBox.IsChecked == true).ShouldBe(!before, $"{Label(theme)} {key} first press");

        Press(key);
        (checkBox.IsChecked == true).ShouldBe(before, $"{Label(theme)} {key} second press");
    }

    [Theory]
    [MemberData(nameof(ThemesAndKeys))]
    public void RadioButton_EnterOrSpace_ChecksIt(string theme, Keys key)
    {
        RadioButton radio = PlaceAlone<RadioButton>(theme, "/RadioButton");
        radio.IsFocused = true;
        Driver.Step();

        Press(key);

        (radio.IsChecked == true).ShouldBeTrue($"{Label(theme)} {key}");
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void Button_DisabledButFocused_IgnoresEnter(string theme)
    {
        Button button = PlaceAlone<Button>(theme, "/ButtonStandard|/Button");
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        button.AllowFocusWhenDisabled = true;
        button.IsEnabled = false;
        button.IsFocused = true;
        Driver.Step();

        Press(Keys.Enter);

        clicks.ShouldBe(0, Label(theme));
    }
}
