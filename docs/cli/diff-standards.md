# diff-standards

```
gumcli diff-standards <project.gumj> [--json]
```

Compares the project's Standard elements against the defaults Gum uses when it creates a new project, and reports every variable that differs. Theme authors and CI use it to keep a theme's Standards matching the defaults.

The command compares the Standards that exist in both places. Standards that only the project has are listed but not compared.

## Options

- `<project.gumj>`: Path to the `.gumj` or `.gumx` project file
- `--json`: Output the drift as a JSON document instead of human-readable text

## Examples

```
gumcli diff-standards MyProject/MyProject.gumj
gumcli diff-standards MyProject/MyProject.gumj --json
```

## Output

**No drift (human-readable):**

```
No drift found.
```

**Drift found (human-readable):**

```
Text.gutx:
  Width: 100 → 120
  Hovered · Red: (absent in Default) → 255

Default Standards missing from project:
  Polygon

2 variable difference(s) across 1 Standard(s).
```

Each line names the variable, preceded by the state name when the difference is not in the `Default` state. A difference is one of three kinds: the value changed, the project added a variable the defaults do not have, or the project removed a variable the defaults have.

**JSON output:**

```json
{
  "hasDrift": true,
  "differences": [
    {
      "standard": "Text",
      "state": "Default",
      "variable": "Width",
      "kind": "Changed",
      "defaultValue": "100",
      "projectValue": "120"
    }
  ],
  "missingFromProject": [ "Polygon" ],
  "projectOnlyStandards": []
}
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | No drift |
| 1 | Drift found |
| 2 | Project file not found or could not be loaded |
