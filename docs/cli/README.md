# GumCli

**GumCli** (`gumcli`) is a cross-platform command-line tool for working with Gum UI projects. It lets you create projects, check for errors, and generate C# code without opening the **Gum** editor.

## When to Use GumCli

Use **GumCli** when:

- You want to edit Gum project XML files by hand or with your own tooling, and need to validate or generate code without launching the editor
- You want to integrate code generation into a build pipeline (CI/CD) without committing generated code to source control
- You are building a custom tool or automation that works with Gum projects
- You are an AI agent working with Gum projects programmatically

{% hint style="info" %}
The full **Gum** tool runs on Linux via WINE if you prefer the visual editor. See the [Setup](../gum-tool/setup/README.md) page for details.
{% endhint %}

## Installation

Install **GumCli** as a .NET global tool:

```
dotnet tool install -g GumCli
```

Once installed, invoke it as `gumcli` from any terminal.

## Commands

| Command | Description |
|---------|-------------|
| `new` | Create a new Gum project |
| `check` | Validate a project for errors |
| `codegen-init` | Initialize code generation settings |
| `codegen` | Generate C# code for project elements |
| `fonts` | Generate missing bitmap font files |
| `pack` | Pack a project into a single `.gumpkg` bundle |
| `screenshot` | Render a Screen or Component to a PNG file |
| `svg` | Render a Screen or Component to an SVG file |

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Errors found (`check`) or elements blocked by errors (`codegen`) or font generation error (`fonts`) |
| 2 | Project could not be loaded, invalid arguments, settings already exist, or non-Windows platform (`fonts`) |
