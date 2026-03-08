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

## Codegen Flow (non-obvious details)

- `codegen` iterates elements individually (not via `GenerateCodeForAllElements`) so it can run per-element error checks first
- Errors (`ErrorSeverity.Error`) block generation for that element; warnings print to stderr but do not block
- `ObjectFinder.Self` cache is managed at the CLI level (enabled before iteration, disabled after)
- `ProjectCodeSettings.codsj` must exist in the same directory as the `.gumx` file; missing config exits with code 2

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
