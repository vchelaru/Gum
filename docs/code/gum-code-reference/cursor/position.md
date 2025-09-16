# Position

## Introduction

Cursor includes a number of properties for getting its current position. The position can be reported in screen pixels or in Gum units (considering zooming).

## Properties

The following properties are available for position?

* `X` - the Cursor's horizontal position measured in screen space&#x20;
* `Y` - the Cursor's vertical position measured in screen space
* `XRespectingGumZoomAndBounds()` - the Cursor's horizontal position respecting camera properties like zoom
* `YRespectingGumZoomAndBounds()` - the Cursor's vertical position respecting camera properties like zoom

## Code Example

The following code shows how to add ColoredRectangleRuntime instances when the Cursor is clicked. This approach respects camera zoom:

```csharp
protected override void Initialize()
{
    GumUI.Initialize(this);
    GumUI.Renderer.Camera.Zoom = 2;
    base.Initialize();
}


protected override void Update(GameTime gameTime)
{
    GumUI.Update(gameTime);

    var cursor = GumUI.Cursor;
    if(cursor.PrimaryPush)
    {
        var rectangle = new ColoredRectangleRuntime();
        rectangle.X = cursor.XRespectingGumZoomAndBounds();
        rectangle.Y = cursor.YRespectingGumZoomAndBounds();
        rectangle.AddToRoot();
    }

    base.Update(gameTime);
}
```

<figure><img src="../../../.gitbook/assets/12_06 40 49.gif" alt=""><figcaption></figcaption></figure>
