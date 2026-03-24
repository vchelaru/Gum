# KernSmith Font Generation Integration Plan

## Background

Gum currently uses `bmfont.exe` to generate bitmap fonts (`.fnt` + `.png`). This has
limitations: Windows-only, one process per font, no advanced effects (color outline,
gradients, drop shadows), and process-launch overhead. KernSmith is a .NET library that
generates BMFont-compatible output entirely in-memory via FreeType, cross-platform, with
a rich feature set.

This plan has two stages. Stage 1 (cleanup) ships independently before any KernSmith work
begins, so we can verify the unified code path works with bmfont.exe first.

---

## Stage 1: Unify Font Generation Path (current branch, pre-KernSmith)

**Goal**: Route all font generation through `HeadlessFontGenerationService` so there is
exactly one code path to replace later.

### Current State

| Component | Path | Uses HeadlessFontGenerationService? |
|---|---|---|
| Gum Tool (FontManager) | FontManager -> HeadlessFontGenerationService | Yes |
| Gum Tool (CustomSetPropertyOnRenderable) | BmfcSave.CreateBitmapFontFilesIfNecessary | **No** |
| Gum CLI (FontsCommand) | HeadlessFontGenerationService directly | Yes |
| GumProjectFontGenerator | HeadlessFontGenerationService directly | Yes |

The `CustomSetPropertyOnRenderable` call site is synchronous and must remain so — this
method handles all property types and is never async.

### Checklist

- [x] Create `IFontManager` interface for `FontManager`
- [x] Make `IHeadlessFontGenerationService` injectable into `FontManager` (instead of `new` in constructor)
- [x] Register `IFontGenerationCallbacks`, `IHeadlessFontGenerationService`, and `IFontManager` in DI (Builder.cs)
- [x] Update all consumers of `FontManager` to use `IFontManager` interface
- [x] Add synchronous `CreateFontIfNecessary(BmfcSave, string, bool)` to `IHeadlessFontGenerationService` / `HeadlessFontGenerationService`
- [x] Expose `CreateFontIfNecessary` on `IFontManager` / `FontManager`
- [x] Reroute `CustomSetPropertyOnRenderable` to call `IFontManager.CreateFontIfNecessary` instead of `BmfcSave.CreateBitmapFontFilesIfNecessary`
- [x] Remove legacy methods from `BmfcSave` (`CreateBitmapFontFilesIfNecessary` static + instance, `WaitForExitAsync`)
- [x] Remove unused `using` statements from `BmfcSave`
- [x] Remove embedded `bmfont.exe` from `RenderingLibrary.csproj` (no longer needed)
- [x] Write `FontManagerTests` (delegation, autoSize passthrough, null project handling)
- [x] Write `CreateFontIfNecessary` platform gate test in `HeadlessFontGenerationServiceTests`
- [ ] Test: confirm fonts generate correctly in the Gum tool when changing font properties
- [ ] Test: confirm `gumcli fonts` still generates fonts correctly
- [ ] Test: confirm "Re-create missing font files" menu item works
- [ ] Merge to master

---

## Stage 2: Integrate KernSmith (after Stage 1 is merged)

**Goal**: Add KernSmith as an alternative font generator alongside bmfont.exe, selectable
per-project. KernSmith is experimental initially — bmfont.exe remains the default.

### Architecture: Strategy Pattern

The only part that differs between generators is "given a BmfcSave and output path, produce
.fnt + .png files." All orchestration (font collection, deduplication, caching, parallelism,
callbacks) stays in `HeadlessFontGenerationService`.

```
IFontFileGenerator
    GeneralResponse GenerateFont(BmfcSave bmfcSave, string outputFntPath)

BmFontExeFileGenerator   — writes .bmfc, launches bmfont.exe (current behavior)
KernSmithFileGenerator   — calls KernSmith API, writes .fnt + .png to disk
```

`HeadlessFontGenerationService` takes an `IFontFileGenerator` and delegates the single-font
generation step to it. The correct implementation is selected based on the project setting.

### Project Setting

Add a `FontGenerator` property to `GumProjectSave`:
- Values: `BmFont` (default), `KernSmith`
- Projects without this property default to `BmFont` (backward compatible)
- Switching generators should prompt the user to regenerate all fonts

### Framework Decision

