# codegen-init

```
gumcli codegen-init <project.gumx> [--force]
```

Auto-configures code generation settings for a Gum project by locating the nearest `.csproj` file above the `.gumx` directory. Writes a `ProjectCodeSettings.codsj` settings file next to the `.gumx`.

{% hint style="info" %}
Most projects do not need to run `codegen-init` explicitly. The `codegen` command auto-detects settings and writes them automatically if they are missing. Run `codegen-init` when you want to review or confirm the detected configuration before running codegen.
{% endhint %}

## Options

- `<project.gumx>` — Path to the `.gumx` project file
- `--force` — Overwrite an existing `ProjectCodeSettings.codsj` without prompting

## What It Detects

- Walks up from the `.gumx` directory to find the nearest `.csproj`
- Derives `CodeProjectRoot` as a relative path from the `.gumx` directory to the `.csproj` directory
- Extracts `RootNamespace` from the `.csproj`, falling back to the `.csproj` filename (with `.`, `-`, and spaces replaced by `_`)
- Detects MonoGame or KNI package references and sets the output library accordingly

## Examples

```
gumcli codegen-init MyProject/MyProject.gumx
gumcli codegen-init MyProject/MyProject.gumx --force
```

Output on success:

```
Code generation settings initialized successfully.
  CodeProjectRoot : ../
  RootNamespace   : MyGame
  OutputLibrary   : MonoGameForms
  Settings saved to: /full/path/to/MyProject/ProjectCodeSettings.codsj
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Settings written successfully |
| 2 | `.csproj` not found, settings file already exists (without `--force`), or the project file was not found |
