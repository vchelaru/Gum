# Batch Key Grouped Orderer

### Introduction

Gum's default renderer walks the visual tree in depth-first order and emits one draw per renderable. When two adjacent renderables go through different batchers — for example, a `SpriteRuntime` (which uses `SpriteBatch`) followed by a Gum.Shapes shape (which uses Apos.Shapes) — the renderer must end one batch and begin another. A scene that alternates between these types ends up with a batch transition on nearly every draw.

`BatchKeyGroupedOrderer` is an opt-in alternative ordering strategy that reorders draws within layer- and clip-bounded windows so that runs of the same `BatchKey` become contiguous. This collapses the per-alternation transitions to roughly one per distinct batch type in the scene.

### When to use it

The default `HierarchicalOrderer` is correct and fast. The grouped orderer helps when:

* Your scene mixes batch types the user can't unify with atlas tricks — most commonly `SpriteBatch` (sprite/text/nineslice/solid rectangle) combined with Gum.Shapes shapes (`CircleRuntime`, `RectangleRuntime`, etc.).
* You're seeing a `SpriteBatch.Begin` count that scales with the number of items in a list or grid rather than with the number of distinct textures.

Within `SpriteBatch` alone, you can usually reduce flushes yourself by packing fonts and sprites into one atlas (see [SinglePixelTexture](singlepixeltexture.md)). Cross-batcher alternation has no such workaround — that's the case this orderer exists to fix.

### Choosing an orderer

`Renderer.SiblingOrdering` is a swap slot: assign it one of the available orderer instances to choose how the main render pass is ordered. The shipped options are:

| Set `Renderer.SiblingOrdering` to | What it does |
|---|---|
| `HierarchicalOrderer.Instance` *(default)* | Depth-first walk, one draw per renderable in tree order. No reordering — this is the order Gum has always used. |
| `BatchKeyGroupedOrderer.Instance` | Reorders draws within layer- and clip-bounded windows so same-`BatchKey` runs become contiguous, cutting batch flushes. Output is pixel-identical to the default. |

Note that the default and the opt-in are **different** types — `HierarchicalOrderer` versus `BatchKeyGroupedOrderer`. Assign the grouped one to turn the optimization on:

```csharp
// Initialize
RenderingLibrary.Graphics.Renderer.SiblingOrdering =
    RenderingLibrary.Graphics.BatchKeyGroupedOrderer.Instance;
```

To switch back, assign `HierarchicalOrderer.Instance`. You can flip the property at any time; no teardown is required, and the next frame uses the new orderer.

### What it preserves

The grouped orderer is pixel-correct for any scene the default orderer renders correctly:

* `BeginClip` / `EndClip` scopes are never crossed by reorder.
* `Layer` boundaries are never crossed.
* Same-Y runs on layers with `SecondarySortOnY` stay independent.
* Two renderables whose absolute bounds intersect always keep their original front-to-back order, so the painter's algorithm result is unchanged.

### Trade-offs

* The orderer runs per layer per frame and builds an overlap graph between renderables in each reorder window. The cost is `O(n²)` in the number of renderables in the window. For typical UI scenes this is negligible; for windows with thousands of renderables, profile before assuming it's free.
* The first-pass scope keys only on the existing `BatchKey` string. Finer texture-level grouping inside a single batch type is a possible future optimization.

### Measuring the win

Use [LastFrameDrawStates](lastframedrawstates.md) to count `SpriteBatch.Begin` calls before and after enabling the orderer. The grouped orderer should reduce the count substantially on any scene that mixes `SpriteBatch` and Apos.Shapes work; it should leave a single-batcher scene unchanged.

Before reaching for the orderer, confirm it can actually help: call `Renderer.GetDrawStateSummary` (see [Summarizing by cause](lastframedrawstates.md#summarizing-by-cause)) and look at the `Apos.Shapes ShapeBatch.Begin(s)` row. The orderer only collapses alternation between the `SpriteBatch` and Apos.Shapes batchers, so if that row is near zero, your begins come from clipping or texture sets instead — and the orderer will leave the count unchanged no matter what. In that case, reduce `ClipsChildren` usage or atlas your textures rather than switching orderers.

{% hint style="info" %}
`LastFrameDrawStates` only sees `SpriteBatch.Begin` calls. Apos.Shapes batch starts are not counted, so the total batch count is higher than the reported number. The orderer's effect on `SpriteBatch.Begin` is still a useful proxy for the overall trend.
{% endhint %}
