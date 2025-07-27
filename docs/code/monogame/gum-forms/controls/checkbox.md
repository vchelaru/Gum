# CheckBox

## Introduction

The CheckBox control provides the ability to display a true/false state and allows the user to toggle the state through clicking.

## Code Example: Adding a CheckBox

```csharp
var checkBox = new CheckBox();
checkBox.AddToRoot();
checkBox.X = 50;
checkBox.Y = 50;
checkBox.Text = "Checkbox";
checkBox.Checked += (_,_) => Debug.WriteLine($"IsChecked:{checkBox.IsChecked}");
checkBox.Unchecked += (_, _) => Debug.WriteLine($"IsChecked:{checkBox.IsChecked}");
```

<figure><img src="../../../../.gitbook/assets/13_08 55 15.gif" alt=""><figcaption><p>CheckBox responding to Checked and Unchecked events by printing output</p></figcaption></figure>

## CheckBox Width and Height

Default CheckBoxes have the following default values:

<table><thead><tr><th width="151.5999755859375">Variable</th><th width="199.5999755859375">Value</th></tr></thead><tbody><tr><td>Width</td><td>128</td></tr><tr><td>WidthUnits</td><td>Absolute</td></tr><tr><td>Height</td><td>32</td></tr><tr><td>HeightUnits</td><td>Absolute</td></tr></tbody></table>

The text within the CheckBox draws and wraps according to the bounds of the CheckBox. In other words, longer text results in line wrapping, as show in the following code block:

```csharp
var checkBox = new CheckBox();
checkBox.Text = "This is some longer text";
stackPanel.AddChild(checkBox);
```

<figure><img src="../../../../.gitbook/assets/13_08 56 16.png" alt=""><figcaption><p>ComboBox with wrapped text</p></figcaption></figure>
