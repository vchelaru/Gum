# Stacking

## Introduction

Gum supports creating vertical and horizontal stacks of children. Stacked children can also be wrapped and support spacing between each item.

This document provides a deep dive into Gum stacking behavior, which is the default behavior for [StackPanel](../controls/stackpanel.md), [ItemsControl](../controls/itemscontrol.md), and [ListBox](../controls/listbox.md).

## Using StackPanel for Stacking

The `StackPanel` provides stacking behavior for its children. The following code shows how to add stacked `Buttons` to a `StackPanel`:

```csharp
var stackPanel = new StackPanel();
stackPanel.AddToRoot();
stackPanel.Anchor(Gum.Wireframe.Anchor.Center);

for(int i = 0; i < 10; i++)
{
    var button = new Button();
    stackPanel.AddChild(button);
    button.Text = $"Button {i}";
}
```

<figure><img src="../../.gitbook/assets/17_18 48 10.png" alt=""><figcaption></figcaption></figure>

StackPanel exposes some of the stack-related properties; however, we will be accessing its Visual to have full control over stacking for the remainder of this documentation.

## Adding Spacing with StackSpacing

The StackSpacing variable can be used to add spacing between each stacked item, as shown in the following code:

<pre class="language-csharp"><code class="lang-csharp">var stackPanel = new StackPanel();
stackPanel.AddToRoot();
stackPanel.Anchor(Gum.Wireframe.Anchor.Center);

<strong>var stackPanelVisual = stackPanel.Visual;
</strong><strong>stackPanelVisual.StackSpacing = 4;
</strong>
for(int i = 0; i &#x3C; 10; i++)
{
    var button = new Button();
    stackPanel.AddChild(button);
    button.Text = $"Button {i}";
}
</code></pre>

<figure><img src="../../.gitbook/assets/17_18 52 41.png" alt=""><figcaption><p>Buttons with spacing</p></figcaption></figure>

## Horizontal Stacking

We can change the stacking direction by changing the `ChildrenLayout` property as shown in the following code. Note that the `Buttons` have been made narrower so they all fit on screen.

<pre class="language-csharp"><code class="lang-csharp">var stackPanel = new StackPanel();
stackPanel.AddToRoot();
stackPanel.Anchor(Gum.Wireframe.Anchor.Center);

var stackPanelVisual = stackPanel.Visual;
<strong>stackPanelVisual.ChildrenLayout = 
</strong><strong>    Gum.Managers.ChildrenLayout.LeftToRightStack;
</strong>
for(int i = 0; i &#x3C; 10; i++)
{
    var button = new Button();
<strong>    button.Width = 60;
</strong>    stackPanel.AddChild(button);
    button.Text = $"Button {i}";
}
</code></pre>

<figure><img src="../../.gitbook/assets/17_18 55 55.png" alt=""><figcaption><p>Buttons stacking horizontally</p></figcaption></figure>

## Wrapping

Stacked children can also be wrapped horizontally. Before wrapping can happen, the parent `StackPanel` must not depend on its children's size in its _primary_ stacking direction. For example, if a `StackPanel` is using its default vertical stacking, then its `Height` must not depend on its children - otherwise it stretches indefinitely to contain its children.

The following code shows how to set an absolute Height on the parent `StackPanel` and enable wrapping:

```csharp
var stackPanel = new StackPanel();
stackPanel.AddToRoot();
stackPanel.Anchor(Gum.Wireframe.Anchor.Center);
stackPanel.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
stackPanel.Height = 150;

var stackPanelVisual = stackPanel.Visual;

stackPanelVisual.WrapsChildren = true;

for(int i = 0; i < 10; i++)
{
    var button = new Button();
    stackPanel.AddChild(button);
    button.Text = $"Button {i}";
}
```

<figure><img src="../../.gitbook/assets/17_19 01 29.png" alt=""><figcaption><p>Children stacked vertically and wrapped</p></figcaption></figure>

Notice that since the `StackPanel` has had `Anchor(Anchor.Center)` called, it remains centered as it wraps and expands horizontally.

## Bottom-Up Stacking

A `StackPanel's` `Visual` only provides two possible stacking modes:

* `ChildrenLayout.TopToBottomStack` (default)
* `ChildrenLayout.LeftToRightStack`&#x20;

By combining Dock and ChildrenLayout, we can create bottom-up and right-to-left stacking. The following code shows how to create a bottom-up stack similar to a chat room or command line:

```csharp
StackPanel stackPanel;

protected override void Initialize()
{
    GumUI.Initialize(this, DefaultVisualsVersion.V2);

    stackPanel = new StackPanel();
    stackPanel.AddToRoot();
    stackPanel.Anchor(Gum.Wireframe.Anchor.Bottom);

    base.Initialize();
}

protected override void Update(GameTime gameTime)
{
    GumUI.Update(gameTime);

    if(GumUI.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Enter))
    {
        var label = new Label();
        stackPanel.AddChild(label);
        int index = stackPanel.Children.Count;
        label.Text = $"{index}: Added at {System.DateTime.Now}";
    }

    base.Update(gameTime);
}
```

<figure><img src="../../.gitbook/assets/17_19 12 47.gif" alt=""><figcaption><p>Bottom-up stack</p></figcaption></figure>

## Evenly-Sized Stacked Children

Stacking can be combined with ratio sizes to create evenly-sized children.

If children use a `Height Units` of `Ratio`, then they are sized according to the size of their parent and the size of their siblings. Since children depend on the parent's size, the parent should not depend on its children for its height.

The following code creates buttons which all share the height of their parent StackPanel.

```csharp
StackPanel stackPanel;
protected override void Initialize()
{
    GumUI.Initialize(this, DefaultVisualsVersion.V2);

    stackPanel = new StackPanel();
    stackPanel.AddToRoot();
    stackPanel.Anchor(Gum.Wireframe.Anchor.Center);
    stackPanel.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
    stackPanel.Height = 200;

    base.Initialize();
}

protected override void Update(GameTime gameTime)
{
    GumUI.Update(gameTime);
    if(GumUI.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Enter))
    {
        var button = new Button();
        stackPanel.AddChild(button);
        button.HeightUnits = Gum.DataTypes.DimensionUnitType.Ratio;
        button.Height = 1;
        button.Text = $"Button {stackPanel.Children.Count}";
    }
    base.Update(gameTime);
}
```

<figure><img src="../../.gitbook/assets/17_19 25 24.gif" alt=""><figcaption><p>Children Buttons stacked with Ratio size</p></figcaption></figure>
