using System;
using System.Collections.Generic;
using WpfDataUi.DataTypes;

namespace WpfDataUi.Controls;

/// <summary>
/// Which entries a combo-box displayer offers: the member's custom options when it has any (a
/// converter may have reduced an enum), otherwise every value of an enum type, and for a nullable
/// enum a "no value" sentinel followed by the values.
/// </summary>
public class ComboBoxDisplayLogic
{
    /// <summary>
    /// The "no value" entry for nullable-enum drop-downs. <c>StateReferencingInstanceMember</c> turns
    /// it back into null when writing the variable; a literal null item does not select cleanly.
    /// </summary>
    public const string NullSentinel = "<None>";

    /// <summary>The options for <paramref name="member"/>, whose property type is <paramref name="propertyType"/>.</summary>
    public IEnumerable<object> GetOptions(InstanceMember? member, Type? propertyType)
    {
        if (member?.CustomOptions != null)
        {
            foreach (object item in member.CustomOptions)
            {
                yield return item;
            }
        }
        // Multi-select can leave the type null.
        else if (propertyType?.IsEnum == true)
        {
            foreach (object item in Enum.GetValues(propertyType))
            {
                yield return item;
            }
        }
        else if (propertyType != null
            && Nullable.GetUnderlyingType(propertyType) is Type underlyingEnumType
            && underlyingEnumType.IsEnum)
        {
            yield return NullSentinel;
            foreach (object item in Enum.GetValues(underlyingEnumType))
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// The item to select for <paramref name="value"/>: the sentinel for a null nullable-enum value,
    /// otherwise the value itself.
    /// </summary>
    public object? GetItemToSelect(object? value, Type? propertyType)
    {
        bool isNullableEnum = propertyType != null && Nullable.GetUnderlyingType(propertyType)?.IsEnum == true;
        return value == null && isNullableEnum ? NullSentinel : value;
    }
}

/// <summary>
/// The value math behind a slider displayer: scaling between the member's value and the shown value
/// by <see cref="DisplayedValueMultiplier"/>, and formatting the slider's position for the text field.
/// </summary>
public class SliderDisplayLogic
{
    /// <summary>Creates the logic with a multiplier of 1 and two decimals.</summary>
    public SliderDisplayLogic()
    {
        DisplayedValueMultiplier = 1;
        DecimalPointsFromSlider = 2;
    }

    /// <summary>
    /// Scales the shown value relative to the member's (2 shows double the underlying value). Applies
    /// to the minimum and maximum too.
    /// </summary>
    public double DisplayedValueMultiplier { get; set; }

    /// <summary>The number of decimals shown in the text field while dragging the slider.</summary>
    public int DecimalPointsFromSlider { get; set; }

    /// <summary>The member's value as shown: multiplied by <see cref="DisplayedValueMultiplier"/>.</summary>
    public object ToDisplayedValue(object valueOnInstance)
    {
        if (DisplayedValueMultiplier == 1)
        {
            return valueOnInstance;
        }

        return valueOnInstance switch
        {
            float asFloat => asFloat * DisplayedValueMultiplier,
            double asDouble => asDouble * DisplayedValueMultiplier,
            int asInt => asInt * DisplayedValueMultiplier,
            decimal asDecimal => asDecimal * (decimal)DisplayedValueMultiplier,
            long asLong => asLong * (long)DisplayedValueMultiplier,
            byte asByte => asByte * (byte)DisplayedValueMultiplier,
            _ => valueOnInstance,
        };
    }

    /// <summary>The shown value converted back to the member's scale and type.</summary>
    public object? ToInstanceValue(object? displayedValue)
    {
        if (DisplayedValueMultiplier == 1 || displayedValue == null)
        {
            return displayedValue;
        }

        return displayedValue switch
        {
            float asFloat => (float)(asFloat / DisplayedValueMultiplier),
            double asDouble => asDouble / DisplayedValueMultiplier,
            int asInt => (int)(asInt / DisplayedValueMultiplier),
            decimal asDecimal => asDecimal / (decimal)DisplayedValueMultiplier,
            long asLong => (long)(asLong / DisplayedValueMultiplier),
            byte asByte => (byte)(asByte / DisplayedValueMultiplier),
            _ => displayedValue,
        };
    }

    /// <summary>The slider's position as text for the member's type: whole numbers for integer types.</summary>
    public string FormatSliderValue(double sliderValue, Type? propertyType)
    {
        if (propertyType == typeof(int) ||
            propertyType == typeof(uint) ||
            propertyType == typeof(long) ||
            propertyType == typeof(ulong) ||
            propertyType == typeof(byte) ||
            propertyType == typeof(short))
        {
            return ((int)sliderValue).ToString();
        }

        return sliderValue.ToString($"f{DecimalPointsFromSlider}");
    }

    /// <summary>The value as a double for positioning the slider, or null for a non-numeric value.</summary>
    public double? ToSliderPosition(object value)
    {
        return value switch
        {
            float asFloat => asFloat,
            double asDouble => asDouble,
            int asInt => asInt,
            decimal asDecimal => (double)asDecimal,
            long asLong => asLong,
            byte asByte => asByte,
            _ => null,
        };
    }
}
