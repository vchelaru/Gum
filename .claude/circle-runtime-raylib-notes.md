# CircleRuntime Raylib Build — Debug Notes

## Branch
`2635-raylib-circle-runtime`

## Modified File
`MonoGameGum/GueDeriving/CircleRuntime.cs`

---

## Root Cause

`CircleRuntime.cs` is a shared file compiled into multiple platform builds via linked files.
For the Raylib build (`RaylibGum.csproj`, `#define RAYLIB`), the types it references must come from `Gum.Renderables`, not `RenderingLibrary.Math.Geometry`.

The Raylib stub `LineCircle` lives at:
`Runtimes/RaylibGum/Renderables/LineCircle.cs` — namespace `Gum.Renderables`

The real `LineCircle` lives at:
`RenderingLibrary/Math/Geometry/LineCircle.cs` — namespace `RenderingLibrary.Math.Geometry`

The stub only has `CircleOrigin` — it does NOT implement `Color` or `Radius`.

---

## Error History

### Run 1 — original state
```
CircleRuntime.cs(31): error CS0234 — 'Geometry' does not exist in namespace 'RenderingLibrary.Math'
CircleRuntime.cs(33): error CS0234 — same
```
Field and property type `global::RenderingLibrary.Math.Geometry.LineCircle` not valid under RAYLIB.

### Run 2 — after partial fix
```
CircleRuntime.cs(3): error CS0234 — 'Geometry' does not exist in namespace 'RenderingLibrary.Math'
```
A `using RenderingLibrary.Math.Geometry;` was added at the top without a `#if` guard.

### Run 3 — after further fix
```
CircleRuntime.cs(100,101): error CS1061 — 'LineCircle' has no 'Color'
CircleRuntime.cs(109,115): error CS1061 — 'LineCircle' has no 'Radius'
CircleRuntime.cs(133,136): error CS0103 — 'ContainedColoredRectangle' does not exist
CircleRuntime.cs(151):     error CS0234 — 'Geometry' still referenced in constructor
```
Namespace error fixed at top, but property bodies and constructor still using unguarded members.

### Run 4 — current
Same `Color`/`Radius`/`ContainedColoredRectangle` errors, now spread across more lines (field lines shifted).
Constructor still has one unguarded `global::RenderingLibrary.Math.Geometry.LineCircle` reference.

---

## What Still Needs Fixing

1. All `ContainedLineCircle.Color` and `ContainedLineCircle.Radius` accesses must be guarded with `#if !RAYLIB` (or equivalent) since the Raylib stub doesn't implement them.
2. The `Color` property `#else` branch references `ContainedColoredRectangle` which doesn't exist — needs to be corrected.
3. The constructor `new global::RenderingLibrary.Math.Geometry.LineCircle()` call must be guarded.
4. Duplicate `using Gum.Renderables` warning (CS0105) — minor, a redundant using to clean up.

---

## Reference: How RectangleRuntime handles it
`MonoGameGum/GueDeriving/RectangleRuntime.cs` uses `global::RenderingLibrary.Math.Geometry.LineRectangle` **without** platform guards — this works because `RaylibGum` has its own `LineRectangle` stub also in `RenderingLibrary.Math.Geometry` namespace (to be verified).

Raylib renderable stubs are in `Runtimes/RaylibGum/Renderables/` under namespace `Gum.Renderables`.
