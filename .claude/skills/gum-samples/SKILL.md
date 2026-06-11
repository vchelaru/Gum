---
name: gum-samples
description: Where runtime-feature demo screens go. Triggers: adding a sample/demo page for a new runtime feature, NineSliceScreen/SpriteScreen, MonoGameGumInCode, Samples/raylib, SilkNetGum, MonoGameGumShapesGallery.
---

# Gum Sample Projects

When a runtime feature needs a visual demo (so a human can eyeball it across backends), add a screen to the **three cross-backend feature samples**, keeping them aligned. Shape features are the exception — they go to the shapes gallery instead.

## The three feature samples (add here by default)

| Sample | Backend | Path | Screen base / registration |
|---|---|---|---|
| MonoGameGumInCode | MonoGame (XNA) | `Samples/MonoGameGumInCode/MonoGameGumInCode/` | `FrameworkElement`; nav button in `Game1.BuildNavStrip` |
| raylib gallery | raylib | `Samples/raylib/` | `FrameworkElement`; nav button in `Program.BuildNavStrip` (`Examples.Shapes` namespace) |
| SilkNetGum | SkiaSharp via Silk.NET | `Samples/SilkNetGum/SilkNetGum/` | `GraphicalUiElement`; factory in `Program.codeScreenFactories` |

Each has a `Screens/` folder with one `*Screen.cs` per feature (`SpriteScreen`, `NineSliceScreen`, …). **MonoGameGumInCode is the reference** — mirror its screen section-for-section in the other two so the same screen can be opened side by side and any per-backend rendering difference stands out as a backend bug. The existing screen headers say exactly this ("Raylib mirror of …", "Mirror of MonoGameGumInCode.Screens…").

### Cross-backend gotchas when mirroring

- **Texture paths differ.** MonoGame uses MGCB `Content/` names (`"Frame.png"`); raylib loads from disk (`"resources\\Frame.png"`, copied via the `resources\**\*.*` glob in `GumTest.csproj`); SilkNetGum loads from `Content/GumProject/`. Drop any new texture into each sample's content dir.
- **Color types differ.** MonoGame `Microsoft.Xna.Framework.Color`, raylib `Raylib_cs.Color`, Skia `SKColor`. Pick the nearest named color per backend.
- **Not every property exists on every backend.** A runtime property may be `#if`-gated away from a backend (see [[gum-cross-platform-unification]]). If the mirrored screen won't compile, the feature isn't wired on that backend yet — that's the actual work, not the screen.
- **raylib screens need `using Gum.Forms.Controls;`** for `FrameworkElement`.

## Shape features → the shapes gallery instead

If the feature touches **shapes** (Circle, Rectangle, RoundedRectangle, Arc, Polygon, Line — anything from [[gum-shapes-xnb-packaging]] / the shape runtimes), do **not** add it to the three samples above. Add it to `Samples/GumShapesGallery/MonoGameGumShapesGallery/` (and its KNI siblings under `Samples/GumShapesGallery/`).

## Verification is human-driven

These samples exist for visual confirmation; they have no automated assertions. After adding a screen, build the sample and tell the user to run it and eyeball the new screen. Behavioral correctness still gets a unit test in the matching `Tests/*` project (see [[tdd]]) — the sample is the visual complement, not a replacement.
