---
name: gum-runtime-animation-chains
description: Reference guide for AnimationChain playback on Sprites and NineSlices — the .achx → AnimationChain pipeline, SpriteAnimationLogic tick loop, the apply-frame vs render-time split, and the RelativeX/Y anchor mismatch with FRB AnimationEditor. Load when working on .achx files, AnimationChain, AnimationFrame, AnimationChainList, SpriteAnimationLogic, ApplyAnimationFrame, RelativeX/Y offsets, CurrentChainName, or animation playback in Sprite / NineSlice.
---

# Runtime Animation Chains

Gum plays back FRB-style `.achx` animations on `Sprite` and `NineSlice`. An `.achx` is an XML file (deserialized as `AnimationChainListSave`) holding one or more named chains; each chain is a list of frames with a texture, source rect, frame length, optional flip flags, and optional `RelativeX`/`RelativeY` per-frame offsets.

## Pipeline: Save → Runtime

`AnimationChainListSave` (XML) → `ToAnimationChainList()` → `AnimationChainList` (a `List<AnimationChain>`). Each `AnimationChainSave.Frames[i]` becomes an `AnimationFrame` via `AnimationFrameSave.ToAnimationFrame(loadTexture, coordinateType)`.

**`TextureCoordinateType` matters at load time, not render time.** The `AnimationFrame.LeftCoordinate/RightCoordinate/TopCoordinate/BottomCoordinate` fields are always stored as UV (0–1). If the source `.achx` declares `<CoordinateType>Pixel</CoordinateType>`, the loader divides by `Texture.Width/Height` during conversion. UV-mode `.achx` files copy the values verbatim. A frame loaded before its texture resolves cannot perform pixel-to-UV conversion — its coords stay zero.

## Playback: SpriteAnimationLogic

`Sprite` composes a `SpriteAnimationLogic` instance. NineSlice does **not** — it has its own near-duplicate implementation inline (see "NineSlice divergence" below).

State lives entirely on `SpriteAnimationLogic`:

- `_currentChainIndex` defaults to 0 so assigning `AnimationChains` + `Animate = true` works without setting `CurrentChainName`. `CurrentChainName` setter sets `_currentChainIndex = -1` and resolves the desired name lazily once chains are populated (`RefreshCurrentChainToDesiredName`).
- `AnimateSelf(secondDifference)` advances `_timeIntoAnimation`, loops or clamps based on `IsAnimationChainLooping`, fires `AnimationChainCycled`, picks a new frame via `UpdateFrameBasedOffOfTimeIntoAnimation`, and — only if the frame index changed — calls `UpdateToCurrentAnimationFrame()`.
- `UpdateToCurrentAnimationFrame()` invokes the `ApplyFrame` delegate the host wired up. **It does not directly mutate the Sprite.**

`AnimateSelf` is driven once per frame by `GraphicalUiElement.AnimateSelf` (recursively). The whole subsystem is platform-agnostic — there is no MonoGame coupling in `SpriteAnimationLogic`.

## Frame application is split across two times

Sprite handles each frame in **two phases**:

1. **On frame change** (`Sprite.ApplyAnimationFrame`, wired into `AnimationLogic.ApplyFrame`): copies `Texture`, computes pixel `SourceRectangle` from UV coords × texture size, and copies `FlipHorizontal`/`FlipVertical`. Persistent state.
2. **At every render** (`Sprite.Render`, when `CurrentFrameIndex < CurrentChain.Count`): re-reads `CurrentChain[CurrentFrameIndex].RelativeX/RelativeY`, mutates `this.X/Y`, calls the static draw, then restores the originals.

This split matters: changing `RelativeX/Y` on a live frame takes effect on the next render without needing `UpdateToCurrentAnimationFrame`. But it also means rotation handling for the offset lives at the render call site, not in `ApplyFrame`.

## The RelativeX/Y anchor mismatch (recurring gotcha)

`Sprite.Render` applies the per-frame offset as a literal pixel shift around a **top-left** anchor (`origin = Vector2.Zero` in the static `Render`). The Y component is negated on apply (`this.Y -= offsetVector.Y`) because `AnimationFrame.RelativeY` follows FRB's Y-up convention while Gum is Y-down.

**The values inside an `.achx` are authored against FRB's center-anchored Sprite.** When the Sprite's destination height is constant (e.g. `HeightUnits = Absolute`), the offset is just a translation and the anchor difference doesn't matter. When destination height tracks the source rect (e.g. `HeightUnits = PercentageOfSourceFile`), the FRB-authored values only compensate **half** the per-frame height change — because the other half was absorbed by FRB's centered anchor. The Gum-rendered sprite drifts in the direction the source rect is shrinking (typically "climbs upward" for collapse animations).

To match FRB visuals on a height-tracking Sprite, the offset has to be applied around the sprite's center, not its top-left — or an extra `(referenceHeight - currentHeight) / 2` must be added to Y.

## NineSlice divergence

`NineSlice` predates `SpriteAnimationLogic` and embeds its own copy of the tick loop and `UpdateToCurrentAnimationFrame`. It does **not** apply `RelativeX/Y` at all — only texture and source rect. If `RelativeX/Y` support is ever needed on NineSlice (or if the duplication is unified onto `SpriteAnimationLogic`), expect to add the offset application in NineSlice's render path the same way Sprite does.

## Key Files

| File | Purpose |
|------|---------|
| `RenderingLibrary/Graphics/Animation/SpriteAnimationLogic.cs` | Platform-agnostic playback state and tick |
| `RenderingLibrary/Graphics/Animation/AnimationFrame.cs` | Runtime frame; `ToAnimationFrame` extension converts from save with `TextureCoordinateType` |
| `RenderingLibrary/Graphics/Animation/AnimationChain.cs` | `List<AnimationFrame>`; `ToAnimationChain` extension |
| `RenderingLibrary/Graphics/Animation/AnimationChainList.cs` | `List<AnimationChain>`; `.achx` deserialization entry point |
| `RenderingLibrary/Graphics/Sprite.cs` | `ApplyAnimationFrame` (frame-change side) and `Render` (per-render `RelativeX/Y` application) |
| `RenderingLibrary/Graphics/NineSlice.cs` | Duplicate inline animation tick; no `RelativeX/Y` support |
| `Gum/Graphics/Animation/Content/AnimationFrameSave.cs` | XML-serializable frame; pixel or UV coords per `AnimationChainListSave.CoordinateType` |
