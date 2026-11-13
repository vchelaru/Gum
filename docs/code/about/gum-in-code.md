# Gum in Code

## Introduction

Gum can be used purely in code (no editor), or by referencing a Gum project. Both approaches are well-supported so you can use Gum to fit your development style.

## Gum Code-Only

Gum can be used fully in code. A code-only setup requires minimal code. The following code creates a few functional Gum controls:

```csharp
using Gum.Forms.Controls;
using Gum.Wireframe;
using Gum;
using System;

// Initialize
var stackPanel = new StackPanel();
stackPanel.AddToRoot();
stackPanel.Spacing = 6;
stackPanel.Anchor(Anchor.Center);

var button = new Button();
stackPanel.AddChild(button);
button.Text = "Click Me";
button.Click += (s, e) => 
{
    button.Text = DateTime.Now.ToString();
};

var textBox = new TextBox();
textBox.Width = 150;
stackPanel.AddChild(textBox);

var listBox = new ListBox();
stackPanel.AddChild(listBox);
for(int i = 0; i < 10; i++)
{
    listBox.Items.Add("Item " + i);
}

// Update
GumUI.Update(gameTime);

// Draw
GumUI.Draw();
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA3VQwU4CMRD9laanJSHNgtEDhIOsxpCoMbKJB9dD3R1kwm5r2lkhEv7ddltAiexh2zfvzUzf2_KZvWsbPiLTQp-3FtWH5aNX7oriBQ0sjGyAv_U5KiSUNX4DH_EvaZglWa6epIKaTZiCNZsfCklvXKgjL66rKtfPWtMpMf-UpdvoBlyddKhyqU0SDpGBIjC-1y9-b4m0ikunHfhnYbbEukqC1rPhJnLYkGst2jQdDrMayxV7gICOolDvqtMJS2yfQY9NPL64LdS2UMx9fwfeSIIcGxCPei1yPSfjfHXP2sVnkxNO9Sa-Ow-oU0TG5V3R0vGDy_SMnajcJ1Gj_TXyPqCzWUS1pxcuW1TE0LWmY3d03jI28CD4Dv_ewW7sFjOCxvqhSUjN4xhnTIxhZ5vvfgAhBmriXQIAAA)

<figure><img src="../../.gitbook/assets/15_07 52 36.gif" alt=""><figcaption><p>Code-only Gum Controls</p></figcaption></figure>

## Gum Tool Projects in Code

The Gum Tool can be used to create projects visually. These projects can be loaded into your project and accessed in a type-safe way using Gum's generated code.

The following image shows a screen similar to the one above created in the Gum tool:

<figure><img src="../../.gitbook/assets/15_08 47 22.png" alt=""><figcaption></figcaption></figure>

After [loading the Gum project](../getting-started/setup/loading-a-gum-project-.gumx.md), the following code can be used to interact with the controls:

```csharp
// Initialize
var screen = new ExampleScreen();
screen.AddToRoot();

screen.ButtonStandardInstance.Click += (s,e) =>
{
    screen.ButtonStandardInstance.Text = DateTime.Now.ToString();
};

for(int i = 0; i < 10; i++)
{
    screen.ListBoxInstance.Items.Add("Item " + (i + 1));
}

// Update
GumUI.Update(gameTime);

// Draw
GumUI.Draw();
```

Generated code is optional - code can access Gum objects without any generated code as shown in the following block:

```csharp
// Initialize
var screen = ObjectFinder.Self.GumProjectSave.Screens
    .First().ToGraphicalUiElement();
screen.AddToRoot();

var button = screen.GetFrameworkElementByName<Button>("ButtonStandardInstance");
button.Click += (s,e) =>
{
    button.Text = DateTime.Now.ToString();
};

var listBox = screen.GetFrameworkElementByName<ListBox>("ListBoxInstance");
for (int i = 0; i < 10; i++)
{
    listBox.Items.Add("Item " + (i + 1));
}

// Update
GumUI.Update(gameTime);

// Draw
GumUI.Draw();
```
