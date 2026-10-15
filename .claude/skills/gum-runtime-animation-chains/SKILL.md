---
name: gum-runtime-animation-chains
description: AnimationChain playback on Sprites and NineSlices — .achx -> AnimationChain pipeline, AnimationChainLogic tick loop, apply-frame vs render-time split, RelativeX/Y mismatch with FRB AnimationEditor. Triggers: .achx, AnimationChain, AnimationFrame, AnimationChainList, AnimationChainLogic, SpriteAnimationLogic, ApplyAnimationFrame, RelativeX/Y offsets, CurrentChainName.
---

# Runtime Animation Chains

Gum plays back FRB-style `.achx`/`.achj` animations on `Sprite` and `NineSlice`. `.achx` (XML) and `.achj` (JSON, matching FlatRedBall2's `AnimationChain.Common` writer) both deserialize into `AnimationChainListSave` — see `AnimationChainListSave.FromFile`. Each chain is a list of frames with a texture, source rect, frame length, optional flip flags, optional `RelativeX`/`RelativeY` per-frame offsets, and optional per-frame color (see below).

## Pipeline: Save → Runtime

`AnimationChainListSave` (XML) → `ToAnimationChainList()` → `AnimationChainList` (a `List<AnimationChain>`). Each `AnimationChainSave.Frames[i]` becomes an `AnimationFrame` via `AnimationFrameSave.ToAnimationFrame(loadTexture, coordinateType)`.

**`TextureCoordinateType` matters at load time, not render time.** The `AnimationFrame.LeftCoordinate/RightCoordinate/TopCoordinate/BottomCoordinate` fields are always stored as UV (0–1). If the source `.achx` declares `<CoordinateType>Pixel</CoordinateType>`, the loader divides by `Texture.Width/Height` during conversion. UV-mode `.achx` files copy the values verbatim. A frame loaded before its texture resolves cannot perform pixel-to-UV conversion — its coords stay zero.

## Playback: AnimationChainLogic

Both `Sprite` and `NineSlice` compose an `AnimationChainLogic` instance (XNA, Sokol, Raylib, Skia all share the same playback type). `SpriteAnimationLogic` is a back-compat `[Obsolete]` subclass; new code should use `AnimationChainLogic`.

State lives entirely on `AnimationChainLogic`:

- `_currentChainIndex` defaults to 0 so assigning `AnimationChains` + `Animate = true` works without setting `CurrentChainName`. `CurrentChainName` setter sets `_currentChainIndex = -1` and resolves the desired name lazily once chains are populated (`RefreshCurrentChainToDesiredName`), then seeds `_isLooping` from the newly-resolved chain's `AnimationChain.Loop` (itself threaded from `AnimationChainSave.Loop`, default `true`) — still freely overridable per-instance afterward via `IsAnimationChainLooping`. The implicit index-0-without-`CurrentChainName` path does **not** reseed; `_isLooping` stays at its own `true` default there.
- `AnimateSelf(secondDifference)` advances `_timeIntoAnimation`, loops or clamps based on `IsAnimationChainLooping`, picks a new frame via `UpdateFrameBasedOffOfTimeIntoAnimation`, and calls `UpdateToCurrentAnimationFrame()` only if the frame index changed.
- Events: `AnimationChainCycled` fires only when a looping chain wraps (either direction). `AnimationChainFinished` fires once when a non-looping chain hits its end (forward: last frame; negative speed: first frame), after `Animate` turns false and the final frame is applied. Both are forwarded by `Sprite`, `NineSlice` and `SpriteRuntime`, and nulled in `Clone`; a new forwarder or event must do both. The frame lookup wraps at exactly `TotalLength`, so the forward end sets the last index directly instead of calling it.
- `UpdateToCurrentAnimationFrame()` invokes the `ApplyFrame` delegate the host wired up. **It does not directly mutate the renderable.**
- With a chain set, an out-of-range frame index or negative time is clamped to the chain (`SeekToFrame`), on set and again in `UpdateToCurrentAnimationFrame` after a chain switch; with no chain they are stored raw.

`AnimateSelf` is driven once per frame by `GraphicalUiElement.AnimateSelf` (recursively). Event handlers may add or remove elements mid-walk, so a list of elements is animated through `GraphicalUiElement.AnimateEach` (children and `GumService.AnimateRoots`), which resumes after the element it just animated wherever it now is; Sokol's `Renderer.TickEach` walks renderables and keeps its own copy. Never `foreach` or snapshot the list (throws or allocates per frame).

## Frame application is split across two times

Sprite handles each frame in **two phases**:

1. **On frame change** (`Sprite.ApplyAnimationFrame`, wired into `AnimationLogic.ApplyFrame`): copies `Texture`, computes pixel `SourceRectangle` from UV coords × texture size, and copies `FlipHorizontal`/`FlipVertical`. Persistent state.
2. **At every render** (`Sprite.Render`, when `CurrentFrameIndex < CurrentChain.Count`): reads `AnimationLogic.GetRenderOffset(absoluteRotation)` (the frame's live `RelativeX/RelativeY`, or the keyframe-evaluated offset, rotated with the sprite and Y-negated for the Y-down screen), mutates `this.X/Y`, calls the static draw, then restores the originals.

This split matters: changing `RelativeX/Y` on a live frame takes effect on the next render without needing `UpdateToCurrentAnimationFrame`. The offset is applied at the render call site, not in `ApplyFrame`.

`FlipDiagonal` is a transpose applied after the H/V flips (flip the source, then reflect across the main diagonal), keeping the sprite's own footprint. MonoGame synthesizes it as a 90 degree rotation plus a horizontal flip and honors it only for a square source rectangle. Skia concatenates a swap-axes matrix onto the canvas in `DrawBound`; raylib draws a swapped-size, 90-degree-turned rectangle with a mirrored source (`Sprite.GetDiagonalFlipDraw`). Both handle non-square sprites, and raylib skips it on its tiled (`Wrap`) and out-of-texture-bounds clamped paths.

Skia and raylib `Sprite` apply the same `GetRenderOffset` at draw time: raylib shifts the destination rectangle, and Skia translates the canvas in `DrawBound`, which is already rotated with the sprite, so it passes `0` rotation.

## The RelativeX/Y anchor mismatch (recurring gotcha)

`Sprite.Render` applies the per-frame offset as a literal pixel shift around a **top-left** anchor (`origin = Vector2.Zero` in the static `Render`). The Y component is negated on apply (`this.Y -= offsetVector.Y`) because `AnimationFrame.RelativeY` follows FRB's Y-up convention while Gum is Y-down.

**The values inside an `.achx` are authored against FRB's center-anchored Sprite.** When the Sprite's destination height is constant (e.g. `HeightUnits = Absolute`), the offset is just a translation and the anchor difference doesn't matter. When destination height tracks the source rect (e.g. `HeightUnits = PercentageOfSourceFile`), the FRB-authored values only compensate **half** the per-frame height change — because the other half was absorbed by FRB's centered anchor. The Gum-rendered sprite drifts in the direction the source rect is shrinking (typically "climbs upward" for collapse animations).

To match FRB visuals on a height-tracking Sprite, the offset has to be applied around the sprite's center, not its top-left — or an extra `(referenceHeight - currentHeight) / 2` must be added to Y.

## NineSlice: same playback as Sprite

All backends' `NineSlice` compose `AnimationChainLogic` and apply the frame offset (`GetRenderOffset`) and keyframe color (`ApplyKeyframeColor`) like `Sprite`. XNA shifts the absolute origin in `Render`, raylib shifts the destination, Skia translates the canvas in `DrawBound`.

All backends flip the whole drawn nine-slice, flipping first and then transposing (a non-square one stretches the transposed image to its footprint): canvas mirror/transpose on Skia; per-section destination mirror plus `Sprite.GetDiagonalFlipDraw` on raylib, where any flip forces the per-section path instead of `DrawTextureNPatch`. XNA places each of the nine child sprites (and each tile) through `NineSlice.SliceLayout.Place`, which mirrors its rectangle, then draws a transposed piece as a quarter turn about its bottom-left corner plus a horizontal mirror, so it works for non-square pieces unlike the square-only `Sprite` composite. XNA `ApplyAnimationFrame` distributes the frame texture to the 9 internal sprites via `SetSingleTexture`.

## Per-frame color (Alpha/Multiply/Add)

`AnimationFrameSave`/`AnimationFrame` carry nullable `Red`/`Green`/`Blue`/`Alpha` (0-255) and an `AnimationFrameColorOperation?` (`Multiply`/`Add`), matching FRB2's Animation Editor fields. Each backend's `ApplyAnimationFrame` sets the renderable's own `Alpha` when authored, and its own `Red`/`Green`/`Blue` whenever a `ColorOperation` is authored — snapshotted once per frame change, not re-combined at render time, so a frame that authors neither leaves the renderable's current color alone.

**There is one set of colors.** `Color` is the only tint source on a renderable; the renderable's `RenderingLibrary.Graphics.ColorOperation` decides how it is read (#4880) — the same shape as `Alpha` and `Blend`. A frame's `AnimationFrameColorOperation` selects that operation:

| Frame op | Sets `ColorOperation` | Unset-channel default |
|----------|----------------------|-----------------------|
| `Multiply` | `Modulate` | 255 (multiply identity) |
| `Add` | `Add` | 0 (add identity) |
| none | resets `Add` → `Modulate` | color untouched |

`Add` draws the texture untinted and adds `Color` on top: a second additive pass on XNA/raylib (`Renderer.DrawAdditiveColorOverlay`, raylib's `AdditiveColorOverlayShader`), and a single-pass `SKColorFilter.CreateColorMatrix` offset on Skia. It works on both `Sprite` and `NineSlice` across XNA/raylib/Skia. There is **no** separate additive color field — an earlier `AdditiveTintColor` was folded into `Color` + `ColorOperation.Add`.

A separate nullable field, `AnimationFrame.RenderColorOperation`/`AnimationFrameSave.RenderColorOperation`, also picks the renderable's `ColorOperation` (its original purpose was the `Modulate`/`ColorTextureAlpha` technique selector). It writes the **same field** as the table above, so when a frame authors both, the `AnimationFrameColorOperation` wins — it is applied second. Applied in `Sprite.ApplyAnimationFrame` only, on every backend's Sprite.

Note for tool work: exposing `ColorOperation` as a tool-editable variable means codegen emits `X.ColorOperation = ...`, which older runtimes' `NineSliceRuntime` does not have. That needs a syntax-version bump and gate — see `gum-runtime-syntax-version`.

## Keyframe interpolation (offset and color glides)

Every frame is a keyframe; a keyframe's curve runs to the next keyframe of the same group (Offset: `RelativeX/Y`, Color: Red/Green/Blue/Alpha). The six per-frame fields (`SkipOffsetKeyframe`, `OffsetInterpolation`, ...) are stored as raw strings so unknown names round-trip. The math is `KeyframeEvaluator`/`KeyframeCurve` (ported from FlatRedBall2's `AnimationChain.Common`, using the `FlatRedBall.InterpolationCore` tweener GumCommon already references).

- A chain without keyframe data (`KeyframeEvaluator.HasOffsetKeyframes`/`HasColorKeyframes`) takes the old path, byte for byte. Only a chain with color keyframes sets `AnimationChainLogic.IsColorDrivenByKeyframes`; each `ApplyAnimationFrame` must then skip its own color step, because `ApplyKeyframeColor` (called right after `ApplyFrame` and on every `AnimateSelf` tick) already applied the inherited color, and a skipped frame's own channels must not leak.
- Only the MonoGame-family, Skia and raylib Sprites and NineSlices wire `ApplyKeyframeColor`. Sokol's Sprite applies no color or offset and ignores `RelativeX/Y` entirely; the MonoGame-family, Skia and raylib Sprites and NineSlices apply offset and color glides.
- In keyframe mode, channels inherit from the last keyframe that set them and the operation inherits through skipped frames. Red/Green/Blue still reach the sprite only under a Multiply or Add operation, matching FlatRedBall2 (`SpriteFrameColor.Apply`); Alpha applies on its own.

## Key Files

| File | Purpose |
|------|---------|
| `RenderingLibrary/Graphics/Animation/AnimationChainLogic.cs` | Platform-agnostic playback state and tick |
| `RenderingLibrary/Graphics/Animation/SpriteAnimationLogic.cs` | `[Obsolete]` back-compat subclass of `AnimationChainLogic` |
| `RenderingLibrary/Graphics/Animation/KeyframeEvaluator.cs`, `KeyframeCurve.cs` | Pure glide math and curve-name parsing (new files must be added to `GumCoreShared.projitems` and each runtime csproj that links `AnimationChainLogic.cs`) |
| `RenderingLibrary/Graphics/Animation/AnimationFrame.cs` | Runtime frame; `ToAnimationFrame` extension converts from save with `TextureCoordinateType` |
| `RenderingLibrary/Graphics/Animation/AnimationChain.cs` | `List<AnimationFrame>`; `ToAnimationChain` extension |
| `RenderingLibrary/Graphics/Animation/AnimationChainList.cs` | `List<AnimationChain>`; `.achx` deserialization entry point |
| `RenderingLibrary/Graphics/Sprite.cs` | `ApplyAnimationFrame` (frame-change side) and `Render` (per-render `RelativeX/Y` application) |
| `RenderingLibrary/Graphics/NineSlice.cs` | `ApplyAnimationFrame` distributes frame texture across 9 slices; applies the frame offset in `Render` |
| `Gum/Graphics/Animation/Content/AnimationFrameSave.cs` | XML/JSON-serializable frame; pixel or UV coords per `AnimationChainListSave.CoordinateType`; per-frame color fields |
| `Gum/Graphics/Animation/Content/AnimationChainListSave.cs` | `.achx` (XmlSerializer) and `.achj` (`ParseJson`/`ParseFrameJson`) parsing, dialect picked by extension in `FromFile` |
