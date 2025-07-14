# Window

## Introduction

Window is a forms control which can be moved and resized with the cursor. It can be used for movable UI such as an inventory popup, or for floating toolbars in game editors and tools.

## Code Example

The following code creates a message box window.

```csharp
var window = new Window();
window.Anchor(Gum.Wireframe.Anchor.Center);
window.Width = 300;
window.Height = 200;
window.AddToRoot();

var textInstance = new Label();
textInstance.Dock(Gum.Wireframe.Dock.Top);
textInstance.Y = 24;
textInstance.Text = "Hello I am a message box";
window.AddChild(textInstance);

var button = new Button();
button.Anchor(Gum.Wireframe.Anchor.Bottom);
button.Y = -10;
button.Text = "Close";
window.AddChild(button.Visual);
button.Click += (_, _) =>
{
    window.RemoveFromRoot();
};

```

<figure><img src="../../../../.gitbook/assets/14_06 14 56.gif" alt=""><figcaption><p>Window responding to move and resize actions</p></figcaption></figure>

## Preventing Sizing and Moving

Resizing can be disabled by setting a Window's `ResizeMode` to `ResizeMode.NoResize`. By default this value is set to `ResizeMode.CanResize`. Note that the ResizeMode property does not affect whether a window can be moved by its title bar.

Window movement and resizing can be restricted by removing or modifying the IsEnabled property on the edges and corners of the window. By default each Window instance contains nine children which are used to resize and move the window. The items have the following names:

* TitleBarInstance (moves the entire window)
* BorderTopLeftInstance
* BorderTopRightInstance
* BorderBottomLeftInstance
* BorderBottomRightInstance
* BorderTopInstance
* BorderBottomInstance
* BorderLeftInstance
* BorderRightInstance

Any of these objects can be disabled to prevent the user from moving or resizing a window.

Note that changing any of these values will not update the visual appearance of the Window. These values only control the behavior of the control. Any changes to apperance must be either performed through the Visual object, or by creating a custom Window implementation.

## Code Example: Disabling Resizing with ResizeMode

The following code disabled resizing by setting ResizeMode:

```csharp
var window = new Window();
window.AddToRoot();
window.ResizeMode = ResizeMode.NoResize;
```

## Code Example: Forced Docking

The following code shows how to force dock a window to the right side of the screen. The user can still resize the window horizontally by grabbing the left edge of the window since the left side has not been disabled.

```csharp
var window = new Window();
window.AddToRoot();

window.Dock(Dock.Right);

// make it so the user cannot resize or move the window
// except for horizontally:
window.GetFrameworkElement("TitleBarInstance").IsEnabled = false;

window.GetFrameworkElement("BorderTopLeftInstance").IsEnabled = false;
window.GetFrameworkElement("BorderTopRightInstance").IsEnabled = false;
window.GetFrameworkElement("BorderBottomLeftInstance").IsEnabled = false;
window.GetFrameworkElement("BorderBottomRightInstance").IsEnabled = false;

window.GetFrameworkElement("BorderTopInstance").IsEnabled = false;
window.GetFrameworkElement("BorderBottomInstance").IsEnabled = false;
window.GetFrameworkElement("BorderRightInstance").IsEnabled = false;
```

<figure><img src="../../../../.gitbook/assets/14_06 16 42.gif" alt=""><figcaption><p>Window resizing only from the left edge</p></figcaption></figure>

Each individual border and title bar instance can be independently disabled to customize the resize/movement behavior. For example, the following line is specifically responsible for disabling the ability to move the window:

```csharp
window.GetFrameworkElement("TitleBarInstance").IsEnabled = false;
```
