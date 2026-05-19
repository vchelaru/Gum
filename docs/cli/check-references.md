# check-references

```
gumcli check-references <project.gumx> [--json] [--fix]
```

Detects (and optionally fixes) `VariableReferences` rows whose left-hand-side scalars are not materialized into the owning state's `Variables`. This inconsistent shape is most commonly produced by AI agents and hand edits that write the references row without running the propagation Gum normally performs when references are authored interactively.

When the Gum tool resolves a reference at author time, it writes the evaluated right-hand-side as a hard scalar into the same state's `Variables`. The references row and the materialized scalar are *both* persisted to disk. Files that have the row but not the scalar read as fallthrough-to-default at runtime — even though the Variables tab in the editor will appear to show the correct value once the file is opened and re-saved.

Use this command from AI agents and CI pre-commit hooks to detect (and optionally repair) these inconsistencies before they reach disk.

## Options

- `<project.gumx>` — Path to the `.gumx` project file
- `--json` — Output results as JSON instead of human-readable text
- `--fix` — Propagate references on affected states and save the modified element files

## Examples

```
gumcli check-references MyProject/MyProject.gumx
gumcli check-references MyProject/MyProject.gumx --json
gumcli check-references MyProject/MyProject.gumx --fix
```

## What it scans

`Screens` and `Components` only. `StandardElements` are intentionally skipped — the system-generated standards contain references whose evaluated values equal the type default (e.g. `Text.Tiny → IsBold = Styles.Tiny.IsBold` resolves to `false`), and Gum's save pipeline correctly elides default-valued scalars. Treating those as failures would produce noisy false positives.

## Output

**No issues (human-readable):**

```
No unpropagated references found.
```

**Issues found (human-readable):**

```
BadComponent [Default]: has VariableReferences but missing materialized scalars
StylesV2 [Hovered]: has VariableReferences but missing materialized scalars

2 element(s) with unpropagated references.
Run with --fix to propagate.
```

Each line follows the format: `<element> [<state>]: <message>`.

**JSON output:**

```json
[
  {
    "element": "BadComponent",
    "states": [ "Default" ]
  },
  {
    "element": "StylesV2",
    "states": [ "Hovered" ]
  }
]
```

**With `--fix`:**

```
fixed: BadComponent
fixed: StylesV2

2 element(s) fixed.
```

If a right-hand side cannot be evaluated (e.g. references a missing element), the corresponding state remains broken and is listed under "The following references could not be evaluated".

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | No unpropagated references (or all were fixed) |
| 1 | One or more elements still have unpropagated references |
| 2 | Project `.gumx` could not be loaded |

{% hint style="info" %}
`--fix` writes evaluated scalars into the affected element files using the same propagation the Gum tool performs at author time. Files are saved in the project's existing format (compact attribute form when the project's `Version` supports it).
{% endhint %}
