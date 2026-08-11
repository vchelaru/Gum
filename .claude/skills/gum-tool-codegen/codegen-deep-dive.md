# Code Generation for Agents

Gum can generate strongly-typed C# from a project's Screens and Components, so game code refers to UI
by name and type instead of by magic string. This page explains the whole system: what gets written,
where it goes, the two instantiation models, how the two halves of each partial class relate, and what
happens to files on disk when an element is renamed, moved, or deleted.

Written for an agent working on a Gum project or on Gum itself. For running generation from the
command line, see [GumCli for Agents](gumcli-for-agents.md) and [`codegen`](../cli/codegen.md).

## The core model: two files per element

Every generated element produces a **pair of files** that form one `partial class`:

| File | Owner | Rule |
|---|---|---|
| `MyScreen.Generated.cs` | Gum | Regenerated from the element on every generation. Never hand-edit. Deleting it is lossless. |
| `MyScreen.cs` | You | Written once as a stub, then never touched again by Gum except to rewrite its `namespace` and class declaration on a rename. |

The generated half declares the fields, states, and construction logic. The custom half exists purely
to hold your code, and starts as a stub containing a single empty `partial void CustomInitialize()`.

The generated constructor calls `CustomInitialize()`, which is how your code runs. If you delete the
method from the custom file, the `partial void` simply compiles away to nothing.

