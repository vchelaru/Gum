# TTF Font File Support — Status

## Summary

Allow `Font` property on Text to accept a `.ttf` file path (e.g. `"fonts/MyFont.ttf"`) in addition to system font names (e.g. `"Arial"`). Detection is automatic based on the `.ttf` extension. No new variables, enums, or runtime breaking changes.

## What's Done

### Phase A: Code-Only Support — COMPLETE

Users can write `textRuntime.Font = "Content/fonts/Bungee-Regular.ttf"` in game code and it renders correctly. Tested end-to-end with a MonoGame project using KernSmith in-memory font generation.

**Gum changes (branch `2493-fix-unit-test-or-add-diagnostics-to-text-crash`):**

- **`BmfcSave.cs`** — Added `FontFile` field, `IsFontFilePath()` static helper, updated `GetFontCacheFileNameFor()` with optional `fontFilePath` parameter (backward compatible), `_ttf` suffix in cache names to prevent collision with same-named system fonts. Updated `Save()` to write `fontFile=` in .bmfc output.
- **`BmfcTemplate.bmfc`** — Added `fontFile=FontFileVariable` line (existing BMFont .bmfc field, not custom).
- **`KernSmithFileGenerator.cs`** — Branches on `bmfcSave.FontFile`: calls `BmFont.Generate(path, options)` for .ttf files, `BmFont.GenerateFromSystem(name, options)` otherwise.
- **`BmFontExeFileGenerator.cs`** — No changes needed; `BmfcSave.Save()` writes the `fontFile=` line and bmfont.exe handles it natively.
- **`HeadlessFontGenerationService.cs`** — `TryGetBmfcSaveFor()` detects .ttf paths via `IsFontFilePath()`, sets `FontFile` on the `BmfcSave`, derives `FontName` from the .ttf filename.
- **`CustomSetPropertyOnRenderable.cs`** — Updated `UpdateToFontValues`, in-memory font creator path, disk-based font service path, and BBCode `GetAndCreateFontIfNecessary` path to pass `FontFile` through cache key generation and `BmfcSave` construction.
- **10 unit tests** in `Tool/Tests/GumToolUnitTests/Fonts/BmfcSaveTests.cs`.

**KernSmith changes (released in KernSmith 0.12.4):**

- **`GumFontGenerator.cs`** (KernSmith.GumCommon) — Branches on `bmfcSave.FontFile` to call `BmFont.Generate(path, options)` instead of `GenerateFromSystem`.
- **`FlatRedBall.GumCommon`** NuGet bumped to `2026.4.10.1`, KernSmith.GumCommon references it.

### NuGet Packages Published

- `FlatRedBall.GumCommon` 2026.4.10.1
- `KernSmith.GumCommon` / `KernSmith.MonoGameGum` / `KernSmith.KniGum` 0.12.4

## What's Done (continued)

### Phase B: Gum Tool UI — COMPLETE

**Variable grid toggle (B1):**

- **`PropertyGridManager.cs`** — Added `AdjustFontSourceToggle` method in `CustomizeVariables`. Inserts a synthetic "Font Source" row (ComboBox: "System Font" / "From File") before the Font row. When "From File" is selected, the Font row swaps from the system font dropdown to a `FileSelectionDisplay` with `.ttf` filter. Swapping is done via remove+reinsert on the `ObservableCollection` to force container recreation without a full grid rebuild.
- When the current `Font` value is a `.ttf` path and the project uses bmfont.exe, the Font row shows detail text: "bmfont cannot generate from .ttf files. Switch to KernSmith in Project Properties."

**SetVariableLogic (B3):**

- Added `"Font"` to `VariablesRequiringRefresh` with `FullGridRefresh` so changing Font triggers grid rebuild (needed because the displayer type changes between ComboBox and FileSelectionDisplay).
- Added `ReactIfChangedMemberIsFontFile` — when Font is set to a `.ttf` path, handles the copy/reference dialog (same UX as SourceFile for textures) and makes the path relative.

