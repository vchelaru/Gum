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
2. Temporary textures and cached-layer render targets are not the performance fix. They add overhead and
   hurt most when content changes. They are a last resort, used only if the maintainer says so.
3. Gum draws directly to the back buffer, with no intermediary texture.
4. Every layer is open to change: Gum, SkiaGum, the BRUTAL host, SkiaGameRendering and RichTextKit. "That
   is fixed overhead" is not an accepted answer. The 0.26 ms empty-scene draw is ours to fix.
5. Stay broad. Users draw text-heavy UI, lists, icons, effects and animation. Work takes one focus at a
   time, but never a single tunnel.
6. Performance work is directed by the maintainer. No investigation starts without that direction.
   Measure first, in Release builds only, change one thing at a time, and record before and after numbers.
7. Report honest numbers: draw time, not the frame column.

## Consequences

- Retained-mode and frame-skipping designs are ruled out as the answer, as are render-target layer caches
  (last resort only).
- Fixes may land in SkiaGameRendering and RichTextKit, not only in this repo.
- Open questions, not decided here: batching, and caching text blobs.
- Out of scope for now: ListBox culling, the Width layout short-circuit, shader warm-up (#8053), and more
  benchmark hosts (BRUTAL Vulkan, Stride, raylib, FNA).
