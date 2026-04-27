# Sticky Headers

## Introduction

A sticky header is a header item inside a `ScrollViewer` that scrolls with the content until it reaches the top of the visible area, at which point it pins to the top until the next sticky header arrives and bumps it off. This is the same behavior used in iOS sectioned tables and CSS `position: sticky`, and it is a common pattern for sectioned lists, settings screens, and chat-style timelines.

Sticky headers are supported directly on `ScrollViewer`, which means they also work for any control that derives from `ScrollViewer`, including `ItemsControl` and `ListBox`.

## How It Works

Each sticky header you register lives in a special overlay container that is a sibling of the inner panel. A separate **placeholder** visual stays in the normal scrollable content where the header would otherwise have been; the placeholder reserves the vertical space so the surrounding items do not shift. As the user scrolls, the `ScrollViewer` recomputes each header's position based on its placeholder's location and pins or bumps the header as needed.

You are responsible for adding both the header and its placeholder to the layout, then calling `RegisterStickyHeader` to wire them together.

## Code Example - Sectioned List with Sticky Headers

The following code creates a `ScrollViewer` containing three sections. Each section has a colored header and several rows. As the user scrolls, the headers pin to the top of the viewport in turn.

The header and placeholder are both `Panel` instances (Forms controls), and the header's background is a `ColoredRectangleRuntime`. `RegisterStickyHeader` accepts either Forms controls or raw visuals, and you can mix them — see the next section.

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
    // 1. The header that will be pinned at the top. Panels default to
    //    sizing to their children, so dock it to fill horizontally and
    //    set a fixed height.
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

    // 2. The placeholder. Add it to the scroll viewer's stack where the
    //    header would normally have lived. Its height will be set
    //    automatically by RegisterStickyHeader to match the header.
    var placeholder = new Panel();
    placeholder.Dock(Gum.Wireframe.Dock.FillHorizontally);
    scrollViewer.AddChild(placeholder);

    // 3. Register the pair. RegisterStickyHeader reparents the header
    //    into the scroll viewer's overlay, sizes the placeholder to match,
    //    and starts tracking scroll position.
    scrollViewer.RegisterStickyHeader(header, placeholder);

    // 4. Normal items follow the placeholder in the stack.
    for (int i = 0; i < 8; i++)
    {
        var label = new Label();
        label.Text = $"{name} item {i + 1}";
        scrollViewer.AddChild(label);
    }
}
```

<figure><img src="../../../.gitbook/assets/sticky-headers.gif" alt=""><figcaption><p>Sticky headers pin to the top of the viewport as the user scrolls</p></figcaption></figure>

## Mixing FrameworkElement and Visual Registrations

`RegisterStickyHeader` has overloads for both Forms controls (`FrameworkElement`) and raw visuals (`GraphicalUiElement`), and you can mix them. For example, a `Button` can serve as a sticky header backed by a `Panel` placeholder:

```csharp
var headerButton = new Button();
headerButton.Text = "Section Header";

var placeholder = new Panel();
placeholder.Dock(Gum.Wireframe.Dock.FillHorizontally);
scrollViewer.AddChild(placeholder);

scrollViewer.RegisterStickyHeader(headerButton, placeholder);
```

## Unregistering and Clearing

Use `UnregisterStickyHeader` to remove a single registration and `ClearStickyHeaders` to remove all of them. Unregistering does not destroy the header or placeholder visuals — it only stops the `ScrollViewer` from tracking them. If you also want the visuals removed from the screen, do that yourself afterwards.

```csharp
scrollViewer.UnregisterStickyHeader(headerButton);
scrollViewer.ClearStickyHeaders();
```

## Resizing Headers

If a header's height changes (for example, because its text is updated and the header is sized to its content), the placeholder is updated automatically and the pinned positions are recomputed. You do not need to re-register.

## Notes and Limitations

* Sticky headers are vertical only. There is currently no horizontal equivalent.
* Headers are pinned in the order their placeholders appear in the layout, not the order they were registered, so registration order does not matter.
* If the underlying Visual does not provide a `StickyHeaderOverlayInstance` container (for example, a fully custom Visual), `RegisterStickyHeader` becomes a no-op. Use the default `ScrollViewer` Visual or include a child container named `StickyHeaderOverlayInstance` inside `ClipContainerInstance` in your custom Visual.
