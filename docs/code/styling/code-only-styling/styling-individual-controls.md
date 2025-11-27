# Styling Individual Controls

{% hint style="info" %}
This document assumes using V3 styles, which were introduced at the end of November 2025. If your project is using V2 visuals, you need to upgrade to V3 before the styling discussed on this document can be used.

For information on upgrading, see the [Migrating to 2025 November](../../../gum-tool/upgrading/migrating-to-2025-november.md) page.
{% endhint %}

## Introduction

Individual controls can be styled through their Visual property. By casting the Visual property to the control-specific type, color values can be assigned on a control.

## Accessing Strongly-Typed Visual

Every control includes a Visual type which can be casted to access control-specific values. The type of each visual is the same name as the control, with the word `Visual` appended.

The following table shows which visuals and properties are available for each type of control:

<table><thead><tr><th>Control</th><th width="216.272705078125">Visual</th><th>Styling Properties</th></tr></thead><tbody><tr><td>Button</td><td>ButtonVisual</td><td><ul><li>BackgroundColor</li><li>FocusedIndicatorColor</li><li>ForegroundColor</li></ul></td></tr><tr><td>CheckBox</td><td>CheckBoxVisual</td><td><ul><li>BackgroundColor</li><li>FocusedIndicatorColor</li><li>ForegroundColor</li><li>CheckColor</li></ul></td></tr><tr><td>ComboBox</td><td>ComboBoxVisual</td><td><ul><li>BackgroundColor</li><li>FocusedIndicatorColor</li><li>ForegroundColor</li><li>DropdownIndicatorColor</li></ul></td></tr><tr><td>ItemsControl</td><td>ItemsControlVisual</td><td><ul><li>BackgroundColor</li><li>FocusedIndicatorColor</li></ul></td></tr><tr><td>Label</td><td>LabelVisual</td><td><ul><li>Color</li></ul></td></tr><tr><td>ListBoxItem</td><td>ListBoxItemVisual</td><td><ul><li>HighlightedBackgroundColor</li><li>SelectedBackgroundColor</li><li>ForegroundColor</li><li>FocusedIndicatorColor</li></ul></td></tr><tr><td>ListBox</td><td>ListBoxVisual</td><td><ul><li>BackgroundColor</li><li>FocusedIndicatorColor</li></ul></td></tr><tr><td>Menuitem</td><td>MenuItemVisual</td><td><ul><li>HighlightedBackgroundColor</li><li>SelectedBackgroundColor</li><li>ForegroundColor</li><li>SubmenuIndicatorColor</li></ul></td></tr><tr><td>Menu</td><td>MenuVisual</td><td><ul><li>BackgroundColor</li></ul></td></tr><tr><td>PasswordBox</td><td>PasswordBoxVisual</td><td><ul><li>BackgroundColor</li><li>ForegroundColor</li><li>SelectionBackgroundColor</li><li>PlaceholderColor</li><li>CaretColor</li><li>FocusedIndicatorColor</li></ul></td></tr><tr><td>RadioButton</td><td>RadioButtonVisual</td><td><ul><li>BackgroundColor</li><li>ForegroundColor</li><li>RadioColor</li><li>FocusedIndicatorColor</li></ul></td></tr><tr><td>ScrollBar</td><td>ScrollBarVisual</td><td><ul><li>TrackBackgroundColor</li><li>ScrollArrowColor</li></ul></td></tr><tr><td>ScrollViewer</td><td>ScrollViewerVisual</td><td><ul><li>BackgroundColor</li><li>FocusedIndicatorColor</li></ul></td></tr><tr><td>Slider</td><td>SliderVisual</td><td><ul><li>BackgroundColor</li><li>TrackBackgroundColor</li><li>FocusedIndicatorColor</li></ul></td></tr><tr><td>Splitter</td><td>SplitterVisual</td><td><ul><li>BackgroundColor</li></ul></td></tr><tr><td>TextBox</td><td>TextBoxVisual</td><td><ul><li>BackgroundColor</li><li>ForegroundColor</li><li>SelectionBackgroundColor</li><li>PlaceholderColor</li><li>CaretColor</li><li>FocusedIndicatorColor</li></ul></td></tr><tr><td>Window</td><td>WindowVisual</td><td><ul><li>BackgroundColor</li></ul></td></tr></tbody></table>

## Code Example: Changing BackgroundColor

The following code shows how to access the Visual on a Button and TextBox to change the background color of each control:

```csharp
var button = new Button();
button.AddToRoot();
var buttonVisual = (ButtonVisual)button.Visual;
buttonVisual.BackgroundColor = Color.Red;

var textBox = new TextBox();
textBox.AddToRoot();
textBox.Y = 32;
var textBoxVisual = (TextBoxVisual)textBox.Visual;
textBoxVisual.BackgroundColor = Color.Blue;
```

<figure><img src="../../../.gitbook/assets/26_07 17 29.png" alt=""><figcaption></figcaption></figure>

{% hint style="info" %}
V3 Visuals no longer require changing colors on each individual state. By changing values like BackgroundColor, the visual automatically uses the color for other states such as Highlighted and Pushed.
{% endhint %}

## Color Properties vs Visual Element Properties

Each color property listed above ultimately sets the color of one of the parts of a control. These individual parts are also accessible through the casted visual `Visual`, but usually these color values should not be directly changed. Setting a property directly on a visual may only be temporary - colors can be reset in response to actions such as highlight, push, or variable changes such as IsEnabled.

For example, the following code sets the `Background.Color` property on a `Button`, and this seems to change the color; however, the background color resets back when the user hovers over the button.

```csharp
var button = new Button();
button.AddToRoot();
button.Anchor(Anchor.Center);
var buttonVisual = (ButtonVisual)button.Visual;
buttonVisual.Background.Color = Color.Pink;
```

<figure><img src="../../../.gitbook/assets/26_08 47 45.gif" alt=""><figcaption><p>Color is only <strong>temporary</strong> - hover resets the color back to BackgroundColor</p></figcaption></figure>

Since ButtonVisual exposes a `BackgroundColor` property, this should be used rather than directly setting the `Background.Color` value. In general, it's best to check if a color property already exists before making any changes to a Visual's child.&#x20;

## Advanced styling

For more control over colors and states, see the [Styling Using States](styling-using-states.md) page.
