# Slider

## Introduction

The Slider control provides a way for the user to change a value by dragging the slider _thumb_.

## Code Example: Creating a Slider

The following code creates a Slider which allows the user to select a value between 0 and 30, inclusive. The `IsSnapToTickEnabled` property results in the value being snapped to the `TickFrequency` value. In this case, the value is used to force whole numbers.

```csharp
// Initialize
var label = new Label();
label.AddToRoot();
label.X = 50;
label.Y = 24;

var slider = new Slider();
slider.AddToRoot();
slider.X = 50;
slider.Y = 50;
slider.Minimum = 0;
slider.Maximum = 30;
slider.TicksFrequency = 1;
slider.IsSnapToTickEnabled = true;
slider.Width = 250;
slider.ValueChanged += (_, _) => 
    label.Text = $"Value: {slider.Value}";
slider.ValueChangeCompleted += (_, _) => 
    label.Text = $"Finished setting Value: {slider.Value}";
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA5WQUWvCMBSF_0oIe1CQ0dXtpaMPm6gI7mWWbYIg0VxsME20STY38b_vJnYaZAz21vPdc85N756OzNBVNLO1gw4VSljBpPgCmtF3VhPJFiBJThR8kLH_brXvZyrQ6wfOC_2stY3YG3rvkpOcokxvUfouIwWHuimbBBGSR35R18BTX6OnF_oJX1y5CmkM2a6B3YgWYrk2gxq2DtTyE4c359nITBTbFNp7-ootJHA0-JucPa-C29L_T7z_hUkHvZKpFSZmLknSx5y05h0yb5Pc627_5xYF7CzGr4IrDcGM7OOew3H0a3tPVxsJ9p9bBngeU2LIgLVCrchfa-nhGwhOLWwOAgAA)

<figure><img src="../../.gitbook/assets/13_22 30 10.gif" alt=""><figcaption><p>Slider reporting its value whenever the value changes or when the change completes</p></figcaption></figure>

## Value

Value is a `double` which represents the number displayed by the Slider. This value can change in response to UI events, binding, or explicit setting in code. Value is always between Minimum and Maximum, inclusive.

The following code directly sets Value:

```csharp
// Initialize
slider.Value = 25;
```

Setting a value outside of its bounds forces the value to the bounds. For example, the following code results in a value of 50:

```csharp
// Initialize
slider.Minimum = 0;
slider.Maximum = 50;
slider.Value = 100; // value is set to 50
```

Value can also be changed by changing either Minimum or Maximum:

```csharp
// Initialize
slider.Minimum = 0;
slider.Maximum = 100;

slider.Value = 80;
slider.Maximum = 75; // this sets Value to 75

slider.Value = 20;
slider.Minimum = 25; // this sets Value to 25
```
