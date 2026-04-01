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

Additional children can be added directly to a control's Visual (top visual), or as a child of existing children.

The following code shows how to add a colored rectangle to a Button.

```csharp
// Initialize
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
// Initialize
var button = new Button();
button.AddToRoot();
button.Anchor(Anchor.Center);

var buttonVisual = (ButtonVisual)button.Visual;
var coloredRectangle = new ColoredRectangleRuntime();
buttonVisual.TextInstance.AddChild(coloredRectangle);
// So the text area doesn't fill the whole button:
buttonVisual.TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
coloredRectangle.Color = Color.Red;
coloredRectangle.Dock(Dock.Bottom);
coloredRectangle.Height = 2;
```

<figure><img src="../../../.gitbook/assets/26_09 08 52.png" alt=""><figcaption><p>Button with red underline under its text</p></figcaption></figure>

## Removing Children Visuals

Gum controls are very flexible and can function even if children are removed. Of course, removing children may limit the behavior of a control. For example, removing the TextInstance from a ButtonVisual results in the Button no longer displaying its Text string.

```csharp
// Initialize
var button = new Button();
button.AddToRoot();
button.Anchor(Anchor.Center);
// if we remove the TextInstanc, we need to make sure the height of the button
// is absolute and no longer depends on its children:
button.Height = 32;
button.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

var buttonVisual = (ButtonVisual)button.Visual;
// setting Parent to null removes a child from its parent:
buttonVisual.TextInstance.Parent = null;
```

<figure><img src="../../../.gitbook/assets/26_09 23 28.png" alt=""><figcaption><p>Button with its TextInstance removed</p></figcaption></figure>

Of course, the TextIntance can also be made invisible, which results in similar behavior:

<pre class="language-csharp"><code class="lang-csharp">// Initialize
var button = new Button();
button.AddToRoot();
button.Anchor(Anchor.Center);
// if we remove the TextInstanc, we need to make sure the height of the button
// is absolute and no longer depends on its children:
button.Height = 32;
button.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

var buttonVisual = (ButtonVisual)button.Visual;
// setting Parent to null removes a child from its parent:
<strong>buttonVisual.TextInstance.Visible = false;
</strong></code></pre>

## Replacing Children with Different Types

Default visual children can be replaced with children of different types to further customize controls. For example, the Button control uses a NineSlice for its background, but this can be replaced with other types such as ColoredRectangleRuntime or SpriteRuntime.

The following code shows how to replace a button's default background with a SpriteRuntime. For simplicity this button uses [Lorem Picsum](https://picsum.photos/).

```csharp
// Initialize
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

## Adding Backgrounds to Layout Containers

StackPanel and Grid do not include any visuals — they are invisible by default. To create a layout container with a visible background, wrap the StackPanel or Grid inside a Panel, then add a ColoredRectangleRuntime as a sibling of the StackPanel.

The following example creates a Panel with a dark background and a StackPanel containing buttons:

```csharp
// Initialize
// Panel to hold both the background and the StackPanel
var panel = new Panel();
panel.AddToRoot();
panel.Anchor(Anchor.Center);
// Panel defaults to SizeToChildren, so set to Absolute for a fixed size
panel.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
panel.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
panel.Width = 240;
panel.Height = 300;

// Background fills the entire Panel
var background = new ColoredRectangleRuntime();
background.Dock(Gum.Wireframe.Dock.Fill);
background.Color = new Color(30, 30, 36);
panel.AddChild(background);

// StackPanel fills the Panel
var stack = new StackPanel();
stack.Dock(Gum.Wireframe.Dock.Fill);
stack.Spacing = 4;
panel.AddChild(stack);

for (int i = 0; i < 5; i++)
{
    var button = new Button();
    button.Text = "Button " + i;
    stack.AddChild(button);
}
```
[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACp1STW_CMAy98yusnopAHdrXYR-HAdrHbQKmXXoJraERwUFJurEh_vucBFbGDpNWqUr6_Gw_P3fTAkie7EO9TK7AmRq7HpAknRRKfiKjyZswsBKECm6B8B2e_T1tX-cU0OyuLCd6pLU7xKiotEm5bvYqDc6MWOIOzAZIDo3nnpzEYlDiTNTKWXAaxtx2ogeVVKVB6oLVYNH5yN3UalU7hJk2IGAm11iCZfq-66ssXfXC4i1L9b2HwonJxwptNpRLJCs1-bCHsn21b82PKOeV-3d6aM6Jp-e9o5IMnvU8GCbui2IxN7qmkidQimeuENgStim6kZN3fNrQou0DrbTBcoSFEzRXOKo5Z4nB9IacDXWxOPLdQ9k99zqihoqH1dOzXpel8nv5Y71hGWmT2d7PMnaMxRU2sxwMYX1816HhBskh9KfayBqvRCFpzoXOf8sKlKjI_xepJAeSqb1rPm7ggo9Op53TJifgJ3hbO6dpp6sfPoImH4-xbIJrv7c8iWG-QAfkjhNVNcYEii-wTVrb1hcglFRDVAMAAA)

To add padding between the background edge and the content, set a negative Width and Height on the StackPanel after docking. For example, `stack.Width = -16` creates 8 pixels of padding on each side.

This same pattern applies to Grid — wrap it in a Panel with a ColoredRectangleRuntime to give it a visible background.

{% hint style="warning" %}
Do not add a ColoredRectangleRuntime as a **background** directly inside a StackPanel or Grid. In a stacking or grid layout, every child participates in the layout — a background rectangle would be treated as a stacked item rather than appearing behind the other children. ColoredRectangleRuntime can still be added as a regular child of a StackPanel (for example, as a divider or visual element), but it should not be used as a background inside a stacking container.
{% endhint %}
