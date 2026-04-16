---
name: gum-tool-codegen
description: Reference guide for the Gum tool's C# code generation system. Load this when working on CodeGenerator, CodeOutputPlugin, generated code structure, .codsj settings, OutputLibrary selection, Forms codegen, state generation, or the tool-side code generation pipeline. For CLI/headless codegen, see the gum-cli skill instead.
---

# Gum Tool Code Generation System

## What It Is

The code generation system produces C# partial classes from Gum Screens and Components. Two files per element: `.Generated.cs` (auto-regenerated, never hand-edit) and `.cs` (user-editable stub with `partial void CustomInitialize()` hook). StandardElements are never generated.

**MonoGameForms is the recommended default OutputLibrary** for new projects. Non-forms (plain MonoGame) exists for legacy and specialized scenarios.

## Architecture

```
Tool UI                              Shared Engine
  CodeOutputPlugin/                    Gum.ProjectServices/CodeGeneration/
    MainCodeOutputPlugin               CodeGenerator (~5700 lines)
    CodeGenerationService              CustomCodeGenerator
    CodeWindow (Code tab)              CodeGenerationFileLocationsService
    ParentSetLogic                     CodeGenerationNameVerifier
    RenameService                      VariableExclusionLogic
                                       CodeOutputProjectSettingsManager
                                       CodeOutputElementSettingsManager
```

`CodeGenerationService` (tool-side) orchestrates generation by calling into `CodeGenerator` (shared engine). The same `CodeGenerator` is used by the CLI via `HeadlessCodeGenerationService` -- see the gum-cli skill for that path.

## Configuration (.codsj files)

**Project-level:** `ProjectCodeSettings.codsj` alongside the `.gumx`. Managed by `CodeOutputProjectSettingsManager`. Key settings: `OutputLibrary`, `CodeProjectRoot`, `RootNamespace`, `ObjectInstantiationType`, `InheritanceLocation`, `AppendFolderToNamespace`.

**Element-level:** `ElementName.codsj` alongside the `.gucx`/`.gusx`. Managed by `CodeOutputElementSettingsManager`. Key settings: `GenerationBehavior`, namespace override, custom output path.

## Key Enums

| Enum | Values | Notes |
|------|--------|-------|
| `OutputLibrary` | XamarinForms(0), WPF(1), Skia(2), Maui(3), MonoGame(4), MonoGameForms(5) | MonoGameForms is recommended default |
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

**Missing dependency auto-generation** -- When generating for an element, the system checks if referenced elements lack code files and offers to generate them too. In auto-generation mode this happens silently.

**ObjectFinder cache** -- Code generation enables/disables `ObjectFinder.Self` cache around generation loops for performance. Must be managed at the call site (not inside `CodeGenerator`).

**VariableExclusionLogic** -- Certain variables are excluded depending on OutputLibrary (e.g., Alpha excluded for XamarinForms). The plugin hooks into the `VariableExcluded` query event to apply this.

**Tool plugin auto-regeneration** -- `MainCodeOutputPlugin` listens to nearly every edit event (variable set, instance add/delete, state changes, etc.) and auto-regenerates if the element's `GenerationBehavior` is `GenerateAutomaticallyOnPropertyChange`.

**RequestCodeGenerationMessage** -- External systems (like FlatRedBall editor integration) can trigger codegen via this CommunityToolkit.Mvvm message.

**C# name compliance** -- `CodeGenerationNameVerifier` prefixes C# keywords with `@`, leading digits with `_`, and replaces spaces with `_`.

**RenameService** -- When elements are renamed in the tool, updates generated code file names and internal references.

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
| `Gum/CodeOutputPlugin/MainCodeOutputPlugin.cs` | Tool UI plugin entry point |
| `Gum/CodeOutputPlugin/Manager/CodeGenerationService.cs` | Tool-side generation orchestration |
| `Gum/CodeOutputPlugin/Manager/ParentSetLogic.cs` | Forms parent relationship handling |
| `Gum/CodeOutputPlugin/Manager/RenameService.cs` | Element rename to code rename |
