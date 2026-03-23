#if FRB
namespace FlatRedBall.Forms.Controls;
#endif

#if !FRB
namespace Gum.Forms.Controls;
#endif

/// <summary>
/// Defines the width properties of a column in a Grid control.
/// </summary>
public class ColumnDefinition
{
    /// <summary>
    /// The width of this column. Defaults to 1 Star (proportional sizing).
    /// </summary>
    public GridLength Width { get; set; }

    /// <summary>
    /// The minimum width of this column in pixels. Applied to Absolute and Auto columns.
    /// Defaults to 0 (no minimum).
    /// </summary>
    public float MinWidth { get; set; } = 0f;

    /// <summary>
    /// The maximum width of this column in pixels. Applied to Absolute and Auto columns.
    /// Defaults to no maximum.
    /// </summary>
    public float MaxWidth { get; set; } = float.PositiveInfinity;

    /// <summary>
    /// Creates a new ColumnDefinition with a default width of 1 Star.
    /// </summary>
    public ColumnDefinition()
    {
        Width = new GridLength(1, GridUnitType.Star);
    }

    /// <summary>
    /// Creates a new ColumnDefinition with the specified width.
    /// </summary>
    public ColumnDefinition(GridLength width)
    {
        Width = width;
    }
}
