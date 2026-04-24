# Tooltip

## Introduction

The Tooltip control displays a short informational message when the cursor hovers over a host `FrameworkElement`. It mirrors WPF's `ToolTip`, including the `FrameworkElement.ToolTip` property, the `ToolTipOpening` and `ToolTipClosing` events, and the static `ToolTipService` for tuning hover delays.

In v1, tooltip content must be a `string`. WPF accepts arbitrary object content, but that is reserved for a future release.

## Code Example: Adding a Tooltip

The simplest way to add a tooltip is to assign a string to any `FrameworkElement`'s `ToolTip` property. Gum Forms creates the `Tooltip` instance internally and registers it with `ToolTipService`:

```csharp
// Initialize
var button = new Button();
button.AddToRoot();
button.X = 50;
button.Y = 50;
button.Width = 150;
button.Height = 50;
button.Text = "Hover me";
button.ToolTip = "Click me";
```

When the cursor hovers over the button and remains there for the `InitialShowDelay`, the tooltip appears near the cursor. When the cursor leaves, the tooltip is hidden.

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA13KwQqCQBCA4VdZ5lQgYkIXo0N1yK4hWGQHy0mHdDfWWYuid88ScvH4f_wv2NRrU0HA2qADpiaZ1xAcoEU3Jo0XnVYIRwdIElNa0hMhgCbV4mSYlRRzIfEulr8YjWeJ7NxdZFmktkqxjbt2n3p97wcdU8ZFaxMbQ6S84MEZ4eNLifE83w9Vg1pU2JU1KVVGdPt_q5LO1_6D9weUDqjH_wAAAA)

## String-Only Content (v1)

`Tooltip.Content` and `FrameworkElement.ToolTip` currently accept only `string` values (or, for `ToolTip`, an existing `Tooltip` instance). Passing any other object type throws `NotSupportedException`:

```csharp
// Initialize
// OK in v1:
button.ToolTip = "A helpful hint";

// NOT supported in v1 - throws NotSupportedException:
// button.ToolTip = new StackPanel { ... };
```

{% hint style="info" %}
Rich, object-based content (for example a `StackPanel` with an icon and formatted text) is planned for a future release to match WPF more completely.
{% endhint %}

## Tuning Delays with ToolTipService

`ToolTipService` is a static class that controls global hover timing. Its three delays mirror WPF's defaults:

* `InitialShowDelay` - time the cursor must rest on an element before the tooltip shows. Default: 500 milliseconds.
* `ShowDuration` - how long the tooltip stays visible while the cursor remains on the host. Default: 5 s.
* `BetweenShowDelay` - after a tooltip closes, the next hover shows immediately if it happens within this window. Default: 100 milliseconds.

You typically set these once during initialization:

```csharp
// Initialize
ToolTipService.InitialShowDelay = TimeSpan.FromMilliseconds(250);
ToolTipService.ShowDuration = TimeSpan.FromSeconds(8);
ToolTipService.BetweenShowDelay = TimeSpan.FromMilliseconds(50);
```

These settings apply to every tooltip in your application.

## Programmatic Show and Hide

You can also construct a `Tooltip` directly and show or hide it from code. This is useful when you need to display a tooltip in response to something other than hover (for example a keyboard shortcut or a validation error):

```csharp
// Initialize
var tooltip = new Tooltip();
tooltip.Content = "Shown from code";

var button = new Button();
button.AddToRoot();
button.Text = "Show Tooltip";
button.Click += (_, _) =>
{
    if (tooltip.IsOpen)
    {
        tooltip.Hide();
    }
    else
    {
        // Show near a specific screen position (in gum units):
        tooltip.Show(cursorX: 50, cursorY: s00);
    }
};
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA2VQTUvDQBD9K8OeEghtiLeUHmwR7UnQgIqREpNJHUxmw35YseS_u8kmUulelvexb9_MSez0rW1FapTFSFhNfNAifRWOXDyRwloVLYq3SBCToaKhHxSp-CoUGCkbQx2sgfEImUdBuMp5UhZbyQbZOEdu4zhJHj_kkaFWsoVSVuhJ5x_S3q0xkqewzQjGLM8vrqsqkw9SmnMyw-__4XOLv-TJuG2o_PS-zRqCfQT7ENYDvrrJ-ZQzuEM1BHPznb7vkEMvTPpwZv2OKhybDGTvL2w0XjxYLmEsxuhmLEB3WFJNJehSITJ0UruturkDYjjYFqzbsg7Tyx-HlKC0Skv1nEISxxF49DKisy79SvS_73TzodcBAAA)

`Show` adds the tooltip's visual to `FrameworkElement.PopupRoot` and clamps it to the screen. `Hide` removes it. `IsOpen` reports the current state.

## Opened and Closed Events

The `Tooltip` class raises `Opened` when it becomes visible and `Closed` when it is hidden. These let you run logic tied to the tooltip's lifecycle:

```csharp
// Initialize
var tooltip = new Tooltip();
tooltip.Content = "Hover details";

tooltip.Opened += (_, _) =>
{
    System.Diagnostics.Debug.WriteLine("Tooltip opened");
};

tooltip.Closed += (_, _) =>
{
    System.Diagnostics.Debug.WriteLine("Tooltip closed");
};

button.ToolTip = tooltip;
```

For events scoped to a specific host element, `FrameworkElement` also exposes `ToolTipOpening` and `ToolTipClosing`, which mirror the WPF events of the same names:

```csharp
// Initialize
button.ToolTipOpening += (_, _) =>
{
    System.Diagnostics.Debug.WriteLine("About to show tooltip for button");
};
button.ToolTipClosing += (_, _) =>
{
    System.Diagnostics.Debug.WriteLine("About to hide tooltip for button");
};
```

