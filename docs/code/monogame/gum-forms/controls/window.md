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

<figure><img src="../../../../.gitbook/assets/03_07 10 38.gif" alt=""><figcaption><p>Window responding to move and resize actions</p></figcaption></figure>
