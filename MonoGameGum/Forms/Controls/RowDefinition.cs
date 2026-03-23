#if FRB
namespace FlatRedBall.Forms.Controls;
#endif

#if !FRB
namespace Gum.Forms.Controls;
#endif

/// <summary>
/// Defines the height properties of a row in a Grid control.
/// </summary>
public class RowDefinition
{
    /// <summary>
    /// The height of this row. Defaults to 1 Star (proportional sizing).
    /// </summary>
    public GridLength Height { get; set; }

    /// <summary>
    /// The minimum height of this row in pixels. Applied to Absolute and Auto rows.
    /// Defaults to 0 (no minimum).
    /// </summary>
    public float MinHeight { get; set; } = 0f;

    /// <summary>
    /// The maximum height of this row in pixels. Applied to Absolute and Auto rows.
    /// Defaults to no maximum.
    /// </summary>
    public float MaxHeight { get; set; } = float.PositiveInfinity;

    /// <summary>
    /// Creates a new RowDefinition with a default height of 1 Star.
    /// </summary>
    public RowDefinition()
    {
        Height = new GridLength(1, GridUnitType.Star);
    }

    /// <summary>
    /// Creates a new RowDefinition with the specified height.
    /// </summary>
    public RowDefinition(GridLength height)
    {
        Height = height;
    }
}
