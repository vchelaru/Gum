# fonts

```
gumcli fonts <project.gumx>
```

Scans all elements and states for font references and generates any missing bitmap font files (`.fnt` + `.png`) in the project's `FontCache/` folder.

{% hint style="warning" %}
The `fonts` command is Windows-only. It requires `bmfont.exe`, which is a Windows application. Running this command on Linux or macOS exits immediately with code 2. Support for other platforms may be added in the future.
{% endhint %}

## Options

- `<project.gumx>` — Path to the `.gumx` project file

## Examples

```
gumcli fonts MyProject/MyProject.gumx
```

## Notes

- Scans all elements and states for Font + FontSize variable pairs
- Skips fonts whose output files already exist in `FontCache/` — only missing files are generated
- Output files are written to `FontCache/` next to the `.gumx` file

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | All missing fonts generated successfully |
| 1 | An error occurred during font generation |
| 2 | Project could not be loaded or the platform is not Windows |
