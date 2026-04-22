# svg

```
gumcli svg <project.gumx> <element> [--output <path>] [--width <px>] [--height <px>]
```

Renders a Gum Screen or Component to a vector SVG file. Useful when you need scalable output for documentation, print, or design tooling rather than a fixed-resolution bitmap.

## Arguments

- `<project.gumx>` — Path to the `.gumx` project file.
- `<element>` — Name of the Screen or Component to render.

## Options

- `--output` — Path for the output SVG file. Defaults to `<element>.svg` in the current directory.
- `--width` — Width of the output SVG in pixels. Defaults to the project canvas width.
- `--height` — Height of the output SVG in pixels. Defaults to the project canvas height.

## Examples

```
gumcli svg MyProject/MyProject.gumx MainMenu
gumcli svg MyProject/MyProject.gumx Controls/Button --output button.svg
gumcli svg MyProject/MyProject.gumx MainMenu --width 1920 --height 1080
```

Output on success:

```
SVG written to: /full/path/to/MainMenu.svg
```

## Notes

- Uses SkiaGum's `SKSvgCanvas` to produce the SVG, so shapes and text are emitted as vector elements.
- Bitmap content (sprites, textures) is embedded as base64-encoded images inside the SVG. The file is self-contained but can be significantly larger than the equivalent PNG when the element contains many bitmaps.
- If you need a rasterized PNG instead, use [`gumcli screenshot`](screenshot.md).

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | SVG written successfully |
| 1 | Export failed (for example, the named element does not exist) |
| 2 | Project file not found |
