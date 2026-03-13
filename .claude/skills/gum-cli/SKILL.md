---
name: gum-cli
description: Reference guide for GumCli — the headless command-line tool for Gum projects. Load this when working on gumcli commands (new, check, codegen, codegen-init), Gum.ProjectServices, HeadlessErrorChecker, ProjectLoader, HeadlessCodeGenerationService, CodeGenerationAutoSetupService, or the FormsTemplateCreator.
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
| `gumcli check <project.gumx> [--json]` | Validate all elements. Human-readable or JSON output. |
| `gumcli codegen <project.gumx> [--element <name>...]` | Generate C# code. Requires `ProjectCodeSettings.codsj`. Per-element error check gates generation. |
| `gumcli codegen-init <project.gumx> [--force]` | Auto-detect `.csproj`, derive namespace and output library, write `ProjectCodeSettings.codsj`. |
| `gumcli fonts <project.gumx>` | Generate missing bitmap font files (.fnt + .png). Windows-only (bmfont.exe). |

**Exit codes:** 0 = success, 1 = errors found / generation blocked, 2 = load failure, bad args, or non-Windows (fonts).

## Architecture

```
Program.cs
  ├── NewCommand      → ProjectCreator / FormsTemplateCreator
  ├── CheckCommand    → ProjectLoader → HeadlessErrorChecker
  ├── CodegenCommand  → ProjectLoader → HeadlessErrorChecker (gates) → HeadlessCodeGenerationService
  ├── CodegenInitCommand → CodeGenerationAutoSetupService
  └── FontsCommand    → ProjectLoader → HeadlessFontGenerationService (Windows-gated)
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

**Non-obvious:** `HeadlessErrorChecker` is not a duplicate of the tool's `ErrorChecker` — the tool's `ErrorChecker` **delegates to** `HeadlessErrorChecker`. Zero duplication by design.

`HeadlessErrorChecker` accepts a second constructor overload taking `IEnumerable<IAdditionalErrorSource>` for extensibility; the CLI uses the single-argument overload.

`HeadlessNameVerifier` only implements real logic in `IsValidCSharpName`; all other `INameVerifier` methods return `true` unconditionally. This is intentional — the CLI only needs C# name validation for codegen.

`ProjectLoader` runs `DetectSilentlyDroppedContent` after deserialization to catch incorrect XML element names (e.g., `<States>` instead of `<State>`, `<InstanceSave>` instead of `<Instance>`) that `XmlSerializer` silently ignores. Without this, AI-generated files with wrong structure load as empty elements with no error.

`CodeGenerationAutoSetupService` detects MonoGame vs non-MonoGame projects by scanning for `<PackageReference Include="MonoGame.Framework.` or `nkast.Xna.Framework` in the `.csproj`, then sets `OutputLibrary` accordingly. Namespace falls back to the `.csproj` filename (dots/dashes/spaces replaced with underscores) when `<RootNamespace>` is absent.

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
