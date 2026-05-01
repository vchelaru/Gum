---
name: gum-monogame-rendering
description: Reference guide for Gum's MonoGame rendering pipeline — the Renderer / SpriteBatchStack / GumBatch architecture, the BatchKey transition machinery, and how SpriteBatch and Apos.Shapes ShapeBatch interleave. Load when working on Renderer.cs, SpriteBatchStack.cs, GumBatch, RenderableShapeBase, SpriteBatchRenderableBase, BatchKey-related logic, draw-order bugs, or the GumRenderBatch integration in FRB2/MonoGameGumImmediateMode samples.
---

# Gum's MonoGame Rendering Pipeline

This skill covers the XNA-family backends only (MonoGame / KNI / FNA). Skia, Raylib, and Sokol have their own renderers and don't go through this code.

## Two Entry Paths into Renderer

### Layered path — `Renderer.RenderLayer`

Used by the Gum tool and any consumer with sorted `Layer.Renderables`. Walks every renderable in the layer in order:

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
2. For any renderable with `IsRenderTarget == true`, calls `RenderToRenderTarget` — which sets a render target on the GraphicsDevice and runs its own SpriteBatch cycle inside.

Phase 2 is why the full PreRender walk **must** run before `BeginSpriteBatch` — once the outer SpriteBatch is begun, you can't safely change the render target or start a nested cycle.

The GumBatch path (`Renderer.Begin/Draw/End`) calls `BeginSpriteBatch` immediately in `Begin`, so it can't host phase 2. `Renderer.Draw(IRenderableIpso)` does run a phase-1-only walk via `InvokePreRenderRecursively` before forwarding to the inner draw, so `AposShapeRuntime.PreRender` and similar hooks fire correctly. **Render targets nested inside a `GumBatch.Draw` tree are not supported on this path** — phase 2 is intentionally skipped to avoid clobbering the outer SpriteBatch.

Practical consequence: any new "runtime resolves a property in `PreRender` and pushes it to the renderable" pattern (the `AposShapeRuntime.StrokeWidth` shape) works on both entry paths, but only the layered path renders nested render targets.

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
- **`SpriteBatchRenderableBase.StartBatch`** calls `spriteRenderer.Begin(false)` — re-uses currentParameters and re-begins SpriteBatch in place. **`EndBatch`** calls `spriteRenderer.End()` — flushes SpriteBatch directly (does NOT pop the SpriteBatchStack).
- **`RenderableShapeBase.StartBatch/EndBatch`** call `ShapeBatch.Begin/End` — completely independent of SpriteBatch state.

## SpriteBatchStack: Push / Pop / Replace

`SpriteBatchStack` wraps a single `SpriteBatch` instance with a stack of `BeginParameters`:

- `PushRenderStates(...)` ≈ `BeginType.Push`: pushes current params onto stack, then `ReplaceRenderStates`.
- `ReplaceRenderStates(...)` ≈ `BeginType.Begin`: ends the SpriteBatch if Began, sets new currentParameters, calls `SpriteBatch.Begin`. **Does not change stack depth.**
- `PopRenderStates()`: pops top of stack. If popped value has params, `ReplaceRenderStates` to it; if null, sets `currentParameters=null` and ends SpriteBatch.
- `Begin(createNewParameters=false)`: ends SpriteBatch if Began, then begins it again. Used by `SpriteBatchRenderableBase.StartBatch` to re-flush sprites mid-walk while keeping the same params.
- `End()`: just ends SpriteBatch (flushes pending sprites). Does not touch stack.

Invariant: every `BeginType.Push` must be balanced by exactly one `EndSpriteBatch` (which calls Pop). `BeginType.Begin` does not enter the stack and doesn't need a balancing pop.

Mid-walk `End()` from `SpriteBatchRenderableBase.EndBatch` does NOT pop the stack — it just flushes. Subsequent `spriteRenderer.Begin(false)` resumes with the same currentParameters. This is how sprite/text renderables can interleave with shape batches without imbalancing the stack.

## Cross-Cycle State (Critical)

`Renderer.currentBatchKey` and `Renderer.lastBatchOwner` are instance fields that **persist across Begin/End cycles**. So when FRB2 draws Card N then Card N+1 in two separate `GumBatch.Begin/End` cycles:

- The outgoing state from Card N (e.g. `currentBatchKey="Apos.Shapes"`, `lastBatchOwner=Back`) is what Card N+1 sees on entry.
- The Apos.Shapes `ShapeBatch` may still be **Begun with queued shapes** at the start of Card N+1's cycle — `Renderer.End` doesn't end it.

This cross-cycle leakage is the single biggest source of "draw order looks weird across N renderables" bugs. Any fix to flushing must end the custom batch at `Renderer.End` so cycle boundaries are clean.

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
