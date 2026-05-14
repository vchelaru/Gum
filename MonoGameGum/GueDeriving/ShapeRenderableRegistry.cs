#if XNALIKE
using System;

namespace Gum.GueDeriving;

/// <summary>
/// Registration point that lets the optional MonoGameGumShapes package supply fill-capable
/// shape renderables to core runtimes (currently <see cref="CircleRuntime"/>) without core
/// MonoGameGum referencing Apos.Shapes.
/// </summary>
/// <remarks>
/// Mirrors the existing optional-package extension pattern used by
/// <c>ElementSaveExtensions.RegisterGueInstantiation</c> and
/// <c>CustomSetPropertyOnRenderable.AdditionalPropertyOnRenderable</c>: core defines the hook,
/// and MonoGameGumShapes fills it from its <c>[ModuleInitializer]</c>
/// (<c>AposShapeRuntime.RegisterRuntimeTypes</c>). When the package is absent the factory
/// stays null and runtimes degrade to outline-only.
///
/// Spike (#2758): throwaway scope — a single circle factory. The real implementation would
/// generalize this to a capability-keyed registry (filled? rounded? gradient?).
/// </remarks>
public static class ShapeRenderableRegistry
{
    /// <summary>
    /// Factory that creates a fill-capable circle renderable, or <c>null</c> when no shapes
    /// package is loaded. Set by MonoGameGumShapes at module-init time; consumed by
    /// <see cref="CircleRuntime"/> when its <c>FillColor</c> is assigned a non-null value.
    /// </summary>
    public static Func<IFilledShapeRenderable>? CreateFilledCircleRenderable { get; set; }
}
#endif
