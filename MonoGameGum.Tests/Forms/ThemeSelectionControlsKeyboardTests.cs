using Gum.Forms.Controls;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Keys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>Arrow, Enter, Space and Escape keys on Slider, ListBox and ComboBox of every packaged theme.</summary>
public class ThemeSelectionControlsKeyboardTests : ThemeKeyboardTestBase
{
    [Theory]
    [MemberData(nameof(Themes))]
    public void Slider_RightAndLeftArrows_StepTheValue(string theme)
    {
        Slider slider = PlaceAlone<Slider>(theme, "/Slider");
        slider.Minimum = 0; slider.Maximum = 100; slider.Value = 50; slider.SmallChange = 5;
        slider.IsFocused = true;
        Driver.Step();

        Press(Keys.Right);
        slider.Value.ShouldBe(55, $"{Label(theme)} Right");

        Press(Keys.Left);
        Press(Keys.Left);
        slider.Value.ShouldBe(45, $"{Label(theme)} Left x2");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void Slider_ArrowAtTheMaximum_ClampsToRange(string theme)
    {
        Slider slider = PlaceAlone<Slider>(theme, "/Slider");
        slider.Minimum = 0; slider.Maximum = 100; slider.Value = 100; slider.SmallChange = 5;
        slider.IsFocused = true;
        Driver.Step();

        Press(Keys.Right);

        slider.Value.ShouldBe(100, Label(theme));
    }

    private ListBox FilledListBox(string theme)
    {
        ListBox listBox = PlaceAlone<ListBox>(theme, "/ListBox");
        for (int i = 0; i < 5; i++) { listBox.Items.Add("Item " + i); }
        Driver.Step(2);
        listBox.SelectedIndex = 0;
        listBox.IsFocused = true;
        Driver.Step();
        Press(Keys.Enter); // a focused list hands focus to its items on the first activation

        return listBox;
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ListBox_DownAndUp_SelectionFollows(string theme)
    {
        ListBox listBox = FilledListBox(theme);

        Press(Keys.Down);
        Press(Keys.Down);
        listBox.SelectedIndex.ShouldBe(2, $"{Label(theme)} Down x2");

        Press(Keys.Up);
        listBox.SelectedIndex.ShouldBe(1, $"{Label(theme)} Up");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ListBox_UpAtTheFirstItem_StaysOnTheFirstItem(string theme)
    {
        ListBox listBox = FilledListBox(theme);

        Press(Keys.Up);

        listBox.SelectedIndex.ShouldBe(0, Label(theme));
    }

    private ComboBox FilledComboBox(string theme)
    {
        ComboBox combo = PlaceAlone<ComboBox>(theme, "/ComboBox");
        for (int i = 0; i < 4; i++) { combo.Items.Add("Choice " + i); }
        Driver.Step(2);
        combo.SelectedIndex = 0;
        combo.IsFocused = true;
        Driver.Step();
        return combo;
    }

    public static IEnumerable<object[]> ThemesAndOpenKeys() =>
        Themes().SelectMany(theme => new[] { Keys.Enter, Keys.Space }.Select(key => new object[] { theme[0], key }));

    [Theory]
    [MemberData(nameof(ThemesAndOpenKeys))]
    public void ComboBox_EnterOrSpace_OpensTheDropDown(string theme, Keys key)
    {
        ComboBox combo = FilledComboBox(theme);

        Press(key);

        combo.IsDropDownOpen.ShouldBeTrue($"{Label(theme)} {key}");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ComboBox_DownThenEnterInOpenDropDown_CommitsTheItemAndCloses(string theme)
    {
        ComboBox combo = FilledComboBox(theme);
        Press(Keys.Enter);
        combo.IsDropDownOpen.ShouldBeTrue(Label(theme));

        Press(Keys.Down);
        Press(Keys.Down);
        Press(Keys.Enter);

        combo.IsDropDownOpen.ShouldBeFalse($"{Label(theme)} still open after Enter");
        combo.SelectedIndex.ShouldBe(2, Label(theme));
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ComboBox_EscapeInOpenDropDown_ClosesAndKeepsFocus(string theme)
    {
        ComboBox combo = FilledComboBox(theme);
        Press(Keys.Enter);
        Press(Keys.Down);

        Press(Keys.Escape);

        combo.IsDropDownOpen.ShouldBeFalse(Label(theme));
        combo.IsFocused.ShouldBeTrue($"{Label(theme)}: focus returns to the ComboBox");
    }
}
