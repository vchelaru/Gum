---
name: skia-game-rendering
description: SkiaGameRendering (SGR), the sibling repo that gives a Skia host a GPU-only canvas per engine. Triggers: SkiaRenderTarget2D, SkiaDirectCanvas, adding or porting a Skia host, CPU fallback, Godot, Stride, Unity or KNI Skia host, ../SkiaGameRendering.
---

# SkiaGameRendering

SGR is a separate repo at `../SkiaGameRendering` (`vchelaru/SkiaGameRendering`, default branch `master`). It draws SkiaSharp into engine textures on the GPU. Gum's Skia hosts supply input and API shape; SGR supplies the canvas. Before designing or reviewing any Skia host, read SGR's `CLAUDE.md`, the platform table in its `README.md`, and `docs/architecture.md`. The strategy, host ledger and new-host checklist are in `Direction/skia-hosts.md`. Host plumbing on the Gum side is in [gum-runtime-topology](../gum-runtime-topology/SKILL.md) ("Adding a Skia-based host").

## Hard rule: GPU only

SGR never uses CPU rasterization or CPU readback, for any backend or failure. When a backend can't run somewhere, the options are to fix it in SGR, narrow the supported range, or hold the release. A Gum host never adds a CPU `SKSurface` plus upload path for an engine. No Gum Skia host has a CPU path; an unsupported graphics API throws `NotSupportedException` naming it (`Runtimes/StrideGum/Gpu/StrideGpuBackend.cs`, `GumRenderer` in the Unity package).

## What it offers

Two draw types per engine: `SkiaRenderTarget2D` (`Begin()`, `Canvas`, `End()`, fixed size, so a resize needs a new one) and `SkiaDirectCanvas` (draws into the engine's current target). Not every engine has both; SGR's `TODO.md` "Direct Canvas Coverage" is the matrix. The README table lists engines and packages:

- MonoGame, KNI, FNA, raylib: OpenGL, D3D11 and WebGL; MonoGame also D3D12.
- Stride: D3D11, D3D12 and Vulkan, each its own package (`SkiaGameRendering.Stride.D3D11`, `.D3D12`, `.VK`).
- Godot 4.7+: one source-only package that picks Vulkan, D3D12, Metal or GL at run time. Render target only, no direct canvas.
- Unity 6: UPM package over D3D11, D3D12, Vulkan, OpenGL, Metal and GLES.
- Not covered: Silk.NET (no adapter; `Samples/SilkNetGum` builds its own GL `GRContext`) and Unity WebGL.

## Landmines

- A new engine host starts by finding its row in SGR's table. If the row is missing, the work is an SGR adapter first, not a Gum fallback.
- SkiaSharp is one version across SGR and every Skia-family Gum project (`eng/Versions.props` there, `Runtimes/SkiaPackages.props` here). `SkiaGameRendering.Stride.D3D11` pins it exactly. Bump order is in [gum-cross-platform-unification](../gum-cross-platform-unification/SKILL.md).
- The three Stride SGR packages declare identical type names, so `Runtimes/StrideGum` separates them with extern aliases and one wrapper per package (`Gpu/`). D3D11 and D3D12 exist only in `net10.0-windows7.0`; plain `net10.0` gets Vulkan only.
- SGR's `Begin()` clears the canvas each frame on D3D11, D3D12 and Vulkan, and the canvas is readable only between `Begin` and `End`.
- Check what SGR supports now rather than trusting a Gum csproj or comment about it; the Stride and Unity hosts' comments about which backends have a GPU path can lag SGR.
