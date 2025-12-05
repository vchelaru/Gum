# Anchor and Dock

## Introduction

`Anchor` and `Dock` are methods used to position and size the calling object.

`Anchor` adjusts the position of an object without changing its size.

<figure><img src="../../.gitbook/assets/15_16 56 39.png" alt=""><figcaption><p>Nine possible anchor values</p></figcaption></figure>

`Dock` changes the position and size of an object. `Dock` can be used to adjust horizontal or vertical values.

<figure><img src="../../.gitbook/assets/15_17 00 23.png" alt=""><figcaption><p>Dock values adjusting horizontal size</p></figcaption></figure>

<figure><img src="../../.gitbook/assets/15_17 03 14.png" alt=""><figcaption><p>Dock values adjusting vertical size</p></figcaption></figure>

`Dock` can also adjust both vertical and horizontal values.



<figure><img src="../../.gitbook/assets/17_06 12 46.png" alt=""><figcaption><p>Fill and SizeToChildren adjusts both Width and Height values</p></figcaption></figure>

## Code Example: Calling Anchor and Dock

The following code shows how to create a centered panel using `Anchor`. Two buttons are added, with the first using `Dock.Top` and the other using `Dock.Bottom`.

```csharp
var panel = new Panel();
panel.AddToRoot();
panel.Anchor(Gum.Wireframe.Anchor.Center);
// give this an explicit size, so docked buttons
// can fill it:
panel.Width = 200;
panel.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
panel.Height = 200;
panel.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

var background = new ColoredRectangleRuntime();
panel.AddChild(background);
background.Color = Color.DarkBlue;
background.Dock(Gum.Wireframe.Dock.Fill);

var button = new Button();
panel.AddChild(button);
button.Dock(Gum.Wireframe.Dock.Top);
button.Text = "Top Docked Button";

var button2 = new Button();
panel.AddChild(button2);
button2.Dock(Gum.Wireframe.Dock.Bottom);
button2.Text = "Bottom Docked Button";
```

<figure><img src="../../.gitbook/assets/17_06 19 42.png" alt=""><figcaption><p>Buttons and background docked to a parent</p></figcaption></figure>

## Modifications After Anchor and Dock

Once either `Anchor` or `Dock` are called, additional property assignments can be made to the object, its parent, or its children, and positions and sizes will adjust accordingly.

For example, we could add events to each of the `Buttons` above to adjust the `Panel's` `Width` and `Height`. When the `Panel` adjusts, the buttons and background automatically in response.

<pre class="language-csharp"><code class="lang-csharp">var panel = new Panel();
panel.AddToRoot();
panel.Anchor(Gum.Wireframe.Anchor.Center);
// give this an explicit size, so docked buttons
// can fill it:
panel.Width = 200;
panel.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
panel.Height = 200;
panel.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

var background = new ColoredRectangleRuntime();
panel.AddChild(background);
background.Color = Color.DarkBlue;
background.Dock(Gum.Wireframe.Dock.Fill);

var button = new Button();
panel.AddChild(button);
button.Dock(Gum.Wireframe.Dock.Top);
button.Text = "Top Docked Button";
<strong>// this button makes the panel wider:
</strong><strong>button.Click += (_, _) => panel.Width += 20;
</strong>
var button2 = new Button();
panel.AddChild(button2);
button2.Dock(Gum.Wireframe.Dock.Bottom);
button2.Text = "Bottom Docked Button";
<strong>// this button makes the panel shorter:
</strong><strong>button2.Click += (_, _) => panel.Height -= 20;
</strong>
</code></pre>

<figure><img src="../../.gitbook/assets/17_06 23 26.gif" alt=""><figcaption></figcaption></figure>

Similarly, we can adjust the position or size of our `Buttons` after calling `Dock` to create margins. Gum does not have Margin or Padding properties, but these can be effectively created using position (`X` or `Y`) and size values (`Width` or `Height`) as shown in the following code:

<pre class="language-csharp"><code class="lang-csharp">var panel = new Panel();
panel.AddToRoot();
panel.Anchor(Gum.Wireframe.Anchor.Center);
// give this an explicit size, so docked buttons
// can fill it:
panel.Width = 200;
panel.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
panel.Height = 200;
panel.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

