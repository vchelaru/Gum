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

**Goal**: Replace `bmfont.exe` with KernSmith library calls inside
`HeadlessFontGenerationService`.

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

- [ ] Resolve .NET target framework: multi-target KernSmith, or upgrade Gum.ProjectServices
- [ ] Add KernSmith source project (`src/KernSmith/KernSmith.csproj`) as a project reference from `Gum.ProjectServices`
- [ ] Add KernSmith's dependencies (FreeTypeSharp, StbImageWriteSharp) to Gum's package refs
- [ ] Create a mapping layer: `BmfcSave` properties -> `FontGeneratorOptions`
  - FontName -> system font name (or .ttf path in future)
  - FontSize -> Size
  - IsBold -> Bold
  - IsItalic -> Italic
  - UseSmoothing -> AntiAlias (Grayscale vs None)
  - OutlineThickness -> Outline
  - Ranges -> CharacterSet.FromRanges()
  - SpacingHorizontal/Vertical -> Spacing
  - OutputWidth/Height -> MaxTextureWidth/Height
- [ ] Replace `CreateBitmapFontFilesIfNecessaryAsync` in `HeadlessFontGenerationService`:
  - Call `BmFont.GenerateFromSystem()` (or `BmFont.Generate(path)`) instead of launching bmfont.exe
  - Write `result.FntText` to the `.fnt` file
  - Write `result.GetPngData()` to `.png` file(s)
  - Keep the same FontCache naming convention (`BmfcSave.FontCacheFileName`)
- [ ] Add support for `.ttf` font file paths (in addition to system font names)
- [ ] Remove `EnsureToolsExtracted` (no more embedded bmfont.exe)
- [ ] Remove `.bmfc` file writing from the generation path (no longer needed)
- [ ] Remove embedded `bmfont.exe` resource from `Gum.ProjectServices`
- [ ] Remove `BmfcTemplate.bmfc` from `Gum.ProjectServices`
- [ ] Update texture size optimization to use KernSmith's `AutofitTexture` option instead of binary-search probing
- [ ] Remove `ThrowIfNotWindows` checks (KernSmith is cross-platform)
- [ ] Test: font generation in Gum tool matches previous bmfont.exe output
- [ ] Test: gumcli fonts works cross-platform
- [ ] Test: outline fonts render correctly
- [ ] Test: font smoothing on/off works correctly
- [ ] Test: bold and italic variants generate correctly
- [ ] Test: custom font ranges generate correctly

### Stage 2b: GumProjectFontGenerator (lower priority)

- [ ] Update GumProjectFontGenerator to use the KernSmith-backed `HeadlessFontGenerationService`
- [ ] Consider whether GumProjectFontGenerator is still needed given gumcli covers the same use case

---

## Stage 3: Cleanup and Future Features (after Stage 2)

- [ ] Remove KernSmith source link and switch to NuGet package when available
- [ ] Evaluate removing `BmfcSave` from the tool-side font generation pipeline entirely (replace with a lighter model or use `FontGeneratorOptions` directly)
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
| `Gum/Services/Fonts/FontManager.cs` | Tool facade for font generation |
| `Tools/Gum.ProjectServices/FontGeneration/HeadlessFontGenerationService.cs` | Core generation logic (the one path to modify) |
| `Tools/Gum.ProjectServices/FontGeneration/IHeadlessFontGenerationService.cs` | Interface |
| `RenderingLibrary/Graphics/Fonts/BmfcSave.cs` | Font data model + legacy generation methods |
| `Gum/Wireframe/CustomSetPropertyOnRenderable.cs` | Divergent call site (Stage 1 fix) |
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
