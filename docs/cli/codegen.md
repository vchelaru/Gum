# codegen

```
gumcli codegen <project.gumx> [--element <name>...] [--prune]
```

Generates C# code for elements in a Gum project. Runs error checks before generating each element — elements with errors are skipped; elements with only warnings are still generated.

## Options

- `<project.gumx>` — Path to the `.gumx` project file
- `--element <name>` — Generate code only for the named element. Can be specified multiple times. Supports folder-qualified names.
- `--prune` — After generating, delete `.Generated.cs` files under `CodeProjectRoot` that no element in the project accounts for.

## Examples

```
gumcli codegen MyProject/MyProject.gumx
gumcli codegen MyProject/MyProject.gumx --element Button
gumcli codegen MyProject/MyProject.gumx --element Button --element Slider
gumcli codegen MyProject/MyProject.gumx --element Controls/Button
```

**Output on success:**

```
Generated code for 12 element(s).
```

**Output with blocked elements:**

```
error: ButtonComponent: Base type 'ButtonBase' not found.
Generated code for 11 element(s).
1 element(s) skipped due to errors.
```

## Notes

- Requires `ProjectCodeSettings.codsj` with `CodeProjectRoot` configured. If the file is missing, `codegen` attempts auto-detection first and writes it before continuing. Exit code 2 only if auto-detection also fails — in that case, run [`codegen-init`](codegen-init.md) first.
- Only Screens and Components are generated. StandardElements are excluded.
- Elements marked `NeverGenerate` in their per-element settings are silently skipped.
- When `--element` is specified, `codegen` also auto-generates code for any referenced elements whose code files do not yet exist.
- Errors go to stderr; the summary line goes to stdout.

## Pruning Orphaned Files

`--prune` runs after generation and removes generated code files that no longer have a matching element, so a deleted or renamed element does not leave a stale `.cs` file compiling into the game forever.

```
gumcli codegen MyProject/MyProject.gumx --prune
```

```
Generated code for 12 element(s).
Pruned C:\MyGame\Screens\DeletedScreen.Generated.cs
Pruned 1 orphaned generated file(s).
Orphaned custom code file (not removed): C:\MyGame\Screens\DeletedScreen.cs
```

A `.Generated.cs` file is a pure function of its element, so deleting one is lossless. Custom `.cs` files and per-element `.codsj` settings files are yours, so `--prune` lists them and leaves them in place.

Detection is conservative and never touches a file Gum did not write. It only recognizes a `.Generated.cs` file carrying the header Gum's code generation writes, and it only considers a custom `.cs` file whose `.Generated.cs` sibling is itself orphaned. Extra hand-written partials such as `MyScreen.Input.cs`, and files belonging to elements set to `NeverGenerate`, are never reported. See [Orphaned Code Files](../gum-tool/code-tab/orphaned-code-files.md) for the tool-side equivalent.

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | All eligible elements generated successfully |
| 1 | One or more elements were skipped due to errors |
| 2 | Project could not be loaded or code generation settings could not be resolved |
