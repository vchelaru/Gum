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
    /// <summary>
    /// Fallback factory that maps a base-type name (e.g. "Container", "Sprite", "Text") to a
    /// raw <see cref="IRenderable"/>. This is the secondary of two registries Gum uses to
    /// build the visual tree; the primary registry — and the source of truth — is
    /// <see cref="GumRuntime.ElementSaveExtensions.RegisterGueInstantiation{T}(string, Func{T})"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The two-registry model.</b> When Gum loads an <c>ElementSave</c>, it tries to build
    /// a strongly-typed <see cref="GraphicalUiElement"/> wrapper first (e.g. <c>ContainerRuntime</c>,
    /// <c>SpriteRuntime</c>, <c>CircleRuntime</c>). Those wrappers are registered by each backend's
    /// <c>SystemManagers.Initialize</c> via <c>RegisterGueInstantiation</c>, and their constructors
    /// install their own inner renderable via <c>SetContainedObject</c>. That is the primary path,
    /// and it is what backends are expected to produce in real-world use.
    /// </para>
    /// <para>
    /// <b>When this fallback fires.</b> Only when the primary path produces a wrapper whose
    /// <c>RenderableComponent</c> is still null after construction (see
    /// <c>ElementSaveExtensions.SetGraphicalUiElement</c>). In practice that is rare: it covers
    /// (a) generic <see cref="GraphicalUiElement"/> instances created as a fallback when no
    /// runtime is registered for a name, (b) the recursion through a custom component's
    /// <c>BaseType</c> chain when an intermediate link has no registration, (c) the tool's
    /// wireframe rendering path (see <c>WireframeObjectManager</c> and
    /// <c>EditorTabPlugin_XNA</c>), which still depends on this factory directly, and (d) test
    /// harnesses that wire it as the only renderable source.
    /// </para>
    /// <para>
    /// <b>Why you should not extend this class.</b> The factory's switch is a parallel
    /// declaration of what each base-type renderable is supposed to be. The primary path's
    /// runtime wrappers can — and do — produce different renderables for the same base-type
    /// name. The classic example is MonoGame's <c>CircleRuntime</c>, which uses
    /// <c>RenderableRegistry.Create&lt;IFilledCircleRenderable&gt;</c> to pick up an
    /// Apos.Shapes-backed filled circle when that optional package is registered; this
    /// fallback unconditionally returns an outline-only <c>LineCircle</c>. A consumer hitting
    /// the fallback path silently gets a different visual than one going through the primary
    /// path. The architectural cleanup tracked in
    /// <see href="https://github.com/vchelaru/Gum/issues/2915">issue #2915</see> is to make
    /// the primary path the single source of truth in both runtime and tool, after which this
    /// class can be retired.
    /// </para>
    /// <para>
    /// <b>Adding a new base type.</b> Register a runtime wrapper via
    /// <c>RegisterGueInstantiation</c> in each backend that needs it. Do not add a case here.
    /// Adding a case here means committing to keep that case in sync with every runtime
    /// wrapper's contained renderable across every backend, forever, by hand — exactly the
    /// drift problem this class causes.
    /// </para>
    /// <para>
    /// This source file is shared into every runtime backend (MonoGame, KNI, FNA, Raylib,
    /// Sokol) plus the tool's <c>EditorTabPlugin_XNA</c> via <c>&lt;Compile Include&gt;</c>
    /// links, with platform-specific switch cases gated by preprocessor symbols.
    /// </para>
    /// <para>
    /// <b>No backend allow-list on the switch itself.</b> The switch body is not gated by a
    /// closed list of backend defines (e.g. <c>#if MONOGAME || RAYLIB || ...</c>) — it relies
    /// on the file's own using-directive block above (<c>#if RAYLIB / #elif SOKOL / #else</c>)
    /// to resolve <c>Sprite</c>, <c>Texture2D</c>, <c>LineRectangle</c>, etc. to the right
    /// per-platform types. A backend that reuses one of those three shapes (raylib's or
    /// sokol's <c>Gum.Renderables</c>, or the MonoGame-family <c>RenderingLibrary.Graphics</c> +
    /// XNA types) just compiles. A backend that fits none of them fails to <i>compile</i> this
    /// file — naming the exact missing type at the exact case — rather than silently linking
    /// and returning <c>null</c> for every standard base type at runtime. See
    /// <see href="https://github.com/vchelaru/Gum/issues/3565">issue #3565</see>.
    /// </para>
    /// </remarks>
    public static class FallbackRenderableFactory
    {
        /// <summary>
        /// Returns the fallback <see cref="IRenderable"/> for the given Gum base-type name,
        /// or <c>null</c> if the name is not a recognized standard base type. Called by
        /// <c>ElementSaveExtensions.CustomCreateGraphicalComponentFunc</c> only when the
        /// primary registration path (see <c>RegisterGueInstantiation</c>) has not already
        /// supplied a renderable; see the class-level remarks for the full decision flow.
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
