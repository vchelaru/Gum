---
name: gum-monogame-rendering
description: Gum's MonoGame rendering pipeline — Renderer/SpriteBatchStack/GumBatch, BatchKey transitions, SpriteBatch ↔ Apos.Shapes ShapeBatch interleaving. Triggers: Renderer.cs, SpriteBatchStack.cs, GumBatch, RenderableShapeBase, SpriteBatchRenderableBase, BatchKey, draw-order bugs, GumRenderBatch in FRB2/MonoGameGumImmediateMode.
---

# Gum's MonoGame Rendering Pipeline

This skill covers the XNA-family backends only (MonoGame / KNI / FNA). Skia, Raylib, and Sokol have their own renderers and don't go through this code.

## Two Entry Paths into Renderer

### Layered path — `Renderer.RenderLayer`

Used by the Gum tool and any consumer with sorted `Layer.Renderables`. Walks every renderable in the layer in order. `Renderer.Draw(SystemManagers, List<Layer>)` and `Renderer.Draw(SystemManagers, Layer)` share a **once-per-host-frame** render-target sweep: the first draw after `SystemManagers.Activity` advances time (or an explicit `Renderer.BeginFrame()`) calls `ClearUnusedRenderTargetsLastFrame()`; subsequent `Draw(layer)` calls in the same host frame accumulate `_usedThisFrame` marks without re-sweeping (#3416). FRB's `GumIdb.Update` already calls `Activity(TimeManager.CurrentTime)` before draw. `Draw(SystemManagers)` (the GumService path) calls `EndFrame()` after each full draw so hosts that skip Activity still get a fresh sweep token on the next full draw.

```csharp
spriteRenderer.BeginSpriteBatch(..., BeginType.Push, ...);  // outer SpriteBatch begin
Render(layer.Renderables, ...);                              // recursive walk
lastBatchOwner?.EndBatch(managers);                          // flush pending custom batch
EndSpriteBatch();                                            // (NET<8 only) outer end
```

### GumBatch path — `Renderer.Begin/Draw/End`

Used by FRB2's `GumRenderBatch`, the immediate-mode samples, and any "I have one renderable, draw it now" consumer. `GumBatch` is a thin wrapper that calls `Renderer.Begin`/`Draw`/`End`.

Key contract difference: each `Renderer.Draw` is one top-level renderable. If a consumer draws N elements, that's `Begin → Draw → Draw → ... → End`. **Multiple Begin/End cycles per frame are normal.** FRB2's Solitaire trace shows one cycle per card.

`Renderer.End` historically was asymmetric with `RenderLayer`'s end-of-walk: it called `EndSpriteBatch` but **did not** flush the pending custom batch, so draws leaked across cycles. Fixed: `Renderer.End` now calls `_batchOrchestrator.FlushAndReset(...)` before `EndSpriteBatch`. If you change the End logic, preserve that ordering.

## PreRender Walk: Layered Path Has Two Phases, GumBatch Path Has One

The layered path runs a recursive `PreRender` pass on `layer.Renderables` **before** `BeginSpriteBatch`. That pass does two jobs:

1. Calls `renderable.PreRender()` on every visible renderable, depth-first. This is the hook `RenderableShapeBase.PreRender` uses to invoke `OnPreRender`, which is wired by `AposShapeRuntime.SetContainedShape` to call `AposShapeRuntime.PreRender`. That's where runtime-only properties (notably `StrokeWidth` with its unit handling) get pushed onto the contained renderable. Without this walk, the renderable keeps its own default values (e.g. `RenderableShapeBase._strokeWidth = 2`) regardless of what the runtime was assigned.
2. For any renderable with `IsRenderTarget == true`, calls `RenderToRenderTarget` — which sets a render target on the GraphicsDevice and runs its own SpriteBatch cycle inside. **Invisible** render targets (`Visible == false`) skip this bake unless a visible `IRenderTargetTextureReferencer` on any layer references them via `RenderTargetTextureSource` (#1643) — the reference set is collected once per host frame across all layers before the bake pass runs.

Phase 2 is why the full PreRender walk **must** run before `BeginSpriteBatch` — once the outer SpriteBatch is begun, you can't safely change the render target or start a nested cycle.

The GumBatch path (`Renderer.Begin/Draw/End`) calls `BeginSpriteBatch` immediately in `Begin`, so it can't host phase 2. `Renderer.Draw(IRenderableIpso)` does run a phase-1-only walk via `InvokePreRenderRecursively` before forwarding to the inner draw, so `AposShapeRuntime.PreRender` and similar hooks fire correctly. **Render targets nested inside a `GumBatch.Draw` tree are not supported on this path** — phase 2 is intentionally skipped to avoid clobbering the outer SpriteBatch.

Practical consequence: any new "runtime resolves a property in `PreRender` and pushes it to the renderable" pattern (the `AposShapeRuntime.StrokeWidth` shape) works on both entry paths, but only the layered path renders nested render targets.

## Cross-Layer `RenderTargetTextureSource` and Per-Layer Draw (issues #3416 / #3417)

`Sprite.RenderTargetTextureSource` lets a sprite sample a cached offscreen target owned by a render-target container on **another** layer. The multi-layer `Draw(SystemManagers, List<Layer>)` path already ran a two-pass pre-render (bake all layers, then bind referencer textures on all layers) before compositing. Per-layer `Draw(SystemManagers, Layer)` — the FRB / `GumIdb` default — did not, so cross-layer references went stale.

**Per-layer draw contract (post-#3417):**

1. **Once per host frame** (first `Draw(Layer)` or explicit `PreRenderLayers`): `TryPreRenderAllLayersForHostFrame` → `PreRenderLayersCore(_layers)` — bake every layer's render targets, then bind every layer's `IRenderTargetTextureReferencer` textures. Token resets when `SystemManagers.Activity` time advances (`NotifyHostFrameAdvanced`) or `EndFrame()` runs.
2. **Every `Draw(Layer)` call** (even when step 1 already ran): `PreRender(currentLayer.Renderables)` + `PreRenderWithSourceRenderTargets(currentLayer.Renderables)` — same per-layer hooks the legacy path always ran. Do **not** skip step 2 when the all-layer bake already ran; layout hooks and texture rebind still need to run for the layer being composited.
3. **Compositing:** `RenderLayer(..., prerender: false)`.

Optional explicit API: `SystemManagers.PreRenderLayers(layers)` / `Renderer.PreRenderLayers(layers)` — bake+bind without drawing (for hosts that want to separate pre-render from compositing).

`ResolveRenderTargetCacheOwner` maps a `GraphicalUiElement` `RenderTargetTextureSource` to its `RenderableComponent` for cache lookup — the cache key is always the contained `IRenderableIpso`, not the GUE wrapper.

**Integration tests:** `CrossLayerRenderTargetTextureSourceTests` (per-layer draw, consumer-first and source-first order) + `RenderTargetSweepTests` (#3416 sweep). Tests share `SystemManagers.Default`; advance Activity time uniquely each frame (`AdvanceHostFrame`) so once-per-frame tokens reset.

## Render-Target Post-Process Effects (issue #816)

A render-target container can carry a post-process shader applied when its cached texture is blitted **back** to the screen — not while children render *into* the target. Storage is `RenderableBase.RenderTargetEffect`, typed `object?` so the shared (non-XNA) rendering layer stays backend-agnostic; the xnalike `Renderer` casts it to a MonoGame `Effect`. The user-facing setter is the strongly-typed `ContainerRuntime.RenderTargetEffect` (`#if XNALIKE`).

Both back-draw sites — `SubmitDrawRenderable` (the live flat-`DrawCommand` pass) and the legacy recursive `Draw` (GumBatch immediate path) — funnel through `Renderer.DrawRenderTargetToScreen`, the single place the effect is bound. When an effect is present the blit becomes its own SpriteBatch cycle: `_batchOrchestrator.FlushAndReset` the open batch, `BeginSpriteBatch(..., effectOverride: effect)`, draw the target, then `BeginSpriteBatch` again with no override to restore the normal effect for following renderables. This mirrors the mid-walk clip-change flush.

`SpriteRenderer.BeginSpriteBatch`'s `effectOverride` replaces Gum's BasicEffect/CustomEffect but keeps the **same** `transformMatrix` Gum passes for every sprite/shape. So the user effect receives its vertex transform via the SpriteBatch `MatrixTransform` convention (the standard MonoGame 2D shader template) exactly as the Apos.Shapes path consumes that matrix — the blit stays aligned with the rest of the layer with no new matrix math. **Contract:** user effects must follow that SpriteBatch convention (pixel-shader post-process over a `MatrixTransform`-driven vertex shader); an effect that hard-codes its own projection won't position correctly. Gum core never compiles or loads the shader — the consumer supplies a constructed `Effect` (content pipeline, `new Effect(gd, bytes)`, or a runtime `.fx` compiler). **Second half of the contract: the blit binds the effect and draws once — it sets NO custom effect parameters and runs a single pass.** So a shader that needs host-set parameters (a blur's `Offset`/`radius`, etc.) runs with parameter *defaults* (zero), and for many effects zero is a visual identity — e.g. a Gaussian blur whose `Offset` defaults to `(0,0)` samples the same texel every tap and renders unblurred, looking like "the shader did nothing." Only **self-contained, parameterless, single-pass** post-process shaders work unmodified (the shipped sample's `Grayscale.fx` is the reference); a separable two-pass blur that expects the host to set `Offset` per pass cannot work through this path. Gum has no API to set effect parameters or chain passes on a render-target effect — that's a real feature gap, not a bug.

**The top-level-vs-nested renderable asymmetry (a real gotcha — bit #816).** The object the main-pass walk hands to `SubmitDrawRenderable`/`DrawRenderTargetToScreen` differs by depth: for a **top-level** render-target container it is the contained renderable (the `InvisibleRenderable`, a `RenderableBase`), because `AddToManagers` adds `mContainedObjectAsIpso` to the layer; for a **nested** one it is the `GraphicalUiElement` wrapper itself, because `AddChild` parents the *GUE* into the parent's child list. So any property the back-draw reads off `renderable` must be reachable on **both** forms. `IsRenderTarget` is fine because it's on the `IRenderableIpso` interface; `RenderTargetEffect` lives on the dedicated **`IRenderTargetRenderable`** interface (declared in `IRenderableIpso.cs`) — NOT on `IRenderableIpso` itself (that would force every backend's renderable to implement it), but a small mix-in implemented by the renderables that can be render-target containers. The renderer reads `(renderable as IRenderTargetRenderable)?.RenderTargetEffect ?? (GUE.RenderableComponent as IRenderTargetRenderable)?.RenderTargetEffect`. Reading only the contained-renderable form silently no-ops for every nested render target — and nested is the common case (any container built inside a Forms screen). Unit-test render-target features at depth, not just top-level.

**Why a shared interface, not just `RenderableBase` (#3210).** The original #816 read cast to `RenderableBase`, which works at runtime (containers are `InvisibleRenderable : RenderableBase`) but **silently fails in the Gum editor**: the editor backs a Container with a `LineRectangle` (the outline visual), which is a `SpriteBatchRenderableBase`, NOT a `RenderableBase` — so it had nowhere to hold the effect and the cast returned null, leaving the WYSIWYG preview unshaded. `IRenderTargetRenderable` is implemented by **both** `RenderableBase` (runtime container) and `LineRectangle` (editor container), so the same back-draw and the same `AssignSourceShaderFileOnContainer(IRenderTargetRenderable, …)` serve both. The dispatch is type-specific: the runtime path sets it in `TrySetPropertyOnContainer`, the editor path in `TrySetPropertyOnLineRectangle` — a Container in the tool is a `LineRectangle`, so a render-target property handled only on the `InvisibleRenderable` branch never fires in the editor. Lesson: render-target features must be carried on something both the runtime's `InvisibleRenderable` and the editor's `LineRectangle` share — verify them in the running tool, not just runtime unit tests.

**Resolving the effect from a `.fx` file reference (#3206).** `ContainerRuntime.SourceShaderFile` (`#if XNALIKE`, write-only) is the file-reference entry point, mirroring how a Sprite references a texture. It routes through the string path (`base.SetProperty("SourceShaderFile", …)`); `CustomSetPropertyOnRenderable.AssignSourceShaderFileOnContainer` resolves the path to a platform `Effect` and drops it into the same `RenderTargetEffect` slot. Gum core links nothing shader-specific — the actual `.fx` → `Effect` compile/load is a pluggable static `CustomSetPropertyOnRenderable.RenderTargetEffectResolver` (`Func<string, object?>`) that the consumer (or a future Gum.Shapes-style library) registers, typically capturing its own `GraphicsDevice` in the closure. No resolver registered → graceful no-op (unshaded), matching a missing texture. The resolved effect is cached in `LoaderManager` by normalized path (one compile per `.fx`, even across containers); a registered-but-failed resolve honors `GraphicalUiElement.MissingFileBehavior`, mirroring Sprite source-file handling. The dispatch + resolver live only in the MonoGame copy of `CustomSetPropertyOnRenderable` (linked into MonoGame/KNI/FNA) — Raylib/Skia have no container dispatch and `RenderTargetEffect` is xnalike-only.

## Two Independent Batchers

1. **MonoGame `SpriteBatch`** — wrapped by `SpriteBatchStack` (push/pop of render-state parameters). Used by sprites, text, NineSlice, SolidRectangle. Anything inheriting `SpriteBatchRenderableBase` declares `BatchKey="SpriteBatch"`.

2. **Apos.Shapes `ShapeBatch`** — owned by `ShapeRenderer.Self`, started/ended by `RenderableShapeBase.StartBatch/EndBatch`. Anything inheriting that base declares `BatchKey="Apos.Shapes"`.

These are **separate GPU command streams**. Within a frame, paint order on screen is determined by the order each batch's `End()` is called — not the order draws were queued.

```
SB.Begin → SB.Draw(spriteA) → ShB.Begin → ShB.Draw(shapeX) → ShB.End → SB.End
                                                              ^         ^
                                                              shapeX    spriteA
                                                              flushed   flushed
                                                              first     second
                                                              (under)   (on top)
```

To get insertion-order paint order across the two batches, every batch transition must `End` the previous batch before queueing into the new one.

## The BatchKey Transition Machinery

`Renderer.Draw` reads `renderable.BatchKey` and tracks `currentBatchKey` and `lastBatchOwner` as **Renderer instance fields** (they persist across Begin/End cycles).

The original transition condition (in `Renderer.cs`):

```csharp
if (!string.IsNullOrEmpty(renderable.BatchKey) && renderable.BatchKey != currentBatchKey)
{
    lastBatchOwner?.EndBatch(managers);
    currentBatchKey = renderable.BatchKey;
    lastBatchOwner = renderable;
    renderable.StartBatch(managers);
}
```

Three behaviors worth internalizing:

- **Empty BatchKey is treated as "no transition required."** A renderable with `BatchKey=""` (containers, GUE wrappers) does NOT flush the current batch. This is intentional for plain wrappers but becomes a bug when something with a non-empty BatchKey claims a batch it doesn't actually start.
- **`SpriteBatchRenderableBase.StartBatch`** calls `spriteRenderer.Begin(false)` followed by `spriteRenderer.ForceSetRenderStatesToCurrent()`. **`EndBatch`** calls `spriteRenderer.End()` — flushes SpriteBatch directly (does NOT pop the SpriteBatchStack). The pairing of `Begin(false)` + `ForceSetRenderStatesToCurrent` is what re-applies the active `BeginParameters` (scissor/raster/blend/sampler/transform) to the underlying SpriteBatch — see "SpriteBatchStack: Begin(false) must re-apply currentParameters" below for why both calls are required.
- **`RenderableShapeBase.StartBatch/EndBatch`** call `ShapeBatch.Begin/End` — separate GPU state stream, but the runtime now plumbs the active scissor rect through so shapes honor `ClipsChildren`. See "Shape Clipping: ShapeBatch Honors Scissor via rasterizerState" below.

## SpriteBatchStack: Push / Pop / Replace

`SpriteBatchStack` wraps a single `SpriteBatch` instance with a stack of `BeginParameters`:

- `PushRenderStates(...)` ≈ `BeginType.Push`: pushes current params onto stack, then `ReplaceRenderStates`.
- `ReplaceRenderStates(...)` ≈ `BeginType.Begin`: ends the SpriteBatch if Began, sets new currentParameters, calls `SpriteBatch.Begin`. **Does not change stack depth.**
- `PopRenderStates()`: pops top of stack. If popped value has params, `ReplaceRenderStates` to it; if null, sets `currentParameters=null` and ends SpriteBatch.
- `Begin(createNewParameters=false)`: ends SpriteBatch if Began, then begins it again, **re-applying the active `currentParameters` to both the GraphicsDevice (ScissorRectangle, RasterizerState) and the underlying `SpriteBatch.Begin` call** (full 7-arg overload). Used by `SpriteBatchRenderableBase.StartBatch` to re-flush sprites mid-walk while keeping the same params. See "SpriteBatchStack: Begin(false) must re-apply currentParameters" below — this contract was silently violated before the fix in #2706 (the parameterless `SpriteBatch.Begin()` was used, which resets to MonoGame defaults including `RasterizerState.CullCounterClockwise` with `ScissorTestEnable=false`, silently dropping clip state).
- `End()`: just ends SpriteBatch (flushes pending sprites). Does not touch stack.

Invariant: every `BeginType.Push` must be balanced by exactly one `EndSpriteBatch` (which calls Pop). `BeginType.Begin` does not enter the stack and doesn't need a balancing pop.

Mid-walk `End()` from `SpriteBatchRenderableBase.EndBatch` does NOT pop the stack — it just flushes. Subsequent `spriteRenderer.Begin(false)` resumes with the same currentParameters. This is how sprite/text renderables can interleave with shape batches without imbalancing the stack.

## Cross-Cycle State (Critical)

`Renderer.currentBatchKey` and `Renderer.lastBatchOwner` are instance fields that **persist across Begin/End cycles**. So when FRB2 draws Card N then Card N+1 in two separate `GumBatch.Begin/End` cycles:

- The outgoing state from Card N (e.g. `currentBatchKey="Apos.Shapes"`, `lastBatchOwner=Back`) is what Card N+1 sees on entry.
- The Apos.Shapes `ShapeBatch` may still be **Begun with queued shapes** at the start of Card N+1's cycle — `Renderer.End` doesn't end it.

This cross-cycle leakage is the single biggest source of "draw order looks weird across N renderables" bugs. Any fix to flushing must end the custom batch at `Renderer.End` so cycle boundaries are clean.

## SpriteBatchStack: Begin(false) must re-apply currentParameters

`Begin(createNewParameters=false)` runs whenever the BatchOrchestrator transitions back to SpriteBatch from a custom batch (Apos.Shapes, future custom batches). It's reached via `SpriteBatchRenderableBase.StartBatch`, which sequences:

```csharp
spriteRenderer.Begin(createNewParameters: false);
spriteRenderer.ForceSetRenderStatesToCurrent();   // calls ReplaceRenderStates with currentParameters' values
```

The reason **both** calls are needed: `SpriteRenderer.Begin(false)` is a guarded passthrough that only invokes `mSpriteBatch.Begin(false)` if SpriteBatch is currently `Ended`. After ShapeBatch took over, SpriteBatch was indeed ended (by the previous `SpriteBatchRenderableBase.EndBatch`). So `Begin(false)` actually executes — and **must** re-apply currentParameters to both the GraphicsDevice and the underlying `SpriteBatch.Begin(...)` call. `ForceSetRenderStatesToCurrent` then immediately runs `ReplaceRenderStates`, which performs an End+Begin cycle with the same parameters.

The historical bug (latent pre-#2582, surfaced when the BatchOrchestrator made the path run on every batch transition): `Begin(false)` was calling parameterless `SpriteBatch.Begin()` instead of the 7-arg overload with currentParameters. That set MonoGame's default rasterizer (`CullCounterClockwise`, `ScissorTestEnable=false`), silently dropping scissor and clipping any subsequent sprites/text drawn under a `ClipsChildren` ancestor. The follow-up `ReplaceRenderStates` did re-apply the right state in *theory*, but the back-to-back Begin/End/Begin sequence ended up with the GPU in the wrong rasterizer state — visually, text labels bled outside their clip region.

**Rule:** any future change to `SpriteBatchStack.Begin(bool)` must keep `Begin(false)`'s behavior equivalent to "call `SpriteBatch.Begin(p.SortMode, p.BlendState, p.SamplerState, p.DepthStencilState, p.RasterizerState, p.Effect, p.TransformMatrix)` with `p = currentParameters.Value`" and explicitly assign `GraphicsDevice.ScissorRectangle` / `GraphicsDevice.RasterizerState` before the call. The empirical canary is "text labels inside a ScrollViewer or ListBox should clip to the container."

## Shape Clipping: ShapeBatch Honors Scissor via rasterizerState

`ShapeBatch.Begin` (Apos.Shapes 0.6.8+) has this signature:

```csharp
public void Begin(Matrix? view = null, Matrix? projection = null,
    BlendState? blendState = null, SamplerState? samplerState = null,
    DepthStencilState? depthStencilState = null, RasterizerState? rasterizerState = null)
```

The `rasterizerState` parameter is how shapes opt into scissor testing. Setting `GraphicsDevice.ScissorRectangle` alone is **not enough** — Apos.Shapes' internal default rasterizer has `ScissorTestEnable=false`, which suppresses scissor regardless of the GraphicsDevice's rect. You must also pass a `RasterizerState` with `ScissorTestEnable=true` to `ShapeBatch.Begin`.

`RenderableShapeBase.StartBatch` reads the active SpriteBatch clip via:

- `SpriteRenderer.CurrentScissorRectangle` — `System.Drawing.Rectangle?`, non-null when SpriteBatch's `currentParameters.RasterizerState.ScissorTestEnable` is true. Mirrors `CurrentTransformMatrix` (the same plumbing pattern for view-matrix sync between SpriteBatch and ShapeBatch).
- `SpriteRenderer.ScissorTestRasterizerState` — the shared `scissorTestEnabled` rasterizer used by Sprite clipping. Same instance is passed to `ShapeBatch.Begin` so both batches see identical scissor behavior.

When a clip is active, `RenderableShapeBase.StartBatch` assigns `ShapeBatch.GraphicsDevice.ScissorRectangle = scissor.ToXNA()` (belt-and-suspenders — the rect may already be set from the prior `SpriteBatch.Begin`, but the explicit assignment makes the contract clear) and passes `ScissorTestRasterizerState` to `ShapeBatch.Begin`. When no clip is active, both pass null and shapes render fullscreen.

**Past wrong turn documented here so it doesn't get re-tried:** an early diagnostic concluded "Apos.Shapes ignores externally-set scissor state" based on a probe that set `GraphicsDevice.ScissorRectangle` + `GraphicsDevice.RasterizerState` before/after `sb.Begin()` and saw no clipping. That was incomplete — the probe never tried `sb.Begin(rasterizerState: scissorEnabled)`. Always check `ShapeBatch.Begin`'s full overload before concluding a clipping-related limitation is upstream.

## Mid-Walk Scissor Change Must Flush the Open Custom Batch

`Renderer.AdjustRenderStates` restarts SpriteBatch (`BeginSpriteBatch(BeginType.Begin)`) whenever blend / color / wrap / **clip** changes on a renderable. SpriteBatch state is reapplied in place, but **a custom batch (Apos.Shapes ShapeBatch) opened earlier in the walk is not touched** — it stays Begun with whatever scissor state it captured at its own `sb.Begin` call.

The hazard pattern: a shape sibling of a clip container is rendered first (e.g. a scrollbar thumb that lives outside `ClipContainerInstance` but in the same `ScrollViewer`). After the scrollbar, `_currentBatchKey = "Apos.Shapes"`, ShapeBatch is Begun with no-scissor state. Then we descend into the clip container — `AdjustRenderStates` restarts SpriteBatch with the new scissor rect, but ShapeBatch is left alone. The first shape descendant inside the clip has `BatchKey="Apos.Shapes"` matching `_currentBatchKey` → `OnRenderable` is a no-op → no fresh `StartBatch` fires → the shape queues into the still-open stale-scissor ShapeBatch. The next non-shape renderable (e.g. a Text inside the same item) finally fires a transition, `ShapeBatch.End` flushes the queued shapes using the *stale* state, and the first shape bleeds past the clip. Item 2+ are fine because by then a transition has fired.

**Rule:** when a clip rect changes mid-walk (entry or exit), call `_batchOrchestrator.FlushAndReset(managers)` **before** `spriteRenderer.BeginSpriteBatch(BeginType.Begin)`. The exit path in `Renderer.Draw` (the `didClipChange` branch) already does this; the entry path in `AdjustRenderStates` must mirror it. Empirical canary: an Apos-shape-backed item background as the first child inside a `ScrollViewer`'s clip container should clip to the container when scrolled past the top edge.

This is clip-specific. Blend / color / wrap state changes don't propagate to ShapeBatch (each `sb.Begin` captures its own), so they don't need the flush. The orchestrator-level contract (`BatchOrchestratorTests.ShapeAfterFlush_FiresFreshStartBatch_EvenWhenKeyMatchesPreFlushKey`) documents the post-flush behavior the fix relies on.

**Test gap:** the orchestrator unit tests cover the "given flush, then next renderable fires Start" contract, but the *integration* — that `Renderer.AdjustRenderStates` actually invokes `FlushAndReset` on a clip change — isn't automated. The empirical canary in checklist item #9 is what catches regressions; a Renderer-level test would need GPU scaffolding similar to `MatrixRoutingTests` (MinimalGame.RunOneFrame) plus a way to observe orchestrator events through a real draw walk. Worth adding if this code path regresses again.

## ContainerRuntime's BatchKey Override (Historical Pitfall)

`MonoGameGum/GueDeriving/ContainerRuntime.cs` historically had:

```csharp
public override string BatchKey => Children?.LastOrDefault()?.BatchKey ?? string.Empty;
```

This is a **broken peephole optimization**. It tries to pre-claim the batch the last child will need, so the transition fires once at the container instead of repeatedly inside. But the Container's `StartBatch` is a no-op (`InvisibleRenderable.StartBatch` does nothing), so the claim is a lie:

- Container reports `BatchKey="Apos.Shapes"` but doesn't actually start ShapeBatch.
- First child (e.g. a shape Background) matches the claimed key → transition skipped.
- Background's `Render` queues into a stale or absent ShapeBatch.

**Rule:** a renderable's `BatchKey` must match what its `StartBatch` actually begins. If `StartBatch` is a no-op, `BatchKey` must be empty.

## How Children Are Walked

`Renderer.Draw(renderable)` recurses via `IRenderableIpso.Children`, which `GraphicalUiElement` implements as:

```csharp
ObservableCollection<IRenderableIpso> IRenderableIpso.Children
    => mContainedObjectAsIpso?.Children ?? EmptyIpsoChildren;
```

So the rendered children list is the **contained renderable's** children — not the public `GUE.Children` wrapper. The two lists are populated together (when a child GUE is parented, it's added to `mContainedObjectAsIpso.Children`), but if you instrument or audit one and not the other, you'll get misleading results. Always verify against `((IRenderableIpso)visual).Children`.

`renderable.BatchKey` invocation goes through `GraphicalUiElement.BatchKey`:

```csharp
public virtual string BatchKey => mContainedObjectAsIpso?.BatchKey ?? string.Empty;
```

So a GUE delegates to its contained renderable's BatchKey. Concrete BatchKey values:

| Renderable | BatchKey |
|---|---|
| `InvisibleRenderable`, `RenderableBase` (default) | `""` |
| `SpriteBatchRenderableBase` (Sprite, Text, NineSlice, SolidRectangle, ColoredRectangle…) | `"SpriteBatch"` |
| `RenderableShapeBase` (RoundedRectangle, Circle, Line, Arc — Apos.Shapes) | `"Apos.Shapes"` |
| Skia equivalents | `""` (Skia doesn't use this batching machinery) |

## Matrix Routing: SpriteBatch vs ShapeBatch Must Agree

Sprites and shapes go through **different** transform paths even when they're queued in the same Renderer.Draw walk. Lining them up on screen requires the effective vertex transform to match. There are three matrix sources to keep in mind:

1. **`SpriteBatch.Begin(transformMatrix:)`** — set by `SpriteBatchStack.ReplaceRenderStates/PushRenderStates` to the composed view (see "Compose, don't replace" below). **When a custom Effect is bound to SpriteBatch (which Gum always does in `UsingEffect` mode — `BasicEffect` for `UseBasicEffectRendering=true`, the custom effect manager otherwise), MonoGame stores this on the unused default `_spriteEffect` and ignores it for vertex transformation.** Its only practical role in Gum is being read back via `SpriteRenderer.CurrentTransformMatrix` (= `mSpriteBatch.CurrentParameters?.TransformMatrix`) so `RenderableShapeBase.StartBatch` can pass it as ShapeBatch's `view`.

2. **`basicEffect.World/View/Projection`** (BasicEffect path) and the matching `effectManager.ParameterViewProj` (custom-effect path) — these are what *actually* transforms sprite vertices. Both branches now use the same composed view; see "Compose, don't replace" below for the formula. `World=Identity`, `Projection = CreateOrthographic(W, -H, -1, 1)` (centered ortho), with an optional `* CenterTranslation(-W/2,-H/2)` folded into `View` for top-left / `IsInScreenSpace` cameras.

3. **`ShapeBatch.Begin(view:)`** — Apos.Shapes' own view matrix, with its own default projection `CreateOrthographicOffCenter(0, W, H, 0, 0, 1)` (top-left ortho). `RenderableShapeBase.StartBatch` reads `SpriteRenderer.CurrentTransformMatrix` and passes it here so shape and sprite see the same view.

**Equivalence:** `View(=Forced) * CenterTranslation * Ortho(W,-H)` ≡ `View(=Forced) * OrthoOffCenter(0,W,H,0)`. So passing `ForcedMatrix` as `view` to ShapeBatch produces the same effective transform as setting it on `basicEffect.View` (with center translate) — *provided neither is also applied a second time*.

**Compose, don't replace.** `ForcedMatrix` is a *world transform that the consumer wants applied on top of the camera view* — not a replacement for it. The current routing:

- `basicEffect.World = Identity`
- `basicEffect.View = ForcedMatrix.HasValue ? ForcedMatrix.Value * GetZoomAndMatrix(...) : GetZoomAndMatrix(...)` (then `* CenterTranslation` if `shouldOffset`)
- The custom-effect path (`Renderer.UseCustomEffectRendering=true`) follows the same compose rule on its own `view` local before feeding `view * projection` into `effectManager.ParameterViewProj`. Both branches must stay in sync — if you change one, change the other.
- `SpriteBatch.transformMatrix = ForcedMatrix.HasValue ? ForcedMatrix.Value * spriteBatchTransformMatrix : spriteBatchTransformMatrix` — read back by shapes via `SpriteRenderer.CurrentTransformMatrix`, so `ShapeBatch.Begin(view:)` sees the same composition. `spriteBatchTransformMatrix` is `GetZoomAndMatrix(layer, camera)` regardless of `UsingEffect` (a pre-#2590 layer-zoom-only special case was the cause of one regression).

This keeps camera position/zoom contributing for `GumBatch` consumers (otherwise they render with no camera at all), and keeps sprite/shape vertices aligned (both apply the same composed view exactly once).

**The double-application trap:** the routing breaks the moment the consumer's `ForcedMatrix` *already includes* something that's also baked into `Camera.Zoom` / `GetZoomAndMatrix`. Example: FRB2's `GumRenderBatch` historically did both `Camera.Zoom = scale` and `Begin(Matrix.Scale(scale))` — the comment said "the cursor reads `Camera.Zoom` for hit-testing, but rendering needs the matrix." Under compose semantics that gives sprites and shapes a consistent `scale²` (worse than the pre-fix `s` vs `s²` drift, but at least *aligned*). The clean answer is on the consumer side: set `Camera.Zoom` for hit-testing OR pass `ForcedMatrix`, not both. If hit-testing must keep reading `Camera.Zoom`, pass `ForcedMatrix = null` (or `Identity`) and let `GetZoomAndMatrix` do all the work.

**Past wrong turns documented here so they don't get re-tried:**
- `World = ForcedMatrix; View = GetZoomAndMatrix` (pre-#2589): aligned for compose-style consumers but *unaligned with shapes* — shapes only saw `ForcedMatrix` (or `spriteBatchTransformMatrix`), so sprite=`F·G` vs shape=`F` drifted at non-1 scale.
- `World = Identity; View = ForcedMatrix ?? GetZoomAndMatrix` (#2589, replace semantics): aligned sprites with shapes BUT silently dropped camera position/zoom whenever `ForcedMatrix` was set, breaking every `GumBatch` consumer that didn't bake camera into its own matrix. Reverted to compose in #2590.
- `spriteBatchTransformMatrix = GetZoomMatrixFromLayerCameraSettings()` under `UsingEffect` (pre-#2590): this matrix is ignored by MonoGame for sprite vertex transformation when a custom Effect is bound, so historically it was set to a weak layer-zoom-only matrix. But it's read back via `SpriteRenderer.CurrentTransformMatrix` and fed into `ShapeBatch.Begin(view:)` — so it must match `basicEffect.View` or shapes desync from sprites at any non-zero `Camera.Zoom`/position. Unified to `GetZoomAndMatrix(layer, camera)` for both paths in #2590.

**Regression test:** `Tests/MonoGameGum.IntegrationTests/MonoGameGum/Rendering/MatrixRoutingTests.cs` asserts that `SpriteRenderer.CurrentTransformMatrix` (the shape-side view) equals the composed `ForcedMatrix * GetZoomAndMatrix` for every combination of `Camera.Zoom` ∈ {1, 2}, scroll, and `ForcedMatrix` ∈ {null, scale}. Each of the past wrong turns above fails at least one row in this test. **Run it before merging any change to matrix routing.**

When changing matrix routing, verify alignment with a two-rectangle test (one `ColoredRectangleRuntime`, one `RoundedRectangleRuntime`, identical Gum coords) at a non-identity scale. Drift between them is the canary for a double-application bug.

## Diagnostic: One-Frame Render Trace

When debugging draw-order bugs, the lowest-cost approach is to add a static `RenderTrace.IsEnabled` toggle and emit `Debug.WriteLine` calls from `Renderer.Draw`, `Renderer.Begin/End`, the `StartBatch/EndBatch` hooks, and `SpriteBatchStack.Begin/End/Push/Pop/Replace`. Indent by recursion depth. Toggle on for one frame via a key press. The trace shows you the exact GPU paint order.

When reading a trace:
- A `Render -> RoundedRectangle` line means a shape was queued into ShapeBatch (NOT drawn yet).
- A `Render -> Sprite/Text` line means a draw call was queued into SpriteBatch.
- Actual GPU draw happens at `>> ShapeBatch.End` and `>> spriteRenderer.End()` lines.
- Cross-reference these against the renderable order to see when each pixel actually hits the framebuffer.

## Known Pitfalls Checklist

When changing batch logic:

1. Does the new `BatchKey` match what `StartBatch` actually begins? If `StartBatch` is a no-op, `BatchKey` must be `""`.
2. Does `Renderer.End` flush `lastBatchOwner`? If not, custom-batch draws leak across cycles.
3. Does the transition logic flush in BOTH directions (custom→sprite and sprite→custom)? Empty BatchKey on a renderable with no real batch shouldn't trigger machinery, but transitions to/from non-empty keys must always flush the outgoing batch.
4. Does mid-walk re-begin of SpriteBatch use `Begin(false)` (preserves params, no stack change) or `BeginType.Push` (changes stack)? Mismatching causes stack underflow at outer EndSpriteBatch.
5. Are you assuming `currentBatchKey` is `""` at the start of a Renderer.Begin cycle? It's not — it persists from the previous cycle. Reset it explicitly if you need a clean state.
6. If you touch `basicEffect.World/View` or `ShapeBatch.Begin(view:)`, does `ForcedMatrix` end up applied **exactly once** to sprite vertices and once to shape vertices? Remember that `SpriteBatch`'s own `transformMatrix` is ignored when a custom Effect is bound — only `basicEffect`'s World*View*Projection moves sprite vertices.
7. If you touch `SpriteBatchStack.Begin(bool)`, does `Begin(false)` still call `SpriteBatch.Begin` with all seven `currentParameters` fields (sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, transformMatrix) AND assign `GraphicsDevice.ScissorRectangle` + `GraphicsDevice.RasterizerState` first? Empirical canary: text labels inside a `ScrollViewer` or `ListBox` clip to the container.
8. If you touch `RenderableShapeBase.StartBatch` or `ShapeBatch.Begin` plumbing, does the active scissor state still flow to the shape batch? Empirical canary: rounded shape bodies inside a `ScrollViewer` / `ListBox` clip to the container. Setting `GraphicsDevice.ScissorRectangle` is not sufficient — `ShapeBatch.Begin` must also receive a `rasterizerState` with `ScissorTestEnable=true`.
9. If you touch `Renderer.AdjustRenderStates` or the clip-change paths in `Renderer.Draw`, does a clip change (entry OR exit) flush the orchestrator (`_batchOrchestrator.FlushAndReset`) BEFORE `BeginSpriteBatch`? Empirical canary: the first item's shape background inside a `ScrollViewer` clips when scrolled past the top edge (not just the second item and beyond).
