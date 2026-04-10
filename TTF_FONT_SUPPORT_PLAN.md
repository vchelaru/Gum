# TTF Font File Support Plan

Support specifying a `.ttf` file path in the `Font` property instead of requiring a system-installed font name. Both BMFont and KernSmith have the underlying capability.

## Core Design

The existing `Font` property on Text accepts either a system font family name (`"Arial"`) or a `.ttf` file path (`"fonts/MyFont.ttf"`). Detection is automatic: if the value ends with `.ttf` (case-insensitive), treat it as a file path. A static helper `BmfcSave.IsFontFilePath()` is the single branch point used everywhere.

No new persisted variables, no new enums, no runtime breaking changes.

## Prerequisite: KernSmith .ttf Loading

**Status:** Confirmed available.

KernSmith has the following public APIs for .ttf file support:

- `BmFont.Generate(string fontPath, FontGeneratorOptions options)` — takes a .ttf file path directly
- `BmFont.Generate(byte[] fontData, FontGeneratorOptions options)` — takes raw font bytes
- `BmFont.RegisterFont(string familyName, byte[] fontData, ...)` — registers font data under a family name so `GenerateFromSystem` can resolve it

The filepath-first approach (`BmFont.Generate(path, options)`) is preferred because:
1. It works symmetrically with BMFont's `fontFile=` .bmfc field
2. No need to invent a family name for registered fonts
3. Both generators use the same `BmfcSave.FontFile` field without generator-specific logic

The `RegisterFont` API remains useful for game developers who want to provide fonts from embedded resources or downloaded bytes, but that's orthogonal to this feature.

**Gum's `KernSmith` package version has been bumped** from 0.9.5 to match the current release which includes these APIs.

### KernSmith-Side Changes Required

The following files in the **KernSmith repo** (`C:\Users\vchel\Documents\GitHub\KernSmith`) need updates:

**`integrations/KernSmith.GumCommon/GumFontGenerator.cs`:**

Currently `Generate()` always calls `BmFont.GenerateFromSystem(bmfcSave.FontName, options)`. Needs a branch:

```
if FontFile is set:
    BmFont.Generate(bmfcSave.FontFile, options)   // file path
else:
    BmFont.GenerateFromSystem(bmfcSave.FontName, options)  // system font
```

This affects all consumers of `GumFontGenerator`: `KernSmithFontCreator` (MonoGameGum), KNI equivalent, and any other platform integrations. The change is in the shared `GumCommon` layer so it flows to all platforms automatically.

**`integrations/KernSmith.MonoGameGum/KernSmithFontCreator.cs`:**

No direct changes needed — it calls `GumFontGenerator.Generate(bmfcSave)` which handles the branching. However, the existing `RegisterFont` methods on this class remain available as a separate (manual) way to provide fonts.

## Phase A: Code-Only Support (Runtime + Generation Pipeline)

Goal: a user can write `textRuntime.Font = "fonts/MyFont.ttf"` in game code and it works.

### A1 — BmfcSave Data Model — DONE

**Files:** `RenderingLibrary/Graphics/Fonts/BmfcSave.cs`, `Gum/Content/BmfcTemplate.bmfc`

- [x] Added `public string? FontFile = null;` field to `BmfcSave`
- [x] Added `public static bool IsFontFilePath(string? fontValue)` helper (checks `.ttf` extension, case-insensitive)
- [x] Updated `GetFontCacheFileNameFor()` with optional `string? fontFilePath = null` parameter. When set, uses the .ttf filename (without extension, sanitized) plus a `_ttf` suffix to prevent collision with same-named system fonts. Example: system "MyFont" -> `Font18MyFont.fnt`, file `MyFont.ttf` -> `Font18MyFont_ttf.fnt`
- [x] Updated `FontCacheFileName` property to pass `FontFile` through
- [x] Updated `Save()` to write `fontFile=` line in the .bmfc output
- [x] Added `fontFile=FontFileVariable` to `BmfcTemplate.bmfc`
- [x] Added 10 unit tests in `Tool/Tests/GumToolUnitTests/Fonts/BmfcSaveTests.cs`

### A2 — KernSmith Generator (Gum.ProjectServices side) — DONE

**File:** `Tools/Gum.ProjectServices/FontGeneration/KernSmithFileGenerator.cs`

- [x] In `GenerateFontCore`, branches on `bmfcSave.FontFile`: calls `BmFont.Generate(path, options)` when set, `BmFont.GenerateFromSystem(name, options)` otherwise

### A3 — BMFont Generator — DONE (no changes needed)

**File:** `Tools/Gum.ProjectServices/FontGeneration/BmFontExeFileGenerator.cs`

- [x] No code changes needed — `BmfcSave.Save()` handles writing `fontFile=` in the .bmfc, and bmfont.exe consumes it natively
- [ ] Still needs verification that .ttf path resolves correctly for bmfont.exe (absolute vs relative)

