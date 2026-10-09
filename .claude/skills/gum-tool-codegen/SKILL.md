---
name: gum-tool-codegen
description: Gum tool C# code generation. Triggers: CodeGenerator, CodeOutputPlugin, generated code structure, .codsj settings, OutputLibrary selection, Forms codegen, state generation. For CLI/headless codegen see gum-cli.
---

# Gum Tool Code Generation System

Full walkthrough in [codegen-deep-dive.md](codegen-deep-dive.md): placement and namespace derivation, `FullyInCode` vs `FindByName` in detail, the custom-file contract, and the rename/delete/orphan reconciliation rules. Self-contained, so it also works handed to an agent on its own.

## What It Is

The code generation system produces C# partial classes from Gum Screens and Components. Two files per element: `.Generated.cs` (auto-regenerated, never hand-edit) and `.cs` (user-editable stub with `partial void CustomInitialize()` hook). StandardElements are never generated.

**Use `OutputLibrary.MonoGameForms` on every runtime.** The Code tab labels it "Gum Forms (recommended)"; the enum name predates the unification. Forms controls live in `GumCommon` and every runtime exposes the same `Gum.GueDeriving` and `GumService` names, so one generated shape serves MonoGame, KNI, FNA, raylib, Skia, Silk.NET, Stride and Unity. Only each runtime's input layer differs. The other `OutputLibrary` values are legacy, kept so existing projects keep generating. See [gum-architecture-layers](../gum-architecture-layers/SKILL.md) and [gum-cross-platform-unification](../gum-cross-platform-unification/SKILL.md).

## Architecture

```
Tool UI (Gum.csproj)                 Headless (Gum.Presentation / Gum.ProjectServices)
  CodeOutputPlugin/                    CodeOutputPlugin/Manager/ (Gum.Presentation)
    MainCodeOutputPlugin                 CodeGenerationService, ParentSetLogic, RenameService
    CodeWindow (Code tab)               CodeGeneration/ (Gum.ProjectServices)
                                          CodeGenerator (~5700 lines), CustomCodeGenerator
                                          CodeGenerationFileLocationsService, CodeGenerationNameVerifier
                                          VariableExclusionLogic, CodeOutputProjectSettingsManager
                                          CodeOutputElementSettingsManager
```

`CodeGenerationService` (tool-side) orchestrates generation by calling into `CodeGenerator` (shared engine). The same `CodeGenerator` is used by the CLI via `HeadlessCodeGenerationService` -- see the gum-cli skill for that path.

Both files live on disk outside the `.gumx`, so a change to an element's identity has to reconcile them: `RenameService` covers rename, folder move and BaseType change, `CodeFileDeleteService` owns the delete decision, and `OrphanCodeFileScanService` is the catch-all for files that orphaned without passing through either. One invariant governs all three: `.Generated.cs` is derived data and can be removed freely, while the custom `.cs` is user-authored, unrecoverable through undo, and never goes without consent or outside the recycle bin.

A Code tab settings edit is the other trigger: `CodeFileLocationWatcher` sends one message when files must move (the migration in `CodeFileMigrator`) and another when only the namespace or base class they declare changes (`CustomCodeHeaderUpdater`, rewriting in place). Both back up through `CodeFileBackupService`, so one Restore menu item undoes either.

## Configuration (.codsj files)

**Project-level:** `ProjectCodeSettings.codsj` alongside the `.gumx`. Managed by `CodeOutputProjectSettingsManager`. Key settings: `OutputLibrary`, `CodeProjectRoot`, `GeneratedCodeFolder`, `GeneratedCodeFolderPrefix`, `RootNamespace`, `ObjectInstantiationType`, `InheritanceLocation`, `AppendFolderToNamespace`. Syntax and C# version detection read the csproj from `CodeProjectCsprojLocator.FindCsproj`: `CsprojPath` when set (no fallback if missing), else the csproj in `CodeProjectRoot`.

