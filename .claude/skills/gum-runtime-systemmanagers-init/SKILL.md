---
name: gum-runtime-systemmanagers-init
description: The SystemManagers.Initialize() registration contract every render backend must fulfill. Triggers: new backend implementer, CustomCreateGraphicalComponentFunc, RegisterGueInstantiation, StandardElementsManager.Initialize, LoaderManager.Self.ContentLoader, FallbackRenderableFactory.
---

# SystemManagers.Initialize() Registration Contract

Each render backend has its own `SystemManagers` (not shared — see [gum-runtime-topology](../gum-runtime-topology/SKILL.md) for why `RenderingLibrary.*` compiles into multiple assemblies). Its `Initialize()`/`Initialize(fullInstantiation: true)` is what makes that backend's renderables visible to Gum's primary runtime-type registry. There is no shared interface or base-class checklist for this — each backend hand-writes the same sequence. New backend implementers (e.g. Silk.NET, #2738) must replicate it.

## The contract

A conforming `Initialize()` must, in some order:

1. **`LoaderManager.Self.ContentLoader = new <Backend>ContentLoader();`** — wires asset loading (textures, fonts).
2. **`GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;`** (+ usually `AddRenderableToManagers`, `RemoveRenderableFromManagers`, `UpdateFontFromProperties`) — routes `.gumx` property assignments into the backend's renderables.
3. **`ElementSaveExtensions.CustomCreateGraphicalComponentFunc = <backend's factory>;`** — the backend's own `IRenderable` factory, tried by the fallback path (see below) before `FallbackRenderableFactory`.
4. **`StandardElementsManager.Self.Initialize();`** — registers default states for the standard element types.
5. **Register the 8 standard shape types** via `ElementSaveExtensions.RegisterGueInstantiation(name, () => new XRuntime())`: **Circle, ColoredRectangle, Container, NineSlice, Polygon, Rectangle, Sprite, Text**. This is the *primary* registry Gum uses to build the `GraphicalUiElement` visual tree — see the two-registry model documented at the top of `Gum/Wireframe/FallbackRenderableFactory.cs`.

Reference implementations: `RenderingLibrary/SystemManagers.cs:139-294` (MonoGame/KNI/FNA, the most complete — also loads embedded default fonts and installs a mobile/browser file-stream hook), `Runtimes/RaylibGum/RenderingLibrary/SystemManagers.cs:115-145`, `Runtimes/SkiaGum/RenderingLibrary/SystemManagers.cs:55-91`, `Runtimes/SokolGum/SystemManagers.cs:65-137`.

## Landmines

- **A missing shape registration doesn't throw — it silently falls back.** If step 5 skips a type, `ElementSaveExtensions`' primary registry has nothing for it, so Gum falls through to `FallbackRenderableFactory.TryHandleAsBaseType` (`Gum/Wireframe/FallbackRenderableFactory.cs`), a hardcoded per-backend switch that returns a plainer renderable (e.g. an outline-only `LineCircle` instead of the backend's preferred filled shape). This is how a backend can look like it "supports" a shape while actually degrading it — there's no compile-time or startup check that all 8 are registered. Don't extend `FallbackRenderableFactory`'s switch to paper over a missing registration in a new backend; register the real runtime wrapper in step 5 instead.
- **These are process-wide static assignments**, not instance state — `ElementSaveExtensions.CustomCreateGraphicalComponentFunc`, `GraphicalUiElement.SetPropertyOnRenderable`, `LoaderManager.Self.ContentLoader`, etc. are shared statics every backend's `Initialize()` overwrites. Running two backends (or two `SystemManagers` instances of different backends) in one process means the last `Initialize()` call wins — intentional for the typical single-backend app, but not safe to assume otherwise.
- **Extended shape types beyond the standard 8 need their own wiring, not just step 5.** Skia additionally registers `Arc`, `ColoredCircle`, `Line`, and handles `RoundedRectangle` — these aren't part of `StandardElementsManager`'s default-state set, so Skia also assigns `StandardElementsManager.Self.CustomGetDefaultState` (`Runtimes/SkiaGum/RenderingLibrary/SystemManagers.cs:78,190-206`) to supply default states for them. Only copy this if the new backend adds shape types beyond the standard 8.
- **Idempotency guards vary and matter.** Skia guards its whole registration block behind `HasInitializedGlobal` (`Runtimes/SkiaGum/RenderingLibrary/SystemManagers.cs:72-88`) because it can be constructed per-window; re-running the block wipes customizations of standard elements. MonoGame/Raylib/Sokol don't guard and expect `Initialize()` to run once per process. Decide which fits the new backend's construction pattern rather than copying one blindly.
