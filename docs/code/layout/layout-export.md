# LayoutExporter

## Introduction

The `LayoutExporter` class provides extension methods for exporting the computed layout of a Gum UI tree as JSON. All values are resolved to absolute pixel coordinates and dimensions, making the output useful for diagnostics and AI-driven layout workflows.

`LayoutExporter` is defined in the `GumRuntime` namespace and extends `GraphicalUiElement`.

## Usage

To use `LayoutExporter`, add the following using statement:

```csharp
using GumRuntime;
```

### Getting JSON as a String

The `ToLayoutJson` extension method returns the full layout tree as a JSON string:

```csharp
// Initialize
string json = root.ToLayoutJson();
```

### Writing JSON to a File

The `ExportLayoutJson` extension method writes the layout tree directly to a file:

```csharp
// Initialize
root.ExportLayoutJson("layout.json");
```

## Output Format

Each element in the JSON output includes the following properties:

| Property   | Type    | Description |
|------------|---------|-------------|
| `type`     | string  | The renderable type name (e.g. `"InvisibleRenderable"`, `"Text"`, `"Sprite"`) |
| `name`     | string  | The element's `Name`. Omitted if null or empty. |
| `x`        | number  | Absolute X position in pixels |
| `y`        | number  | Absolute Y position in pixels |
| `width`    | number  | Absolute width in pixels |
| `height`   | number  | Absolute height in pixels |
| `visible`  | boolean | Effective visibility, accounting for parent visibility |
| `text`     | string  | The text content. Only present on Text elements. |
| `children` | array   | Child elements, recursively. Omitted if the element has no children. |

{% hint style="info" %}
All position and size values are resolved absolute pixel values, not Gum-unit values. For example, an element set to `50%` of its parent's width appears as the computed pixel value (e.g. `400`).
{% endhint %}

## Example Output

The following code:

```csharp
// Initialize
ContainerRuntime root = new ContainerRuntime();
root.Name = "Root";
root.Width = 800;
root.Height = 600;
root.WidthUnits = DimensionUnitType.Absolute;
root.HeightUnits = DimensionUnitType.Absolute;

TextRuntime title = new TextRuntime();
title.Name = "TitleLabel";
title.X = 100;
title.Y = 50;
title.Width = 600;
title.Height = 40;
title.WidthUnits = DimensionUnitType.Absolute;
title.HeightUnits = DimensionUnitType.Absolute;
title.Text = "Game Over";
root.AddChild(title);

ContainerRuntime panel = new ContainerRuntime();
panel.Name = "ScorePanel";
panel.X = 150;
panel.Y = 120;
panel.Width = 500;
panel.Height = 300;
panel.WidthUnits = DimensionUnitType.Absolute;
panel.HeightUnits = DimensionUnitType.Absolute;
root.AddChild(panel);

string json = root.ToLayoutJson();
```

Produces output similar to:

```json
{
  "type": "InvisibleRenderable",
  "name": "Root",
  "x": 0,
  "y": 0,
  "width": 800,
  "height": 600,
  "visible": true,
  "children": [
    {
      "type": "Text",
      "name": "TitleLabel",
      "x": 100,
      "y": 50,
      "width": 600,
      "height": 40,
      "visible": true,
      "text": "Game Over"
    },
    {
      "type": "InvisibleRenderable",
      "name": "ScorePanel",
      "x": 150,
      "y": 120,
      "width": 500,
      "height": 300,
      "visible": true
    }
  ]
}
```

## Invisible Elements

Elements with `Visible` set to `false` are included in the output with `"visible": false`. This allows diagnostic tools and AI to reason about the full layout tree, including hidden elements.

## Intended Use Cases

- **AI-assisted layout** - An AI agent building layouts in code can call `ToLayoutJson` to inspect what it produced without visual rendering.
- **Layout debugging** - Export the layout tree to a file for offline inspection during development.
- **Automated testing** - Verify layout structure and positioning in integration tests.
