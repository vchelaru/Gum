using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if GUM
using Gum.Services;
using Gum.ToolStates;
#endif

#if RAYLIB
using Gum.Renderables;
using Raylib_cs;
#elif SOKOL
using Gum.Renderables;
#else
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Math.Geometry;
#endif

namespace Gum.Wireframe
{
    // Two-registry model: Gum tries a strongly-typed GraphicalUiElement wrapper first
    // (ContainerRuntime, SpriteRuntime, CircleRuntime, etc.), registered per backend via
    // RegisterGueInstantiation; that's the primary path and source of truth. This class is
    // the fallback, used only when the primary path leaves RenderableComponent null after
    // construction (see ElementSaveExtensions.SetGraphicalUiElement) — generic
    // GraphicalUiElement instances with no registered runtime, BaseType-chain recursion
    // gaps, the tool's wireframe rendering path (WireframeObjectManager /
    // EditorTabPlugin_XNA), and test harnesses.
    //
    // Do not extend this switch when a base type is missing a case — register a runtime
    // wrapper via RegisterGueInstantiation in the backend that needs it instead. The
    // primary path's wrappers can produce a different renderable than this fallback's
    // hardcoded pick for the same base-type name (e.g. MonoGame's CircleRuntime prefers an
    // Apos.Shapes-backed filled circle when available; this fallback always returns an
    // outline-only LineCircle) — adding cases here means hand-keeping this switch in sync
    // with every runtime wrapper across every backend forever. Issue #2915 tracks retiring
    // this class once the primary path is the single source of truth everywhere.
    //
    // Shared into every runtime backend (MonoGame, KNI, FNA, Raylib, Sokol) plus the tool's
    // EditorTabPlugin_XNA via <Compile Include> links. The switch below has no backend
    // allow-list of its own — it relies on the using-directive block above (#if RAYLIB /
    // #elif SOKOL / #else) to resolve Sprite/Texture2D/LineRectangle/etc. to the right
    // per-platform types. A backend reusing one of those three shapes just compiles; one
    // that fits none of them fails to compile here (naming the missing type) instead of
    // silently linking and returning null for every base type at runtime.
    /// <summary>
    /// Fallback factory that maps a base-type name (e.g. "Container", "Sprite", "Text") to a
    /// raw <see cref="IRenderable"/>. This is the secondary of two registries Gum uses to
    /// build the visual tree; the primary registry — and the source of truth — is
    /// <see cref="GumRuntime.ElementSaveExtensions.RegisterGueInstantiation{T}(string, Func{T})"/>.
    /// </summary>
    public static class FallbackRenderableFactory
    {
        /// <summary>
        /// Returns the fallback <see cref="IRenderable"/> for the given Gum base-type name,
        /// or <c>null</c> if the name is not a recognized standard base type. Called by
        /// <c>ElementSaveExtensions.CustomCreateGraphicalComponentFunc</c> only when the
        /// primary registration path (see <c>RegisterGueInstantiation</c>) has not already
        /// supplied a renderable; see the class-level comment for the full decision flow.
        /// </summary>
        /// <param name="baseType">Gum base-type name (e.g. <c>"Container"</c>, <c>"Sprite"</c>, <c>"Text"</c>).</param>
        /// <param name="managers">The platform's <c>SystemManagers</c>; cast internally as needed.</param>
        /// <returns>An <see cref="IRenderable"/> matching <paramref name="baseType"/>, or <c>null</c> for unrecognized names.</returns>
        public static IRenderable TryHandleAsBaseType(string baseType, ISystemManagers? managers)
        {
            SystemManagers? systemManagers = managers as SystemManagers;
            IRenderable? containedObject = null;
            switch (baseType)
            {
                case "Container":
                case "Component": // this should never be set in Gum, but there could be XML errors or someone could have used an old Gum...
#if RAYLIB || SOKOL
                    containedObject = new InvisibleRenderable();
#else
                    var showComponentLineRectangles = GraphicalUiElement.ShowLineRectangles;
                    if (showComponentLineRectangles)
                    {
                        LineRectangle lineRectangle = new LineRectangle(systemManagers);
                        lineRectangle.Color = System.Drawing.Color.FromArgb(255, 255, 255, 255);
#if GUM
                        lineRectangle.IsDotted = true;

                        var projectState = Locator.GetRequiredService<IProjectState>();
                        lineRectangle.Color = System.Drawing.Color.FromArgb(
                            255,
                            projectState.GeneralSettings.OutlineColorR,
                            projectState.GeneralSettings.OutlineColorG,
                            projectState.GeneralSettings.OutlineColorB
                            );
#endif
                        containedObject = lineRectangle;
                    }
                    else
                    {
                        containedObject = new InvisibleRenderable();
                    }
#endif
                    break;

                case "Rectangle":
#if (MONOGAME || FNA || KNI) && !FRB
                    // Issue #2925 — prefer a registry-supplied stroked rectangle (Apos.Shapes
                    // RoundedRectangle when MonoGameGumShapes is loaded) over the legacy
                    // LineRectangle. The IStrokedRectangleRenderable interface is XNALIKE-only
                    // (registry is shared via GumCommon but the interface ships under #if XNALIKE
                    // in MonoGameGum), so RAYLIB/SOKOL fall through to the unconditional legacy
                    // path below. In XNALIKE builds the core default factory
                    // (DefaultStrokedRectangleRenderable, a LineRectangle subclass) is registered
                    // at module load; tests that call RenderableRegistry.Reset() will see null
                    // here and fall through to the legacy path.
                    if (RenderableRegistry.Create<IStrokedRectangleRenderable>() is IRenderable registryRectangle)
                    {
                        containedObject = registryRectangle;
                        break;
                    }
#endif
                    LineRectangle rectangle = new LineRectangle(systemManagers);
                    rectangle.IsDotted = false;
                    containedObject = rectangle;
                    break;
                case "Circle":
#if (MONOGAME || FNA || KNI) && !FRB
                    // Issue #2925 — prefer a registry-supplied stroked circle (Apos.Shapes Circle
                    // when MonoGameGumShapes is loaded) over the legacy LineCircle so the tool
                    // and runtime render the same Apos-backed shape that the MonoGameGum
                    // CircleRuntime constructor resolves. Same XNALIKE gating as Rectangle above.
                    if (RenderableRegistry.Create<IStrokedCircleRenderable>() is IRenderable registryCircle)
                    {
                        containedObject = registryCircle;
                        break;
                    }
#endif
                    LineCircle circle = new LineCircle(systemManagers);
                    circle.CircleOrigin = CircleOrigin.TopLeft;
                    containedObject = circle;
                    break;
                case "Polygon":
                    LinePolygon polygon = new LinePolygon(systemManagers);
                    containedObject = polygon;
                    break;
                case "ColoredRectangle":
                    SolidRectangle solidRectangle = new SolidRectangle();
                    containedObject = solidRectangle;
                    break;
                case "Sprite":
                    Texture2D? texture = null;

                    Sprite sprite = new Sprite(texture);
                    containedObject = sprite;

                    break;
                case "NineSlice":
                    {
                        NineSlice nineSlice = new NineSlice();
                        containedObject = nineSlice;
                    }
                    break;
                case "Text":
                    {
                        Text text = new Text(systemManagers);
                        text.RawText = string.Empty;
                        containedObject = text;
                    }
                    break;

#if SKIA
                case "Arc":
                    return new SkiaGum.Renderables.Arc();
                case "ColoredCircle":
                    return new SkiaGum.Renderables.Circle();
                case "RoundedRectangle":
                    return new SkiaGum.Renderables.RoundedRectangle();

#endif


            }
            return containedObject;
        }

    }

    /// <summary>
    /// Obsolete alias for <see cref="FallbackRenderableFactory"/>. Preserved so external
    /// consumers compiling against the old name continue to build. New code must call
    /// <see cref="FallbackRenderableFactory"/> directly — and read its remarks first, because
    /// the name change is a signal that this is a fallback path, not the primary registry.
    /// </summary>
    [Obsolete("Renamed to FallbackRenderableFactory to clarify that this is a fallback, not the primary registry. See https://github.com/vchelaru/Gum/issues/2915.")]
    public static class RuntimeObjectCreator
    {
        /// <inheritdoc cref="FallbackRenderableFactory.TryHandleAsBaseType(string, ISystemManagers?)"/>
        [Obsolete("Renamed to FallbackRenderableFactory.TryHandleAsBaseType. See https://github.com/vchelaru/Gum/issues/2915.")]
        public static IRenderable TryHandleAsBaseType(string baseType, ISystemManagers? managers)
            => FallbackRenderableFactory.TryHandleAsBaseType(baseType, managers);
    }
}
