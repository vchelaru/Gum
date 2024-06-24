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
