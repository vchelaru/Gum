# screenshot

```
gumcli screenshot <project.gumx> <element> [--output <path>] [--width <px>] [--height <px>] [--backend <name>]
```

Renders a Gum Screen or Component to a PNG file. The element is laid out and drawn using the same backend a shipped game would use, so the output is pixel-accurate and suitable for visual regression testing, documentation screenshots, or asset pipelines.

## Arguments

- `<project.gumx>` — Path to the `.gumx` project file.
- `<element>` — Name of the Screen or Component to render (for example, `MainMenu` or `Controls/Button`).

## Options

- `--output` — Path for the output PNG file. Defaults to `<element>.png` in the current directory.
- `--width` — Width of the output image in pixels. Defaults to the project canvas width.
- `--height` — Height of the output image in pixels. Defaults to the project canvas height.
- `--backend` — Rendering backend to use: `monogame` (default, DesktopGL) or `raylib`.

## Examples

```
gumcli screenshot MyProject/MyProject.gumx MainMenu
gumcli screenshot MyProject/MyProject.gumx Controls/Button --output button.png
gumcli screenshot MyProject/MyProject.gumx MainMenu --width 1920 --height 1080
gumcli screenshot MyProject/MyProject.gumx MainMenu --backend raylib
```

Output on success:

```
Screenshot written to: /full/path/to/MainMenu.png
```

## Notes

- Works cross-platform — the default `monogame` backend uses DesktopGL, not a Windows-specific rendering path.
- Any fonts referenced by the element must already exist in the project's `FontCache/` folder. Run [`gumcli fonts`](fonts.md) first if fonts are missing.
- Bitmap content (sprites, nine-slices, text) is rasterized into the PNG at the requested resolution.
- To compare the two backends' output for every element in a project at once, see [`diff-screenshots`](diff-screenshots.md).

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Screenshot written successfully |
| 1 | Rendering failed (for example, the named element does not exist) |
| 2 | Project file not found, or `--backend` is not `monogame` or `raylib` |
