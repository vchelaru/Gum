# fonts

```
gumcli fonts <project.gumj> [--font <name> --size <n>... [--bold] [--italic] [--outline <n>]]
```

Scans all elements and states for font references and generates any missing bitmap font files (`.fnt` + `.png`) in the project's `FontCache/` folder. BBCode tags in Text values are scanned too: `[Font=...]`, `[FontSize=...]`, `[OutlineThickness=...]`, `[IsBold=...]`, `[IsItalic=...]` and `[HasDropshadow=...]` each produce the font their run uses, nested tags included, in every state.

The backend used to bake fonts is chosen by the project's **Font Generator** setting (see [Fonts](../gum-tool/fonts.md#font-generator)):

* **KernSmith** — cross-platform. Runs on Windows, Linux, and macOS. This is the default for new projects.
* **BMFont** — runs `bmfont.exe`, which only exists on Windows. On Linux and macOS, `gumcli fonts` generates a BMFont project's fonts with KernSmith instead.

{% hint style="info" %}
The two generators are different programs, so a BMFont project's `FontCache` built on Linux or macOS may not match one built on Windows. To get the same files everywhere, switch the project's Font Generator to **KernSmith**. Switching wipes and re-creates the FontCache, so review the [Font Generator section of the Fonts page](../gum-tool/fonts.md#font-generator) first.
{% endhint %}

## Options

- `<project.gumj>` — Path to the `.gumj` or `.gumx` project file
- `--font <name>` — Also generate this font, even though no element uses it: a system font family or a `.ttf`/`.otf` path relative to the project. Requires `--size`.
- `--size <n>...` — One or more sizes to generate for `--font`, for example `--size 18 24 30`.
- `--bold`, `--italic` — Generate the `--font` sizes bold and/or italic.
- `--outline <n>` — Outline thickness in pixels for the `--font` sizes. Default is 0.

Use the `--font` options for fonts the game only needs at runtime, such as the sizes [font oversampling](../code/files-and-fonts/font-oversampling.md) asks for or the bold and italic runs of BBCode text set from code. Each run of the command adds one font with one style across the listed sizes; run it again for another style or font. These options cannot ask for no smoothing or a drop shadow, so Text styles that use those (`_noSmooth`, `_ds` files) cannot be pre-baked this way.

## Examples

```
gumcli fonts MyProject/MyProject.gumj
```

Fonts the game needs at runtime but no element uses:

```
gumcli fonts MyProject/MyProject.gumj --font "Liberation Sans" --size 18 24 30
gumcli fonts MyProject/MyProject.gumj --font "Liberation Sans" --size 12 --bold
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
| 2 | Project could not be loaded, or the `--font` options are invalid (`--size` without `--font`, `--font` without `--size`, a size that is not a whole number above zero, or an outline that is not a whole number of zero or more) |
