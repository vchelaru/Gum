# CheckBox

## Introduction

The CheckBox control provides the ability to display a true/false state and allows the user to toggle the state through clicking.

## Code Example: Adding a CheckBox

```csharp
// Initialize
var checkBox = new CheckBox();
checkBox.AddToRoot();
checkBox.X = 50;
checkBox.Y = 50;
checkBox.Text = "Checkbox";
checkBox.Checked += (_,_) => checkBox.Text = $"IsChecked:{checkBox.IsChecked}";
checkBox.Unchecked += (_, _) => checkBox.Text = $"IsChecked:{checkBox.IsChecked}";
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA6tW8ix2L81VsiopKk3VUcrMyyzJTMzJrEpVslIqSyxSSM5ITc52yq9QsFXISy1XcIZyNTStY_JgcnqOKSkh-UH5-SWowhFATaYGyCKRGCIhqRUlQMGYUgMDIyOw6Un5FRAesjKwTGoKRJ2TrYJGvE68poItiGvsqoBumgrEAM9iqDarargKuFgtpiWhECaqNQpUskepFgDci7UfawEAAA)

<figure><img src="../../.gitbook/assets/13_21 40 32.gif" alt=""><figcaption><p>CheckBox responding to Checked and Unchecked events by printing output</p></figcaption></figure>

## IsChecked

IsChecked is a nullable bool value.

| Value   | Details                               |
| ------- | ------------------------------------- |
| `true`  | CheckBox is checked                   |
| `false` | CheckBox is not checked               |
| `null`  | CheckBox is in an indeterminate state |

The following code creates three checkboxes each set to one of the three `IsChecked` values:

```csharp
// Initialize
var stackPanel = new StackPanel();
stackPanel.AddToRoot();

var checkBox = new CheckBox();
stackPanel.AddChild(checkBox);
checkBox.IsChecked = true;

var checkBox2 = new CheckBox();
stackPanel.AddChild(checkBox2);
checkBox2.IsChecked = false;

var checkBox3 = new CheckBox();
stackPanel.AddChild(checkBox3);
checkBox3.IsChecked = null;
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA6tW8ix2L81VsiopKk3VUcrMyyzJTMzJrEpVslIqSyxSKC5JTM4OSMxLzVGwVchLLVcIhgtoaFrH5CHk9RxTUkLyg_LzS8ASIM3JGanJ2U75FVCtzlAuFo3OGZk5KRow9SB5GFvPsxisLzUFaArIkWhmG5FquBGy6UYoxqcl5hSjm29MqvnGyOYbo5ifV5qTY61UCwD-QP0DdAEAAA)

### Three State CheckBox (IsThreeState)

By default, a CheckBox only cycles between `Checked` and `Unchecked` when clicked. Setting `IsThreeState` to `true` allows the user to cycle through all three states by clicking.

When `IsThreeState` is true, clicking the CheckBox follows this cycle:
**Unchecked -> Checked -> Indeterminate -> Unchecked**

The following code creates a CheckBox that supports three states:

```csharp
// Initialize
var checkBox = new CheckBox();
checkBox.AddToRoot();
checkBox.Text = "Three State CheckBox";
checkBox.IsThreeState = true;
```

[Try on XnaFiddle.NET](...)

## CheckBox Width and Height

Default CheckBoxes have the following default values:

<table><thead><tr><th width="151.5999755859375">Variable</th><th width="199.5999755859375">Value</th></tr></thead><tbody><tr><td>Width</td><td>128</td></tr><tr><td>WidthUnits</td><td>Absolute</td></tr><tr><td>Height</td><td>32</td></tr><tr><td>HeightUnits</td><td>Absolute</td></tr></tbody></table>

The text within the CheckBox draws and wraps according to the bounds of the CheckBox. In other words, longer text results in line wrapping, as show in the following code block:

```csharp
// Initialize
var checkBox = new CheckBox();
checkBox.Y = 24;
checkBox.Text = "This is some longer text";
checkBox.AddToRoot();
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA6tW8ix2L81VsiopKk3VUcrMyyzJTMzJrEpVslIqSyxSSM5ITc52yq9QsFXISy1XcIZyNTStY_JgcnqRQFkjE2SRkNSKEqBgTKmBgZFRSEZmsQIQFefnpirk5OelpxYplAAVQGSRtTmmpITkB-XnlwDNV6oFAAAIwCycAAAA)

<figure><img src="../../.gitbook/assets/13_08 56 16.png" alt=""><figcaption><p>ComboBox with wrapped text</p></figcaption></figure>
