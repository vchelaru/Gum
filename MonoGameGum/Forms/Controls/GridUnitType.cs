#if FRB
namespace FlatRedBall.Forms.Controls;
#endif

#if !FRB
namespace Gum.Forms.Controls;
#endif

/// <summary>
/// Describes the kind of value a GridLength holds.
/// Matches WPF's GridUnitType enum.
/// </summary>
public enum GridUnitType
{
    /// <summary>
    /// Size is determined automatically based on content.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Size is a fixed absolute value.
    /// </summary>
    Absolute = 1,

    /// <summary>
    /// Size is a weighted proportion of available space.
    /// </summary>
    Star = 2
}
