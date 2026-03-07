# gumcli

Command-line tool for Gum UI projects. Creates projects, checks for errors, and generates C# code without the Gum editor.

## Commands

### `gumcli new <path>`

Creates a new Gum project with standard elements and folder structure.

```
gumcli new MyProject
gumcli new path/to/MyProject.gumx
```

- If `<path>` has no `.gumx` extension, creates `<path>/<name>.gumx`
- Creates subfolders: Screens, Components, Standards, Behaviors
- Writes 9 standard elements (Circle, ColoredRectangle, Component, Container, NineSlice, Polygon, Rectangle, Sprite, Text)
- Copies `ExampleSpriteFrame.png` (default NineSlice texture)

### `gumcli check <project.gumx> [--json]`

Loads a project and reports errors.

```
gumcli check MyProject.gumx
gumcli check MyProject.gumx --json
```

- Human-readable output by default
- `--json` outputs a JSON array of `{ element, message, severity }` objects
- Exit code `0` = no errors, `1` = errors found, `2` = load failure

### `gumcli codegen <project.gumx> [--element <name>...]`

Generates C# code for elements in a Gum project.

```
gumcli codegen MyProject.gumx
gumcli codegen MyProject.gumx --element Button
gumcli codegen MyProject.gumx --element Button --element Slider
```

- Requires `ProjectCodeSettings.codsj` with `CodeProjectRoot` configured
- Without `--element`, generates all elements not set to `NeverGenerate`
- `--element` filters to specific elements (case-insensitive, supports folder-qualified names like `Controls/Button`)
- Runs error checks before generating each element; errors block generation for that element
- Warnings are printed to stderr but do not block generation
- Exit code `0` = success, `1` = elements blocked by errors, `2` = load failure or missing configuration

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Errors found (check) or elements blocked (codegen) |
| 2 | Project could not be loaded or invalid arguments |
