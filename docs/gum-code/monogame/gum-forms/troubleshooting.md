# Troubleshooting

### Introduction

This page discusses common troubleshooting techniques when your Gum Forms objects are not behaving as expected.

### Cursor.WindowOver

You can check the WindowOver property to see what the Cursor believes it is over. This can tell you if the Cursor is over the object that you expect it to be over. The following code can be used to output the CursorOver to Visual Studio's output window:

```csharp
string windowOver = "<null>";
var cursor = FormsUtilities.Cursor;
if(cursor.WindowOver != null)
{
    windowOver = $"{cursor.WindowOver.GetType().Name} with name {cursor.WindowOver.Name}";
}
System.Diagnostics.Debug.WriteLine($"Window over: {windowOver}");

SystemManagers.Default.Activity(gameTime.TotalGameTime.TotalSeconds);
```

### Clicks are Offset

Clicks could be offset if the GraphicalUiElement's CanvasWidth and CanvasHeight are set incorrectly. For more information, see the [CanvasHeight page](../../gum-code-reference/graphicaluielement/canvasheight.md).

You can also output the Cursor's X and Y values to the screen to compare them to the desired object's bounds, as shown in the following code:

```csharp
var cursor = FormsUtilities.Cursor;
var cursorX = cursor.X;
var cursorY = cursor.Y;
var left = FormsObject.Visual.GetAbsoluteLeft();
var right = FormsObject.Visual.GetAbsoluteRight(); 
var top = FormsObject.Visual.GetAbsoluteTop(); 
var bottom = FormsObject.Visual.GetAbsoluteBottom(); 
System.Diagnostics.Debug.WriteLine($"Cursor ({cursorX}, {cursorY}) vs Left:{left} Right:{right} Top:{top} Bottom:{bottom}");
```
