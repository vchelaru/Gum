using System.Numerics;

namespace Gum.Input;

/// <summary>
/// One finger on a touch screen for the current frame.
/// </summary>
public class TouchLocation
{
    public TouchLocation()
    {
    }

    public TouchLocation(int id, Vector2 position)
    {
        Id = id;
        Position = position;
    }

    /// <summary>
    /// The platform's identifier for this finger, stable while the finger stays down.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// The finger's position in window pixels.
    /// </summary>
    public Vector2 Position { get; }
}
