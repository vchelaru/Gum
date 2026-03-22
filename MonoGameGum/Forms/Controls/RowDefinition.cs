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
    /// Creates a new RowDefinition with a default height of 1 Star.
    /// </summary>
    public RowDefinition()
    {
        Height = new GridLength(1, GridUnitType.Star);
    }
}
