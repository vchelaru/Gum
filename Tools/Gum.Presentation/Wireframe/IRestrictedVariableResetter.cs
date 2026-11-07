using RenderingLibrary.Graphics;

namespace Gum.Wireframe;

/// <summary>
/// Puts every variable the loaded project's rules hide back to its standard-type default on the canvas
/// objects, so an element draws as an engine that does not support the variable would. The stored values
/// are untouched.
/// </summary>
public interface IRestrictedVariableResetter
{
    /// <summary>
    /// Resets the restricted variables on <paramref name="root"/> and every object under it. Does nothing
    /// when the project has no variable rules.
    /// </summary>
    void Reset(GraphicalUiElement? root);
}
