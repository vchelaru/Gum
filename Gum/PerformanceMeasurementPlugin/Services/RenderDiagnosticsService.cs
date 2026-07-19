using Gum.Services;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System.Linq;

namespace PerformanceMeasurementPlugin.Services;

/// <summary>
/// <see cref="IRenderDiagnosticsService"/> backed by the concrete XNALIKE-only
/// RenderingLibrary.Graphics.Renderer/SystemManagers. Lives in this plugin project because it
/// already carries the KniGum project reference needed for compile-time access to those types
/// (see PerformanceMeasurementPlugin.csproj) — Gum.csproj itself does not reference the KNI
/// runtime, so the headless Gum.Presentation assembly can't either (issue #3754).
/// </summary>
public class RenderDiagnosticsService : IRenderDiagnosticsService
{
    private static Renderer? ActiveRenderer => SystemManagers.Default?.Renderer;

    public int SpriteBatchBeginCount => ActiveRenderer?.SpriteRenderer?.LastFrameDrawStates.Count() ?? 0;

    public int ShapeBatchBeginCount => ActiveRenderer?.RenderStateChangeStatistics?.ShapeBatchBeginCount ?? 0;

    public bool SortByBatchKey
    {
        get => ReferenceEquals(Renderer.SiblingOrdering, BatchKeyGroupedOrderer.Instance);
        set => Renderer.SiblingOrdering = value
            ? BatchKeyGroupedOrderer.Instance
            : HierarchicalOrderer.Instance;
    }

    public bool CullOffscreenWhenClipped
    {
        get => Renderer.CullOffscreenWhenClipped;
        set => Renderer.CullOffscreenWhenClipped = value;
    }
}