KernSmith currently targets .NET 10.0. Gum runtime libraries must stay on .NET 8.0.
Options:
- **Preferred**: Have KernSmith multi-target `net8.0;net10.0` (needs verification that
  KernSmith code doesn't use .NET 10-only APIs)
- **Alternative**: Move only `Gum.ProjectServices` (and the Gum tool) to .NET 10 while
  keeping runtime libs at 8.0 — but this may require GitHub build tool updates

This decision should be resolved before beginning Stage 2 work.

### Font Source Strategy

KernSmith supports both system fonts (`GenerateFromSystem`) and font file paths
(`Generate(path)`). We should support both:
- System fonts for backward compatibility with current projects
- `.ttf` file paths for distributable Gum projects that bundle their fonts

### Checklist

#### 2a: Extract strategy interface and refactor bmfont.exe into it

- [ ] Create `IFontFileGenerator` interface with `GenerateFont(BmfcSave, string outputFntPath)` method
- [ ] Create `BmFontExeFileGenerator` implementing `IFontFileGenerator` — extract the bmfont.exe process-launch logic from `HeadlessFontGenerationService.CreateBitmapFontFilesIfNecessaryAsync`
- [ ] Refactor `HeadlessFontGenerationService` to accept `IFontFileGenerator` and delegate to it
- [ ] Add `FontGenerator` enum and property to `GumProjectSave` (default: `BmFont`)
- [ ] Wire up generator selection based on project setting (in DI or factory)
- [ ] Test: existing bmfont.exe behavior is unchanged with the refactored code

#### 2b: Add KernSmith generator

- [ ] Resolve .NET target framework: multi-target KernSmith, or upgrade Gum.ProjectServices
- [ ] Add KernSmith source project (`src/KernSmith/KernSmith.csproj`) as a project reference from `Gum.ProjectServices`
- [ ] Add KernSmith's dependencies (FreeTypeSharp, StbImageWriteSharp) to Gum's package refs
- [ ] Create `KernSmithFileGenerator` implementing `IFontFileGenerator`:
  - Map `BmfcSave` properties → `FontGeneratorOptions`
    - FontName → system font name (or .ttf path in future)
    - FontSize → Size
    - IsBold → Bold
    - IsItalic → Italic
    - UseSmoothing → AntiAlias (Grayscale vs None)
    - OutlineThickness → Outline
    - Ranges → CharacterSet.FromRanges()
    - SpacingHorizontal/Vertical → Spacing
    - OutputWidth/Height → MaxTextureWidth/Height
  - Call `BmFont.GenerateFromSystem()` (or `BmFont.Generate(path)`)
  - Write `result.FntText` to `.fnt` file
  - Write `result.GetPngData()` to `.png` file(s)
  - Keep the same FontCache naming convention (`BmfcSave.FontCacheFileName`)
- [ ] Add support for `.ttf` font file paths (in addition to system font names)
- [ ] Consider using KernSmith's `AutofitTexture` option instead of the binary-search size probing
- [ ] Remove `ThrowIfNotWindows` from code paths that use KernSmith (it's cross-platform)
- [ ] Test: font generation produces valid .fnt + .png output
- [ ] Test: outline fonts render correctly
- [ ] Test: font smoothing on/off works correctly
- [ ] Test: bold and italic variants generate correctly
- [ ] Test: custom font ranges generate correctly
- [ ] Test: gumcli fonts works with KernSmith setting

#### 2c: GumProjectFontGenerator (lower priority)

- [ ] Update GumProjectFontGenerator to respect the project's generator setting
- [ ] Consider whether GumProjectFontGenerator is still needed given gumcli covers the same use case

---

## Stage 3: Cleanup and Future Features (after Stage 2)

- [ ] Remove KernSmith source link and switch to NuGet package when available
- [ ] If KernSmith proves stable, consider making it the default for new projects
- [ ] Remove bmfont.exe dependency entirely once KernSmith is mature enough
- [ ] Evaluate removing `BmfcSave` from the tool-side font generation pipeline entirely
  (replace with a lighter model or use `FontGeneratorOptions` directly)
- [ ] Explore advanced KernSmith features:
  - Color outlines
  - Gradients
  - Drop shadows
  - SDF generation
  - Channel packing
  - Batch generation with font cache reuse

---

## Key Files

| File | Role |
|---|---|
| `Gum/Services/Fonts/IFontManager.cs` | Interface for the tool-facing font manager |
| `Gum/Services/Fonts/FontManager.cs` | Tool facade — delegates to headless service via DI |
| `Tools/Gum.ProjectServices/FontGeneration/HeadlessFontGenerationService.cs` | Orchestration: font collection, caching, parallelism, delegates to IFontFileGenerator |
| `Tools/Gum.ProjectServices/FontGeneration/IHeadlessFontGenerationService.cs` | Interface |
| `Tools/Gum.ProjectServices/FontGeneration/IFontFileGenerator.cs` | Strategy interface for single-font generation (Stage 2) |
| `Tools/Gum.ProjectServices/FontGeneration/BmFontExeFileGenerator.cs` | bmfont.exe implementation (Stage 2) |
| `Tools/Gum.ProjectServices/FontGeneration/KernSmithFileGenerator.cs` | KernSmith implementation (Stage 2) |
| `RenderingLibrary/Graphics/Fonts/BmfcSave.cs` | Font data model, cache file naming, range utilities |
| `Tools/Gum.Cli/Commands/FontsCommand.cs` | CLI font generation |
| `GumProjectFontGenerator/Program.cs` | Standalone font generator |

## KernSmith Reference

- **Source**: `C:\Users\vchel\Documents\GitHub\KernSmith`
- **GitHub**: https://github.com/kaltinril/Kernsmith
- **Entry point**: `BmFont.GenerateFromSystem(fontFamily, options)` or `BmFont.Generate(path, options)`
- **Config class**: `FontGeneratorOptions`
- **Result class**: `BmFontResult` (provides `.FntText`, `.GetPngData()`, `.ToFile()`)
- **Dependencies**: FreeTypeSharp 3.1.0, StbImageWriteSharp 1.16.7
- **Target**: .NET 10.0 (multi-target to net8.0 TBD)
