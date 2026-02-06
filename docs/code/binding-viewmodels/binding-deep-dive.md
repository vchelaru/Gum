# Binding Visual Properties

## Introduction

Visual properties can be directly bound. These can be directly bound to properties on a ViewModel, or they can be bound through custom properties on the Forms control. This section discusses the multiple ways that binding can be performed on visuals.

{% hint style="warning" %}
Typical ViewModels do not include view-specific properties such as colors, fonts, or margins. This allows ViewModels to be reusable across multiple views and even across different platforms (such as Gum Forms vs WPF). However, there are times when binding view properties is preferred. This section discusses how to do so, but it is up to the developer to decide when such binding should be performed.
{% endhint %}

### Binding Visual Properties Directly

Visual properties can be bound directly. If the property is not specific to a type of Visual, then the Visual can be directly bound without being casted. For example, the following code shows how to bind the width of a Button to a ButtonWidth property on a ViewModel.

```csharp
// assume MyButton is a valid button
var buttonVisual = MyButton.Visual;
buttonVisual.SetBinding(
    nameof(buttonVisual.Width),
    nameof(ViewModel.ButtonWidth));
```

If the property requires a specific type, then the Visual can be casted to access type-specific properties, as shown in the following code block:

```csharp
// This assumes the project is code-only
var buttonVisual = (ButtonVisual)MyButton.Visual;
var text = buttonVisual.TextInstance;
text.SetBinding(
    nameof(Text.FontScale),
    nameof(ViewModel.ButtonFontScale));
```

### Binding Converter Properties (Code Generation)

If your project uses the Gum tool and code generation, then each component and screen has a _custom code_ file which can contain converter properties that can be bound. This allows ViewModels to avoid adding view-specific properties.

For example, consider a screen named `GameScreenHud` which contains a single label named `HealthLabel`.

<figure><img src="../../.gitbook/assets/23_10 58 42.png" alt=""><figcaption></figcaption></figure>

The ViewModel may contain general properties such as `CurrentHealth` and `IsLowHealth`, but the view will process these to display the health with a prefix, and to modify the Label property.

The following code could be added to `GameScreenHud.cs`, which is the custom code file created when the `GameScreenHud` is generated:

```csharp
partial class GameScreenHud
{
    public int CurrentHealth
    {
        set => this.HealthLabel.Text = 
                $"Current Health: {value}";
    }

    public bool IsLowHealth
    {
        set
        {
            var textInstance = HealthLabel.GetVisual<TextRuntime>();
            textInstance.Color = value 
                ? Microsoft.Xna.Framework.Color.Red 
                : Microsoft.Xna.Framework.Color.White;
        }
    }

    GameScreenViewModel ViewModel => (GameScreenViewModel)BindingContext;

    partial void CustomInitialize()
    {
        this.SetBinding(
            nameof(this.CurrentHealth),
            nameof(ViewModel.CurrentHealth));

        this.SetBinding(
            nameof(this.IsLowHealth),
            nameof(ViewModel.IsLowHealth));
    }
}

```
