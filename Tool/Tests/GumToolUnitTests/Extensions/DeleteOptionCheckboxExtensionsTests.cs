using Gum.Extensions;
using Gum.Services.Dialogs;
using Shouldly;
using System.Windows;
using System.Windows.Controls;
using Xunit;

namespace GumToolUnitTests.Extensions;

public class DeleteOptionCheckboxExtensionsTests
{
    [StaFact]
    public void ToCheckBox_ShouldCarryCheckedState()
    {
        CheckBox checkBox = new DeleteOptionCheckboxViewModel { Label = "Anything", IsChecked = true }
            .ToCheckBox();

        checkBox.IsChecked.ShouldBe(true);
    }

    [StaFact]
    public void ToCheckBox_ShouldNotConstrainWidth_SoLongLabelsAreNotClipped()
    {
        CheckBox checkBox = new DeleteOptionCheckboxViewModel
        {
            Label = "Delete custom code file (contains your code)"
        }.ToCheckBox();

        double.IsNaN(checkBox.Width).ShouldBeTrue();
    }

    [StaFact]
    public void ToCheckBox_ShouldLeftAlign_SoTheLabelDoesNotFloatFromTheEdge()
    {
        CheckBox checkBox = new DeleteOptionCheckboxViewModel { Label = "Anything" }.ToCheckBox();

        checkBox.HorizontalAlignment.ShouldBe(HorizontalAlignment.Left);
    }

    [StaFact]
    public void ToCheckBox_ShouldWrapLabel()
    {
        CheckBox checkBox = new DeleteOptionCheckboxViewModel
        {
            Label = "Delete custom code file (contains your code)"
        }.ToCheckBox();

        TextBlock content = checkBox.Content.ShouldBeOfType<TextBlock>();
        content.Text.ShouldBe("Delete custom code file (contains your code)");
        content.TextWrapping.ShouldBe(TextWrapping.Wrap);
    }

    /// <summary>
    /// The themed CheckBox template measures its content in an Auto column, so TextWrapping alone
    /// never engages - an explicit constraint on the TextBlock is what makes the label wrap.
    /// </summary>
    [StaFact]
    public void ToCheckBox_ShouldConstrainLabelWidth_SoWrappingActuallyEngages()
    {
        CheckBox checkBox = new DeleteOptionCheckboxViewModel { Label = "Anything" }.ToCheckBox();

        TextBlock content = checkBox.Content.ShouldBeOfType<TextBlock>();
        double.IsPositiveInfinity(content.MaxWidth).ShouldBeFalse();
    }
}
