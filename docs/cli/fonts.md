# fonts

```
gumcli fonts <project.gumx>
```

Scans all elements and states for font references and generates any missing bitmap font files (`.fnt` + `.png`) in the project's `FontCache/` folder.

The backend used to bake fonts is chosen by the project's **Font Generator** setting (see [Project Properties](../gum-tool/project-properties.md#font-generator)):

* **KernSmith** — cross-platform. Runs on Windows, Linux, and macOS. This is the default for new projects.
* **BMFont** — Windows-only. Runs `bmfont.exe` under the hood; invoking it on Linux or macOS fails with exit code 1.

{% hint style="info" %}
If you need `gumcli fonts` to run in a Linux or macOS CI container, switch the project's Font Generator to **KernSmith** in Project Properties first. Switching wipes and re-creates the FontCache — review the [Font Generator section of Project Properties](../gum-tool/project-properties.md#font-generator) before flipping the setting.
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
| 1 | An error occurred during font generation (including running the **BMFont** backend on a non-Windows host) |
| 2 | Project could not be loaded |
