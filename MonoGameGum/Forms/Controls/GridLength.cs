#if FRB
namespace FlatRedBall.Forms.Controls;
#endif

#if !FRB
namespace Gum.Forms.Controls;
#endif

/// <summary>
/// Represents the length of elements that support Star, Pixel, and Auto sizing.
/// Matches WPF's GridLength struct but uses float instead of double.
/// </summary>
public struct GridLength
{
    /// <summary>
    /// The numeric value of this GridLength.
    /// </summary>
    public float Value { get; }

    /// <summary>
    /// The type of sizing this GridLength represents.
    /// </summary>
    public GridUnitType GridUnitType { get; }

    /// <summary>
    /// Whether this GridLength represents an absolute value.
    /// </summary>
    public bool IsAbsolute => GridUnitType == GridUnitType.Absolute;

    /// <summary>
    /// Whether this GridLength represents automatic sizing.
    /// </summary>
    public bool IsAuto => GridUnitType == GridUnitType.Auto;

    /// <summary>
    /// Whether this GridLength represents a weighted proportion of available space.
    /// </summary>
    public bool IsStar => GridUnitType == GridUnitType.Star;

    /// <summary>
    /// Returns a GridLength configured for automatic sizing.
    /// </summary>
    public static GridLength Auto => new GridLength(1.0f, GridUnitType.Auto);

    /// <summary>
    /// Creates a GridLength with the specified absolute value.
    /// </summary>
    /// <param name="value">The absolute size value.</param>
    public GridLength(float value) : this(value, GridUnitType.Absolute)
    {
    }

    /// <summary>
    /// Creates a GridLength with the specified value and unit type.
    /// </summary>
    /// <param name="value">The numeric value.</param>
    /// <param name="type">The unit type for this GridLength.</param>
    public GridLength(float value, GridUnitType type)
    {
        Value = value;
        GridUnitType = type;
    }
}