### A4 — Font Collection (Headless Service) — DONE

**File:** `Tools/Gum.ProjectServices/FontGeneration/HeadlessFontGenerationService.cs`

- [x] In `TryGetBmfcSaveFor()`: when the `Font` value is a .ttf path (via `IsFontFilePath`), sets `bmfcSave.FontFile` to the value and derives `bmfcSave.FontName` from the filename
- [ ] Validate the .ttf file exists; if not, log via callbacks and return null (deferred — not critical for initial testing)
- [ ] Canonicalize .ttf paths before deduplication in `CollectRequiredFonts` (deferred — not critical for initial testing)

### A5 — Runtime Font Loading — DONE

**File:** `Gum/Wireframe/CustomSetPropertyOnRenderable.cs`

- [x] In `UpdateToFontValues` non-custom path: detects .ttf via `BmfcSave.IsFontFilePath()`, passes `fontFilePath` to `GetFontCacheFileNameFor`
- [x] When building `BmfcSave` for `InMemoryFontCreator` and `FontService`, sets `bmfcSave.FontFile` and derives `FontName` from the .ttf filename
- [x] BBCode path (`GetAndCreateFontIfNecessary`): same pattern applied to both in-memory and disk-based BmfcSave construction, plus `GetFontFileName` cache key generation
- [ ] Update error messages in `GetMissingWidthMessage` for .ttf paths (deferred — cosmetic)

### A6 — KernSmith GumCommon (KernSmith repo) — DONE (by Victor)

**File:** `integrations/KernSmith.GumCommon/GumFontGenerator.cs` (in KernSmith repo)

- [x] In `Generate()`, branches on `bmfcSave.FontFile`
- This change flows to all platform integrations (MonoGameGum, KniGum) automatically since they call `GumFontGenerator.Generate()`

**Blocker:** KernSmith.GumCommon references `BmfcSave` via the `FlatRedBall.GumCommon` NuGet package, which hasn't been published with the `FontFile` field yet. Need to publish `FlatRedBall.GumCommon` NuGet first, or temporarily switch to a local project reference for testing.

### Validation

Unit test or small MonoGame sample that sets `Font` to a `.ttf` path and confirms the `.fnt` gets generated and loaded correctly.

## Phase B: Gum Tool UI

Goal: the Gum tool's variable grid provides a clean UI for selecting .ttf files.

### Design

The Font property editor displays one of two controls based on the current value:

- **No extension** (e.g. `"Arial"`): system font dropdown (existing `FontTypeConverter` behavior)
- **Has `.ttf` extension** (e.g. `"fonts/MyFont.ttf"`): file picker control

A toggle (radio button or similar) above the control lets the user switch between modes:

- **System -> File**: clears the value and opens a file dialog filtered to `.ttf`
- **File -> System**: reverts to `"Arial"` and shows the dropdown

The toggle state is not persisted — it is derived from the current `Font` value. This is similar to how texture coordinate values show/hide based on the texture address mode selection.

### B1 — Variable Grid Toggle

Research and implement the conditional UI in `DataUiGrid` / `InstanceMember` to swap between dropdown and file picker based on the `Font` value.

### B2 — FontTypeConverter

**File:** `Gum/PropertyGridHelpers/Converters/FontTypeConverter.cs`

No change needed — only used when in system font mode.

### B3 — SetVariableLogic

**File:** `Gum/Plugins/InternalPlugins/VariableGrid/SetVariableLogic.cs`

Handle the mode toggle interaction: intercept the toggle, clear/set the `Font` value appropriately, refresh the variable grid to show the correct control.

## Key Files Reference

| File | Role |
|------|------|
| `RenderingLibrary/Graphics/Fonts/BmfcSave.cs` | Font data model, .bmfc serialization, cache naming |
| `Gum/Content/BmfcTemplate.bmfc` | Template with placeholders for bmfont.exe config |
| `Tools/Gum.ProjectServices/FontGeneration/KernSmithFileGenerator.cs` | KernSmith-based font generation |
| `Tools/Gum.ProjectServices/FontGeneration/BmFontExeFileGenerator.cs` | bmfont.exe-based font generation |
| `Tools/Gum.ProjectServices/FontGeneration/HeadlessFontGenerationService.cs` | Font collection, deduplication, bulk generation |
| `Gum/Wireframe/CustomSetPropertyOnRenderable.cs` | Runtime font loading cascade |
| `Gum/PropertyGridHelpers/Converters/FontTypeConverter.cs` | System font dropdown |
| `Gum/Plugins/InternalPlugins/VariableGrid/ExclusionsPlugin.cs` | Variable visibility logic |
| `Gum/Plugins/InternalPlugins/VariableGrid/SetVariableLogic.cs` | Font property change reactions |
| `Gum/Managers/StandardElementsManager.cs` | Text standard element variable definitions |
