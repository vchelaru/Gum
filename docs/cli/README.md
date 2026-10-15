# GumCli

**GumCli** (`gumcli`) is a cross-platform command-line tool for working with Gum UI projects. It lets you create projects, check for errors, and generate C# code without opening the **Gum** editor.

## When to Use GumCli

Use **GumCli** when:

- You want to edit Gum project XML files by hand or with your own tooling, and need to validate or generate code without launching the editor
- You want to integrate code generation into a build pipeline (CI/CD) without committing generated code to source control
- You are building a custom tool or automation that works with Gum projects
- You are an AI agent working with Gum projects programmatically

{% hint style="info" %}
The full **Gum** tool runs natively on Windows, macOS, and Linux if you prefer the visual editor. See the [Setup](../gum-tool/setup/README.md) page for details.
{% endhint %}

## Installation

Install **GumCli** as a .NET global tool:

```
dotnet tool install -g GumCli
```

Once installed, invoke it as `gumcli` from any terminal.

To install a preview version, which includes changes that have not reached a stable release yet, add `--prerelease`:

```
dotnet tool install -g GumCli --prerelease
```

## Version

```
gumcli --version
```

Every command also prints `gumcli v<version>` to stderr, so the version appears at the top of any run's output. Compare it to the latest on [NuGet](https://www.nuget.org/packages/GumCli).

## Updating

`gumcli` is not updated when you update the Gum NuGet packages in your game. Update it separately:

```
dotnet tool update -g GumCli
```

To update to the latest preview version, add `--prerelease`:

```
dotnet tool update -g GumCli --prerelease
```

Update whenever you update the Gum runtime packages. Generated code can target APIs a stale `gumcli` doesn't know about.

If you run a preview version of the **Gum** tool, use the `gumcli` that matches it. Each **Gum** tool release includes a `GumCli` folder built from the same commit, so its output matches the tool.

## Commands

| Command | Description |
|---------|-------------|
| `new` | Create a new Gum project |
| `add-forms` | Add the Forms controls to an existing project |
| `check` | Validate a project for errors |
| `check-references` | Detect (and optionally fix) `VariableReferences` rows with un-materialized scalars |
| `convert-to-json` | Write a JSON copy of an XML project and its element files |
| `diff-standards` | Report how a project's Standard elements differ from the defaults |
| `import-screen` | Import a Screen file into an existing project |
| `resave` | Load and save a project with no edits to check round trips |
| `stage-forms-behaviors` | Write a Forms theme's resolved behaviors into one folder |
| `codegen-init` | Initialize code generation settings |
| `codegen` | Generate C# code for project elements |
| `fonts` | Generate missing bitmap font files |
| `pack` | Pack a project into a single `.gumpkg` bundle |
| `screenshot` | Render a Screen or Component to a PNG file |
| `diff-screenshots` | Render every Screen and Component via MonoGame and raylib and report mismatches |
| `svg` | Render a Screen or Component to an SVG file |

## Output Streams

`gumcli` writes machine-readable command output (JSON, file paths, summaries) to **stdout**, and informational chrome (the version banner, progress messages, error details) to **stderr**. This split means automation can consume stdout directly without filtering:

```
gumcli check MyProject/MyProject.gumj --json | jq .
```

When debugging, capture both streams with `2>&1`.

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Errors found (`check`) or elements blocked by errors (`codegen`) or font generation error (`fonts`) |
| 2 | Project could not be loaded, invalid arguments, or settings already exist |
