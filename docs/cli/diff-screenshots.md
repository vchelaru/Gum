# diff-screenshots

```
gumcli diff-screenshots <project.gumx> [--output <dir>] [--tolerance <0-255>] [--proximity <px>] [--json]
```

Renders every Screen and Component in a project through both the `monogame` and `raylib` [`screenshot`](screenshot.md) backends and reports any pixel-level mismatch between the two. Use this to catch a runtime backend silently rendering a project differently than the tool's preview, across an entire project at once.

## Options

- `<project.gumx>` — Path to the `.gumx` project file
- `--output` — Directory the rendered PNGs are written to, under `A/` (MonoGame) and `B/` (raylib) subfolders. Defaults to a new temp directory (the path is always printed)
- `--tolerance` — Maximum per-channel pixel difference (0-255) still considered a color match. Defaults to `2`
- `--proximity` — How many pixels away to search for a matching color before counting a pixel as a real mismatch. Defaults to `1`
- `--json` — Output the diff as a JSON document instead of human-readable text

{% hint style="info" %}
Two different renderers never produce byte-identical antialiasing at an edge, so a strict same-coordinate pixel comparison reports mismatches even between visually identical renders. A pixel only counts as a real mismatch if no pixel within `--proximity` of it matches within `--tolerance`, absorbing a renderer's few-pixel positional jitter at edges without masking pixels that are actually wrong. Raise `--proximity` if a project's antialiasing needs more slack; raise `--tolerance` if the color drift itself (not position) is the source of noise.
{% endhint %}

## Examples

```
gumcli diff-screenshots MyProject/MyProject.gumx
gumcli diff-screenshots MyProject/MyProject.gumx --output diffs/
gumcli diff-screenshots MyProject/MyProject.gumx --tolerance 4 --proximity 2
gumcli diff-screenshots MyProject/MyProject.gumx --json
```

## Output

**Human-readable:**

```
MATCH  Controls/Button
DIFF   Elements/Dialog: 42 px mismatched (0.219%), region (100, 50)-(140, 90)
DIFF   Screens/MainMenu: pixel dimensions differ

Rendered PNGs: C:\Users\me\AppData\Local\Temp\GumScreenshotDiff_a1b2c3
HTML report:   C:\Users\me\AppData\Local\Temp\GumScreenshotDiff_a1b2c3\report.html
2 of 3 element(s) mismatched.
```

Every run also writes `report.html` into the output directory: one row per element with MonoGame's render on the left and raylib's on the right, mismatched elements listed first, so you can scroll through a whole project visually instead of opening each PNG.

**JSON output:**

```json
{
  "hasMismatch": true,
  "outputDirectory": "C:\\Users\\me\\AppData\\Local\\Temp\\GumScreenshotDiff_a1b2c3",
  "reportPath": "C:\\Users\\me\\AppData\\Local\\Temp\\GumScreenshotDiff_a1b2c3\\report.html",
  "elements": [
    {
      "element": "Controls/Button",
      "matches": true,
      "errorMessage": null,
      "monoGamePath": "...\\A\\Controls\\Button.png",
      "raylibPath": "...\\B\\Controls\\Button.png",
      "mismatchedPixelCount": 0,
      "totalPixelCount": 48000,
      "mismatchPercentage": 0.0,
      "boundingBox": null,
      "dimensionMismatch": null
    },
    {
      "element": "Elements/Dialog",
      "matches": false,
      "errorMessage": null,
      "monoGamePath": "...\\A\\Elements\\Dialog.png",
      "raylibPath": "...\\B\\Elements\\Dialog.png",
      "mismatchedPixelCount": 42,
      "totalPixelCount": 19200,
      "mismatchPercentage": 0.219,
      "boundingBox": { "minX": 100, "minY": 50, "maxX": 140, "maxY": 90 },
      "dimensionMismatch": null
    }
  ]
}
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Every element matched |
| 1 | One or more elements mismatched, or a backend failed to render an element |
| 2 | Project `.gumx` file could not be loaded |
