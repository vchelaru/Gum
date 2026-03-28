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
// Initialize
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

    button.Click += (_, _) =>
    {
        viewModel.NumberOfClicks++;
    };

    base.Initialize();
}
```
[Try on XnaFiddle.NET](https://xnafiddle.net/#code=H4sIAAAAAAAACpVVyW7bMBC9-yvYoAcJCYQut7oJkNiNkYMbIIvToCgC2hrbRCjSICk5baB_71CkJdGSkZYXyjNvVr4Z55qJFZlKISc0g0meDQf5viiZ5DAGxQqU79RWfClVpjuCZCSFUZL3aMawpDk3M6ZzynUy-9yGTIsia_9-YAqWCjOoM2ILJbVcmuSHoMmlVW2len5DnUwU3azZAtMZbPI5Zwuy4FRrcpEbI8UDM-vbfG7gxZAvXjZ4HRA8dyi7yYVhGZAn0_xAR1btnWmjbHTvo9I4c3tWYMjpWWCdWLfDGqHB2YR29rAliTqG5N0pKSjPIQ6woaU9XUtv2IS2p6x_ua8yqK3ToijGJs2phsjFb-I6qHtZUrjrlERtcewu3779JBEtYNtuehQ3uZo108l5mo7WjKdBW1qgoOZzsVhLFQVM8sJkBMKAOmT5iKl8-nBAOZJcKgRUd_IInMutg5aDco9h315otuEwY7CdyhQ49q7-9hzzeCYM-Z5nc1DXyxEKnnU_kSZgviL2rN0a7VS3YCLHjF061fVzDBsQqb4WkcAOyGUUxonjXz1sdrox0xtOf1vv749eQ7sSa8Q70vHRsFO43RsfsVp7-0J3UziGgi1gSgVdgSKrejYrTJ7dgrJ6-3l_VVVcy3bbIxy_KhTS8rX25YnUGzCyPIqH5EpPZa7xZTSbc0s9o3AyavYraWBhICWyAKVYCqSQLCVXghlGOfvTpX-Vb9IC2EAnJFx4M1CaSYF7D99ob3Lae2heSXwdPTPYvL1D2sG4kzdS9ur-Zw68jd8XR9VTE5xNZDEOPb50jexwu6i_XN77gJ7ULphIkW32_8IvqB24A0V2e3QU7C_P6R3KdSg-6cPUzpOA3XH7LbwfV_cx7q-nE_IUIw8P7OjGZzgdx8dNBWXbP3axzZJwVg_x7n6TUgORpfqd3ZQr_9FPQo-uQa1e2uj96jcyGCu6_Yf4wchhF4GqyC3KkVRiiasS1EWzopqcK__7mVbCTp7l4C_6LtHtswgAAA)

<figure><img src="../../.gitbook/assets/14_06 37 46.gif" alt=""><figcaption><p>Button responding to clicks and updating itself through the ViewModel</p></figcaption></figure>

