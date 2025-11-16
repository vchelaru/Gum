# Button

## Introduction

The Button control providing an event for handling clicks.

## Code Example: Adding a Button

The following code adds a button which increments every time it is clicked:

{% tabs %}
{% tab title="Separate Method" %}
```csharp
Button button;

void SomeInitializationFunction()
{
    ...
    button = new Button();
    button.AddToRoot();
    button.X = 50;
    button.Y = 50;
    button.Width = 100;
    button.Height = 50;
    button.Text = "Hello MonoGame!";
    int clickCount = 0;
    button.Click += HandleClick
}

void HandleClick(object sender, EventArgs args)
{
    clickCount++;
    button.Text = $"Clicked {clickCount} times";
}
```
{% endtab %}

{% tab title="Lambda" %}
```csharp
var button = new Button();
button.AddToRoot();
button.X = 50;
button.Y = 50;
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
{% endtab %}
{% endtabs %}



<figure><img src="../../.gitbook/assets/13_08 53 05.gif" alt=""><figcaption><p>Button responding to clicks by incrementing clickCount</p></figcaption></figure>

## Clicking Programmatically

Clicking can be performed programmatically by calling PerformClick. The following example shows how to click a button when the Enter key is pressed:

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
