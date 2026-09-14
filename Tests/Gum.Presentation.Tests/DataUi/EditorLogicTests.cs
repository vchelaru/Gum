using System.Collections.Generic;
using System.Linq;
using Shouldly;
using WpfDataUi;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests.DataUi;

/// <summary>The combo-box, slider, and text helpers that both heads' editors share.</summary>
public class EditorLogicTests
{
    private enum SampleEnum
    {
        Alpha,
        Beta
    }

    [Fact]
    public void ComboBoxDisplayLogic_GetOptions_PrefersCustomOptions_ThenEnumValues_ThenNullableSentinel()
    {
        ComboBoxDisplayLogic logic = new ComboBoxDisplayLogic();
        InstanceMember withOptions = new InstanceMember { CustomOptions = new List<object> { "One", "Two" } };

        logic.GetOptions(withOptions, typeof(SampleEnum)).ShouldBe(new object[] { "One", "Two" });
        logic.GetOptions(new InstanceMember(), typeof(SampleEnum)).ShouldBe(new object[] { SampleEnum.Alpha, SampleEnum.Beta });
        logic.GetOptions(new InstanceMember(), typeof(SampleEnum?))
            .ShouldBe(new object[] { ComboBoxDisplayLogic.NullSentinel, SampleEnum.Alpha, SampleEnum.Beta });
        logic.GetOptions(new InstanceMember(), typeof(int?)).ShouldBeEmpty();
        logic.GetOptions(null, null).ShouldBeEmpty();
    }

    [Fact]
    public void ComboBoxDisplayLogic_GetItemToSelect_MapsNullNullableEnumToTheSentinel()
    {
        ComboBoxDisplayLogic logic = new ComboBoxDisplayLogic();

        logic.GetItemToSelect(null, typeof(SampleEnum?)).ShouldBe(ComboBoxDisplayLogic.NullSentinel);
        logic.GetItemToSelect(null, typeof(string)).ShouldBeNull();
        logic.GetItemToSelect(SampleEnum.Beta, typeof(SampleEnum?)).ShouldBe(SampleEnum.Beta);
    }

    [Fact]
    public void SliderDisplayLogic_ScalesToAndFromTheShownValue_KeepingTheType()
    {
        SliderDisplayLogic logic = new SliderDisplayLogic { DisplayedValueMultiplier = 100 };

        logic.ToDisplayedValue(0.5f).ShouldBe(50.0);
        logic.ToInstanceValue(50f).ShouldBe(0.5f);
        logic.ToInstanceValue(250).ShouldBe(2);
        logic.ToInstanceValue(null).ShouldBeNull();
    }

    [Fact]
    public void SliderDisplayLogic_WithMultiplierOne_PassesValuesThrough()
    {
        SliderDisplayLogic logic = new SliderDisplayLogic();

        logic.ToDisplayedValue(7).ShouldBe(7);
        logic.ToInstanceValue(7).ShouldBe(7);
        logic.ToSliderPosition(7m).ShouldBe(7.0);
        logic.ToSliderPosition("text").ShouldBeNull();
    }

    [Fact]
    public void SliderDisplayLogic_FormatSliderValue_UsesWholeNumbersForIntegerTypes()
    {
        SliderDisplayLogic logic = new SliderDisplayLogic { DecimalPointsFromSlider = 1 };

        logic.FormatSliderValue(12.75, typeof(int)).ShouldBe("12");
        logic.FormatSliderValue(12.75, typeof(float)).ShouldBe(12.75.ToString("f1"));
    }

    [Theory]
    [InlineData("WidthUnits", "Width Units")]
    [InlineData("Already Spaced", "Already Spaced")]
    [InlineData("X", "X")]
    [InlineData("", "")]
    public void DataUiText_InsertSpacesInCamelCase(string input, string expected)
    {
        DataUiText.InsertSpacesInCamelCase(input).ShouldBe(expected);
    }
}
