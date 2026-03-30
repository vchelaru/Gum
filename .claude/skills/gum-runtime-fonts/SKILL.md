---
name: gum-runtime-fonts
description: Reference guide for Gum's runtime font loading system in MonoGame/KNI — the three font loading paths (custom font, font-property cache lookup, in-memory generation), the lookup cascade, FontCache naming, and common gotchas. Load when working on TextRuntime font properties, BitmapFont loading, CustomSetPropertyOnRenderable.UpdateToFontValues, IInMemoryFontCreator, or font-related documentation.
---

# Runtime Font Loading

Gum renders text using **BitmapFont** — a `.fnt` descriptor file plus one or more `.png` texture atlases. There are three ways to get a BitmapFont onto a TextRuntime, each with different tradeoffs.

## Three Font Loading Paths

### Path 1: Custom Font File (UseCustomFont = true)

User provides a pre-built `.fnt` file directly:
```
textRuntime.UseCustomFont = true;
textRuntime.CustomFontFile = "fonts/MyFont.fnt";
```

- File path resolves relative to `FileManager.RelativeDirectory` (typically `Content/`)
- Loaded via `new BitmapFont(path)`, cached in `LoaderManager`
- If the file doesn't exist, load silently skips — element gets `DefaultBitmapFont`
- No property-to-filename mapping; user controls the exact file

### Path 2: Font Property Cache Lookup (UseCustomFont = false, the default)

Six properties combine into a deterministic filename in `FontCache/`:

| Property | Default | Effect on filename |
|----------|---------|-------------------|
| Font / FontFamily | "Arial" | Base name (spaces → underscores) |
| FontSize | 18 | Base size number |
| OutlineThickness | 0 | `_o{N}` suffix if non-zero |
| UseFontSmoothing | true | `_noSmooth` suffix if false |
| IsItalic | false | `_Italic` suffix if true |
| IsBold | false | `_Bold` suffix if true |

**Naming formula:** `FontCache/Font{size}{name}[_o{N}][_noSmooth][_Italic][_Bold].fnt`

Examples: `FontCache/Font18Arial.fnt`, `FontCache/Font24Times_New_Roman_o1_Bold.fnt`

`BmfcSave.GetFontCacheFileNameFor()` produces this name. Every property setter on TextRuntime (Font, FontSize, etc.) calls `UpdateToFontValues()`, which regenerates the filename and attempts to load.

**Key gotcha:** Unless an `IInMemoryFontCreator` or `IRuntimeFontService` is registered, the `.fnt` file must already exist in `FontCache/`. Users often set `FontSize = 24` expecting it to work, but silently get `DefaultBitmapFont` because `Font24Arial.fnt` was never generated. There is no error or warning — the text just renders in the default font.

### Path 3: In-Memory Font Creation (IInMemoryFontCreator) — New

Generates a `BitmapFont` entirely in memory at runtime — no pre-built `.fnt` files needed. The loading code already checks for this; it slots into the cascade between embedded resources and disk-based generation.

When registered on `CustomSetPropertyOnRenderable.InMemoryFontCreator`, font-property changes (Path 2) automatically create fonts on demand. This eliminates the FontCache pre-population requirement.

## Lookup Cascade

When `UseCustomFont = false` and a font property changes, `UpdateToFontValues` tries these sources in order:

1. **LoaderManager cache** — already-loaded BitmapFont by full path
2. **Embedded resource** — MonoGameGum ships `Font18Arial` (plus Bold/Italic/Bold_Italic variants) as embedded resources; these are the default fonts
3. **IInMemoryFontCreator** — generates BitmapFont in memory, no disk I/O
4. **IRuntimeFontService** — generates `.fnt`/`.png` files on disk, then falls through to step 5 (typically tool-only, not used in game code)
5. **Disk load** — `new BitmapFont(fullPath)` if the file exists
6. **DefaultBitmapFont fallback** — `Text.DefaultBitmapFont` (Font18Arial, set during `SystemManagers` initialization)

The result is cached in `LoaderManager` so subsequent lookups for the same font properties hit step 1.

## Wiring

`SystemManagers` initialization (called by `GumService.Initialize`) sets up the font system:
- Loads embedded Font18Arial as `Text.DefaultBitmapFont`
- Wires `GraphicalUiElement.UpdateFontFromProperties` → `CustomSetPropertyOnRenderable.UpdateToFontValues`

Game code can optionally set `CustomSetPropertyOnRenderable.InMemoryFontCreator` to enable Path 3.

## Key Files

| File | Purpose |
|------|---------|
| `MonoGameGum/GueDeriving/TextRuntime.cs` | User-facing font properties; each setter calls `UpdateToFontValues()` |
| `RenderingLibrary/Graphics/Text.cs` | Renderable; holds `BitmapFont` instance and static `DefaultBitmapFont` |
| `RenderingLibrary/Graphics/Fonts/BitmapFont.cs` | Loads `.fnt` + `.png` textures; stores character metrics |
| `RenderingLibrary/Graphics/Fonts/BmfcSave.cs` | `GetFontCacheFileNameFor()` — deterministic cache filename from properties |
| `Gum/Wireframe/CustomSetPropertyOnRenderable.cs` | `UpdateToFontValues()` — orchestrates the lookup cascade |
| `RenderingLibrary/Graphics/Fonts/IInMemoryFontCreator.cs` | Interface for runtime font generation without disk I/O |
| `RenderingLibrary/Graphics/Fonts/IRuntimeFontService.cs` | Interface for disk-based font generation (typically tool-only) |
| `RenderingLibrary/SystemManagers.cs` | Wires font delegates and loads default embedded font |
