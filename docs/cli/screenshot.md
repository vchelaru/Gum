# screenshot

```
gumcli screenshot <project.gumj> <element> [--output <path>] [--width <px>] [--height <px>] [--backend <name>] [--background <hex>] [--scale <n>] [--set <Name=Value>]...
```

Renders a Gum Screen or Component to a PNG file. The element is laid out and drawn using the same backend a shipped game would use, so the output is pixel-accurate and suitable for visual regression testing, documentation screenshots, or asset pipelines.

## Arguments

- `<project.gumj>`: Path to the `.gumj` or `.gumx` project file.
- `<element>`: Name of the Screen or Component to render (for example, `MainMenu` or `Controls/Button`).

## Options

- `--output`: Path for the output PNG file. Defaults to `<element>.png` in the current directory.
- `--width`: Width of the output image in pixels. Defaults to the project canvas width.
- `--height`: Height of the output image in pixels. Defaults to the project canvas height.
- `--backend`: Rendering backend to use: `monogame` (default, DesktopGL), `raylib`, or `skia` (SkiaGum, for games that draw with Skia).
- `--background`: Background color to clear to before rendering, as 6-digit (`RRGGBB`) or 8-digit (`RRGGBBAA`) hex, with or without a leading `#`. Defaults to fully transparent.
- `--scale`: Whole number of 1 or more that enlarges the finished image with nearest-neighbor sampling, so pixel art stays sharp. The element lays out at the canvas size (or `--width` and `--height`) first, so `--width 256 --scale 3` writes a 768-pixel-wide image. Defaults to `1`.
- `--set`: Sets a variable for this render only, as `Name=Value`. Repeat it to set several. Use `Instance.Variable` for an instance's variable and the bare name for the rendered element's own (`Width=300`). States work the same way: `Hud.BossState=Visible` sets the `Boss` category's state on `Hud`. Values convert like [`edit`](edit.md)'s `setVariable`. Variable references that read an overridden variable update. Nothing is saved to the project.

## Examples

```
gumcli screenshot MyProject/MyProject.gumj MainMenu
gumcli screenshot MyProject/MyProject.gumj Controls/Button --output button.png
gumcli screenshot MyProject/MyProject.gumj MainMenu --width 1920 --height 1080
gumcli screenshot MyProject/MyProject.gumj MainMenu --backend raylib
gumcli screenshot MyProject/MyProject.gumj MainMenu --background 1E2A38
gumcli screenshot MyProject/MyProject.gumj GameScreen --scale 3
gumcli screenshot MyProject/MyProject.gumj GameScreen --set Hud.Score=99999 --set Hud.BossState=Visible
```

Output on success:

```
Screenshot written to: /full/path/to/MainMenu.png
```

## Notes

- Works cross-platform. The default `monogame` backend uses DesktopGL, not a Windows-specific rendering path.
- Any fonts referenced by the element must already exist in the project's `FontCache/` folder. Run [`gumcli fonts`](fonts.md) first if fonts are missing.
- Bitmap content (sprites, nine-slices, text) is rasterized into the PNG at the requested resolution.
- To compare the two backends' output for every element in a project at once, see [`diff-screenshots`](diff-screenshots.md).
- Rendering against the default transparent background can make some visual differences (a semi-transparent panel or border, for example) hard to see once the image is opened in isolation. Use `--background` to composite against an opaque color, ideally the same clear color your game actually uses, for a more representative comparison.

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Screenshot written successfully |
| 1 | Rendering failed (for example, the named element does not exist, or a `--set` names an unknown instance, variable or state, or has a value that does not convert) |
| 2 | Project file not found, `--backend` is not `monogame`, `raylib`, or `skia`, `--background` is not a valid hex color, `--scale` is not a whole number of 1 or more, or a `--set` value has no `=` |
