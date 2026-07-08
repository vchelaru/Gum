# Animation Chains

## Introduction

An `.achx` file is an FRB-style (FlatRedBall) XML animation chain — a list of named animations, where each animation is a sequence of frames that swap texture, source rectangle, and flip state over time. This is frame-by-frame "flipbook" animation, the kind you'd use for a walk cycle or an explosion sprite sheet. Gum's runtime can load an `.achx` and play it back on a `SpriteRuntime` or a `NineSliceRuntime`.

{% hint style="info" %}
Don't confuse animation chains with the [Animation](../animationruntime.md) (`AnimationRuntime`) system. `AnimationRuntime` plays back keyframed *property* animations authored in the Gum tool (position, color, visibility, and so on) on any `GraphicalUiElement`. Animation chains are texture-swapping animations authored in an external FRB-format `.achx` file and only apply to `Sprite` and `NineSlice`. The two systems are unrelated and can be used together.
{% endhint %}

## From `.achx` to `AnimationChainList`

An `.achx` file deserializes into an `AnimationChainListSave` (namespace `Gum.Content.AnimationChain`) — an XML-serializable, disk-shaped representation. Calling `.ToAnimationChainList()` converts it into the runtime representation:

* `AnimationChainList` (namespace `Gum.Graphics.Animation`) — a `List<AnimationChain>`.
* `AnimationChain` — a `List<AnimationFrame>`, one per named animation (for example `"Walk"` or `"Explode"`).
* `AnimationFrame` — one frame: its `Texture`, a source rectangle (`LeftCoordinate`/`RightCoordinate`/`TopCoordinate`/`BottomCoordinate`, stored as UV), `FrameLength`, `FlipHorizontal`/`FlipVertical`, and optional `RelativeX`/`RelativeY` offsets.

You can index an `AnimationChainList` by chain name (`chains["Walk"]`) to get the `AnimationChain` directly.

### TextureCoordinateType: Pixel vs. UV

`AnimationFrame`'s source-rectangle fields are always stored as UV coordinates (0–1) at runtime, but the `.achx` file itself can author frames in either coordinate space via `AnimationChainListSave.CoordinateType`:

* **UV** (the default) — coordinates in the XML are already 0–1 and are copied verbatim.
* **Pixel** — coordinates in the XML are pixel values against the frame's texture. These are divided by the texture's width/height and converted to UV **at load time**, inside `AnimationFrameSave.ToAnimationFrame()`.

This conversion happens once, when the `.achx` loads — not every frame during rendering. If you ever see an animation frame showing the wrong region of a texture, check whether the `.achx`'s `CoordinateType` matches what the tool that authored it intended.

## Loading in Code

There are two ways to get an `.achx` onto a `SpriteRuntime` or `NineSliceRuntime`. Both work the same way on either type, and both work across MonoGame, KNI, FNA, Raylib, and Skia.

### Auto-Load via SourceFileName

The simplest path is to assign `SourceFileName` to a path ending in `.achx`, the same way you'd assign a `.png`. Gum detects the extension, loads and converts the `.achx`, populates `AnimationChains`, and advances to the first frame automatically:

```csharp
// Initialize
var sprite = new SpriteRuntime();
sprite.SourceFileName = "MyAnimation.achx";
sprite.CurrentChainName = "Animation1";
sprite.Animate = true;
```

This is the same `SourceFileName` property documented on the [File Loading](file-loading.md) page — the `.achx` extension is just one more file type it recognizes.

### Manual Construction via AnimationChainListSave

If you want the `AnimationChainList` object itself — to inspect it, or to build/modify chains in code before handing them to a runtime object — load it directly instead of going through `SourceFileName`:

```csharp
// Initialize
using Gum.Content.AnimationChain;
using Gum.Graphics.Animation;

AnimationChainListSave save = AnimationChainListSave.FromFile("AnimatedFrame1.achx");
AnimationChainList chains = save.ToAnimationChainList();

// The AnimationChainList is a plain object you can inspect or modify before assigning it.
chains["Animation1"].FrameTime = 0.1f;

sprite.AnimationChains = chains;
sprite.CurrentChainName = "Animation1";
sprite.Animate = true;
```

### File Caching

Both loading paths go through `LoaderManager`, so an `.achx` loaded from the same path more than once is only read from disk once when `LoaderManager.Self.CacheTextures` is `true` (the default). See [File Loading](file-loading.md#file-caching) for the general caching rules, which apply here the same way they do to textures and fonts.

## Playback

Once `AnimationChains` is populated, set `CurrentChainName` to the chain you want to play and set `Animate` to `true`. From there, the chain advances on its own — every call to `GumService.Default.Update` drives the animation for the entire visual tree, so you don't need to advance frames yourself.

{% hint style="warning" %}
Screenshot/GIF needed: an animated GIF showing a `SpriteRuntime` playing back an `.achx` chain frame-by-frame.
{% endhint %}

If you need lower-level control — for example, driving animation outside of `GumService.Default.Update` — see the [AnimateSelf](../gum-code-reference/graphicaluielement/animateself.md) page.

## Per-Frame Position Offsets (Sprite Only)

Each `AnimationFrame` carries optional `RelativeX`/`RelativeY` values, and `Sprite` applies them as a position offset on every render while an animation with those values is playing. `NineSlice` does not apply `RelativeX`/`RelativeY` at all — a `NineSliceRuntime` ignores them even if the source `.achx` sets them.

These offsets are authored against FlatRedBall's center-anchored `Sprite`, while Gum positions from a configurable origin (top-left by default). For a `SpriteRuntime` with a fixed size this difference doesn't matter, but on a sprite whose size tracks its source rectangle (for example `HeightUnits = PercentageOfSourceFile`), the offset can visibly drift from what FRB's `AnimationEditor` previewed.

## Related Pages

* [File Loading](file-loading.md) — `RelativeDirectory`, file caching, and general file loading rules that also apply to `.achx` files.
* [AnimateSelf](../gum-code-reference/graphicaluielement/animateself.md) — the method that advances animation chain playback each frame.
* [SpriteRuntime](../standard-visuals/spriteruntime/README.md) and [NineSliceRuntime](../standard-visuals/ninesliceruntime.md) — the two runtime types that can play animation chains.
* [Animation](../animationruntime.md) — Gum's separate, unrelated keyframed property-animation system.
