# Adding and Removing Visual Children

{% hint style="info" %}
This document assumes using V3 styles, which were introduced at the end of November 2025. If your project is using V2 visuals, you need to upgrade to V3 before the styling discussed on this document can be used.

For information on upgrading, see the [Migrating to 2025 November](../../../gum-tool/upgrading/migrating-to-2025-november.md) page.
{% endhint %}

## Introduction

Every control in Gum has a Visual property which defines its appearance and size. Some controls have simple Visuals, such as a single ContainerRuntime as the Visual for StackPanel. Other controls are made of multiple children, such as the Button containing a top-level ContainerRuntime with a NineSliceRuntime and TextRuntime.

This document shows how children can be added or removed from a control's Visuals to customize its appearance.

For information on working with standard visuals, see the [Standard Visuals](../../standard-visuals/) section.

## Adding Children Visuals

Additional children can be added directly to a control's visual. New children can be added directly to a control's Visual (top visual), or as a child of existing children.

The following code shows how to add a colored rectangle to a Button.

```csharp
var button = new Button();
button.AddToRoot();
button.Anchor(Anchor.Center);

var buttonVisual = (ButtonVisual)button.Visual;
var coloredRectangle = new ColoredRectangleRuntime();
buttonVisual.AddChild(coloredRectangle);
coloredRectangle.Color = Color.Red;
coloredRectangle.Anchor(Anchor.Left);
coloredRectangle.X = 8;
coloredRectangle.Width = 8;
coloredRectangle.Height = 8;
```

<figure><img src="../../../.gitbook/assets/26_09 02 55.png" alt=""><figcaption></figcaption></figure>

As mentioned above, new children can be added directly to the root, or they can be added as children of existing children. For example, the following code could be used to underline text on a Button:

```csharp
var button = new Button();
button.AddToRoot();
button.Anchor(Anchor.Center);

var buttonVisual = (ButtonVisual)button.Visual;
var coloredRectangle = new ColoredRectangleRuntime();
buttonVisual.TextInstance.AddChild(coloredRectangle);
// So the text area doesn't fill the whole button:
buttonVisual.TextInstance.WidthUnits = DimensionUnitType.RelativeToChildren;
coloredRectangle.Color = Color.Red;
coloredRectangle.Dock(Dock.Bottom);
coloredRectangle.Height = 2;
```

<figure><img src="../../../.gitbook/assets/26_09 08 52.png" alt=""><figcaption><p>Button with red underline under its text</p></figcaption></figure>

## Removing Children Visuals

Gum controls are very flexible and can function even if children are removed. Of course, removing children may limit the behavior of a control. For example, removing the TextInstance from a ButtonVisual results in the Button no longer displaying its Text string.

```csharp
var button = new Button();
button.AddToRoot();
button.Anchor(Anchor.Center);
// if we remove the TextInstanc, we need to make sure the height of the button
// is absolute and no longer depends on its children:
button.Height = 32;
button.HeightUnits = DimensionUnitType.Absolute;

var buttonVisual = (ButtonVisual)button.Visual;
// setting Parent to null removes a child from its parent:
buttonVisual.TextInstance.Parent = null;
```

<figure><img src="../../../.gitbook/assets/26_09 23 28.png" alt=""><figcaption><p>Button with its TextInstance removed</p></figcaption></figure>

Of course, the TextIntance can also be made invisible, which results in similar behavior:

<pre class="language-csharp"><code class="lang-csharp">var button = new Button();
button.AddToRoot();
button.Anchor(Anchor.Center);
// if we remove the TextInstanc, we need to make sure the height of the button
// is absolute and no longer depends on its children:
button.Height = 32;
button.HeightUnits = DimensionUnitType.Absolute;

var buttonVisual = (ButtonVisual)button.Visual;
// setting Parent to null removes a child from its parent:
<strong>buttonVisual.TextInstance.Visible = false;
</strong></code></pre>

## Replacing Children with Different Types

Default visual children can be replaced with children of different types to further customize controls. For example, the Button control uses a NineSlice for its background, but this can be replaced with other types such as ColoredRectangleRuntime or SpriteRuntime.

The following code shows how to replace a button's default background with a SpriteRuntime. For simplicity this button uses [Lorem Picsum](https://picsum.photos/).

```csharp
var button = new Button();
button.AddToRoot();
button.Anchor(Anchor.Center);
        
var buttonVisual = (ButtonVisual)button.Visual;
// setting Parent to null removes a child from its parent:
buttonVisual.Background.Parent = null;

var newBackground = new SpriteRuntime();
newBackground.SourceFileName = "https://picsum.photos/200/300";
// insert at 0 so it appears behind the text
buttonVisual.Children.Insert(0, newBackground);
// Size the button according to its new background:
button.Dock(Dock.SizeToChildren);

buttonVisual.ForegroundColor = Color.Black;

button.Click += (_, _) => button.Text = $"Clicked at {DateTime.Now}";
```

<figure><img src="../../../.gitbook/assets/26_09 42 13.png" alt=""><figcaption></figcaption></figure>

{% hint style="info" %}
By removing the background, the highlighting behavior is no longer functional. This can be fixed by updating states. For more information, see the [Styling Using States](styling-using-states.md) page.
{% endhint %}
