# 0023. UI performance direction: sub-millisecond full redraw, drawn straight to the back buffer

- **Status:** Accepted
- **Date:** 2026-11-15
- **Deciders:** Victor Chelaru, Claude

## Context

Gum is about to be announced as the UI framework for BRUTAL, a young library with an active community
and several users already on it. Users ask for sub-millisecond UI rendering.

Numbers so far (Release, RTX 5060 laptop, 1280x720, a busy scene of 718 nodes, 329 text draws and 131
NineSlices), CPU draw time per frame:

- BRUTAL OpenGL: about 2.8 to 3.4 ms
- MonoGame: about 2.9 ms
- KNI Skia: 4.0 to 6.2 ms

An empty scene still costs 0.26 ms per frame in our stack. The benchmark's "frame" column includes swap
and `glFinish`, which are not UI cost. Draw time is the UI number.

## Decision

1. The target is sub-millisecond UI draw with a full redraw every frame. Skipping unchanged frames or
   replaying a retained recording is not the solution.
2. No temporary textures or cached-layer render targets, and no render caching of any kind yet (replayed
   frames, textures, retained layers, recorded pictures such as SKPicture). It is considered only after
   every other option is exhausted, and only if the maintainer says so. The reason for refusing texture
   caching is to avoid introducing render state changes.
3. Gum draws directly to the back buffer, with no intermediary texture.
4. Every layer is open to change: Gum, SkiaGum, the BRUTAL host, SkiaGameRendering and RichTextKit. "That
   is fixed overhead" is not an accepted answer. The 0.26 ms empty-scene draw is ours to fix.
5. Stay broad. Users draw text-heavy UI, lists, icons, effects and animation. Work takes one focus at a
   time, but never a single tunnel.
6. Performance work is directed by the maintainer. No investigation starts without that direction.
   Measure first, in Release builds only, change one thing at a time, and record before and after numbers.
7. Report honest numbers: draw time, not the frame column.
8. The number is the steady-state cost of rendering a full frame, every frame, with nothing skipped.
   Layout is not part of it: layout results are already cached, and layout calls are reduced as a separate
   effort.

## Consequences

- Retained-mode and frame-skipping designs are ruled out as the answer, as are render-target layer caches
  (not allowed yet).
- Fixes may land in SkiaGameRendering and RichTextKit, not only in this repo.
- Caching shaped text blobs is allowed: it is layout work, not rendering.
- Open question, not decided here: batching draws into fewer Skia calls.
- Out of scope for now: ListBox culling, the Width layout short-circuit, shader warm-up (#8053), and more
  benchmark hosts (BRUTAL Vulkan, Stride, raylib, FNA).
