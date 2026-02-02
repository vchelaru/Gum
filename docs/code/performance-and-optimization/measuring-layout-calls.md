# Measuring Layout Calls

## Introduction

Gum's layout engine, although fairly efficient, can introduce performance problems for complex scenes, or scenes which are updated frequently. This page discusses:

* Gum's default layout behavior
* How to measure layouts for performance problems
* How to improve your layout performance

## Default Layout Behavior

`GraphicalUiElements` (visuals for controls) provide many variables for controlling absolute position and size. These include:

* Position values such as X and Y
* Size values such as Width and Height
* Unit values such as XUnits or WidthUnits
* Rotation

A `GraphicalUiElement's` absolute position or size can also be affected by its `Parent`, `Children`, and even siblings. Therefore, changing any of these values can result in one or more objects performing a layout.

Layouts are performed immediately after a value is set so that absolute values are (by default) always up-to-date.

For example, we can see that a `StackPanel's` `Visual` always reflects its size after every child is added.

```csharp
StackPanel stackPanel = new();
stackPanel.AddToRoot();
stackPanel.Anchor(Anchor.Center);

for(int i = 0; i < 10; i++)
{
    float heightBefore = stackPanel.Visual.GetAbsoluteHeight();

    Label label = new();
    stackPanel.AddChild(label);

    float heightAfter = stackPanel.Visual.GetAbsoluteHeight();

    label.Text = 
        $"Label {i + 1} (StackPanel Height: {heightBefore} -> {heightAfter})";
}
```

<figure><img src="../../.gitbook/assets/02_07 57 16.png" alt=""><figcaption><p>StackPanel displaying its size</p></figcaption></figure>

This code shows that a `StackPanel's` absolute height is calculated after every `AddChild` call. Note that the absolute height is calculated whether we call `GetAbsoluteHeight` or not - this method simply retrieves the already-calculated value.

This behavior has the benefit of an object always updating its most up-to-date position and size, but it has the downside of performing potentially unnecessary layout calls.

Most of the time these extra layout calls don't have an impact on performance; however complex layouts may slow down projects due to this behavior.

## Measuring Layouts

The number of layout calls which have been performed can be obtained from the `GraphicalUiElement.UpdateLayoutCallCount` static property. This reports the total number of layouts, so usually a before-and-after is often useful.

We can modify the code above to display layout call counts when each label is created. Note that this code increases to create 20 Label instances:

<pre class="language-csharp"><code class="lang-csharp">StackPanel stackPanel = new();
stackPanel.AddToRoot();
stackPanel.Anchor(Anchor.Center);

<strong>for(int i = 0; i &#x3C; 20; i++)
</strong>{
    float heightBefore = stackPanel.Visual.GetAbsoluteHeight();

    Label label = new();
    stackPanel.AddChild(label);

    float heightAfter = stackPanel.Visual.GetAbsoluteHeight();

<strong>    label.Text = 
</strong><strong>        $"Label {i + 1}, layout calls: {GraphicalUiElement.UpdateLayoutCallCount}";
</strong>}
</code></pre>

<figure><img src="../../.gitbook/assets/02_08 07 15.png" alt=""><figcaption><p>Layout calls</p></figcaption></figure>

{% hint style="info" %}
Although each call count has a slight performance cost, Gum can efficiently perform hundreds of layout calls. Most of the time these layout will not cause performance problems for games.

Also, the layout calls displayed above are for illustrative purposes. The exact number of calls is not important, and these call counts are likely to change as future versions of Gum are released.
{% endhint %}

We can notice a few things when looking at this code:

1. `UpdateLayoutCallCount` increases after every item is added
2. The amount of `UpdateLayoutCallCount` increase grows as more items are added to the StackPanel

Based on the screenshot above, our project is performing over 1000 unnecessary layout call counts. At least, in a typical game the final layout is only needed when drawing is performed, not after every item add.

## Reducing Layout Calls

We can improve the performance of our code by setting `GraphicalUiElement.IsAllLayoutSuspended` to true, then resuming layout it after the adds have finished:

<pre class="language-csharp"><code class="lang-csharp"><strong>GraphicalUiElement.IsAllLayoutSuspended = true;
</strong>
StackPanel stackPanel = new();
stackPanel.AddToRoot();
stackPanel.Anchor(Anchor.Center);

for(int i = 0; i &#x3C; 20; i++)
{
    float heightBefore = stackPanel.Visual.GetAbsoluteHeight();

    Label label = new();
    stackPanel.AddChild(label);

    float heightAfter = stackPanel.Visual.GetAbsoluteHeight();

    label.Text = 
        $"Label {i + 1}, layout calls: {GraphicalUiElement.UpdateLayoutCallCount}";
}

<strong>GraphicalUiElement.IsAllLayoutSuspended = false;
</strong><strong>// Now do a layout:
</strong><strong>stackPanel.Visual.UpdateLayout();
</strong></code></pre>

<figure><img src="../../.gitbook/assets/02_08 34 32.png" alt=""><figcaption><p>Significantly reduced layout calls</p></figcaption></figure>
