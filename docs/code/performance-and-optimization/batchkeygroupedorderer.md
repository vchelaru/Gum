# Batch Key Grouped Orderer

### Introduction

Gum's default renderer walks the visual tree in depth-first order and emits one draw per renderable. Two kinds of alternation force extra work when adjacent renderables don't match:

* **Different batcher.** A `SpriteRuntime` (which uses `SpriteBatch`) next to a Gum.Shapes shape (which uses Apos.Shapes) forces the renderer to end one batch and begin another. A scene that alternates between these types ends up with a batch transition on nearly every draw.
* **Same batcher, different texture.** A `NineSliceRuntime` frame next to a `TextRuntime` label both use `SpriteBatch`, so no batch transition happens, but they draw from different textures. `SpriteBatch` only merges *consecutive* draws that share a texture, so alternating frame-then-text-then-frame still costs one GPU draw call per element.

`BatchKeyGroupedOrderer` is an opt-in alternative ordering strategy that reorders draws within layer- and clip-bounded windows so that runs sharing the same batcher, and within that the same texture, become contiguous. This collapses both kinds of alternation down to roughly one draw call per distinct batcher/texture combination in the scene.

{% hint style="info" %}
The batcher-level grouping described here has been available for a while. The texture-level grouping (the "How it groups draws" section below, `BatchSortKey`, and `RenderStateChangeStatistics.DrawCallCount`) is available in September 2026, or now if building Gum from source.
{% endhint %}

### When to use it

The default `HierarchicalOrderer` is correct and fast. The grouped orderer helps when:

* Your scene mixes batch types the user can't unify with atlas tricks, most commonly `SpriteBatch` (sprite/text/nineslice/solid rectangle) combined with Gum.Shapes shapes (`CircleRuntime`, `RectangleRuntime`, etc.).
* You're seeing a `SpriteBatch.Begin` count that scales with the number of items in a list or grid rather than with the number of distinct textures.
* You have a list or grid whose rows mix textures under a single batcher, such as a `NineSliceRuntime` frame with a `TextRuntime` label on top, and the GPU draw-call count scales with the row count instead of the number of distinct textures on screen.

Within `SpriteBatch` alone, you can also reduce draw calls by packing fonts and sprites into one atlas (see [SinglePixelTexture](singlepixeltexture.md)); an atlas removes the alternation entirely, so it wins over grouping when it's practical. Reach for the grouped orderer when an atlas isn't practical (mixed art sources, dynamically loaded textures) or when the alternation crosses batchers, which no atlas can fix.

### Choosing an orderer

`Renderer.SiblingOrdering` is a swap slot: assign it one of the available orderer instances to choose how the main render pass is ordered. The shipped options are:

| Set `Renderer.SiblingOrdering` to | What it does |
|---|---|
| `HierarchicalOrderer.Instance` *(default)* | Depth-first walk, one draw per renderable in tree order. No reordering, this is the order Gum has always used. |
| `BatchKeyGroupedOrderer.Instance` | Reorders draws within layer- and clip-bounded windows so matching draws become contiguous, cutting both batch flushes and GPU draw calls. Output is pixel-identical to the default. |

Note that the default and the opt-in are **different** types, `HierarchicalOrderer` versus `BatchKeyGroupedOrderer`. Assign the grouped one to turn the optimization on:

```csharp
// Initialize
RenderingLibrary.Graphics.Renderer.SiblingOrdering =
    RenderingLibrary.Graphics.BatchKeyGroupedOrderer.Instance;
```

To switch back, assign `HierarchicalOrderer.Instance`. You can flip the property at any time; no teardown is required, and the next frame uses the new orderer.

### How it groups draws

The orderer sorts on two levels:

1. **Batcher first.** Every renderable reports a `BatchKey` string identifying which batcher it draws with (`"SpriteBatch"` or the Apos.Shapes key). The orderer keeps same-`BatchKey` draws together first, so `SpriteBatch` work and shape work each land in one contiguous run.
2. **Texture within that.** Renderables that draw from a texture also report a `BatchSortKey`, typically the `Texture2D` they use. Within a `BatchKey` run, the orderer further groups draws that share the same `BatchSortKey`, so all the frame draws land together and all the text draws land together.

`Sprite` and `NineSlice` report their texture as their `BatchSortKey`. `Text` reports its font's texture when it renders character-by-character (the default text rendering mode); text rendered through a cached render target or an XNA `SpriteFont` has no exposed texture reference and falls back to no finer grouping. A renderable with no `BatchSortKey` still groups correctly by `BatchKey`, it just doesn't get the finer texture-level pass.

You don't need to set `BatchSortKey` yourself. It's a read-only hint the built-in renderables already provide; it exists for the orderer to read, not for game code to assign.

### What it preserves

The grouped orderer is pixel-correct for any scene the default orderer renders correctly:

* `BeginClip` / `EndClip` scopes are never crossed by reorder.
* `Layer` boundaries are never crossed.
* Same-Y runs on layers with `SecondarySortOnY` stay independent.
* Two renderables whose absolute bounds intersect always keep their original front-to-back order, so the painter's algorithm result is unchanged.

### Trade-offs

The orderer runs per layer per frame and builds an overlap graph between renderables in each reorder window. The cost is `O(n²)` in the number of renderables in the window. For typical UI scenes this is negligible; for windows with thousands of renderables, profile before assuming it's free.

### Measuring the win

The two cases the orderer targets show up in different diagnostics, because only one of them changes how many times `SpriteBatch.Begin` is called:

* **Cross-batcher alternation** (`SpriteBatch` next to Apos.Shapes) reduces the `SpriteBatch.Begin` count. Use [LastFrameDrawStates](lastframedrawstates.md) or `Renderer.GetDrawStateSummary` to count begins before and after enabling the orderer.
* **Same-batcher, different-texture alternation** never changes the `SpriteBatch.Begin` count, since both renderables already share the `SpriteBatch` batch. The win is fewer actual GPU draw calls inside that one batch. Measure it with `RenderStateChangeStatistics.DrawCallCount` (see [Measuring GPU draw calls](lastframedrawstates.md#measuring-gpu-draw-calls-drawcallcount)), or watch `GetDrawStateSummary`'s `TextureSetCount` drop.

Before reaching for the orderer, confirm it can actually help. Call `Renderer.GetDrawStateSummary` (see [Summarizing by cause](lastframedrawstates.md#summarizing-by-cause)) and check two rows:

* **`Apos.Shapes ShapeBatch.Begin(s)`:** if this is near zero, your scene has no cross-batcher alternation to collapse.
* **Texture sets within batches:** if this is near zero, your scene has no same-batcher texture alternation to collapse either.

If both rows are low, your begins come from clipping instead, and the orderer will leave your draw-call count unchanged no matter what. In that case, reduce `ClipsChildren` usage rather than switching orderers.

{% hint style="info" %}
`LastFrameDrawStates` only sees `SpriteBatch.Begin` calls. Apos.Shapes batch starts are not counted there, so the total batch count is higher than the reported number. `RenderStateChangeStatistics.DrawCallCount` is backend-neutral and counts every actual GPU draw call, including Apos.Shapes ones, so prefer it when you want a single before/after number.
{% endhint %}
