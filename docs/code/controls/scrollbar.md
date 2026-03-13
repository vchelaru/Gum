# ScrollBar

## Introduction

ScrollBar is a Forms control that lets the user select a numeric value by interacting with a draggable thumb, up/down buttons, and a track. It is most commonly used internally by [ScrollViewer](scrollviewer/README.md), which creates and manages a ScrollBar automatically. Use ScrollBar directly when you need a standalone scrollbar that is not tied to a ScrollViewer — for example, to control a custom camera offset or to expose a scrollable range in a data-driven panel.

If you want a scrollable container that manages content layout for you, use [ScrollViewer](scrollviewer/README.md) instead.

<figure><img src="../../.gitbook/assets/image (202).png" alt=""><figcaption><p>ScrollBar control</p></figcaption></figure>

## Key Properties

- **`Value`** — The current numeric value. Always clamped between `Minimum` and `Maximum`. Set this in code to scroll the bar programmatically.
- **`Minimum`** — The lower bound of the range (inclusive). Defaults to `0`.
- **`Maximum`** — The upper bound of the range (inclusive). Defaults to `100`.
- **`SmallChange`** — The amount `Value` changes when the user clicks the up or down arrow buttons, or scrolls the mouse wheel over the scrollbar.
- **`LargeChange`** — The amount `Value` changes when the user clicks on the track (the area between the thumb and the arrow buttons).

## Events

- **`ValueChanged`** — Raised whenever `Value` changes, regardless of whether the change came from the UI or from code.
- **`ValueChangeCompleted`** — Raised when a discrete value change finishes (e.g. after releasing the thumb or clicking an arrow button). Use this to avoid reacting every frame during a drag.

## Code Example: Creating a Standalone ScrollBar

The following code creates a vertical ScrollBar, configures its range and change amounts, and subscribes to `ValueChanged` to react whenever the value is updated.

```csharp
// Initialize
var scrollBar = new ScrollBar();
scrollBar.AddToRoot();
scrollBar.X = 50;
scrollBar.Y = 50;
scrollBar.Width = 24;
scrollBar.Height = 200;
scrollBar.Minimum = 0;
scrollBar.Maximum = 100;
scrollBar.SmallChange = 5;
scrollBar.LargeChange = 20;
scrollBar.Value = 0;
scrollBar.ValueChanged += (_, _) =>
    System.Diagnostics.Debug.WriteLine($"ScrollBar value: {scrollBar.Value}");
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACm3QW0vDMBgG4PtC_8NH8KLFUebQm40K6kCFeWPFKRRGXD_SD3KANJmHsf9u6qGQaq6S582bhOzTBIDddtdesTk463HyJaTJEZf0gYHZjlvottZIeRlmJWh8hep3neWLWg9pcdE0D-beGDfyp9A7m0b0_JfW1Lg28Ow04hsk0brep_H-u_BQ5VVIRs7ffvxk1KgUl_Kq5Vpgf32UrbgVOGSzuPjIpe_5H_2uNHBcQraZwCaH8rzWEEb13jlUxZK40KZztO2KJb54UawtOVyRxuyoZsNfwq4_bg770QWHmuULliaHNPkE38k-YLIBAAA" target="_blank">Try on XnaFiddle.NET</a>

## ScrollBar and ScrollViewer

In most cases you do not need to create a ScrollBar directly. [ScrollViewer](scrollviewer/README.md) instantiates and manages one internally, and exposes `VerticalScrollBarValue` to read or set the scroll position in code. Use a standalone ScrollBar only when you need fine-grained control over a scrollable range that is not tied to a ScrollViewer's content panel.