Standard Elements (`Text`, `ColoredRectangle`, `Sprite`, and so on) are **never** generated as element
pairs. See [Standard Elements fallback file](#standard-elements-fallback-file) for the one file that
does concern them.

### Why this split matters

`.Generated.cs` is derived data: a pure function of the element plus the settings. Regenerating
recreates it byte-identical, so anything in the tool may delete it freely.

`MyScreen.cs` is unrecoverable. Gum's undo covers project state, not files on disk, so deleting an
element and pressing undo restores the element but not the file. Every code path in Gum treats these
two files asymmetrically for this reason, and any change you make to Gum must preserve that.

## Where files are placed

### The output root

`CodeProjectRoot` (a project-level setting, relative to the `.gumx`) is the folder everything is
written under. It normally points at the game's `.csproj` folder. When it is empty, generation is
effectively disabled: file-path resolution returns null and the tool silently skips writing.

### Folder layout

Within `CodeProjectRoot`, an element's path mirrors its type and its folder inside the Gum project:

```
<CodeProjectRoot>/
  Screens/
    MainMenu.Generated.cs
    MainMenu.cs
  Components/
    Buttons/                     <- folders inside the Gum project are preserved
      IconButton.Generated.cs
      IconButton.cs
  StandardElements.Generated.cs  <- project-wide, not under a subfolder
```

The `Screens` / `Components` / `Standards` prefix comes from the element's type, not from where the
`.gucx` sits on disk.

A per-element `GeneratedFileName` setting overrides this entirely. When set, that path is used verbatim
(resolved against the Gum project directory if relative), and the folder convention above does not
apply.

### Custom file path is derived, never stored

The custom file's path is always computed from the generated file's: take the generated path and
replace the `.Generated.cs` suffix with `.cs`. There is no separate setting. Anything that changes
where the generated file lands moves the custom file with it.

### Namespace derivation

If the element has a per-element `Namespace` override, it wins outright. Otherwise, when
`RootNamespace` is set:

```
<RootNamespace>.<Screens|Components|Standards>[.<folder path inside the Gum project>]
```

The trailing folder segment is appended only when `AppendFolderToNamespace` is true (the default for
new projects). So a component named `Buttons/IconButton` with root namespace `MyGame` becomes
`MyGame.Components.Buttons`.

When `RootNamespace` is empty, generation emits **no namespace at all**. This has a consequence worth
knowing: the rename logic will not rewrite a `namespace` line in your custom file in that
configuration, because there is no generated namespace to match it to, so a hand-written one is left
alone.

## Project settings versus element settings

Two `.codsj` files (JSON despite the extension) drive everything.

**`ProjectCodeSettings.codsj`**, alongside the `.gumx`:

| Setting | Effect |
|---|---|
| `OutputLibrary` | Which runtime to target. Drives almost every shape decision below. |
| `CodeProjectRoot` | Output folder, relative to the `.gumx`. |
| `RootNamespace` | Namespace root. Empty means no namespace is emitted. |
| `AppendFolderToNamespace` | Append the element's Gum folder path to its namespace. |
| `ObjectInstantiationType` | `FullyInCode` or `FindByName`. See below. |
| `InheritanceLocation` | Whether the base class is declared in the generated or the custom file. |
| `DefaultScreenBase` | Optional base class for Screens. Empty means the generator picks a library-appropriate default. |
| `CommonUsingStatements` | Usings prepended to every file, generated and custom. |
| `BaseTypesNotCodeGenerated` | Base types to skip. |
| `AdjustPixelValuesForDensity` | Scale emitted pixel values by display density. |
| `GenerateGumDataTypes` | Emit Gum data types alongside the UI classes. |
| `SyntaxVersion` | `"*"` auto-detects the referenced runtime's syntax version. A number pins it. |
| `Version` | Schema version of the file itself, used for migrations on load. |

**`<ElementName>.codsj`**, alongside the element's `.gucx` / `.gusx`, not in the code folder:

| Setting | Effect |
|---|---|
| `GenerationBehavior` | `NeverGenerate`, `GenerateManually`, or `GenerateAutomaticallyOnPropertyChange`. |
| `Namespace` | Overrides the derived namespace for this element. |
| `UsingStatements` | Extra usings for this element only. |
| `GeneratedFileName` | Overrides the output path for this element. |
| `LocalizeElement` | Emit an `ApplyLocalization()` pass for this element. |

`NeverGenerate` means the element is hand-managed. Nothing generates it, and the file-reconciliation
paths described later deliberately leave its files alone.

## The two instantiation models

`ObjectInstantiationType` is the single biggest fork in what gets generated. Both models produce the
same class surface, so calling code looks identical either way. They differ in where the objects come
from.

### FullyInCode

The generated code **creates** the visual tree. It emits instantiation for every instance, parents
them, and applies every default variable:

* `InitializeInstances()` news up each instance.
* `AddToParents()` builds the hierarchy.
* `ApplyDefaultVariables()` writes every variable value the element defines.

Use this when the C# is the source of truth at runtime and no `.gumx` ships with the game.

### FindByName

The generated code **binds to** a visual tree that something else already created, normally by loading
the Gum project at runtime. Instead of instantiation, each instance field is assigned by looking it up:

```csharp
// In generated code, roughly
myButton = Gum.Forms.GraphicalUiElementFormsExtensions
    .TryGetFrameworkElementByName<ButtonRuntime>(this.Visual, "myButton");
```

`AddToParents()` and `ApplyDefaultVariables()` are **not generated at all** in this mode, because the
loaded project already carries that structure and those values. `InitializeInstances()` is still
generated in both modes, and in `FindByName` it is where the lookups live and where `CustomInitialize()`
is called from.

This is the mode to reach for when the game loads `.gumx` content at runtime and you want typed
accessors over it.

{% hint style="warning" %}
`OutputLibrary.Raylib` currently supports `FindByName` only. The headless CLI throws
`NotSupportedException` and exits 1 on `Raylib` + `FullyInCode`; the interactive tool quietly snaps the
setting back to `FindByName` instead.
{% endhint %}

## Output libraries

`OutputLibrary` selects the target runtime: `MonoGame`, `MonoGameForms`, `Raylib`, `Skia`, `Silk`,
`WPF`, `XamarinForms`, `Maui`. **MonoGameForms is the recommended default** for new projects; plain
`MonoGame` exists for legacy and specialized cases.

Two consequences worth knowing:

* **MonoGameForms wraps visuals.** Property access on a Forms object goes through `.Visual`
  (`this.Visual`, `this.MyInstance.Visual`). The generator treats anything that is not a Standard
  Element as a Forms object.
* **Forms base types come from behaviors.** For MonoGameForms, the generated base class is chosen by
  scanning the element's behaviors, so a component carrying `ButtonBehavior` generates as a `Button`.
  Screen inheritance resolves in the order `element.BaseType`, then `DefaultScreenBase`, then a
  library-appropriate fallback (`FrameworkElement` for MonoGameForms, `GraphicalUiElement` otherwise).

`MonoGame` and `Raylib` emit an identical shape, because the underlying runtime API is unified across
them. Code that needs to branch on this uses a shared predicate rather than testing the two values
separately.

## Anatomy of a generated file

The generated file is written in a fixed order:

1. First line: a `//Code for <element>` header. This is the marker that proves Gum wrote the file, and
   the orphan scan relies on it.
2. Using statements, auto-detected from the instances plus `CommonUsingStatements`.
3. Namespace, if one is derived.
4. `partial class`, with the base class if `InheritanceLocation` is `InGeneratedCode`.
5. One enum per state category.
6. State properties that call `ApplyState()`.
7. Instance fields.
8. Custom variables (user-defined properties on the element).
9. Exposed variables, which delegate to a child instance's variable.
10. Constructor, calling `InitializeInstances()`, then in `FullyInCode` also `AddToParents()` and
    `ApplyDefaultVariables()`.
11. `ApplyState()` methods.
12. `ApplyLocalization()`, when `LocalizeElement` is set.

State code is deliberately **not** generated when the output library is MonoGameForms and the state
container is a Standard Element; the Forms framework handles those itself.

## Anatomy of the custom file

The stub is intentionally minimal:

```csharp
using Gum.Converters;
using Gum.DataTypes;
// ...CommonUsingStatements, then any element-level UsingStatements

namespace MyGame.Components
{
    partial class IconButton
    {
        partial void CustomInitialize()
        {

        }
    }
}
```

Two details matter if you are editing Gum itself:

* The class declaration carries **no access modifier**, on purpose, so you can add one without the
  generator fighting you.
* The base class appears here only when `InheritanceLocation` is `InCustomCode`. In the default
  `InGeneratedCode` mode the generated half declares it and the custom half must not.

Everything else in the file is yours. Add fields, methods, event handlers, additional interfaces.

### Extra hand-written partials

Nothing stops you adding `IconButton.Input.cs` as a third partial. Gum has **no knowledge of these
files**. They are not moved when the element is renamed, and they are never reported as orphans when
the element is deleted. That is a deliberate limit, not an oversight: Gum only touches files it can
prove it wrote. Clean them up yourself.

### Standard Elements fallback file

`StandardElements.Generated.cs` is written directly under `CodeProjectRoot`, with no subfolder. It
registers Standard-Element-owned category and state assignments so they still work in a code-only game.
It belongs to the project rather than to any element, so no reconciliation path ever treats it as
orphaned.

## When generation runs

| Trigger | Path |
|---|---|
| Code tab button in the tool | Manual generation for the selected element or the whole project. |
| Any edit to an element set to `GenerateAutomaticallyOnPropertyChange` | The tool's code output plugin listens to nearly every edit event and regenerates. |
| `gumcli codegen <project.gumx>` | Headless generation, per-element error checks gate each element. |
| `RequestCodeGenerationMessage` | Sent by external integrations such as the FlatRedBall editor. |

Automatic regeneration must never show a dialog. The established mechanism is a `showPopups: false`
argument threaded through the generation call. If you add a prompt to any generation path, check that
flag or you will spawn a dialog on every keystroke.

Generation also notices when an element it depends on has no code file yet, and offers to generate the
missing dependency too. In automatic mode it does so silently.

## File reconciliation: rename, move, delete

Generated code lives outside the `.gumx`, so anything that changes an element's identity has to
reconcile the files on disk. Identity here is the tuple of name, folder, and base type, and deletion is
the case where the new identity is nothing. SDK-style `.csproj` files glob every `.cs` under the
project, so a stale file keeps compiling forever with no warning. That is the failure this whole area
exists to prevent.

### Rename, folder move, and base type change

All three funnel through the same path, `RenameService`:

1. Delete the old `.Generated.cs`. Lossless, so no prompt.
2. Move the custom `.cs` to its new path, creating the destination directory if needed. If a file is
   already sitting at the target, you are asked before it is displaced, and the displaced file goes to
   the Recycle Bin rather than being destroyed.
3. Rewrite the custom file's `namespace` line and its `partial class` declaration to match the new
   identity. A folder move changes the namespace when `AppendFolderToNamespace` is on, and a custom
   file left with the old namespace silently stops being the same partial class, so `CustomInitialize`
   never runs.
4. Move the element's `.codsj` alongside it, so per-element settings survive.
5. Regenerate this element and everything that references it.

A case-only rename (`Foo` to `foo`) moves through a temporary name, because the filesystem is
case-insensitive on Windows and macOS while git is not.

### Delete

The delete dialog decides per file, not all-or-nothing:

* `.Generated.cs` and `.codsj` are removed unconditionally, with no prompt. Both are derived.
* The custom `.cs` is a separate, unchecked checkbox, and it only appears when the file actually holds
  code. An untouched stub is removed silently.
* Everything removed goes to the Recycle Bin, never a hard delete.
* A multi-element or whole-folder delete shows **one** dialog for the batch, not one per element.

"Holds code" is decided structurally: strip usings, namespace, class header, comments and whitespace,
and if all that remains is an empty-bodied `CustomInitialize`, the file is an untouched stub. Whitespace
and comments deliberately do not count as user code. The generated stub itself carries a whitespace-only
line inside `CustomInitialize`, so treating whitespace as an edit would flag every pristine file.

### Orphan scan

Files still orphan without passing through the delete dialog: you decline the prompt, a `.gucx` is
deleted in Explorer or by a branch switch, `CodeProjectRoot` was empty at the time, or the project was
last edited by an older Gum version.

The scan runs on project load and on demand from **Content > Scan for Orphaned Code Files**, and
reports each finding as a **GUM0005** row in the Errors tab with a **Delete File** action.

How it decides something is an orphan, and the limits that follow:

* It enumerates `*.Generated.cs` only. A hand-written partial is never even looked at.
* A generated file counts as Gum's only if its first line carries the `//Code for` header. Files
  written by other tools, or by a Gum old enough to predate the header, are invisible to it.
* A custom `.cs` is flagged only when its generated sibling is itself a proven orphan. It is never
  judged on its own content. One consequence: a custom file you chose to keep at delete time can never
  be found later, because its anchor is gone.
* `bin` and `obj` are skipped, `NeverGenerate` elements are left alone, and an element whose source
  file is merely missing keeps its code files off the list.

### Command line

`gumcli codegen <project.gumx> --prune` regenerates and then removes generated files with no matching
element. It only ever deletes `.Generated.cs`; orphaned custom and `.codsj` files are listed for you to
decide on. For a project under source control this is usually the better tool than deleting during
editing, because it is explicit, batched, and lands as a reviewable diff.

## Invariants to preserve

If you are changing Gum's code generation, these are the rules the existing code follows:

1. **Never destroy a custom `.cs` without consent, and never with a plain `File.Delete`.** Use the
   Recycle Bin path.
2. **`.Generated.cs` may be deleted freely.** It is derived data.
3. **Never prompt during automatic regeneration.** Honor `showPopups: false`.
4. **Leave `NeverGenerate` elements alone** in every reconciliation path.
5. **Fail toward an unnecessary prompt, never toward silent deletion.** Anything the stub detector
   cannot account for must read as "not a stub".
6. **Only touch files Gum can prove it wrote.** The `//Code for` header is that proof.

## Key files

| File | Purpose |
|---|---|
| `Tools/Gum.ProjectServices/CodeGeneration/CodeGenerator.cs` | The engine. Large. |
| `Tools/Gum.ProjectServices/CodeGeneration/CustomCodeGenerator.cs` | The user-editable stub. |
| `Tools/Gum.ProjectServices/CodeGeneration/CodeGenerationFileLocationsService.cs` | Output path resolution. |
| `Tools/Gum.ProjectServices/CodeGeneration/CodeOutputProjectSettings.cs` | Project settings and the enums. |
| `Tools/Gum.ProjectServices/CodeGeneration/CodeOutputElementSettings.cs` | Per-element settings. |
| `Tools/Gum.ProjectServices/CodeGeneration/CustomCodeStubDetector.cs` | Untouched-stub detection. |
| `Tools/Gum.ProjectServices/CodeGeneration/OrphanCodeFileScanService.cs` | Orphan detection. |
| `Tools/Gum.ProjectServices/CodeGeneration/CodeGenerationNameVerifier.cs` | C# name compliance. |
| `Tools/Gum.Presentation/CodeOutputPlugin/Manager/CodeGenerationService.cs` | Generation orchestration. |
| `Tools/Gum.Presentation/CodeOutputPlugin/Manager/RenameService.cs` | Rename, move, base type change. |
| `Tools/Gum.Presentation/CodeOutputPlugin/Manager/CodeFileDeleteService.cs` | Delete decisions. |
| `Gum/CodeOutputPlugin/MainCodeOutputPlugin.cs` | Tool UI entry point. |
| `Tools/Gum.Cli/Commands/CodegenCommand.cs` | The CLI command, including `--prune`. |

## Gotchas

* **Names are made C#-safe automatically.** A keyword gets an `@` prefix, a leading digit gets `_`, and
  spaces become `_`. An element named `class` in Gum generates as `@class`.
* **Certain variables are excluded per output library.** Exclusion runs through a query event, so a
  variable visible in the tool may legitimately not appear in generated code.
* **Syntax version gates emitted code.** `"*"` auto-detects from the referenced Gum runtime assembly.
  Pinning it wrong produces code that does not compile against the runtime actually referenced.
* **The `ObjectFinder` cache is toggled around generation loops** for performance, and is managed by the
  caller rather than by the generator.
