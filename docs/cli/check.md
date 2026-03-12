# check

```
gumcli check <project.gumx> [--json]
```

Loads a Gum project and reports all errors, including malformed XML in element files, missing referenced files, and semantic errors such as invalid base types and missing behavior instances.

## Options

- `<project.gumx>` — Path to the `.gumx` project file
- `--json` — Output errors as a JSON array instead of human-readable text

## Examples

```
gumcli check MyProject/MyProject.gumx
gumcli check MyProject/MyProject.gumx --json
```

## Output

**No errors (human-readable):**

```
No errors found.
```

**Errors found (human-readable):**

```
error: ButtonComponent: Base type 'ButtonBase' not found.
warning: SliderComponent: Variable 'Thumb Width' references a missing variable.

2 error(s) found.
```

Each line follows the format: `<severity>: <element>: <message>`

**JSON output:**

```json
[
  {
    "element": "ButtonComponent",
    "message": "Base type 'ButtonBase' not found.",
    "severity": "Error"
  },
  {
    "element": "SliderComponent",
    "message": "Variable 'Thumb Width' references a missing variable.",
    "severity": "Warning"
  }
]
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | No errors found |
| 1 | One or more errors found |
| 2 | Project `.gumx` file could not be loaded |

{% hint style="info" %}
Warnings are included in the output but do not cause a non-zero exit code. Only items with severity `Error` result in exit code 1.
{% endhint %}
