# fonts

```
gumcli fonts <project.gumj>
```

Scans all elements and states for font references and generates any missing bitmap font files (`.fnt` + `.png`) in the project's `FontCache/` folder.

The backend used to bake fonts is chosen by the project's **Font Generator** setting (see [Project Properties](../gum-tool/project-properties.md#font-generator)):

* **KernSmith** — cross-platform. Runs on Windows, Linux, and macOS. This is the default for new projects.
* **BMFont** — runs `bmfont.exe`, which only exists on Windows. On Linux and macOS, `gumcli fonts` generates a BMFont project's fonts with KernSmith instead.

{% hint style="info" %}
The two generators are different programs, so a BMFont project's `FontCache` built on Linux or macOS may not match one built on Windows. To get the same files everywhere, switch the project's Font Generator to **KernSmith**. Switching wipes and re-creates the FontCache, so review the [Font Generator section of Project Properties](../gum-tool/project-properties.md#font-generator) first.
{% endhint %}

## Options

- `<project.gumj>` — Path to the `.gumj` or `.gumx` project file

## Examples

```
gumcli fonts MyProject/MyProject.gumj
```

## Notes

- Scans all elements and states for Font + FontSize variable pairs
- Skips fonts whose output files already exist in `FontCache/` — only missing files are generated
- Output files are written to `FontCache/` next to the `.gumj` file

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | All missing fonts generated successfully |
| 1 | An error occurred during font generation |
| 2 | Project could not be loaded |
