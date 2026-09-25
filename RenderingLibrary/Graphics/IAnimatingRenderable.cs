namespace RenderingLibrary.Graphics;

/// <summary>
/// A renderable whose appearance can change on its own over time, such as a sprite playing an
/// animation chain. Hosts that draw only on demand keep drawing while <see cref="IsAnimating"/> is true.
/// </summary>
public interface IAnimatingRenderable
{
    /// <summary>
    /// Whether the renderable currently changes on its own, so it needs to be drawn again without
    /// any other change.
    /// </summary>
    bool IsAnimating { get; }
}
