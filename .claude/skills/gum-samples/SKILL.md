---
name: gum-samples
description: Where runtime-feature demo screens go. Triggers: adding a sample/demo page for a new runtime feature, NineSliceScreen/SpriteScreen, MonoGameGumInCode, Samples/raylib, SilkNetGum, MonoGameGumShapesGallery.
---

# Gum Sample Projects

When a runtime feature needs a visual demo (so a human can eyeball it across backends), add a screen to the **three cross-backend feature samples**, keeping them aligned. Shape features are the exception — they go to the shapes gallery instead.

The "**big three**" refers to the three backends below (Silk.NET/Skia, raylib, MonoGame). Note that the **XNA-likes (MonoGame, KNI) are split across two sample projects** — the feature sample *plus* the shapes gallery — for the reason explained under "Shape features" below. So "big three" counts backends, not project folders.

## The three feature samples (add here by default)

| Sample | Backend | Path | Screen base / registration |
|---|---|---|---|
| MonoGameGumInCode | MonoGame (XNA) | `Samples/MonoGameGumInCode/MonoGameGumInCode/` | `FrameworkElement`; nav button in `Game1.BuildNavStrip` |
| raylib gallery | raylib | `Samples/raylib/` | `FrameworkElement`; nav button in `Program.BuildNavStrip` (`Examples.Shapes` namespace) |
| SilkNetGum | SkiaSharp via Silk.NET | `Samples/SilkNetGum/SilkNetGum/` | `FrameworkElement`; factory in `Program.codeScreenFactories`, keyboard nav |

Each has a `Screens/` folder with one `*Screen.cs` per feature (`SpriteScreen`, `NineSliceScreen`, …). **MonoGameGumInCode is the reference** — mirror its screen section-for-section in the other two so the same screen can be opened side by side and any per-backend rendering difference stands out as a backend bug. The existing screen headers say exactly this ("Raylib mirror of …", "Mirror of MonoGameGumInCode.Screens…").

**Mirror even when the underlying bug is backend-specific.** A fix that only reproduces on one backend (e.g. a raylib-only render-target/framebuffer quirk) is still a reason to add the demo *cell* to every mirrored screen, not just the one you're fixing — the point of mirroring is a side-by-side visual comparison, and skipping the other backends breaks that even though their code has nothing to fix. This applies to adding a new cell to an existing gallery screen (e.g. one more case in `RenderTargetScreen`), not just to creating a brand-new screen file. (Missed on issue #3464: only the raylib cell was added at first, because the fix itself was raylib-only — caught in review.)

**Mirroring means the same relative *position*, not just presence.** With `TopToBottomStack` layout, visual order is call order, so a new section must land at the same point in the sequence in every mirror, not just be appended wherever happens to compile. Landing it after a block of code that only exists in one backend shifts every later row out of alignment in that file relative to the others, breaking the side-by-side comparison even though every screen still "has" the section. Verify by reading all mirrored files top to bottom, not just diffing for the cell's presence. (Missed on issue #3496: MonoGame's `TextScreen.cs` carried a legacy `AddCustomOutlineText` block raylib never had, so a later section landed three rows lower in MonoGame than in raylib.)

### Cross-backend gotchas when mirroring

- **Texture paths differ.** MonoGame uses MGCB `Content/` names (`"Frame.png"`); raylib loads from disk (`"resources\\Frame.png"`, copied via the `resources\**\*.*` glob in `GumTest.csproj`); SilkNetGum loads from `Content/GumProject/`. Drop any new texture into each sample's content dir.
- **Color types differ.** MonoGame `Microsoft.Xna.Framework.Color`, raylib `Raylib_cs.Color`, Skia `SKColor`. Pick the nearest named color per backend.
- **Not every property exists on every backend.** A runtime property may be `#if`-gated away from a backend (see [[gum-cross-platform-unification]]). If the mirrored screen won't compile, the feature isn't wired on that backend yet — that's the actual work, not the screen.
- **raylib and SilkNetGum screens need `using Gum.Forms.Controls;`** for `FrameworkElement`.

## Shape features → the shapes gallery instead

If the feature touches **shapes** (Circle, Rectangle, RoundedRectangle, Arc, Polygon, Line — anything from [[gum-shapes-xnb-packaging]] / the shape runtimes), only the XNA-like slot changes: still mirror to the raylib and SilkNet feature samples (they render shapes natively, so their shape screens live *in* the feature samples — `Samples/raylib/Screens/CirclesScreen.cs`, `Samples/SilkNetGum/…`), but for MonoGame/KNI add to `Samples/GumShapesGallery/MonoGameGumShapesGallery/` (and its KNI siblings) **instead of** `MonoGameGumInCode`. So a shape cell still lands in three galleries — raylib + SilkNet + GumShapesGallery — just not `MonoGameGumInCode`.

**Why shapes get their own project:** raylib and Skia render shapes natively (built-in, full support), so shape demos for those backends live in the regular feature samples. XNA-likes (MonoGame, KNI) have **no built-in shape rendering** — they must be supplemented with the separate **Gum.Shapes** library, so their shape coverage lives in the dedicated `GumShapesGallery` project rather than `MonoGameGumInCode`. That's the split referenced above. This is expected to be unified eventually.

## Verification is human-driven

These samples exist for visual confirmation; they have no automated assertions. After adding a screen, build the sample and tell the user to run it and eyeball the new screen. Behavioral correctness still gets a unit test in the matching `Tests/*` project (see [[tdd]]) — the sample is the visual complement, not a replacement.
