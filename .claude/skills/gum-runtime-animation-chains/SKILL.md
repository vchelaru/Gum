---
name: gum-runtime-animation-chains
description: AnimationChain playback on Sprites and NineSlices — .achx -> AnimationChain pipeline, AnimationChainLogic tick loop, apply-frame vs render-time split, RelativeX/Y mismatch with FRB AnimationEditor. Triggers: .achx, AnimationChain, AnimationFrame, AnimationChainList, AnimationChainLogic, SpriteAnimationLogic, ApplyAnimationFrame, RelativeX/Y offsets, CurrentChainName.
---

# Runtime Animation Chains

Gum plays back FRB-style `.achx` animations on `Sprite` and `NineSlice`. An `.achx` is an XML file (deserialized as `AnimationChainListSave`) holding one or more named chains; each chain is a list of frames with a texture, source rect, frame length, optional flip flags, and optional `RelativeX`/`RelativeY` per-frame offsets.

## Pipeline: Save → Runtime

`AnimationChainListSave` (XML) → `ToAnimationChainList()` → `AnimationChainList` (a `List<AnimationChain>`). Each `AnimationChainSave.Frames[i]` becomes an `AnimationFrame` via `AnimationFrameSave.ToAnimationFrame(loadTexture, coordinateType)`.

**`TextureCoordinateType` matters at load time, not render time.** The `AnimationFrame.LeftCoordinate/RightCoordinate/TopCoordinate/BottomCoordinate` fields are always stored as UV (0–1). If the source `.achx` declares `<CoordinateType>Pixel</CoordinateType>`, the loader divides by `Texture.Width/Height` during conversion. UV-mode `.achx` files copy the values verbatim. A frame loaded before its texture resolves cannot perform pixel-to-UV conversion — its coords stay zero.

## Playback: AnimationChainLogic

Both `Sprite` and `NineSlice` compose an `AnimationChainLogic` instance (XNA, Sokol, Raylib, Skia all share the same playback type). `SpriteAnimationLogic` is a back-compat `[Obsolete]` subclass; new code should use `AnimationChainLogic`.

State lives entirely on `AnimationChainLogic`:

- `_currentChainIndex` defaults to 0 so assigning `AnimationChains` + `Animate = true` works without setting `CurrentChainName`. `CurrentChainName` setter sets `_currentChainIndex = -1` and resolves the desired name lazily once chains are populated (`RefreshCurrentChainToDesiredName`).
- `AnimateSelf(secondDifference)` advances `_timeIntoAnimation`, loops or clamps based on `IsAnimationChainLooping`, fires `AnimationChainCycled`, picks a new frame via `UpdateFrameBasedOffOfTimeIntoAnimation`, and — only if the frame index changed — calls `UpdateToCurrentAnimationFrame()`.
- `UpdateToCurrentAnimationFrame()` invokes the `ApplyFrame` delegate the host wired up. **It does not directly mutate the renderable.**

`AnimateSelf` is driven once per frame by `GraphicalUiElement.AnimateSelf` (recursively). The whole subsystem is platform-agnostic — there is no MonoGame coupling in `AnimationChainLogic`.

## Frame application is split across two times

Sprite handles each frame in **two phases**:

1. **On frame change** (`Sprite.ApplyAnimationFrame`, wired into `AnimationLogic.ApplyFrame`): copies `Texture`, computes pixel `SourceRectangle` from UV coords × texture size, and copies `FlipHorizontal`/`FlipVertical`. Persistent state.
2. **At every render** (`Sprite.Render`, when `CurrentFrameIndex < CurrentChain.Count`): re-reads `CurrentChain[CurrentFrameIndex].RelativeX/RelativeY`, mutates `this.X/Y`, calls the static draw, then restores the originals.

This split matters: changing `RelativeX/Y` on a live frame takes effect on the next render without needing `UpdateToCurrentAnimationFrame`. But it also means rotation handling for the offset lives at the render call site, not in `ApplyFrame`.

## The RelativeX/Y anchor mismatch (recurring gotcha)

`Sprite.Render` applies the per-frame offset as a literal pixel shift around a **top-left** anchor (`origin = Vector2.Zero` in the static `Render`). The Y component is negated on apply (`this.Y -= offsetVector.Y`) because `AnimationFrame.RelativeY` follows FRB's Y-up convention while Gum is Y-down.

**The values inside an `.achx` are authored against FRB's center-anchored Sprite.** When the Sprite's destination height is constant (e.g. `HeightUnits = Absolute`), the offset is just a translation and the anchor difference doesn't matter. When destination height tracks the source rect (e.g. `HeightUnits = PercentageOfSourceFile`), the FRB-authored values only compensate **half** the per-frame height change — because the other half was absorbed by FRB's centered anchor. The Gum-rendered sprite drifts in the direction the source rect is shrinking (typically "climbs upward" for collapse animations).

To match FRB visuals on a height-tracking Sprite, the offset has to be applied around the sprite's center, not its top-left — or an extra `(referenceHeight - currentHeight) / 2` must be added to Y.

## NineSlice: same playback, no RelativeX/Y

XNA `NineSlice` now composes `AnimationChainLogic` (same pattern as Sprite) — the inline tick loop is gone. Its `ApplyFrame` handler distributes the frame's texture to all 9 internal sprites via `SetSingleTexture`, derives the `SourceRectangle` from the frame's UV coords, and copies `FlipHorizontal`. It does **not** apply `RelativeX/Y`. If `RelativeX/Y` support is ever needed on NineSlice, add the offset application in NineSlice's render path the same way Sprite does.

Skia and Raylib `NineSlice` renderables do not yet expose `AnimationLogic` — animation on those backends is a follow-up tracked under #2753.

## Key Files

| File | Purpose |
|------|---------|
| `RenderingLibrary/Graphics/Animation/AnimationChainLogic.cs` | Platform-agnostic playback state and tick |
| `RenderingLibrary/Graphics/Animation/SpriteAnimationLogic.cs` | `[Obsolete]` back-compat subclass of `AnimationChainLogic` |
| `RenderingLibrary/Graphics/Animation/AnimationFrame.cs` | Runtime frame; `ToAnimationFrame` extension converts from save with `TextureCoordinateType` |
| `RenderingLibrary/Graphics/Animation/AnimationChain.cs` | `List<AnimationFrame>`; `ToAnimationChain` extension |
| `RenderingLibrary/Graphics/Animation/AnimationChainList.cs` | `List<AnimationChain>`; `.achx` deserialization entry point |
| `RenderingLibrary/Graphics/Sprite.cs` | `ApplyAnimationFrame` (frame-change side) and `Render` (per-render `RelativeX/Y` application) |
| `RenderingLibrary/Graphics/NineSlice.cs` | `ApplyAnimationFrame` distributes frame texture across 9 slices; no `RelativeX/Y` support |
| `Gum/Graphics/Animation/Content/AnimationFrameSave.cs` | XML-serializable frame; pixel or UV coords per `AnimationChainListSave.CoordinateType` |
