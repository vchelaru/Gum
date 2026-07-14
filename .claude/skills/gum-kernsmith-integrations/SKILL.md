---
name: gum-kernsmith-integrations
description: KernSmith runtime bitmap-font packages. Triggers: Integrations/KernSmith, KernSmith.MonoGameGum, KernSmith.RaylibGum, KernSmith.KniGum, KernSmith.FnaGum, KernSmith.GumCommon, InMemoryFontCreator.
---

# KernSmith Integrations

[KernSmith](https://github.com/kaltinril/KernSmith) is a third-party, cross-platform, in-memory BMFont rasterizer. `Integrations/KernSmith/*` are optional first-party Gum packages that bridge it into each runtime — this is unrelated to the tool's `bmfont.exe`-based generation pipeline (see `gum-tool-font-generation`).

## Package map

| Package | Role |
|---|---|
| `KernSmith.GumCommon` | Shared mapping layer: Gum's `BmfcSave` → KernSmith's `FontGeneratorOptions` |
| `KernSmith.MonoGameGum` | MonoGame `IRuntimeFontCreator`-style wiring, produces `BitmapFont` |
| `KernSmith.KniGum` / `KernSmith.FnaGum` | Same shape, KNI/FNA targets |
| `KernSmith.RaylibGum` | `KernSmithRaylibFontCreator`, produces `Raylib_cs.Font` |

Each platform package plugs into `CustomSetPropertyOnRenderable.InMemoryFontCreator` (a game project sets this at startup) so Gum rasterizes fonts on the fly instead of loading pre-generated `.fnt`/`.png` files.

## Gotcha

These are opt-in NuGet add-ons a *game project* references directly — Gum does not wire one up by default. If `InMemoryFontCreator` is never set, Gum falls back to the tool's pre-generated bmfont.exe pipeline. Seeing a `KernSmith.*` package reference alongside `Gum.MonoGame`/etc. in a user's `.csproj` is expected, not a conflicting fork.

See `docs/code/files-and-fonts/font-strategies.md` for the user-facing font strategy comparison.
