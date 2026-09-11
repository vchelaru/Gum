using System.Collections.Generic;
using Shouldly;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;
using Xunit;

namespace Gum.Presentation.Tests.DataUi;

public class InlineChannelsDisplayLogicTests
{
    [Fact]
    public void BuildFieldStates_ShouldReturnOneStatePerChannel_InOrder()
    {
        FakeChannelMember x = new("X") { BackingValue = 1f };
        FakeChannelMember y = new("Y") { BackingValue = 2f };

        InlineChannelFieldState[] states = InlineChannelsDisplayLogic.BuildFieldStates(new List<InstanceMember> { x, y });

        states.Length.ShouldBe(2);
        states[0].Label.ShouldBe("X");
        states[0].Text.ShouldBe("1");
        states[1].Label.ShouldBe("Y");
        states[1].Text.ShouldBe("2");
    }

    [Fact]
    public void BuildFieldStates_ShouldUseDisplayNameAsLabel_WhenCustomDisplayNameSet()
    {
        FakeChannelMember x = new("X") { BackingValue = 0f, DisplayName = "Horizontal" };

        InlineChannelFieldState[] states = InlineChannelsDisplayLogic.BuildFieldStates(new List<InstanceMember> { x });

        states[0].Label.ShouldBe("Horizontal");
    }

    [Fact]
    public void BuildFieldStates_ShouldCarryIsDefaultAndIsIndeterminate_FromEachChannel()
    {
        FakeChannelMember defaultChannel = new("X") { IsDefault = true };
        FakeChannelMember indeterminateChannel = new("Y") { IsDefault = false, IsIndeterminateOverride = true };

        InlineChannelFieldState[] states = InlineChannelsDisplayLogic.BuildFieldStates(
            new List<InstanceMember> { defaultChannel, indeterminateChannel });

        states[0].IsDefault.ShouldBeTrue();
        states[0].IsIndeterminate.ShouldBeFalse();
        states[1].IsDefault.ShouldBeFalse();
        states[1].IsIndeterminate.ShouldBeTrue();
    }

    [Theory]
    [InlineData(1f, "1")]
    [InlineData(1.5f, "1.5")]
    [InlineData(1.23456f, "1.2346")]
    [InlineData(-2f, "-2")]
    public void FormatChannelValue_ShouldFormatFloats_WithAtMostFourDecimals(float value, string expected)
    {
        string result = InlineChannelsDisplayLogic.FormatChannelValue(value);

        result.ShouldBe(expected);
    }

    [Fact]
    public void FormatChannelValue_ShouldReturnEmptyString_ForNull()
    {
        string result = InlineChannelsDisplayLogic.FormatChannelValue(null);

        result.ShouldBe(string.Empty);
    }

    private class FakeChannelMember : InstanceMember
    {
        private bool _isDefault;

        public float BackingValue { get; set; }

        public bool IsIndeterminateOverride { get; set; }

        public override bool IsDefault
        {
            get => _isDefault;
            set => _isDefault = value;
        }

        public override bool IsIndeterminate => IsIndeterminateOverride;

        public FakeChannelMember(string name) : base(name, null!)
        {
            CustomGetEvent += _ => BackingValue;
            CustomGetTypeEvent += _ => typeof(float);
            CustomSetPropertyEvent += (_, _) => { };
        }
    }
}
