using System;
using Shouldly;
using WpfDataUi;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace GumToolUnitTests.PropertyGridHelpers;

/// <summary>
/// Verifies that <see cref="SliderDisplay"/> preserves a value that falls outside its
/// [MinValue, MaxValue] slider range. The slider thumb may peg at the boundary, but the
/// value the control reports must stay intact — when WPF coerces the slider position into
/// range, that coerced position must not be echoed back over the real value (a silent
/// data-loss bug).
/// </summary>
public class SliderDisplayRangeTests : BaseTestClass
{
    [StaFact]
    public void TrySetValueOnUi_ValueAboveMax_PreservesReportedValue()
    {
        SliderDisplay display = new SliderDisplay();
        display.MinValue = 0;
        display.MaxValue = 255;

        // Seed the slider with an in-range value so the out-of-range push below
        // produces a real slider-value change (and thus the coercion that triggers
        // the clobber). Without a prior in-range value the slider never moves.
        InstanceMember member = MakeFloatMember(100f);
        display.InstanceMember = member;

        display.TrySetValueOnUi(300f);

        ApplyValueResult result = display.TryGetValueOnUi(out object? shown);

        result.ShouldBe(ApplyValueResult.Success);
        Convert.ToSingle(shown).ShouldBe(300f);
    }

    [StaFact]
    public void TrySetValueOnUi_ValueBelowMin_PreservesReportedValue()
    {
        SliderDisplay display = new SliderDisplay();
        display.MinValue = 10;
        display.MaxValue = 255;

        InstanceMember member = MakeFloatMember(200f);
        display.InstanceMember = member;

        display.TrySetValueOnUi(5f);

        ApplyValueResult result = display.TryGetValueOnUi(out object? shown);

        result.ShouldBe(ApplyValueResult.Success);
        Convert.ToSingle(shown).ShouldBe(5f);
    }

    private static InstanceMember MakeFloatMember(float value)
    {
        InstanceMember member = new InstanceMember { Name = "StrokeWidth" };
        member.CustomGetTypeEvent += _ => typeof(float);
        member.CustomGetEvent += _ => value;
        return member;
    }
}
