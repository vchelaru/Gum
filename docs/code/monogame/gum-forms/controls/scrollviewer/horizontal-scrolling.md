# Horizontal Scrolling

## Introduction

ScrollViewer supports horizontal scrolling. Once enabled, horizontal scrolling can be performed with the horizontal scroll bar.

## InnerPanelInstance and Horizontal Scrolling

The InnerPanelInstance must be modified to enable scrolling. By default InnerPanelInstance has its WidthUnits set to RelativeToParent, which means it will not resize itself to be any larger than the parent's width regardless of its contents.

The easiest way to modify this is to change the InnerPanelInstance so it sizes itself according to its children both vertically and horizontally. This can be performed by calling Dock on the InnerPanelInstance as shown in the following code:

```csharp
var scrollViewer = new ScrollViewer();
scrollViewer.AddToRoot();
scrollViewer.Anchor(Anchor.Center);

var visual = (ScrollViewerVisual)scrollViewer.Visual;
visual.InnerPanelInstance.Dock(Dock.SizeToChildren);

// Now any child added to the scrollviewer will cause it to expand:
var random = Random.Shared;
for (int i = 0; i < 10; i++)
{
    var coloredRectangle = new ColoredRectangleRuntime();
    scrollViewer.AddChild(coloredRectangle);
    coloredRectangle.X = random.Next(0, 150);
    // no need to specify Y - the scroll viewer already
    // stacks its children
    coloredRectangle.Red = (byte)random.Next(0, 256);
    coloredRectangle.Green = (byte)random.Next(0, 256);
    coloredRectangle.Blue = (byte)random.Next(0, 256);
}
```

<figure><img src="../../../../../.gitbook/assets/20_05 27 11.gif" alt=""><figcaption><p>ScrollViewer with horizontal scrolling</p></figcaption></figure>
