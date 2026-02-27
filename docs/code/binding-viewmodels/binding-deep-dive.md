# Binding Visual Properties

## Introduction

Visual properties can be directly bound, either directly bound to properties on a ViewModel, or through custom properties on the Forms control. This section discusses the multiple ways that binding can be performed on visuals.

{% hint style="warning" %}
Although typical ViewModels do not include view-specific properties such as colors, fonts, or margins, sometimes binding view-specific properties is useful. This section discusses how to do so, but it is up to the developer to decide when such binding should be performed.
{% endhint %}

### Binding Visual Properties Directly

Visual properties can be bound directly. For example, the following code shows how to bind the width of a Button to a ButtonWidth property on a ViewModel.

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

The ViewModel may contain general properties such as `CurrentHealth` and `IsLowHealth`, but the view can process these properties to display the health with a prefix, and to modify the Label property.

The following code could be added to `GameScreenHud.cs`, which is the custom code file created when the `GameScreenHud` is generated:

```csharp
// assuming a ViewModel with the following properties:
class GameScreenViewModel : ViewModel
{
    public int Health
    {
        get => Get<int>();
        set => Set(value);
    }

    [DependsOn(nameof(Health))]
    public bool IsLowHealth => Health < 10;
}

partial class GameScreenHud
{
    // Processing is performed in the View, so the ViewModel doesn't need
    // to include view-specific properties.
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

### Binding to States (Code Generation)

If your project uses code generation, enums are automatically created for every category in your screens and components. The current state for each category can be bound to a ViewModel property. This can be done by adding a type to your ViewModel, or through a binding converter property.

For this example, consider a component named ComponentWithState which has a SizeCategory with two states: Large and Small.

<figure><img src="../../.gitbook/assets/27_06 19 01.png" alt=""><figcaption></figcaption></figure>

This component generates an enum similar to the following block (you do not need to write this):

```csharp
public enum SizeCategory
{
    Large,
    Small,
}
```

Similarly, the generated code also includes a property which can be bound:

```csharp
public SizeCategory? SizeCategoryState
{
    get => _sizeCategoryState;
    set
    {
        _sizeCategoryState = value;
        ...
```

The ViewModel can directly contain a property of type SizeCategory, or a more abstract property can be used with a binding converter property in the custom code. Which you choose depends on how you like to organize your code.

{% tabs %}
{% tab title="Binding Converter Property" %}
```csharp
class MyViewModel : ViewModel
{
    // This could be a property with getter and setter, or it could
    // be a getter-only property which depends on a different property
    public bool IsLarge
    {
        get => Get<bool>();
        set => Set(value);
    }
}

protected override void Initialize()
{
    // either one of these:
    GumUI.Initialize(this, "GumProject/GumProject.gumx");

    var screen = new MainMenu();
    screen.AddToRoot();

    screen.BindingContext = new MyViewModel();
    screen.ComponentWithStateInstance.SetBinding(
        nameof(screen.ComponentWithStateInstance.IsLarge),
        nameof(MyViewModel.IsLarge));
    // rest of code...
}
```

This requires an extra property in the custom code for the component:

```csharp
// in ComponentWithState.cs
partial class ComponentWithState
{
    public bool IsLarge
    {
        set => SizeCategoryState = value ? SizeCategory.Large : SizeCategory.Small;
    }

    partial void CustomInitialize()
    {   
    }
}
```
{% endtab %}

{% tab title="SizeCategory in ViewModel" %}
```csharp
class MyViewModel : ViewModel
{
    // This could be a property with getter and setter, or it could
    // be a getter-only property which depends on a different property
    public ComponentWithState.SizeCategory SizeCategory
    {
        get => Get<ComponentWithState.SizeCategory>();
        set => Set(value);
    }
}

protected override void Initialize()
{
    // either one of these:
    GumUI.Initialize(this, "GumProject/GumProject.gumx");

    var screen = new MainMenu();
    screen.AddToRoot();

    screen.BindingContext = new MyViewModel();
    screen.ComponentWithStateInstance.SetBinding(
        nameof(screen.ComponentWithStateInstance.SizeCategoryState),
        nameof(MyViewModel.SizeCategory));
    // rest of code ...
}
```
{% endtab %}
{% endtabs %}



