#if XNALIKE
namespace Gum.GueDeriving;

/// <summary>
/// Opt-in blend surface for fill/stroke renderables in the two-slot shape runtime model
/// (issue #2937). Implemented by the optional MonoGameGumShapes <c>Circle</c> /
/// <c>RoundedRectangle</c> / <c>Arc</c> / <c>Line</c> (which inherit the property from
/// <c>RenderableShapeBase</c>) so <see cref="CircleRuntime"/> and <see cref="RectangleRuntime"/>
/// can push the blend mode to BOTH slots. Both slots must carry the same blend: blend is folded
/// into each renderable's <c>BatchKey</c>, so a fill/stroke disagreement would split them across
/// two ShapeBatches and render the stroke with the wrong blend. Core defaults like
/// <see cref="MonoGameGum.Renderables.DefaultStrokedCircleRenderable"/> deliberately do NOT
/// implement this interface — the setter round-trips on the runtime but renders as a no-op
/// without the optional package (graceful degradation, matching <see cref="IGradientedRenderable"/>).
/// </summary>
public interface IBlendedRenderable
{
    Gum.RenderingLibrary.Blend Blend { get; set; }
}
#endif
