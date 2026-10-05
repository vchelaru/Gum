# check

```
gumcli check <project.gumj> [--json]
```

Loads a Gum project and reports all errors, including malformed XML in element files, missing referenced files, and semantic errors such as invalid base types and missing behavior instances.

## Options

- `<project.gumj>` — Path to the `.gumj` or `.gumx` project file
- `--json` — Output errors as a JSON array instead of human-readable text

## Examples

```
gumcli check MyProject/MyProject.gumj
gumcli check MyProject/MyProject.gumj --json
```

## Output

**No errors (human-readable):**

```
No errors found.
```

**Errors found (human-readable):**

```
error: ButtonComponent: ButtonComponent has a base type of ButtonBase which does not exist
warning: SliderComponent: SliderComponent references "Textures/Thumb.png", which was not found on disk.

1 error(s), 1 warning(s) found.
```

Each line follows the format: `<severity>: <element>: <message>`

**JSON output:**

```json
[
  {
    "element": "ButtonComponent",
    "message": "ButtonComponent has a base type of ButtonBase which does not exist",
    "severity": "Error",
    "code": null
  },
  {
    "element": "SliderComponent",
    "message": "SliderComponent references \"Textures/Thumb.png\", which was not found on disk.",
    "severity": "Warning",
    "code": "GUM0006"
  }
]
```

`code` is the error's stable code, such as `GUM0006`. Match on `code` rather than `message` in scripts, because message wording can change between releases. Errors that have no code yet report `null`.

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | No errors found |
| 1 | One or more errors found |
| 2 | Project file could not be loaded |

{% hint style="info" %}
Warnings are included in the output but do not cause a non-zero exit code. Only items with severity `Error` result in exit code 1.
{% endhint %}
