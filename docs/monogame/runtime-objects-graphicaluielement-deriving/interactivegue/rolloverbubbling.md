# RollOverBubbling

### Introduction

The RollOverBubbling event is raised whenever the cursor rolls over an InteractiveGue. A roll is defined as the cursor being positioned over the bounds of an InteractiveGue when the cursor's X or Y values have changed.

This event is raised top-down, with children object having the first opportunity to handle the event. If the event is not handled by the child, then parents have the opportunity to handle the event. If the RoutedEventArgs Handled property is set to true, then no parents receive the event.

### Code Example - Scrolling with Cursor Press

The following code shows how to implement ListBox scrolling using the cursor when the cursor is pressed.

```csharp
scrollViewer.Visual.RollOverBubbling += (sender, args) =>
{
    var cursor = FormsUtilities.Cursor;

    // Only handle this if the crusor is pressed
    if (cursor.PrimaryDown)
    {
        // we can get the vertical scroll bar through visuals:
        var scrollBarVisual = (InteractiveGue) scrollViewer.Visual.GetChildByNameRecursively(
            ListBox.VerticalScrollBarInstanceName);

        // InteractiveGues provide a FormsControlAsObject property to access
        // the forms object.
        var scrollBar = (ScrollBar)scrollBarVisual.FormsControlAsObject;

        // Change the ScrollBar's value which results in the ListBox scrolling.
        scrollBar.Value -= cursor.YChange /
            global::RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom;

        // set the Handled property true so children do not get this
        args.Handled = true;
    }
};

```

<figure><img src="../../../.gitbook/assets/16_19 51 01.gif" alt=""><figcaption><p>Scrolling a ListBox by pressing and moving the cursor</p></figcaption></figure>
