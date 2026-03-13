# ScrollViewer

## Introduction

The ScrollViewer control provides a container which can hold Gum objects (including other Gum Forms objects). The user can scroll the ScrollViewer with the mouse or ScrollBar.

By default the ScrollViewer's InnerPanel expands automatically in response to its children and stacks its children top-to-bottom. Of course, this behavior can be changed since the InnerPanel is a standard GraphicalUiElement.

## Code Example: Creating a ScrollViewer with Forms Children

ScrollViewers can contain other forms controls. The following code creates a ScrollViewer and adds buttons using the AddChild method.

```csharp
// Initialize
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
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACm2PT0vDQBDF74F8hyGnhJQlKF6sEbSC9iJign8gUNZkbYZuZ2U7MWLpd3eTKNSme5md33szzNv6HkAw39w26-Ac2DZq0hMkZJQav5XDwae0sCmt0foJVasspECqhWwPhdG0oH2PuKqq3Dwaw2PpxS04Sw7p61H6jBXXTjlJRtKdwmXNx7U5kbIPkpQWGctylX3IEmnZmZ21oHdjIURiQIeSqSsXcNrVOI4K2hYE7nW53xpmQ7-Jr_umD9Tph3lnNeoqHCb-PEMncvXVXVoEwwr3gRjwv2emsVxBnEK4mMAigvRykMdrbiSrHNdK3JtW5CZj67L1Z-0C39v53g-0fe7K2AEAAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../.gitbook/assets/13_09 43 23.gif" alt=""><figcaption><p>Children in a ScrollViewer</p></figcaption></figure>

## Scrolling a ScrollViewer

ScrollViewers can be scrolled by the user using by performing any of the following actions:

* Clicking on the ScrollBar buttons or track
* Dragging the ScrollBar thumb
* Using the mouse wheel to scroll vertically
* Using shift+mouse wheel to scroll horizontally. For more information see the [Horizontal Scrolling](horizontal-scrolling.md) page.

## Code Example: Creating a ScrollViewer With Non-Forms Children

The following code creates a ScrollViewer and adds ColoredRectangleRuntimes to the ScrollViewer. Any non-Forms visual object can be added to the ScrollViewer through AddChild.

```csharp
// Initialize
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
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACnVRwU4CMRS8k_APL3tahCyI4SJ6MByUizG7RiXZS7P7ZF_svpq2iEr8d9sqZIGllyYz03lvpptuByCam9tVHV2C1SscBISYLAlJ3-jg6ENoMIVWUj4RrlHDNTCuIWtAcW-ac1OT3JTlo0qVssfUizOYjA7RRSv6TKWtHDMeHVF3SMvKtnNzZtQPglEmmRXFW_YuCuKlFztpzj6SFlyqehvmy1iskzRgYeVXpSEmtkBOMpq66wou_N3v93Le5AzueB_ys1IsrOClxH-_mZJKY7mD0xVbqjE4-4eHXc0qkmW8b7XV7qOhvr_dk3v8tJnLJZ0xnMF56M8_GQ6BlVsES7AKDLoU-yYLMMQFAoqiAr1b3viyTOvY7U-4Dlr53XecEoRKmvXEjRTxeDLpDZq5TiC-lJ-o2_npdn4Bp89eML4CAAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../.gitbook/assets/13_09 44 12.gif" alt=""><figcaption><p>ScrollViewer displaying multiple ColoredRectangles</p></figcaption></figure>

## Code Example: Wrapping Children

The following code shows how to wrap children in a ScrollViewer. It modifies the `InnerPanel` to change the layout type.

```csharp
// Initialize
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
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACmVQPU_DMBDdI-U_nCyGVEVRKgYkSoe2A6rUAdGILl5M4pIT4Vy5FwpE-e_YidomxYvt93F69-owABCrw1P1KR6AbaVvWwQJGVWJv9rB4ktZOGTWlOUr6qO2MAPSR9j0oGg0ldTXxFvMuXDKu0lyTc3zPDUvxvB_15yywtiou-KlJtbWi3wEJNL2WZEu3diBa3VmnPQii5cFlrnVtFY_pmLnGgLxWu_YJcH3gjesso-he2vV_nByOLOvxykk7VxCJAacQTIFhEeYJP4xHo8k1ZLAHR_4rWI23unbWrSfdmPPd9y5pXtfUg9P9bfPeyPFgglqbKQYCoZFpWbvdzkNv267XSLqnF7TiDBowuAP09hyWv4BAAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../../.gitbook/assets/11_07 54 07.png" alt=""><figcaption><p>ScrollViewer with wrapped children</p></figcaption></figure>