**FontTypeConverter (B2):** No changes needed — only used in system font mode, as designed.

### TTF Drag-Drop — COMPLETE

- **`DragDropManager.cs`** / **`IDragDropManager.cs`** — Added `ValidFontExtensions` property (yields `"ttf"`), updated `IsValidExtensionForFileDrop` to accept both texture and font extensions.
- **`MainEditorTabPlugin.cs`** — `GetBaseTypeForExtension` returns `"Text"` for `.ttf`. `AddNewInstanceForDrop` sets `Font` instead of `SourceFile` for Text instances. `TryHandleFileDropOnInstance` and `TryHandleFileDropOnComponent` detect font files and show "Set font on [name]" / "Add new Text" dialogs. Added `FindInstanceWithFontProperty` to find Text instances under the cursor.

### Font File Path Resolution — COMPLETE

- **`CustomSetPropertyOnRenderable.cs`** — Added `ResolveFontFilePath` helper. All 4 occurrences of `bmfcSave.FontFile = ...` now resolve relative paths to absolute using the project directory, so both KernSmith and bmfont.exe can find the `.ttf` file.
- **`HeadlessFontGenerationService.cs`** — In `GenerateMissingFontsFor`, resolves relative `FontFile` paths to absolute after `CollectRequiredFonts`.
- **`SetVariableLogic.cs`** — `ReactIfChangedMemberIsFontFile` handles absolute paths correctly (checks `FileManager.IsRelative` before prepending project directory).

### KernSmith Rasterizer Registration — COMPLETE

- **`Gum.ProjectServices.csproj`** — Added `KernSmith.Rasterizers.FreeType` NuGet package reference.
- **`KernSmithFileGenerator.cs`** — Explicitly registers the FreeType rasterizer via `RasterizerFactory.Register` on construction (auto-discovery was not working).

### Code Cleanup — COMPLETE

- **`ElementSaveDisplayer.cs`** — Moved 8 `Locator.GetRequiredService` calls from `CreateSrimFromPropertyData` and the variable list loop into the constructor as `private readonly` fields. Updated call site in `PropertyGridManager.InitializeEarly()`.

### Project Properties Refresh — COMPLETE

- **`MainPropertiesWindowPlugin.cs`** — When `FontGenerator` property changes, refreshes the variable grid so the bmfont warning detail text updates.

## What's Left

### Minor Phase A polish (not blocking)

- [ ] **File existence validation** — In `TryGetBmfcSaveFor()`, validate the .ttf file exists before returning the `BmfcSave`; log via callbacks if missing
- [ ] **Path canonicalization** — In `CollectRequiredFonts`, canonicalize .ttf paths before deduplication to avoid `fonts/Foo.ttf` vs `./fonts/Foo.ttf` duplicates
- [ ] **Error messages** — Update `GetMissingWidthMessage` in `CustomSetPropertyOnRenderable.cs` for .ttf paths (cosmetic)

### Known Limitations

- **bmfont.exe cannot use `.ttf` files** — bmfont.exe ignores the `fontFile` field in `.bmfc` configs (at least in command-line mode). Users must switch to KernSmith in Project Properties to use `.ttf` file fonts. A detail text warning is shown in the variable grid when this situation is detected.

## How It Works (Technical)

1. User sets `Font = "fonts/MyFont.ttf"` (code or tool)
2. `BmfcSave.IsFontFilePath()` detects the `.ttf` extension
3. `BmfcSave.FontFile` is set to the path; `FontName` derived from filename
4. Cache key includes `_ttf` suffix: `FontCache/Font18MyFont_ttf.fnt`
5. **KernSmith path**: `GumFontGenerator` calls `BmFont.Generate(fontFilePath, options)` → in-memory `BitmapFont`
6. **BMFont path**: `BmfcSave.Save()` writes `fontFile=<path>` in .bmfc → bmfont.exe uses it natively
7. Font loaded and cached via the existing `LoaderManager` cascade
