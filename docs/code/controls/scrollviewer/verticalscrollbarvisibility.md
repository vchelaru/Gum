# VerticalScrollBarVisibility

## Introduction

VerticalScrollBarVisibility controls the visibility of the vertical scroll bar, specifically in regards to the number of items in the ScrollViewer. The available values are:

* Auto - the ScrollBar displays only if needed based on the size of the inner panel
* Hidden - the ScrollBar remains invisible even if the contents of the inner panel exceed the size of its container
* Visible - the ScrollBar always displays

## Default Behavior

The default is Auto which means that the scroll bar only displays if necessary. This can be modified to change the ScrollBar behavior.

```csharp
var scrollViewer = new ScrollViewer();
scrollViewer.AddToRoot();
scrollViewer.X = 50;
scrollViewer.Y = 50;
scrollViewer.Width = 200;
scrollViewer.Height = 200;
scrollViewer.InnerPanel.StackSpacing = 2;
scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

var button = new Button();
button.AddToRoot();
button.Text = "Add Item";
button.X = 50;
button.Y = 10;
var random = new System.Random();
button.Click += (_, _) =>
{
    var innerRectangle = new ColoredRectangleRuntime();
    // no need to set innerRectangle.Y since each rectangle stacks
    innerRectangle.Width = 30;
    innerRectangle.Height = 30;
    innerRectangle.Color = new Color(random.Next(255), random.Next(255), random.Next(255));
    scrollViewer.InnerPanel.Children.Add(innerRectangle);
};
```

<figure><img src="../../../.gitbook/assets/13_09 47 19.gif" alt=""><figcaption><p>VerticalScrollBarVisibility set to Auto only shows the ScrollBar if enough items are added to the ScrollView</p></figcaption></figure>

## Code Example: Setting a ScrollViewer's VerticalScrollBarVisibility

The following code sets the visibility to never show:

```csharp
scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
```