**Element-level:** `ElementName.codsj` alongside the `.gucx`/`.gusx`. Managed by `CodeOutputElementSettingsManager`. Key settings: `GenerationBehavior`, namespace override, custom output path.

## Key Enums

| Enum | Values | Notes |
|------|--------|-------|
| `OutputLibrary` | XamarinForms(0), WPF(1), Skia(2), Maui(3), MonoGame(4), MonoGameForms(5), Raylib(6), Silk(7) | MonoGameForms ("Gum Forms") is the value for new projects on any runtime; the rest are legacy. Raylib and Silk support only `ObjectInstantiationType.FindByName` (see below) |
| `ObjectInstantiationType` | FullyInCode, FindByName | FullyInCode generates all creation; FindByName wires references to externally-created instances |
| `InheritanceLocation` | InGeneratedCode, InCustomCode | Controls which partial class file declares the base class |
| `VisualApi` | Gum, XamarinForms | Internal enum; Gum for MonoGame/MonoGameForms/Skia/raylib, XamarinForms for Xamarin/MAUI |
| `GenerationBehavior` | NeverGenerate, GenerateManually, GenerateAutomaticallyOnPropertyChange | Per-element setting |

## Generated Code Structure (in order)

1. Using statements (auto-detected from instances)
2. Namespace (root + optional folder path)
3. Partial class with optional inheritance
4. State enums (one per category)
5. State properties with `ApplyState()` calls
6. Instance fields
7. Custom variables (user-defined properties)
8. Exposed variables (delegate to child instances)
9. Constructor chain: `InitializeInstances()` then `AddToParents()` then `ApplyDefaultVariables()`
10. `ApplyState()` methods
11. `ApplyLocalization()` (if enabled)
12. `partial void CustomInitialize()`

## Non-Obvious Behavior

**MonoGameForms `.Visual` wrapping** -- When OutputLibrary is MonoGameForms, property access on instances goes through `.Visual` (e.g., `this.Visual` for root, `this.InstanceName.Visual` for children). The generated code treats everything that is not a StandardElement as a Forms object.

**Forms base type from behaviors** -- MonoGameForms determines the generated base class by scanning the element's behaviors (e.g., ButtonBehavior maps to Button). The method `GetGumFormsTypeFromBehaviors` drives this.

**Screen inheritance resolution order** -- `CodeGenerator.GetInheritance` for `ScreenSave` resolves inheritance in this priority: `element.BaseType` > `projectSettings.DefaultScreenBase` > library-appropriate fallback (`FrameworkElement` for MonoGameForms, `GraphicalUiElement` otherwise). `DefaultScreenBase` defaults to empty string so users can switch `OutputLibrary` without stale base classes bleeding through.

**State generation suppressed for Forms standards** -- When OutputLibrary is MonoGameForms and the state container is a `StandardElementSave`, state code is not generated; the Forms framework handles it.

**Forms placeholders** -- `Gum.ProjectServices` can't reference MonoGameGum, so `FormsControlPlaceholders.cs` hand-mirrors the real Forms controls' members. Codegen reflects on it to skip or `new`-mark generated members that would hide an inherited one (CS0108/CS0114). When a template component gains an exposed variable or state category that collides with a real base member, mirror that member (and its base class) there; `FormsTemplateCodegenHidingTests` in `Gum.Cli.Tests` compares generated output to the real types and fails on a miss.

**Checked-in generated output** -- Any change to generated-code output also requires regenerating the `Tests/CodeGen_*` projects (`Tests/GenerateAllCodeGenProjects.bat`: build `Gum.Cli`, run `gumcli codegen` on each) and committing the diff, or CI's "Codegen Drift Check" fails. On macOS the CLI writes CRLF; `git add` normalizes it, so judge the diff with `git diff --cached --stat`, not `git status`.

**Missing dependency auto-generation** -- When generating for an element, the system checks if referenced elements lack code files and offers to generate them too. In auto-generation mode this happens silently.

