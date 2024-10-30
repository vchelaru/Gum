# Button

### Introduction

The Button control providing an event for handling clicks.

### Code Example: Adding a Button

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

<figure><img src="../../../.gitbook/assets/24_06 36 41 (1).gif" alt=""><figcaption><p>Button responding to clicks by incrementing clickCount</p></figcaption></figure>
