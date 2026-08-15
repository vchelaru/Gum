---
name: gum-kernsmith-integrations
description: KernSmith runtime bitmap-font packages. Triggers: Integrations/KernSmith, KernSmith.MonoGameGum, KernSmith.RaylibGum, KernSmith.KniGum, KernSmith.FnaGum, KernSmith.GumCommon, InMemoryFontCreator.
---

# KernSmith Integrations

[KernSmith](https://github.com/kaltinril/KernSmith) is a third-party, cross-platform, in-memory BMFont rasterizer. `Integrations/KernSmith/*` are optional first-party Gum packages that bridge it into each runtime. The tool itself also uses KernSmith directly as an alternate offline generator backend — see `gum-tool-font-generation` for that (`KernSmithFileGenerator`, unrelated to the runtime packages below).

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

## Rasterizer backend gotcha (wasm/AOT)

KernSmith picks a glyph rasterizer via `RasterizerBackend`, each backend its own opt-in NuGet package (`KernSmith.Rasterizers.FreeType`/`Gdi`/`DirectWrite.TerraFX`/`StbTrueType`) so a consumer only pays for the one it needs — see the [KernSmith README](https://github.com/kaltinril/KernSmith). Gum's `KernSmith.GumCommon` references only `FreeType` (upstream's default, and native-only). `StbTrueType` is the sole pure-C# backend and the only one that runs on browser-wasm/AOT, but no Gum `KernSmith.*` package references it — a wasm consumer must add `KernSmith.Rasterizers.StbTrueType` themselves, and separately call `RuntimeHelpers.RunClassConstructor(typeof(StbTrueTypeRasterizer).TypeHandle)` before using it, since KernSmith's reflection-based backend auto-discovery is trimmed away under wasm/AOT publish (silent "backend is not registered" failure otherwise). Any fix that wires this into Gum's own packages must condition both the reference and the call on the browser-wasm RID — adding it unconditionally defeats the per-backend package split's whole purpose.

See `docs/code/files-and-fonts/font-strategies.md` for the user-facing font strategy comparison.
