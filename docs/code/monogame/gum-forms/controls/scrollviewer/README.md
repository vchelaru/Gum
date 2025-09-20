# ScrollViewer

## Introduction

The ScrollViewer control provides a container which can hold Gum objects (including other Gum Forms objects). The user can scroll the ScrollViewer with the mouse or ScrollBar.

By default the ScrollViewer's InnerPanel expands automatically in response to its children and stacks its children top-to-bottom. Of course, this behavior can be changed since the InnerPanel is a standard GraphicalUiElement.

## Code Example: Creating a ScrollViewer with Forms Children

ScrollViewers can contain other forms controls. The following code creates a ScrollViewer and adds buttons using the AddChild method.

```csharp
var scrollViewer = new ScrollViewer();
scrollViewer.AddToRoot();
scrollViewer.X = 50;
scrollViewer.Y = 50;
scrollViewer.Width = 200;
scrollViewer.Height = 200;
scrollViewer.InnerPanel.StackSpacing = 2;

for (int i = 0; i < 30; i++)
{
    var button = new Button();
    scrollViewer.AddChild(button);
    button.Text = "Button " + i;
    button.Click += (_, _) =>
        button.Text = DateTime.Now.ToString();
}
```

<figure><img src="../../../../../.gitbook/assets/13_09 43 23.gif" alt=""><figcaption><p>Children in a ScrollViewer</p></figcaption></figure>

## Scrolling a ScrollViewer

ScrollViewers can be scrolled by the user using by performing any of the following actions:

* Clicking on the ScrollBar buttons or track
* Dragging the ScrollBar thumb
* Using the mouse wheel to scroll vertically
* Using shift+mouse wheel to scroll horizontally. For more information see the [Horizontal Scrolling](horizontal-scrolling.md) page.

## Code Example: Creating a ScrollViewer With Non-Forms Children

The following code creates a ScrollViewer and adds ColoredRectangleRuntimes to the ScrollViewer. Any non-Forms visual object can be added to the ScrollViewer through AddChild.

```csharp
var scrollViewer = new ScrollViewer();
scrollViewer.AddToRoot();
scrollViewer.X = 50;
scrollViewer.Y = 50;
scrollViewer.Width = 200;
scrollViewer.Height = 200;
scrollViewer.InnerPanel.StackSpacing = 2;

var random = new System.Random();
for (int i = 0; i < 30; i++)
{
    var innerRectangle = new ColoredRectangleRuntime();
    scrollViewer.AddChild(innerRectangle);
    innerRectangle.X = random.NextSingle() * 150;
    // no need to set innerRectangle.Y since each rectangle stacks
    innerRectangle.Width = 30;
    innerRectangle.Height = 30;
    innerRectangle.Color = new Color(random.Next(255), random.Next(255), random.Next(255));
}
```

<figure><img src="../../../../../.gitbook/assets/13_09 44 12.gif" alt=""><figcaption><p>ScrollViewer displaying multiple ColoredRectangles</p></figcaption></figure>

## Code Example: Wrapping Children

The following code shows how to wrap children in a ScrollViewer. It modifies the `InnerPanel` to change the layout type.

```csharp
var scrollViewer = new ScrollViewer();
scrollViewer.Width = 310;
scrollViewer.AddToRoot();
scrollViewer.Anchor(Anchor.Center);
var innerPanel = scrollViewer.InnerPanel;
innerPanel.ChildrenLayout = ChildrenLayout.LeftToRightStack;
innerPanel.WrapsChildren = true;

for(int i= 0; i < 100; i++)
{
    var button = new Button();
    button.Width = 70;
    button.Text = $"Btn {i}";
    button.Anchor(Anchor.TopLeft);
    scrollViewer.AddChild(button);
}
```

<figure><img src="../../../../../.gitbook/assets/11_07 54 07.png" alt=""><figcaption><p>ScrollViewer with wrapped children</p></figcaption></figure>

## VerticalScrollBarVisibility

VerticalScrollBarVisibility controls the visibility of the vertical scroll bar, specifically in regards to the number of items in the ScrollViewer. The available values are:

* Auto - the ScrollBar displays only if needed based on the size of the inner panel
* Hidden - the ScrollBar remains invisible even if the contents of the inner panel exceed the size of its container
* Visible - the ScrollBar always displays

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



<figure><img src="../../../../../.gitbook/assets/13_09 47 19.gif" alt=""><figcaption><p>VerticalScrollBarVisibility set to Auto only shows the ScrollBar if enough items are added to the ScrollView</p></figcaption></figure>
