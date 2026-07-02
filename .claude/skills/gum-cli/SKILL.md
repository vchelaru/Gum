---
name: gum-cli
description: GumCli — headless CLI for Gum projects. Triggers: gumcli commands (new, check, diff-standards, codegen, codegen-init, fonts, screenshot, svg), Gum.ProjectServices, HeadlessErrorChecker, ProjectLoader, HeadlessCodeGenerationService, CodeGenerationAutoSetupService, FormsTemplateCreator, DiffStandardsService.
---

# GumCli Reference

## What It Is

**GumCli** (`gumcli`) is a cross-platform .NET 8.0 console app that lets developers create, validate, and generate code for Gum projects without the WPF editor. Primary use cases: CI pipelines, scripting, editor integrations.

**Location:** `Tools/Gum.Cli/`
**Depends on:** `Gum.ProjectServices` → `GumCommon`

## Commands

| Command | Purpose |
|---------|---------|
| `gumcli new <path> [--template]` | Create a new project. Templates: `forms` (default, includes all Forms UI controls) or `empty` (minimal). |
| `gumcli check <project.gumx> [--json]` | Validate all elements (.gusx, .gutx, .gucx) that belong to the project. Human-readable or JSON output. Use this for post-write validation of any element file, not just the .gumx. |
| `gumcli check-references <project.gumx> [--json] [--fix]` | Detect (and optionally fix) `VariableReferences` rows whose left-hand-side scalars are not materialized into the state's `Variables` — the inconsistent shape commonly produced by AI agents and hand edits that bypass the Gum tool's author-time propagation. `--fix` runs `ApplyVariableReferences` on affected states and saves the modified element files. Scans Screens and Components only (StandardElements have known default-evaluating refs whose missing scalars are correct on-disk state — see [gum-tool-variable-references](../gum-tool-variable-references/SKILL.md) for the model). Exits 1 if anything is unpropagated. |
| `gumcli diff-standards <project.gumx> [--json]` | Compare the project's Standards against `StandardElementsManager.Self`'s programmatic defaults (the same source the Gum tool's File → New uses) and report variable-level drift. Exits 1 on drift, 0 on clean. Theme authors and CI use it to enforce the "Standards must match Default" invariant. |
| `gumcli codegen <project.gumx> [--element <name>...]` | Generate C# code. Requires `ProjectCodeSettings.codsj`. Per-element error check gates generation. |
| `gumcli codegen-init <project.gumx> [--force] [--csproj <path>]` | Auto-detect `.csproj`, derive namespace and output library, write `ProjectCodeSettings.codsj`. Use `--csproj` when the Gum project is not inside the MonoGame project directory. |
| `gumcli fonts <project.gumx>` | Generate missing bitmap font files (.fnt + .png). Windows-only (bmfont.exe). |
| `gumcli screenshot <project.gumx> <element> [--output] [--width] [--height]` | Render a Screen or Component to a PNG via MonoGame DesktopGL. Pixel-accurate; cross-platform. |
| `gumcli svg <project.gumx> <element> [--output] [--width] [--height]` | Render a Screen or Component to a vector SVG via SkiaGum's `SKSvgCanvas`. Bitmaps embed as base64. |

**Exit codes:** 0 = success, 1 = errors found / generation blocked, 2 = load failure, bad args, or non-Windows (fonts).

## Architecture

```
Program.cs
  ├── NewCommand      → ProjectCreator / FormsTemplateCreator
  ├── CheckCommand    → ProjectLoader → HeadlessErrorChecker
  ├── CheckReferencesCommand → ProjectLoader → ReferencePropagationService (Detect / PropagateReferences). Wires GumExpressionService so literal/expression RHSes evaluate; sets ObjectFinder.Self.GumProjectSave so cross-element refs resolve.
  ├── DiffStandardsCommand → ProjectLoader → DiffStandardsService (project Standards vs StandardElementsManager.Self defaults)
  ├── CodegenCommand  → ProjectLoader → HeadlessErrorChecker (gates) → HeadlessCodeGenerationService
  ├── CodegenInitCommand → CodeGenerationAutoSetupService
  ├── FontsCommand    → ProjectLoader → HeadlessFontGenerationService (Windows-gated)
  ├── ScreenshotCommand → MonoGameScreenshotService (Gum.ProjectServices.MonoGame)
  └── SvgCommand      → SkiaGumSvgExportService (Gum.ProjectServices.SkiaGum)
```

Each command class has a static `Create()` returning a `System.CommandLine` `Command` with handler, then a static `Execute()` doing the work.

**CLI-specific adapters:**
- `ConsoleCodeGenLogger` — implements `ICodeGenLogger`, writes to stdout/stderr
- `HeadlessNameVerifier` — implements `INameVerifier` for C# name validation

## Gum.ProjectServices

The headless service library GumCli depends on. All logic lives here; the CLI just wires it together.

**Key types:**

| Type | Role |
|------|------|
| `ProjectLoader` / `IProjectLoader` | Loads `.gumx`; detects malformed XML; returns `ProjectLoadResult` with fatal errors and non-fatal `LoadErrors` |
| `HeadlessErrorChecker` / `IHeadlessErrorChecker` | Validates base types, behaviors, parent refs, variable types. Delegates from tool's `ErrorChecker`. |
| `ProjectCreator` / `IProjectCreator` | Creates blank projects with subfolder structure |
| `FormsTemplateCreator` / `IFormsTemplateCreator` | Extracts embedded Forms template resources |
| `HeadlessCodeGenerationService` | Orchestrates per-element code file generation |
| `CodeGenerationAutoSetupService` | Walks up to find `.csproj`, derives `CodeProjectRoot`, namespace, output library |
| `CodeOutputProjectSettingsManager` | Loads/saves `ProjectCodeSettings.codsj` |
| `ErrorResult` | POCO: `ElementName`, `Message`, `Severity` (`Warning`/`Error`) |
| `DiffStandardsService` / `IDiffStandardsService` | Compares a loaded project's Standards against `StandardElementsManager.Self`'s programmatic defaults. Returns `DiffStandardsResult` with `Differences`, `MissingFromProject`, `ProjectOnlyStandards`. |
| `ReferencePropagationService` / `IReferencePropagationService` | Detects states where a `VariableReferences` row exists without the corresponding materialized scalars in `Variables`, and propagates them on demand. `Detect` returns `DetectUnpropagatedReferencesResult`. `PropagateReferences` mutates the project (runs the static `ElementSaveExtensions.ApplyVariableReferences` per offending state) and returns the modified elements; the caller persists. Walks Screens + Components only; Standards are intentionally skipped (their default-evaluating refs would produce false positives). Expression evaluation depends on whoever wires `ElementSaveExtensions.CustomEvaluateExpression` — the CLI calls `GumExpressionService.Initialize()` before invoking. |

**Non-obvious:** `HeadlessErrorChecker` is not a duplicate of the tool's `ErrorChecker` — the tool's `ErrorChecker` **delegates to** `HeadlessErrorChecker`. Zero duplication by design.

`HeadlessErrorChecker` accepts a second constructor overload taking `IEnumerable<IAdditionalErrorSource>` for extensibility; the CLI uses the single-argument overload.

`HeadlessNameVerifier` only implements real logic in `IsValidCSharpName`; all other `INameVerifier` methods return `true` unconditionally. This is intentional — the CLI only needs C# name validation for codegen.

`ProjectLoader` runs `DetectSilentlyDroppedContent` after deserialization to catch incorrect XML element names (e.g., `<States>` instead of `<State>`, `<InstanceSave>` instead of `<Instance>`) that `XmlSerializer` silently ignores. Without this, AI-generated files with wrong structure load as empty elements with no error.

`CodeGenerationAutoSetupService` detects MonoGame vs non-MonoGame projects by scanning for `<PackageReference Include="MonoGame.Framework.` or `nkast.Xna.Framework` in the `.csproj`, then sets `OutputLibrary` accordingly (to `MonoGameForms`, not plain `MonoGame`). It also detects `<PackageReference Include="Raylib-cs"` and sets `OutputLibrary.Raylib` — there is no "RaylibForms" auto-detect target since Raylib codegen only supports `ObjectInstantiationType.FindByName` so far (see gum-tool-codegen). Namespace falls back to the `.csproj` filename (dots/dashes/spaces replaced with underscores) when `<RootNamespace>` is absent.

`codegen` fails fast with exit code 1 if the resolved settings request an unsupported combination (currently: `OutputLibrary.Raylib` + `ObjectInstantiationType.FullyInCode`) — `CodeGenerator.AssertSupportedCombination` throws `NotSupportedException`, and `CodegenCommand` catches it before touching the file system.

`CodeGenerationAutoSetupService` has two `Run` overloads: `Run(gumxFilePath)` for auto-detection, and `Run(gumxFilePath, explicitCsprojPath)` for when the caller already knows the `.csproj` path. The explicit overload validates the file exists and skips directory walking entirely; all namespace/OutputLibrary derivation logic is shared via the private `BuildResultFromCsprojDirectory` helper.

`DiffStandardsService` compares the loaded project against a fresh reference built by `StandardElementsManager.Self.PopulateProjectWithDefaultStandards(...)` — the same path the tool's File → New uses. The CLI matches the tool's import-dialog drift detection by construction. Note: the on-disk `Templates/Default/Standards/*.gutx` files (extracted by `gumcli new --template empty`) are a separate snapshot of the defaults and have known drift from `StandardElementsManager`; that drift is a distinct issue from theme-vs-Default drift.

Font files are named like `Font18Arial.fnt` and `Font18Arial_0.png` (size+name convention, zero-indexed). Always use `gumcli fonts <project.gumx>` to generate missing bitmap fonts — never create `.fnt` files manually.

## Codegen Flow (non-obvious details)

- `codegen` iterates elements individually (not via `GenerateCodeForAllElements`) so it can run per-element error checks first; `GenerateCodeForAllElements` exists on `HeadlessCodeGenerationService` but the CLI does not call it
- Only Screens and Components are generated; StandardElements are intentionally excluded
- Errors (`ErrorSeverity.Error`) block generation for that element; warnings print to stderr but do not block
- `ObjectFinder.Self` cache is managed at the CLI level (enabled before the loop, disabled in `finally`)
- If `ProjectCodeSettings.codsj` is missing, `codegen` attempts `CodeGenerationAutoSetupService` auto-detection first and writes the settings file before continuing; exit code 2 only if auto-detection also fails
- When `--element` is specified, `checkForMissing: true` is passed so `GenerateCodeForElement` auto-generates any referenced elements whose code files do not yet exist; full-run mode skips that check
- `codegen-init` exits with code 2 (not 1) if settings already exist and `--force` is absent

## Key Files

| File | Purpose |
|------|---------|
| `Tools/Gum.Cli/Program.cs` | Entry point, assembles RootCommand |
| `Tools/Gum.Cli/Commands/` | One file per command |
| `Gum.ProjectServices/ProjectLoader.cs` | Headless project loading |
| `Gum.ProjectServices/HeadlessErrorChecker.cs` | All headless error checks |
| `Gum.ProjectServices/CodeGeneration/HeadlessCodeGenerationService.cs` | Headless codegen orchestration |
| `Gum.ProjectServices/CodeGeneration/CodeGenerator.cs` | Main codegen engine (~5400 lines) |
| `Tests/Gum.Cli.Tests/` | CLI command tests |
| `Tests/Gum.ProjectServices.Tests/` | Service layer tests (36 tests) |

## Testing Notes

- `ObjectFinder` is a singleton — tests disable parallel execution (`TestAssemblyInitialize`)
- `BaseTestClass` pre-populates `GumProjectSave` with standard elements and handles `ObjectFinder.Self` cleanup
