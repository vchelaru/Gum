# Gum CLI Reference

`gumcli` is a cross-platform .NET 8.0 command-line tool for creating, validating, and generating code for Gum UI projects — no GUI required. Use it in CI pipelines, build scripts, or editor integrations.

## Commands

| Command | Syntax | Purpose |
|---------|--------|---------|
| `new` | `gumcli new <path> [--template forms\|empty]` | Create a new Gum project at the given path. `forms` (default) includes all Forms UI controls; `empty` is a minimal blank project. |
| `check` | `gumcli check <project.gumx> [--json]` | Validate all element files (.gusx, .gutx, .gucx) that belong to the project. Outputs human-readable text or JSON. Run this after writing or editing any element file. |
| `codegen` | `gumcli codegen <project.gumx> [--element <name>...]` | Generate C# code for screens and components. Requires `ProjectCodeSettings.codsj` to exist (or be auto-detectable). |
| `codegen-init` | `gumcli codegen-init <project.gumx> [--force] [--csproj <path>]` | Auto-detect the `.csproj`, derive namespace and output library, and write `ProjectCodeSettings.codsj`. Use `--csproj` when the Gum project is not inside the game project directory. |
| `fonts` | `gumcli fonts <project.gumx>` | Generate missing bitmap font files (`.fnt` + `.png`). Windows-only. |

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Errors found or code generation blocked by validation errors |
| 2 | Load failure, bad arguments, or unsupported platform (fonts on non-Windows) |

## Codegen Flow

Before running `codegen`, run `codegen-init` once to create `ProjectCodeSettings.codsj`. This file records the output path, namespace, and target library for generated code.

- `codegen-init` walks up the directory tree from the `.gumx` file to find the nearest `.csproj`. Use `--csproj <path>` to specify it explicitly when the Gum project is stored outside the game project.
- `ProjectCodeSettings.codsj` is written next to the `.gumx` file. It stores the code project root, namespace, and output library (e.g. MonoGame).
- `codegen-init` exits with code 2 if `ProjectCodeSettings.codsj` already exists and `--force` is not passed.
- `codegen` will attempt auto-detection if `ProjectCodeSettings.codsj` is missing, and will write it before continuing. Exit code 2 only if auto-detection also fails.
- Only Screens and Components generate code; Standard Elements are intentionally excluded.
- Validation errors block code generation for the affected element. Warnings print to stderr but do not block.
- Use `--element <name>` to generate code for specific elements only.

## Font Naming Convention

Bitmap fonts follow the pattern `Font<size><name>.fnt` and `Font<size><name>_0.png`. For example, a Text element using `Font=Arial` at `FontSize=18` requires `Font18Arial.fnt` and `Font18Arial_0.png`.

Never create `.fnt` files manually. Run `gumcli fonts <project.gumx>` to generate all missing font files for the project.

## Validating AI-Written Files

After an AI agent writes or modifies any `.gusx`, `.gucx`, or `.gutx` file, always run:

```
gumcli check <project.gumx>
```

This catches issues like wrong XML tag names that `XmlSerializer` silently ignores, invalid variable references, and missing base types. See `gum-xml-format.md` for common pitfalls in AI-generated Gum XML.
