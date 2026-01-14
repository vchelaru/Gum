# Binding Custom View Properties

## Introduction

Binding in Gum supports binding to built-in properties as well as custom properties. This page shows how to perform binding on custom properties.

## Custom Properties in Controls

Custom properties are typically added to views to control additional visuals. For example, a button may have a TextRuntime subtext, and its Text property might be exposed through a property as is shown in the following code:

```csharp
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Microsoft.Xna.Framework;
using MonoGameGum.GueDeriving;
using System;

namespace MonoGameAndGum;

public class ButtonWithSubtext : Button
{
    TextRuntime _textRuntime;

    public string Subtext
    {
        get => _textRuntime.Text;
        set
        {
            if (_textRuntime.Text != value)
            {
                _textRuntime.Text = value;
            }
        }
    }

    public ButtonWithSubtext() : base()
    {
        ButtonVisual visual = (ButtonVisual)Visual;

        _textRuntime = new TextRuntime();
        this.AddChild(_textRuntime);
        _textRuntime.Anchor(Gum.Wireframe.Anchor.Center);
        _textRuntime.Y = 20;
        _textRuntime.Color = Color.Yellow;
    }
}
```

An instance of this view could be created using the following code:

```csharp
ButtonWithSubtext button = new ();
button.AddToRoot();
button.Anchor(Anchor.Center);
button.Text = "Click me please";
button.Subtext = "0 clicks";
```

Although this property is a standard C# property, it can be (one way) bound with no additional code on the view. This makes binding very easy since it does not require the creation of a dependency property.

The following code shows how to create a view model, and bind the Subtext property:

```csharp
class ExampleViewModel : ViewModel
{
    public int NumberOfClicks
    {
        get => Get<int>();
        set => Set(value);
    }

    [DependsOn(nameof(NumberOfClicks))]
    public string ClicksDisplay => $"{NumberOfClicks} click(s)";
}

protected override void Initialize()
{
    // either one of these:
    //GumUI.Initialize(this, "GumProject/GumProject.gumx");
    GumUI.Initialize(this, Gum.Forms.DefaultVisualsVersion.Newest);

    ButtonWithSubtext button = new ();
    button.AddToRoot();
    button.Anchor(Anchor.Center);
    button.Text = "Click me please";

    ExampleViewModel viewModel = new ();
    button.BindingContext = viewModel;
    button.SetBinding(
        nameof(button.Subtext),
        nameof(viewModel.ClicksDisplay));

    button.Click += (not, used) => 
    {
        viewModel.NumberOfClicks++;
    };

    base.Initialize();
}
```

<figure><img src="../../.gitbook/assets/14_06 37 46.gif" alt=""><figcaption><p>Button responding to clicks and updating itself through the ViewModel</p></figcaption></figure>

