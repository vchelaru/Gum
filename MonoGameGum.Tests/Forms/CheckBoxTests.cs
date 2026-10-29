using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

public class CheckBoxTests : BaseTestClass
{
    [Fact]
    public void CheckBox_TwoState_ShouldCycleFalseTrue()
    {
        var checkBox = new CheckBox();
        checkBox.IsThreeState = false;
        checkBox.IsChecked = false;

        checkBox.Visual.CallClick();
        checkBox.IsChecked.ShouldBe(true);

        checkBox.Visual.CallClick();
        checkBox.IsChecked.ShouldBe(false);
    }

    [Fact]
    public void CheckBox_ThreeState_ShouldCycleFalseTrueNull()
    {
        var checkBox = new CheckBox();
        checkBox.IsThreeState = true;
        checkBox.IsChecked = false;

        // Cycle 1: False -> True
        checkBox.Visual.CallClick();
        checkBox.IsChecked.ShouldBe(true);

        // Cycle 2: True -> Null (Indeterminate)
        checkBox.Visual.CallClick();
        checkBox.IsChecked.ShouldBe(null);

        // Cycle 3: Null -> False
        checkBox.Visual.CallClick();
        checkBox.IsChecked.ShouldBe(false);
    }

    static CheckBoxVisual VisualOf(CheckBox checkBox) => (CheckBoxVisual)checkBox.Visual;

    [Fact]
    public void CheckBox_Visual_ShouldShowCheckOnlyWhenCheckedOrIndeterminate()
    {
        CheckBox checkBox = new();
        checkBox.IsThreeState = true;
        checkBox.IsChecked = false;
        VisualOf(checkBox).InnerCheck.Visible.ShouldBeFalse();

        checkBox.IsChecked = true;
        VisualOf(checkBox).InnerCheck.Visible.ShouldBeTrue();

        checkBox.IsChecked = null;
        VisualOf(checkBox).InnerCheck.Visible.ShouldBeTrue();

        checkBox.IsChecked = false;
        VisualOf(checkBox).InnerCheck.Visible.ShouldBeFalse();
    }

    [Fact]
    public void CheckBox_Click_ShouldUpdateCheckVisual()
    {
        CheckBox checkBox = new();
        checkBox.IsChecked = false;

        checkBox.Visual.CallClick();

        VisualOf(checkBox).InnerCheck.Visible.ShouldBeTrue();
    }

    [Fact]
    public void CheckBox_Disabled_ShouldChangeBackgroundColor_AndKeepCheckVisible()
    {
        CheckBox checkBox = new();
        checkBox.IsChecked = true;
        var enabledColor = VisualOf(checkBox).CheckBoxBackground.Color;

        checkBox.IsEnabled = false;

        VisualOf(checkBox).CheckBoxBackground.Color.ShouldNotBe(enabledColor);
        VisualOf(checkBox).InnerCheck.Visible.ShouldBeTrue();
    }
}
