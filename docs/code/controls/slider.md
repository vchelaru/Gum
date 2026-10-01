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

## Clicking the Track

Clicking the track (the bar the thumb slides along) moves the thumb in one of two ways, controlled by `IsMoveToPointEnabled`. The default is `false`.

### Stepping with LargeChange

By default, each click on the track changes `Value` by `LargeChange` toward the cursor, and holding the button repeats the step until the thumb reaches the cursor. `LargeChange` defaults to 25 no matter what `Minimum` and `Maximum` are.

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
slider.Width = 250;
slider.Minimum = 0;
slider.Maximum = 100;
slider.LargeChange = 10;
slider.ValueChanged += (_, _) =>
    label.Text = $"Value: {slider.Value}";
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA12P0QqCMBSGX-UwuiiQMKsbxYuKiMBuSipBiMVGDeYE3SoK3705h412te_7z_5xPmhbb1SBQlkp6iEmmGSYszdFIXrgCji-Ug4xCPqEpL0PR1EujB0vCEnLfVlKx5317NzvMdMYzDTmom2rOSO0snUHA-Zt5_8KrewbLWd_fGJE3ttvXLnTixSq0NqV-GXlxHd0gqsbXd2xuFET_ZIj5somBHLl-8EyhuHFg8sI4pan61yAPt2yKX1J3TAwk4F5HMLH7Wq6KELNF2tmwsF4AQAA)

Because the step size does not scale with the range, the thumb can land past the cursor, or barely move. With the default `LargeChange` of 25, a click on a slider from 0 to 1 jumps the whole range, while a click on a slider from 0 to 1000 moves the thumb by 2.5%. Set `LargeChange` to match your range, or use `IsMoveToPointEnabled`.

### Moving to the Cursor

Setting `IsMoveToPointEnabled` to `true` moves the thumb directly to the cursor when the track is pushed, and keeps it under the cursor while the button is held. `LargeChange` is ignored.

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
slider.Width = 250;
slider.Minimum = 0;
slider.Maximum = 100;
slider.IsMoveToPointEnabled = true;
slider.ValueChanged += (_, _) =>
    label.Text = $"Value: {slider.Value}";
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA12QUWvCMBDHv8oR9qAgo1b3UunDJiKCgmxFJxQkkkMDaQJtoqL43XdJiwvmKb_f3f3D5c4WzdxVLLO1wwGTWlrJlbwhy9iZ16D4ARXkoPECS3_v9SelDvb9U4jCfBtjI_dLvR_JE3eE6Ziw1D6tUVJg3cX9BAizrX8J7OQzsePdC2-lsCf_TCxXtEjlKtKx5NdODpNIL5qVOWNh1kZqO9P8oFBQj_-Q_6YNVw6nJ66PVCxdkqRfOfT2A9j3Ifc8mpUa6LR7F3i1lPEWOtMwnME9znq0pQl7_AHKGcQUgwEAAA)

With `IsMoveToPointEnabled` set to `true`, `ValueChanged` is raised as the thumb moves, and `ValueChangeCompleted` is raised once when the button is released. Use `ValueChangeCompleted` for work you do not want to repeat every frame, such as saving a setting. If `IsSnapToTickEnabled` is `true`, the thumb snaps to the nearest tick in either mode.
