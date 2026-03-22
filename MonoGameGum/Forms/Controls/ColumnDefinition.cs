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
    /// Creates a new ColumnDefinition with a default width of 1 Star.
    /// </summary>
    public ColumnDefinition()
    {
        Width = new GridLength(1, GridUnitType.Star);
    }
}
