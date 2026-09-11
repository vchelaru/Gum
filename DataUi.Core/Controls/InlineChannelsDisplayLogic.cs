using System;
using System.Collections.Generic;
using System.Globalization;
using WpfDataUi.DataTypes;

namespace WpfDataUi.Controls
{
    /// <summary>
    /// Display state for one field of an <see cref="InlineChannelsDisplay"/> composite row: the channel's
    /// label, its formatted text, and its default/indeterminate state.
    /// </summary>
    public readonly struct InlineChannelFieldState
    {
        public string Label { get; }
        public string Text { get; }
        public bool IsDefault { get; }
        public bool IsIndeterminate { get; }

        public InlineChannelFieldState(string label, string text, bool isDefault, bool isIndeterminate)
        {
            Label = label;
            Text = text;
            IsDefault = isDefault;
            IsIndeterminate = isIndeterminate;
        }
    }

    /// <summary>
    /// Pure, WPF-control-agnostic logic behind <see cref="InlineChannelsDisplay"/>: given a composite
    /// member's channels, decides what each field's label, formatted text, and default/indeterminate
    /// state should be. Kept separate from the control so it is testable without a WPF UserControl.
    /// </summary>
    public static class InlineChannelsDisplayLogic
    {
        public static InlineChannelFieldState[] BuildFieldStates(IReadOnlyList<InstanceMember> channelMembers)
        {
            InlineChannelFieldState[] states = new InlineChannelFieldState[channelMembers.Count];

            for (int i = 0; i < channelMembers.Count; i++)
            {
                InstanceMember channel = channelMembers[i];
                states[i] = new InlineChannelFieldState(
                    channel.DisplayName,
                    FormatChannelValue(channel.Value),
                    channel.IsDefault,
                    channel.IsIndeterminate);
            }

            return states;
        }

        public static string FormatChannelValue(object? value)
        {
            return value switch
            {
                null => string.Empty,
                float floatValue => floatValue.ToString("0.####", CultureInfo.InvariantCulture),
                double doubleValue => doubleValue.ToString("0.####", CultureInfo.InvariantCulture),
                decimal decimalValue => decimalValue.ToString("0.####", CultureInfo.InvariantCulture),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
            };
        }
    }
}
