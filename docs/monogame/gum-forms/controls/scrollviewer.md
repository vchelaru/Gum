# ScrollViewer

### Introduction

The ScrollViewer control provides a container which can hold Gum objects (including other Gum Forms objects). The user can scroll the ScrollViewer with the mouse or ScrollBar.

By default the ScrollViewer's InnerPanel expands automatically in response to its children and stacks its children top-to-bottom. Of course, this behavior can be changed since the InnterPanel is a standard GraphicalUiElement.

### Code Example: Creating a ScrollViewer

The following code creates a ScrollViewer and adds ColoredRectangleRuntimes to the ScrollViewer.

```csharp
var scrollViewer = new ScrollViewer();
this.Root.Children.Add(scrollViewer.Visual);
scrollViewer.X = 50;
scrollViewer.Y = 50;
scrollViewer.Width = 200;
scrollViewer.Height = 200;
scrollViewer.InnerPanel.StackSpacing = 2;

var random = new System.Random();
for (int i = 0; i < 30; i++)
{
    var innerRectangle = new ColoredRectangleRuntime();
    innerRectangle.X = random.NextSingle() * 150;
    // no need to set innerRectangle.Y since each rectangle stacks
    innerRectangle.Width = 30;
    innerRectangle.Height = 30;
    innerRectangle.Color = new Color(random.Next(255), random.Next(255), random.Next(255));
    scrollViewer.InnerPanel.Children.Add(innerRectangle);
}
```

<figure><img src="../../../.gitbook/assets/24_07 07 19.gif" alt=""><figcaption><p>ScrollViewer displaying multiple ColoredRectangles</p></figcaption></figure>

### VerticalScrollBarVisibility

VerticalScrollBarVisibility controls the visibility of the vertical scroll bar, specifically in regards to the number of items in the ScrollViewer. The available values are:

* Auto - the ScrollBar displays only if needed based on the size of the inner panel
* Hidden - the ScrollBar remains invisible even if the contents of the inner panel exceed the size of its container
* Visible - the ScrollBar always displays

The default is Auto which means that the scroll bar only displays if necessary.

<figure><img src="../../../.gitbook/assets/30_12 14 23.gif" alt=""><figcaption><p>VerticalScrollBarVisibilty set to Auto only shows the ScrollBar if enough items are added to the ScrollView</p></figcaption></figure>
