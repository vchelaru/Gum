using System.Collections.Generic;

namespace RenderingLibrary.Graphics;

/// <summary>
/// Produces the flat <see cref="DrawCommand"/> sequence for a layer's main render pass.
/// Pluggable so that alternative orderings (e.g. batch-grouped) can be swapped in without
/// touching the renderer's submit phase. The default implementation is
/// <see cref="HierarchicalOrderer"/>, which preserves the legacy depth-first walk.
/// </summary>
public interface IRenderableOrderer
{
    /// <summary>
    /// Builds the ordered command list for <paramref name="layer"/> into the caller-owned
    /// <paramref name="destination"/>. Implementations MUST clear <paramref name="destination"/>
    /// before appending so the renderer can pool a single buffer across layers and frames.
    /// </summary>
    void BuildDrawList(Layer layer, List<DrawCommand> destination);
}
