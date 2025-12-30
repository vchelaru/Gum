# Event Bounds and Clipping

## Introduction

Gum raises events in response to the Cursor moving over the bounds of an object. This page discusses the details about event bounds.

## Default Behavior

By default the bounds of an object are based on its size. That is, by changing an object's Height or Width, its bounds update in response to this change. We can see this by creating a few buttons of various size in a StackPanel.

```csharp
StackPanel panel = new ();
panel.AddToRoot();
panel.Spacing = 10;
panel.Anchor(Anchor.Center);

Button defaultButton = new();
panel.AddChild(defaultButton);
defaultButton.Text = "Default";

Button largeButton = new();
panel.AddChild(largeButton);
largeButton.Text = "Large";
largeButton.Width = 200;
largeButton.Height = 100;

Button smallButton = new();
panel.AddChild(smallButton);
smallButton.Text = "Small";
smallButton.Width = 50;
smallButton.Height = 25;
```

<figure><img src="../../.gitbook/assets/19_14 01 12.gif" alt=""><figcaption><p>Buttons reacting to cursor hover</p></figcaption></figure>

## Background Separate from Control

It might seem as if the background of the Button is deciding whether the Button should display its hover state, but we can show that the background is actually not responsible for this, but rather the entire Visual itself.

For example the following code creates two buttons. One button has a background that is smaller than the button, and one has a background that is larger - it spills over the edge.

```csharp
StackPanel panel = new ();
panel.AddToRoot();
panel.Spacing = 10;
panel.Anchor(Anchor.Center);

Button smallerBackgroundButton = new();
panel.AddChild(smallerBackgroundButton);
smallerBackgroundButton.Text = "Smaller Background";
var background = ((ButtonVisual)smallerBackgroundButton.Visual).Background;
background.WidthUnits = DimensionUnitType.RelativeToParent;
background.Width = -30;

Button largerBackgroundButton = new();
panel.AddChild(largerBackgroundButton);
largerBackgroundButton.Text = "Larger Background";
var background2 = ((ButtonVisual)largerBackgroundButton.Visual).Background;
background2.WidthUnits = DimensionUnitType.RelativeToParent;
background2.Width = 30;
```

<figure><img src="../../.gitbook/assets/19_14 07 04.gif" alt=""><figcaption><p>Buttons with larger and smaller backgrounds</p></figcaption></figure>

The code above sets the background widths relative to their parent. The smaller button shrinks the background by 30 pixels, which adds 15 pixel padding on each side. The larger button increases the background by 30 pixels, adding 15 pixels of spillover on each side.

We can visualize the actual bounds of the button as shown in the following image:

<figure><img src="../../.gitbook/assets/19_14 15 08.png" alt=""><figcaption></figcaption></figure>

## Parent Bounds

By default, children controls only respond to cursor events if they are in their parent bounds. If a child is either partly or completely outside of the bounds of its parent, it will not raise events.

The following code shows a few buttons added to a Window. Notice that only the buttons contained within the bounds of its parent respond to the Cursor.

```csharp
Window window = new ();
window.AddToRoot();
window.Anchor(Anchor.Center);

for(int i = 0; i < 5; i++)
{
    Button button = new();
    window.AddChild(button);
    button.X = 20;
    button.Y = 20 + i * 40;
    button.Height = 36;
    button.HeightUnits = DimensionUnitType.Absolute;
}
```

<figure><img src="../../.gitbook/assets/19_14 23 02.gif" alt=""><figcaption><p>Buttons in a Window responding to Cursor actions</p></figcaption></figure>

## Raising Events Outside of Parent Bounds

We can force a parent to still raise events on its children even if the children are placed outside of its bounds by setting the Visual's `RaiseChildrenEventsOutsideOfBounds` property to true. Keep in mind that some controls, such as Window, may have multiple layers of children.

Windows have an InnerPanel which is where children are added when AddChild is called. We can recursively set `RaiseChildrenEventsOutsideOfBounds` to `true` to make buttons react to Cursor actions even when the button bounds spill over the edge of the window as shown in the following code:

```csharp
Window parent = new ();
parent.AddToRoot();
parent.Anchor(Anchor.Center);

var innerPanel = parent.InnerPanel as InteractiveGue;

while(innerPanel != null)
{
    innerPanel.RaiseChildrenEventsOutsideOfBounds = true;

    // climb up the hierarchy so all parents allow 
    // checking out of bounds:
    innerPanel = innerPanel.Parent as InteractiveGue;
}

for(int i = 0; i < 5; i++)
{
    Button button = new();
    parent.AddChild(button);
    button.X = 20;
    button.Y = 20 + i * 40;
    button.Height = 36;
    button.HeightUnits = DimensionUnitType.Absolute;
}
```

<figure><img src="../../.gitbook/assets/19_14 33 00.gif" alt=""><figcaption><p>Children outside the bounds of a window responding to Cursor actions</p></figcaption></figure>

