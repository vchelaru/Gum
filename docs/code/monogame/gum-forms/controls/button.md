# Button

## Introduction

The Button control providing an event for handling clicks.

## Code Example: Adding a Button

The following code adds a button which increments every time it is clicked:

```csharp
var button = new Button();
Root.Children.Add(button.Visual);
button.X = 0;
button.Y = 0;
button.Width = 100;
button.Height = 50;
button.Text = "Hello MonoGame!";
int clickCount = 0;
button.Click += (_, _) =>
{
    clickCount++;
    button.Text = $"Clicked {clickCount} times";
};
```

<figure><img src="../../../../.gitbook/assets/24_06 36 41 (1).gif" alt=""><figcaption><p>Button responding to clicks by incrementing clickCount</p></figcaption></figure>

## Clicking Programatically

Clicking can be performed programatically by calling PerformClick. The following example shows how to click a button when the Enter key is pressed:

```csharp
var keyboard = FormsUtilities.Keyboard;
if(keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Enter))
{
    button.PerformClick();
}
```

Optionally you can pass the input device to the PerformClick method:

```csharp
button.PerformClick(keyboard);
```
