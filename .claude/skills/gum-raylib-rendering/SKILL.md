---
name: gum-raylib-rendering
description: RaylibGum's rendering pipeline and blend-mode handling. Triggers: RaylibRenderer.cs, BatchDrawCallCounter, BlendModeExtensions.cs, Rlgl.SetBlendFactorsSeparate, RenderTexture2D compositing, IsRenderTarget on raylib, translucent-content-looks-darker-in-game-than-in-tool bugs.
---

# RaylibGum's Rendering Pipeline

This skill covers the raylib backend only. MonoGame/KNI/FNA go through a different renderer entirely — see [gum-monogame-rendering](../gum-monogame-rendering/SKILL.md).

## Architecture Seam: BatchDrawCallCounter

`Renderer.Draw` routes every raylib scissor/blend/mode-2D/shader state change through `BatchDrawCallCounter` (`Runtimes/RaylibGum/RenderingLibrary/BatchDrawCallCounter.cs`) rather than calling raylib directly, so draw-call stats stay accurate. Per-element renderables (`Sprite.cs`, `NineSlice.cs`, `Text.cs`) call `BatchDrawCallCounter.BeginBlendMode(Blend.Value)`/`EndBlendMode()` around their own draw **whenever `Blend.HasValue`** — and every Sprite/NineSlice gets `Blend = Blend.Normal` by default (`StandardElementsManager.AddBlendVariable`), so this fires for virtually all content, not just elements with an explicit non-default `Blend`.

## The Render-Target Premultiply Pass

`BeginRenderTargetBlend()`/`_renderTargetBlendActive` (`BatchDrawCallCounter.cs`) make a render-target bake (`Renderer.cs`'s `BakeRenderTarget`) draw premultiplied with correct alpha, Normal and Additive included (`BeginBlendMode` skips the canned modes while the pass is active). `Renderer.IsDrawingToTransparentTarget` runs every layer inside the same pass, for a frame saved as an image (`RaylibScreenshotService`, #7992).

## The Underlying Alpha-Channel Gotcha

Raylib's canned `BlendMode.Alpha` uses the **same** GL factors (`SrcAlpha`/`OneMinusSrcAlpha`) for the color *and* alpha channels. A single translucent draw over an opaque backdrop still computes the correct **color** (color blending doesn't depend on the backdrop's alpha), but the resulting **alpha** comes out short of 255 (`srcA² + dstA*(1-srcA)` instead of the correct `srcA + dstA*(1-srcA)`). That's invisible as long as nothing downstream reads the render target's own alpha again — but any consumer that composites that render target a *second* time (blits it onto another target, reads it back for export) re-multiplies by that leftover alpha and visibly darkens exactly the translucent regions.

A host application that bakes Gum's draw output into its own render target and later composites that render target elsewhere (e.g. blitting it to the window) must not weight that second pass by the render target's own alpha — make it a straight replace-copy, or flatten the alpha first. [Airpig PR #200](https://github.com/profexorgeek/Airpig/pull/200) works through a concrete instance of this. `gumcli screenshot --backend raylib` did hit it over a transparent background (the PNG kept srcA²); it now draws with `IsDrawingToTransparentTarget` and un-premultiplies, pinned by `RaylibScreenshotAlphaTests`.

**No user-facing docs exist for this yet** — MonoGame has an equivalent gotcha documented (`docs/code/rendering/gumbatch.md`'s "RenderTargets" section, with a corrected `BlendState`); raylib has no counterpart. Tracked in [#4205](https://github.com/vchelaru/Gum/issues/4205).