**ObjectFinder cache** -- Code generation enables/disables `ObjectFinder.Self` cache around generation loops for performance. Must be managed at the call site (not inside `CodeGenerator`).

**VariableExclusionLogic** -- Certain variables are excluded depending on OutputLibrary (e.g., Alpha excluded for XamarinForms). The plugin hooks into the `VariableExcluded` query event to apply this.

**Tool plugin auto-regeneration** -- `MainCodeOutputPlugin` listens to nearly every edit event (variable set, instance add/delete, state changes, etc.) and auto-regenerates if the element's `GenerationBehavior` is `GenerateAutomaticallyOnPropertyChange`.

**RequestCodeGenerationMessage** -- External systems (like FlatRedBall editor integration) can trigger codegen via this CommunityToolkit.Mvvm message.

**Culture-independent output** -- Generated code must be identical on every machine, so sorting, casing and number formatting in the generators use the invariant culture (`CultureInfo.InvariantCulture`, `StringComparison.Ordinal`/`InvariantCulture`, `ToUpperInvariant`).

**C# name compliance** -- `CodeGenerationNameVerifier` prefixes C# keywords with `@`, leading digits with `_`, and replaces spaces with `_`.

**RenameService** -- When elements are renamed in the tool, updates generated code file names and internal references.

**Legacy Raylib and Silk output** -- `OutputLibrary.Raylib` and `Silk` emit the same shape as legacy `MonoGame` (no Forms wrapping), and support only `ObjectInstantiationType.FindByName`. `CodeGenerator.UsesUnifiedGumRuntime` is the shared predicate for these three values. `AssertSupportedCombination` throws `NotSupportedException` for `FullyInCode`, so the CLI exits 1; the tool calls `CoerceToSupportedCombination` to snap back to `FindByName`. `ResolveSyntaxVersion` floors both at syntax version 3 because they never had the pre-unification namespaces. `GetGumServiceNamespace` takes `isRaylib` because the legacy shim namespace is `RaylibGum`, not `MonoGameGum`; passing the wrong one emits an unresolvable `using MonoGameGum;`.

## Key Files

| File | Purpose |
|------|---------|
| `Tools/Gum.ProjectServices/CodeGeneration/CodeGenerator.cs` | Core codegen engine (~5700 lines) |
| `Tools/Gum.ProjectServices/CodeGeneration/CustomCodeGenerator.cs` | User-editable partial class stub |
| `Tools/Gum.ProjectServices/CodeGeneration/CodeOutputProjectSettings.cs` | Project settings classes + enums |
| `Tools/Gum.ProjectServices/CodeGeneration/CodeOutputElementSettings.cs` | Element settings class |
| `Tools/Gum.ProjectServices/CodeGeneration/CodeGenerationFileLocationsService.cs` | Output path resolution |
| `Tools/Gum.ProjectServices/CodeGeneration/CodeGenerationNameVerifier.cs` | C# name compliance |
| `Tools/Gum.ProjectServices/CodeGeneration/VariableExclusionLogic.cs` | Platform-specific variable exclusion |
| `Tools/Gum.Presentation/CodeOutputPlugin/CodeOutputPluginBase.cs` | Plugin body shared by both heads; `Gum/CodeOutputPlugin/MainCodeOutputPlugin.cs` (frozen WPF) and `Tool/Gum.Avalonia/Plugins/CodeOutput/` (Avalonia) are thin view heads |
| `Tools/Gum.Presentation/CodeOutputPlugin/Manager/CodeGenerationService.cs` | Generation orchestration (headless) |
| `Tools/Gum.Presentation/CodeOutputPlugin/Manager/ParentSetLogic.cs` | Forms parent relationship handling (headless) |
| `Tools/Gum.Presentation/CodeOutputPlugin/Manager/RenameService.cs` | Element rename to code rename (headless) |
