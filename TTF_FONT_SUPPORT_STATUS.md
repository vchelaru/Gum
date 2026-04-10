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

## What's Left

### Minor Phase A polish (not blocking)

- [ ] **bmfont.exe path verification** — Verify .ttf path resolves correctly for bmfont.exe (absolute vs relative to .bmfc location)
- [ ] **File existence validation** — In `TryGetBmfcSaveFor()`, validate the .ttf file exists before returning the `BmfcSave`; log via callbacks if missing
- [ ] **Path canonicalization** — In `CollectRequiredFonts`, canonicalize .ttf paths before deduplication to avoid `fonts/Foo.ttf` vs `./fonts/Foo.ttf` duplicates
- [ ] **Error messages** — Update `GetMissingWidthMessage` in `CustomSetPropertyOnRenderable.cs` for .ttf paths (cosmetic)

### Phase B: Gum Tool UI — NOT STARTED

Goal: the Gum tool's variable grid provides a clean UI for selecting .ttf files.

**Design:**

The Font property editor displays one of two controls based on the current `Font` value:
- **No extension** (e.g. `"Arial"`): system font dropdown (existing behavior)
- **Has `.ttf` extension** (e.g. `"fonts/MyFont.ttf"`): file picker control

A toggle (radio button or similar) above the control lets the user switch modes:
- **System → File**: clears the value, opens a file dialog filtered to `.ttf`
- **File → System**: reverts to `"Arial"`, shows the dropdown

The toggle is **not persisted** — it is derived from the current `Font` value. Similar to how texture coordinate values show/hide based on texture address mode.

**Steps:**

- [ ] **B1 — Variable Grid Toggle**: Research and implement conditional UI in `DataUiGrid` / `InstanceMember` to swap between dropdown and file picker based on `Font` value
- [ ] **B2 — FontTypeConverter**: No change needed — only used in system font mode
- [ ] **B3 — SetVariableLogic**: Handle mode toggle interaction — intercept toggle, clear/set `Font` value, refresh variable grid

**Key files for Phase B:**

| File | Role |
|------|------|
| `Gum/PropertyGridHelpers/Converters/FontTypeConverter.cs` | System font dropdown |
| `Gum/Plugins/InternalPlugins/VariableGrid/ExclusionsPlugin.cs` | Variable visibility logic |
| `Gum/Plugins/InternalPlugins/VariableGrid/SetVariableLogic.cs` | Font property change reactions |
| `Gum/Managers/StandardElementsManager.cs` | Text standard element variable definitions |

## How It Works (Technical)

1. User sets `Font = "fonts/MyFont.ttf"` (code or tool)
2. `BmfcSave.IsFontFilePath()` detects the `.ttf` extension
3. `BmfcSave.FontFile` is set to the path; `FontName` derived from filename
4. Cache key includes `_ttf` suffix: `FontCache/Font18MyFont_ttf.fnt`
5. **KernSmith path**: `GumFontGenerator` calls `BmFont.Generate(fontFilePath, options)` → in-memory `BitmapFont`
6. **BMFont path**: `BmfcSave.Save()` writes `fontFile=<path>` in .bmfc → bmfont.exe uses it natively
7. Font loaded and cached via the existing `LoaderManager` cascade
