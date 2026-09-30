# codegen-init

```
gumcli codegen-init <project.gumx> [--force] [--csproj <path>]
```

Auto-configures code generation settings for a Gum project by locating the nearest `.csproj` file above the `.gumx` directory. Writes a `ProjectCodeSettings.codsj` settings file next to the `.gumx`.

{% hint style="info" %}
Most projects do not need to run `codegen-init` explicitly. The `codegen` command auto-detects settings and writes them automatically if they are missing. Run `codegen-init` when you want to review or confirm the detected configuration before running codegen.
{% endhint %}

## Options

- `<project.gumx>` — Path to the `.gumx` project file
- `--force` — Overwrite an existing `ProjectCodeSettings.codsj` without prompting
- `--csproj <path>`: Use this `.csproj` instead of searching for one. Gum records it as `CsprojPath` in the settings, so `codegen` reads this `.csproj` to detect the Gum runtime version and the C# version. Use it when the `.csproj` is not above the `.gumx`, or when a folder has several and the game is not `Assembly-CSharp.csproj` or the shortest name.

## What It Detects

- Walks up from the `.gumx` directory to find the nearest `.csproj`
- If that folder has several `.csproj` files, uses `Assembly-CSharp.csproj`, then the shortest name
- Derives `CodeProjectRoot` as a relative path from the `.gumx` directory to the `.csproj` directory
- Extracts `RootNamespace` from the `.csproj`, falling back to the `.csproj` filename (with `.`, `-`, and spaces replaced by `_`)
- Detects MonoGame or KNI package references and sets the output library accordingly. A `Raylib-cs` package reference sets `OutputLibrary` to `Raylib`.
- For a Unity project (`Assembly-CSharp.csproj` next to an `Assets` folder), sets `GeneratedCodeFolder` to `Assets/`, because Unity compiles only code under `Assets`

{% hint style="info" %}
`Raylib-cs` detection is available in the Gum July 2026 release and newer.
{% endhint %}

{% hint style="info" %}
`--csproj` recording `CsprojPath`, and the Unity `Assets/` default, are available in October 2026, or now if building Gum from source.
{% endhint %}

## Examples

```
gumcli codegen-init MyProject/MyProject.gumx
gumcli codegen-init MyProject/MyProject.gumx --force
gumcli codegen-init Assets/StreamingAssets/GumProject/GumProject.gumx --csproj Assembly-CSharp.csproj
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
