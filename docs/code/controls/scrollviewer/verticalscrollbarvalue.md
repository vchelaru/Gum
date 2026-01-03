# VerticalScrollBarValue

## Introduction

`VerticalScrollBarValue` is the current value of the `ScrollViewer`'s vertical scroll bar. This value returns the current scroll bar value, and it can be assigned to force the scroll position.

This range for this value is between 0 and `VerticalScrollBarMaximum`.

## Code Example - Getting VerticalScrollBarValue

The following code gets the VerticalScrollBarValue and displays it using a Label.

```csharp
var mainPanel = new StackPanel();
mainPanel.AddToRoot();

var label = new Label();
mainPanel.AddChild(label);

var scrollViewer = new ScrollViewer();
scrollViewer.InnerPanel.StackSpacing = 3;

for (int i = 0; i < 20; i++)
{
    var coloredRectangle = new ColoredRectangleRuntime();
    coloredRectangle.Color = Color.Red;
    scrollViewer.AddChild(coloredRectangle);
}

scrollViewer.ScrollChanged += (sender, args) =>
{
    label.Text =
        $"Scroll position: {scrollViewer.VerticalScrollBarValue:N0}/" +
        $"{scrollViewer.VerticalScrollBarMaximum}";
};
mainPanel.AddChild(scrollViewer);
```

<figure><img src="../../../.gitbook/assets/13_09 50 41.gif" alt=""><figcaption><p>VerticalScrollBarValue displayed by a Label</p></figcaption></figure>
