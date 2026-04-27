# Sticky Headers

## Introduction

A sticky header is a header item inside a `ScrollViewer` that scrolls with the content until it reaches the top of the visible area, at which point it pins to the top until the next sticky header arrives and bumps it off. This is the same behavior used in iOS sectioned tables and CSS `position: sticky`, and it is a common pattern for sectioned lists, settings screens, and chat-style timelines.

Sticky headers are supported directly on `ScrollViewer`, which means they also work for any control that derives from `ScrollViewer`, including `ItemsControl` and `ListBox`.

## Registering a Sticky Header

To make a child a sticky header, add it to the `ScrollViewer` as you normally would, then call `RegisterStickyHeader`:

```csharp
scrollViewer.AddChild(header);
scrollViewer.RegisterStickyHeader(header);
```

The `ScrollViewer` takes care of the rest. Internally it pulls the header out of the scrollable stack into an overlay layer, drops in a same-height placeholder so the surrounding items don't shift, and tracks scroll position to pin or bump the header as needed. If the header's height changes later (because its text or contents change), the placeholder is updated automatically — you don't need to re-register.

`RegisterStickyHeader` accepts both Forms controls (`FrameworkElement`) and raw visuals (`GraphicalUiElement`). The header must already be a child of the `ScrollViewer` before you register it; otherwise an `ArgumentException` is thrown.

## Code Example - Sectioned List with Sticky Headers

The following code creates a `ScrollViewer` containing three sections. Each section's header is a `Panel` (a Forms control) with a colored background and a `Label`. As the user scrolls, the headers pin to the top of the viewport in turn.

```csharp
//initialize

var scrollViewer = new ScrollViewer();
scrollViewer.AddToRoot();
scrollViewer.Anchor(Anchor.Center);
scrollViewer.Width = 300;
scrollViewer.Height = 400;

string[] sectionNames = { "Vegetables", "Fruits", "Grains" };

foreach (var name in sectionNames)
{
    // Build the header. Panels default to sizing to their children, so dock
    // to fill horizontally and set a fixed height.
    var header = new Panel();
    header.Dock(Gum.Wireframe.Dock.FillHorizontally);
    header.Height = 30;
    header.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

    var headerBackground = new ColoredRectangleRuntime();
    headerBackground.Dock(Gum.Wireframe.Dock.Fill);
    headerBackground.Color = Color.DarkSlateBlue;
    header.AddChild(headerBackground);

    var headerLabel = new Label();
    headerLabel.Text = name;
    headerLabel.X = 8;
    header.AddChild(headerLabel);

    // Add the header to the scroll viewer in its natural place in the stack,
    // then mark it as sticky.
    scrollViewer.AddChild(header);
    scrollViewer.RegisterStickyHeader(header);

    // Normal items follow.
    for (int i = 0; i < 8; i++)
    {
        var label = new Label();
        label.Text = $"{name} item {i + 1}";
        scrollViewer.AddChild(label);
    }
}
```

<figure><img src="../../../.gitbook/assets/sticky-headers.gif" alt=""><figcaption><p>Sticky headers pin to the top of the viewport as the user scrolls</p></figcaption></figure>

## Unregistering and Clearing

`UnregisterStickyHeader` removes a single registration; `ClearStickyHeaders` removes all of them. In both cases, each header is moved back to its placeholder's slot in the stack and the placeholder is destroyed — undoing exactly what `RegisterStickyHeader` did.

```csharp
scrollViewer.UnregisterStickyHeader(header);
scrollViewer.ClearStickyHeaders();
```

If you want to remove the header from the screen entirely, call `scrollViewer.RemoveChild(header)` after unregistering.

## Notes and Limitations

* Sticky headers pin vertically. Horizontal scrolling is supported underneath them — when the user scrolls horizontally, the header stays locked to the top of the viewport rather than scrolling sideways with the content, which matches iOS-style behavior.
* Headers are pinned in the order their placeholders appear in the stack, not the order they were registered, so registration order does not matter.
* If the underlying Visual does not provide a `StickyHeaderOverlayInstance` container (for example, a fully custom Visual), `RegisterStickyHeader` becomes a no-op. Use the default `ScrollViewer` Visual or include a child container named `StickyHeaderOverlayInstance` inside `ClipContainerInstance` in your custom Visual.
