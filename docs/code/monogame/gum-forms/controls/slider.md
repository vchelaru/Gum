# Slider

### Introduction

The Slider control provides a way for the user to change a value by dragging the slider _thumb_.

### Code Example: Creating a Slider

The following code creates a Slider which allows the user to select a value between 0 and 30, inclusive.  The `IsSnapToTickEnabled` property results in the value being snapped to the `TickFrequency` value. In this case, the value is used to force whole numbers.

```csharp
var slider = new Slider();
this.Root.Children.Add(slider.Visual);
slider.X = 50;
slider.Y = 50;
slider.Minimum = 0;
slider.Maximum = 30;
slider.TicksFrequency = 1;
slider.IsSnapToTickEnabled = true;
slider.Width = 250;
slider.ValueChanged += (_, _) => 
    Debug.WriteLine($"Value: {slider.Value}");
slider.ValueChangeCompleted += (_, _) => 
    Debug.WriteLine($"Finished setting Value: {slider.Value}");
```

<figure><img src="../../../../.gitbook/assets/24_07 18 09.gif" alt=""><figcaption><p>Slider reporting its value whenenver the value changes or when the change completes</p></figcaption></figure>