var background = new ColoredRectangleRuntime();
panel.AddChild(background);
background.Color = Color.DarkBlue;
background.Dock(Gum.Wireframe.Dock.Fill);

<strong>float buttonMargin = 8;
</strong>
var button = new Button();
panel.AddChild(button);
button.Dock(Gum.Wireframe.Dock.Top);
<strong>button.Y = buttonMargin;
</strong><strong>button.Width = -buttonMargin * 2;
</strong>button.Text = "Top Docked Button";
// this button makes the panel wider:
button.Click += (_, _) => panel.Width += 20;

var button2 = new Button();
panel.AddChild(button2);
button2.Dock(Gum.Wireframe.Dock.Bottom);
<strong>button2.Y = -buttonMargin;
</strong><strong>button2.Width = -buttonMargin * 2;
</strong>button2.Text = "Bottom Docked Button";
// this button makes the panel shorter:
button2.Click += (_, _) => panel.Height -= 20;
</code></pre>

<figure><img src="../../.gitbook/assets/17_06 27 34.png" alt=""><figcaption><p>Buttons with margins</p></figcaption></figure>

To create the top margin for the top `Button`, a positive `Y` value is used to move the `Button` downward. By contrast, a negative `Y` value is used to move the bottom `Button` upward.

Once a `Button` is given a `Top` or `Bottom` `Dock`, its `Width` value is relative to its parent (the `Panel`), so a negative `Width` value shrinks the `Button` relative to its parent. The `buttonMargin` must be applied on both the left and right side, so the `Width` value is modified by twice the `buttonMargin` value.

Alternatively, an internal `Panel` can be used to create a margin. Although this does add extra code, additional `Panels` can be used to create advanced layouts or to organize your elements for maintainability. The following code uses a dedicated `Panel` named `innerPanel` to create margins for the `Buttons`:

<pre class="language-csharp"><code class="lang-csharp">var panel = new Panel();
panel.AddToRoot();
panel.Anchor(Gum.Wireframe.Anchor.Center);
// give this an explicit size, so docked buttons
// can fill it:
panel.Width = 200;
panel.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
panel.Height = 200;
panel.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

var background = new ColoredRectangleRuntime();
panel.AddChild(background);
background.Color = Color.DarkBlue;
background.Dock(Gum.Wireframe.Dock.Fill);

float buttonMargin = 8;

<strong>var innerPanel = new Panel();
</strong><strong>panel.AddChild(innerPanel);
</strong><strong>innerPanel.Dock(Gum.Wireframe.Dock.Fill);
</strong><strong>innerPanel.Width = -buttonMargin * 2;
</strong><strong>innerPanel.Height = -buttonMargin * 2;
</strong>
var button = new Button();
<strong>innerPanel.AddChild(button);
</strong>button.Dock(Gum.Wireframe.Dock.Top);
button.Text = "Top Docked Button";
// this button makes the panel wider:
button.Click += (_, _) => panel.Width += 20;

var button2 = new Button();
<strong>innerPanel.AddChild(button2);
</strong>button2.Dock(Gum.Wireframe.Dock.Bottom);
button2.Text = "Bottom Docked Button";
// this button makes the panel shorter:
button2.Click += (_, _) => panel.Height -= 20;
</code></pre>

## Dock and Parent/Child Dependencies

When working with Dock calls, it's important to understand the dependencies created between parent and child.

Our code above creates a top-down dependency where `panel` ultimately decides the size of its children.

The bottom-most children (`button` and `button2`) depend on their parent for their effective positioning and sizing. The parent for both Buttons is either `innerPanel` or `panel`, depending on whether the `innerPanel` is being used for margins. In either case, the Button `Width` values are _relative_ to their parent's effective width.

Similarly, `background` also depends on its parent panel for both its `Width` and `Height`.

If `innerPanel` is included, then the dependency exists across multiple levels. In other words, `panel's` `Width` changes `innerPanel's` effective width, which changes `button` and `button2's` effective width.

### Circular Dependencies

Since the `panel` is ultimately the deciding control for width, we explicitly set its `WidthUnits` and `HeightUnits` to `Absolute`. If we remove this, then the panel's default `WidthUnits` and `HeightUnits` are used, which result in the `Panel` being sized to its children. This creates a circular dependency, resulting in our buttons having an effective width of 0 as shown in the following code:

