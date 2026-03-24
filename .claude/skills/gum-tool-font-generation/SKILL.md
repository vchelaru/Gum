---
name: gum-tool-font-generation
description: Reference guide for Gum's bitmap font generation pipeline — how the tool converts font properties into .fnt/.png files via bmfont.exe. Load this when working on BmfcSave, HeadlessFontGenerationService, FontManager, BmfcTemplate.bmfc, font cache naming, texture size estimation, or the GumProjectFontGenerator CLI.
---

# Font Generation Pipeline

Gum generates BMFont-format bitmap fonts (`.fnt` + `.png` atlas) by shelling out to `bmfont.exe`. The pipeline is: **collect font properties → build BmfcSave → write .bmfc file → invoke bmfont.exe → produce .fnt + .png**.

> **Future direction:** The bmfont.exe dependency is being evaluated for replacement due to platform limitations (Windows-only) and other concerns.

## Architecture

```
FontManager (tool facade)
  └─ HeadlessFontGenerationService (core logic, headless)
       ├─ BmfcSave (data model + .bmfc serialization)
       │    └─ BmfcTemplate.bmfc (template file with placeholders)
       └─ bmfont.exe (external process, one per font, run in parallel)
```

**FontManager** is the tool-facing entry point. It wires `IFontGenerationCallbacks` (UI output, spinner, file-watch suppression) and delegates all real work to **HeadlessFontGenerationService** via `IHeadlessFontGenerationService`.

**HeadlessFontGenerationService** is platform-checked (Windows-only, throws `PlatformNotSupportedException` otherwise). It owns:
- Collecting all unique fonts a project needs (`CollectRequiredFonts`)
- Deciding whether a font file already exists or needs (re)generation
- Launching bmfont.exe processes — one per font, all awaited via `Task.WhenAll` for parallelism
- Texture size estimation (heuristic) and optimization (binary search over `AvailableSizes`)

**BmfcSave** holds the six font properties (FontName, FontSize, OutlineThickness, UseSmoothing, IsItalic, IsBold) plus ranges, spacing, and output dimensions. Its `Save()` method loads `BmfcTemplate.bmfc` and does string replacement to produce the `.bmfc` file that bmfont.exe consumes. It also owns `FontCacheFileName` which determines the output path.

## Generation Flow

1. **Property collection**: `TryGetBmfcSaveFor` reads font properties from a `StateSave` (with optional instance prefix and forced overrides) and returns a `BmfcSave` or null.
2. **Deduplication**: `CollectRequiredFonts` iterates all elements/states/instances and deduplicates by `FontCacheFileName` (a deterministic name encoding all font parameters).
3. **Size estimation**: Before generating, `AssignEstimatedNeededSizeOn` either runs a binary-search optimization (`GetOptimizedSizeFor`, controlled by `AutoSizeFontOutputs` project setting) or uses a heuristic lookup table (`EstimateBlocksNeeded`) based on effective font size.
4. **Template expansion**: `BmfcSave.Save()` loads `Content/BmfcTemplate.bmfc`, replaces placeholders, and writes the `.bmfc` file alongside the target `.fnt` path.
5. **bmfont.exe invocation**: `CreateBitmapFontFilesIfNecessaryAsync` launches `bmfont.exe -c "<bmfc>" -o "<fnt>"` via `Process.Start` with `UseShellExecute = true`. The process is awaited async (`WaitForExitAsync`) or synchronously depending on `createTask`.
6. **File-watch suppression**: Before writing, all expected output paths (.bmfc, .fnt, .png pages) are registered with `IFontGenerationCallbacks.OnIgnoreFileChange` so the file watcher doesn't trigger reloads.

## Font Cache Naming

`BmfcSave.FontCacheFileName` produces paths like `FontCache/Font18Arial.fnt`. The name encodes all parameters that affect output:
- Base: `Font{size}{name}` (spaces in font name → underscores)
- Suffixes: `_o{N}` (outline), `_noSmooth`, `_Italic`, `_Bold`

This means two elements with identical font settings share one cached file.

## Channel Behavior (Outline vs No-Outline)

When OutlineThickness is 0: alpha=0, RGB channels=4 (glyph in alpha channel).
When OutlineThickness > 0: alpha=1, RGB channels=0 (outline uses color channels).

## Texture Size Optimization

`GetOptimizedSizeFor` does a binary search over `AvailableSizes` (32x32 up to 8192x8192) to find the smallest texture that keeps the font on a single page. Each probe generates the font to a temp directory and parses the `pages=` line from the resulting `.fnt` file.

The heuristic fallback (`EstimateBlocksNeeded`) uses a lookup table mapping effective font size to a number of 256-pixel blocks, then factors the block count into width x height.

## Character Ranges

- Default: `32-126,160-255` (ASCII + Latin-1 Supplement)
- Project-level `FontRanges` setting overrides for all fonts
- Ranges are validated (no spaces, proper start < end), with automatic fallback to default
- Space character (32) is always included via `EnsureRangesContainSpace`
- Large range sets are split across multiple `chars=` lines (max 10 blocks per line) for bmfont.exe compatibility
- `GenerateRangesFromFile` can derive ranges from a text file's unique characters

## Embedded Resources

`bmfont.exe` and `BmfcTemplate.bmfc` are embedded in the `Gum.ProjectServices` assembly and extracted on first use by `EnsureToolsExtracted`. The template uses placeholder tokens like `FontNameVariable`, `FontSizeVariable`, `{UseSmoothing}`, etc.

## Standalone CLI

`GumProjectFontGenerator` is a standalone console app that loads a `.gumx` project and generates all missing fonts. It uses `HeadlessFontGenerationService` directly with no callbacks.

## Unified Code Path

All font generation now routes through `HeadlessFontGenerationService`. The `CustomSetPropertyOnRenderable` call site uses `IFontManager.CreateFontIfNecessary(BmfcSave)` which delegates to `HeadlessFontGenerationService.CreateFontIfNecessary` (synchronous, `createTask: false`). `CustomSetPropertyOnRenderable.FontService` is a static property assigned externally by `EditorTabPlugin_XNA.StartUp()` — the class has no DI dependency. Game runtimes assign their own `IRuntimeFontService` implementation directly. Legacy `BmfcSave` generation methods and the embedded bmfont.exe in RenderingLibrary have been removed.

## Key Files

| File | Purpose |
|------|---------|
| `Gum/Services/Fonts/IFontManager.cs` | Interface for the tool-facing font manager |
| `Gum/Services/Fonts/FontManager.cs` | Tool facade — delegates to headless service via DI |
| `Tools/Gum.ProjectServices/FontGeneration/HeadlessFontGenerationService.cs` | Core generation logic — collection, size estimation, bmfont.exe invocation |
| `Tools/Gum.ProjectServices/FontGeneration/IHeadlessFontGenerationService.cs` | Interface for the headless service |
| `Tools/Gum.ProjectServices/FontGeneration/IFontGenerationCallbacks.cs` | Callback interface (output, spinner, file-watch ignore) |
| `Gum/Services/Fonts/ToolFontGenerationCallbacks.cs` | Tool-specific callback implementation |
| `RenderingLibrary/Graphics/Fonts/BmfcSave.cs` | Font data model, .bmfc serialization, cache file naming, range utilities |
| `Gum/Content/BmfcTemplate.bmfc` | Template with placeholders for bmfont.exe config |
| `GumProjectFontGenerator/Program.cs` | Standalone CLI for batch font generation |
