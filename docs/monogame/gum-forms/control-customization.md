# Control Customization

### Introduction

Gum Forms provide fully functional controls with minimal setup. These controls can be restyled in code, either per-instance, or globally per control type.

### Customizing an Instance

Individual instances can be customized by modifying the built-in states which are added automatically by the default implementations.

The following code shows how to modify a Button instance. Note that all code in this document assumes that Gum Forms has already been initialized.

A default button can be constructed using the following code:

```csharp
var customizedButton = new Button();
this.Root.Children.Add(customizedButton.Visual);
```

Notice that this button has subtle color changes when the cursor hovers over or pushes on it.

<figure><img src="../../.gitbook/assets/26_07 28 29.gif" alt=""><figcaption><p>Button responding to hover and push events</p></figcaption></figure>

These events result in different states being applied to the button. These states are initialized by default when calling the following code:

```csharp
FormsUtilities.InitializeDefaults();
```

We can customize the state by modifying the values. For example, we can change the color of the background by adding the following code:

```csharp
// ButtonCategory is the category that all Buttons must have
var category = customizedButton.Visual.Categories["ButtonCategory"];

// Highlighted state is applied when the button is hovered over
var highlightedState = category.States.Find(item => item.Name == "Highlighted");
// remove all old styling:
highlightedState.Variables.Clear();
// Add the new color:
highlightedState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
{
    Name = "ButtonBackground.Color",
    Value = Color.Yellow
});
```

Now the button highlights yellow instead of a lighter blue.

<figure><img src="../../.gitbook/assets/26_07 35 48.gif" alt=""><figcaption><p>Button highlighting yellow on hover</p></figcaption></figure>

Note that any property on the button or its children can be modified through states. For example, we can also change the text color and size as shown in the following code. Note that you may need to make the button bigger so it can contain the larger text.

```csharp
// make the button bigger to hold the larger text:
customizedButton.Width = 200;
customizedButton.Height = 50;

highlightedState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
{
    Name = "TextInstance.Color",
    Value = Color.Black
});

highlightedState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
{
    Name = "TextInstance.FontScale",
    // FontScale expects a float value, so use 2.0f instead of 2
    Value = 2.0f
});
```

The button text now becomes black and is twice as big when highlighted but notice that the text changes are not undone when the cursor moves off of the button (when the Highlighted state is unset).

<figure><img src="../../.gitbook/assets/26_07 46 55.gif" alt=""><figcaption><p>Hover state is applied, but not undone</p></figcaption></figure>

The reason that the hover state is not unset is because all variables which are set through states persist until they are undone. Typically if you create states in the Gum tool, the Gum tool forces any state which is set in a category to also be propagated through all other states in the same category. However, when we're setting states in code, we must make sure to apply any states that we care to all other categories.

In this case we can fix the larger text by setting the TextInstance's color and FontScale back to its default:

```csharp
var enabledState = category.States.Find(item => item.Name == "Enabled");
enabledState.Variables.Clear();
enabledState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
{
    Name = "ButtonBackground.Color",
    Value = new Color(0, 0, 128),
});

enabledState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
{
    Name = "TextInstance.Color",
    Value = Color.White
});

enabledState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
{
    Name = "TextInstance.FontScale",
    // FontScale expects a float value, so use 2.0f instead of 2
    Value = 1.0f
});
```

<figure><img src="../../.gitbook/assets/26_07 50 10.gif" alt=""><figcaption><p>Enabled state resetting text color and size</p></figcaption></figure>

