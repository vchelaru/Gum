using Gum.Forms.Controls;
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
}