<pre class="language-csharp"><code class="lang-csharp">var panel = new Panel();
panel.AddToRoot();
panel.Anchor(Gum.Wireframe.Anchor.Center);
<strong>// Remove the explicit size so that it sizes to its contents, causing
</strong><strong>// a circular reference:
</strong><strong>//panel.Width = 200;
</strong><strong>//panel.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
</strong><strong>//panel.Height = 200;
</strong><strong>//panel.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
</strong>
var background = new ColoredRectangleRuntime();
panel.AddChild(background);
background.Color = Color.DarkBlue;
background.Dock(Gum.Wireframe.Dock.Fill);

float buttonMargin = 8;

var button = new Button();
panel.AddChild(button);
button.Dock(Gum.Wireframe.Dock.Top);
button.Y = buttonMargin;
button.Width = -buttonMargin * 2;
button.Text = "Top Docked Button";
// this button makes the panel wider:
button.Click += (_, _) => panel.Width += 20;

var button2 = new Button();
panel.AddChild(button2);
button2.Dock(Gum.Wireframe.Dock.Bottom);
button2.Y = -buttonMargin;
button2.Width = -buttonMargin * 2;
button2.Text = "Bottom Docked Button";
// this button makes the panel shorter:
button2.Click += (_, _) => panel.Height -= 20;
</code></pre>

<figure><img src="../../.gitbook/assets/image.png" alt=""><figcaption><p>Buttons with 0 width, wrapping their text</p></figcaption></figure>

We can resolve this by bringing back the code to make `panel` be sized with absolute units (pixels), or we can size everything bottom-up as shown in the next section.

### Bottom-Up Dependencies

We can invert the dependencies by giving the bottom-most children (`button` and `button2`) absolute size values, then sizing their parents according to children. For example, the following code sizes the entire `Panel` according to its children.

<pre class="language-csharp"><code class="lang-csharp">float buttonMargin = 8;

var panel = new Panel();
panel.AddToRoot();
panel.Anchor(Gum.Wireframe.Anchor.Center);
panel.Dock(Gum.Wireframe.Dock.SizeToChildren);
<strong>// Since the panel now depends on its children, the panel
</strong><strong>// is the one in charge of bottom and right margins.
</strong><strong>// Top margin is handled by the first button.
</strong><strong>panel.Width = buttonMargin * 2;
</strong><strong>panel.Height = buttonMargin;
</strong>
var background = new ColoredRectangleRuntime();
panel.AddChild(background);
background.Color = Color.DarkBlue;
background.Dock(Gum.Wireframe.Dock.Fill);

var button = new Button();
panel.AddChild(button);
<strong>button.Anchor(Gum.Wireframe.Anchor.Top);
</strong><strong>// the button adjusts its Y value to produce a top margin:
</strong><strong>button.Y = buttonMargin;
</strong><strong>button.Width = 200;
</strong>button.Text = "Top Docked Button";
<strong>button.Click += (_, _) => button.Width += 20;
</strong>
var button2 = new Button();
panel.AddChild(button2);
<strong>button2.Anchor(Gum.Wireframe.Anchor.Top);
</strong><strong>button2.Y = 200;
</strong><strong>button2.Width = 200;
</strong>button2.Text = "Bottom Docked Button";
<strong>// this button makes the panel shorter:
</strong><strong>button2.Click += (_, _) => button2.Y -= 20;
</strong></code></pre>

<figure><img src="../../.gitbook/assets/17_18 32 01.gif" alt=""><figcaption><p>Buttons now deciding the parent's size</p></figcaption></figure>

## Bottom-Up and Top-Down Mixed Dependencies

Although the section above focuses on bottom-up dependencies, the control is actually using both bottom-up and top-down dependencies. The relationships are as follows:

* `button` and `button2` are using absolute `Width` and `Y` values
* `panel` depends on its children, so it sizes itself according to `button` and `button2`
* `background` is sized according to its parent (`panel`), so it resolves its size after the parent `panel` determines its own size

In situations where dependencies go both-ways, Gum attempts to resolve absolute positions and widths first, then performs layout logic in order of dependencies.
