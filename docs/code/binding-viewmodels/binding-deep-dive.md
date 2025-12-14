# Binding Deep Dive

## DependsOn

Gum's `ViewModel` supports using the `DependsOn` attribute to define dependencies between properties. By using this attribute, changes in one property can result in changes to other properties. The same property can be used as a dependency for multiple properties, allowing changes to one property resulting in many pieces of UI being updated.

The following code shows how a single variable can be used to update multiple UI properties. First we can declare a ViewModel using `DependsOn` to make `IsBrokeTextVisible` and `MoneyDisplay` depend on `Money`.

```csharp
class PlayerViewModel : ViewModel
{
    public int Money
    {
        get => Get<int>();
        set => Set(value);
    }

    [DependsOn(nameof(Money))]
    public bool IsBrokeTextVisible => Money <= 0;

    [DependsOn(nameof(Money))]
    public string MoneyDisplay => $"${Money:N0}";
}
```

We can use the `PlayerViewModel` to update the UI in response to `Money` changing. The following block of code shows how to do so in a code-only project.

```csharp
var viewModel = new PlayerViewModel();

var mainPanel = new StackPanel();
mainPanel.AddToRoot();
mainPanel.BindingContext = viewModel;

var addMoneyButton = new Button();
mainPanel.AddChild(addMoneyButton);
addMoneyButton.Text = "+";
addMoneyButton.Click += (_, _) =>
    viewModel.Money += 100;

var subtractMoneyButton = new Button();
mainPanel.AddChild(subtractMoneyButton);
subtractMoneyButton.Text = "-";
subtractMoneyButton.Click += (_, _) =>
    viewModel.Money -= 100;

var moneyLabel = new Label();
mainPanel.AddChild(moneyLabel);
moneyLabel.SetBinding(
    nameof(moneyLabel.Text),
    nameof(viewModel.MoneyDisplay));

var isBrokeLabel = new Label();
mainPanel.AddChild(isBrokeLabel);
isBrokeLabel.Text = "No more money!";
isBrokeLabel.SetBinding(
    nameof(isBrokeLabel.IsVisible),
    nameof(viewModel.IsBrokeTextVisible));
```

If this code were in a Screen defined in Gum with code generation, the code might look like the following block:

```csharp
void CustomInitialize()
{
    var viewModel = new PlayerViewModel();
    // Can be added to the entire screen:
    this.BindingContext = viewModel;
    
    AddMoneyButton.Click += (_, _) =>
        viewModel.Money += 100;
        
    SubtractMoneyButton.Click += (_, _) =>
        viewModel.Money -= 100;
        
    MoneyLabel.SetBinding(
        nameof(MoneyLabel.Text),
        nameof(viewModel.MoneyDisplay));
        
    IsBrokeLabel.SetBinding(
        nameof(IsBrokeLabel.IsVisible),
        nameof(viewModel.IsBrokeTextVisible));
}
```

<figure><img src="../../.gitbook/assets/13_08 51 15.gif" alt=""><figcaption><p>Two buttons updating money</p></figcaption></figure>

## Binding Visual Properties

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
