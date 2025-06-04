# ScrollViewer

## Introduction

The ScrollViewer control provides a container which can hold Gum objects (including other Gum Forms objects). The user can scroll the ScrollViewer with the mouse or ScrollBar.

By default the ScrollViewer's InnerPanel expands automatically in response to its children and stacks its children top-to-bottom. Of course, this behavior can be changed since the InnterPanel is a standard GraphicalUiElement.

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
    button.Height = 36;
    button.Click += (_, _) => 
        button.Text = DateTime.Now.ToString();
}
```

<figure><img src="../../../../../.gitbook/assets/09_19 09 46.gif" alt=""><figcaption><p>Children in a ScrollViewer</p></figcaption></figure>

## Code Example: Creating a ScrollViewer With Non-Forms Children

The following code creates a ScrollViewer and adds ColoredRectangleRuntimes to the ScrollViewer. Any non-Forms visual object can be be added to the ScrollViewer through its InnerPanel.

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
    innerRectangle.X = random.NextSingle() * 150;
    // no need to set innerRectangle.Y since each rectangle stacks
    innerRectangle.Width = 30;
    innerRectangle.Height = 30;
    innerRectangle.Color = new Color(random.Next(255), random.Next(255), random.Next(255));
    scrollViewer.InnerPanel.Children.Add(innerRectangle);
}
```

<figure><img src="../../../../../.gitbook/assets/24_07 07 19.gif" alt=""><figcaption><p>ScrollViewer displaying multiple ColoredRectangles</p></figcaption></figure>

## VerticalScrollBarVisibility

VerticalScrollBarVisibility controls the visibility of the vertical scroll bar, specifically in regards to the number of items in the ScrollViewer. The available values are:

* Auto - the ScrollBar displays only if needed based on the size of the inner panel
* Hidden - the ScrollBar remains invisible even if the contents of the inner panel exceed the size of its container
* Visible - the ScrollBar always displays

The default is Auto which means that the scroll bar only displays if necessary.

<figure><img src="../../../../../.gitbook/assets/30_12 14 23.gif" alt=""><figcaption><p>VerticalScrollBarVisibility set to Auto only shows the ScrollBar if enough items are added to the ScrollView</p></figcaption></figure>
