# MakeSizedToChildren

## Introduction

`MakeSizedToChildren` can be used to size a ScrollViewer to its children. Note that this method only exists on the ScrollViewerVisual if V2 or newer styling (code-only) is used.

## Code Example: Sizing a ScrollViewer to its Children

The following code shows how to size a ScrollViewer to its children:

```csharp
var stackPanel = new StackPanel();
stackPanel.AddToRoot();

var button = new Button();
stackPanel.AddChild(button);
button.Text = "Add Child";

var scrollViewer = new ScrollViewer();
stackPanel.AddChild(scrollViewer);
var visual = (ScrollViewerVisual)scrollViewer.Visual;
visual.MakeSizedToChildren();

button.Click += (sender, args) =>
{
    // add a button to the scroll viewer
    var newButton = new Button();
    newButton.Text = $"Btn {scrollViewer.InnerPanel.Children.Count}";
    scrollViewer.AddChild(newButton);
};
```

<figure><img src="../../../.gitbook/assets/11_06 12 46.gif" alt=""><figcaption><p>ScrollViewer automatically sizing in response to children being added</p></figcaption></figure>
