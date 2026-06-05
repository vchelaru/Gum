# Culling Off-Screen Renderables

## Introduction

When content lives inside a clipping container — such as a `ListBox`, `ScrollViewer`, or any element with `ClipsChildren` set to `true` — items scrolled out of view are still walked and drawn every frame. The clip region only hides them *visually*; each off-screen item still costs a draw call, and can introduce render-state breaks (see [Measuring Draw Calls](lastframedrawstates.md)).

To avoid that wasted work, Gum skips drawing any renderable that falls entirely outside the active clip region, along with all of its children. For a long list this can remove most of the per-frame draw cost, since only the handful of visible items are drawn.

## How It Works

* The cull only happens when a clip region is active. Content that is not inside a `ClipsChildren` container is never culled.
* A renderable whose bounds fall completely outside the clip region is skipped, and Gum does not walk into its children either — so an off-screen list item and everything inside it cost nothing to draw.
* A small margin is added around the clip region before the test, so content that extends slightly past its own bounds (for example a drop shadow or a border) is not clipped early.

Layout and visual state still run for off-screen content, so an item that scrolls back into view is already correct — only the drawing is skipped.

## Disabling the Cull

The cull is on by default. You can turn it off globally:

```csharp
// Initialize
RenderingLibrary.Graphics.Renderer.CullOffscreenWhenClipped = false;
```

Set it back to `true` to re-enable. The setting applies to the whole renderer, so set it once during initialization.

## When to Disable It

The cull decides what to skip from each renderable's *own* bounds. That is correct for self-contained content, but a renderable can intentionally draw outside its own bounds, for example:

* A child positioned outside a non-clipping parent's bounds (deliberate overflow).
* A large glow or shadow that extends well beyond the margin.

In those cases an item sitting just past the edge of a scrolled region could be culled even though part of it should still be visible. If you see content disappear at the edge of a clipped area that should be on screen, either set `CullOffscreenWhenClipped` to `false`, or give the overflowing content a parent that clips it so its footprint stays within its bounds.

{% hint style="info" %}
**As of June 2026:** Off-screen culling is on by default but is still considered experimental while it is validated across real projects. If it causes incorrect clipping in your project, disable it with `Renderer.CullOffscreenWhenClipped = false` and please report the case.
{% endhint %}

Use [Measuring Draw Calls](lastframedrawstates.md) to confirm the reduction in draw states with the cull on versus off.
