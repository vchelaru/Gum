---
name: gum-property-assignment
description: Reference guide for how Gum applies variables and sets properties on renderables. Load this when working on ApplyState, SetProperty, SetVariablesRecursively, CustomSetPropertyOnRenderable, font loading, IsAllLayoutSuspended, or isFontDirty.
---

# Gum Property Assignment Reference

## Two Paths for Setting Properties on a GUE

**Direct property setters** (e.g. `textRuntime.Font = "Arial"`) — these are typed C# properties
on `TextRuntime` or `GraphicalUiElement` that immediately call helpers like `UpdateToFontValues()`.

**String-based path** (e.g. `SetProperty("Font", "Arial")`) — used by `ApplyState` and
`SetVariablesRecursively` when processing state variables. This path goes through:

```
ApplyState / SetVariablesRecursively
  → GraphicalUiElement.SetProperty(string, object)
  → CustomSetPropertyOnRenderable.SetPropertyOnRenderable(...)
      → TrySetPropertyOnText / TrySetPropertyOnSprite / etc.
          → modifies the underlying renderable directly
```

`CustomSetPropertyOnRenderable` is the bridge between the string-based variable system and the
actual renderable objects. It lives in `Gum/Wireframe/CustomSetPropertyOnRenderable.cs` (with
parallel copies in `Runtimes/SkiaGum/` and `Runtimes/RaylibGum/`).

The delegate `GraphicalUiElement.UpdateFontFromProperties` is wired to the static
`CustomSetPropertyOnRenderable.UpdateToFontValues(IText, GUE)` at startup. This is how the
string path and the instance method path both ultimately call the same font loading logic.

---

## Font Deferred-Loading System

Loading a `.fnt` file is expensive. A text element has ~6 font-related properties (Font,
FontSize, IsBold, IsItalic, UseFontSmoothing, OutlineThickness), so without deferral each
property assignment during a screen load triggers a separate disk read.

### The Flag: `isFontDirty`

`GraphicalUiElement.isFontDirty` is set to `true` instead of loading the font when layout is
suspended. It is consumed (font loaded, flag cleared) by `UpdateFontRecursive()`.

### Where deferral happens

| Path | Defers for `IsAllLayoutSuspended`? | Defers for `IsLayoutSuspended`? |
|------|------------------------------------|---------------------------------|
| `GUE.UpdateToFontValues()` (direct setters) | Yes | Yes |
| `CustomSetPropertyOnRenderable.UpdateToFontValues` (string path) | Yes | **No** |

The string path deliberately does **not** defer for instance-level `IsLayoutSuspended`. Doing so
would cause cascading parent layout calls when `UpdateFontRecursive` later assigns the `BitmapFont`
to a `Text` with `RelativeToChildren` dimensions inside `ResumeLayoutUpdateIfDirtyRecursive`. See
the long comment at the top of that static method for the full explanation.

### Where fonts actually load

Two code paths consume `isFontDirty`:

1. **`WireframeObjectManager`** (Gum tool screen load): after `IsAllLayoutSuspended = false`,
   calls `RootGue.UpdateFontRecursive()` then `RootGue.UpdateLayout()`. At this point all
   elements have `mIsLayoutSuspended = false` (because `ApplyState` skips `SuspendLayout` when
   the global flag is set), so every dirty text element loads its font in one pass.

2. **`ResumeLayoutUpdateIfDirtyRecursive`** (instance-level suspension): sets
   `mIsLayoutSuspended = false` on the current element *before* calling `UpdateFontRecursive()`,
   then recurses to children. This ordering is critical — if `mIsLayoutSuspended` were still true
   when `UpdateFontRecursive` runs, that element's font load would be skipped.

### Known gap

When fonts are set via the string path (`SetProperty`) during an `ApplyState` that uses
instance-level `SuspendLayout` (not `IsAllLayoutSuspended`), fonts still load immediately —
one disk read per property assignment. This is the `MonoGame ApplyState` path; fixing it
requires solving the cascading layout problem described in `CustomSetPropertyOnRenderable`.

---

## Key Files

| File | Role |
|------|------|
| `GumRuntime/GraphicalUiElement.cs` | `SetProperty`, `ApplyState`, `UpdateToFontValues` (instance), `UpdateFontRecursive`, `isFontDirty` |
| `Gum/Wireframe/CustomSetPropertyOnRenderable.cs` | String-path dispatch + static `UpdateToFontValues(IText, GUE)` |
| `GumRuntime/ElementSaveExtensions.cs` | `SetVariablesRecursively` — iterates state variables and calls `ApplyState` |
| `Gum/Wireframe/WireframeObjectManager.cs` | Sets `IsAllLayoutSuspended` around screen load, calls `UpdateFontRecursive` after |
