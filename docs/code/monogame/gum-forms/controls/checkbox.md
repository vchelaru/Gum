# CheckBox

### Introduction

The CheckBox control provides the ability to display a true/false state and allows the user to toggle the state through clicking.

### Code Example: Adding a CheckBox

```csharp
var checkBox = new CheckBox();
Root.Children.Add(checkBox.Visual);
checkBox.X = 50;
checkBox.Y = 50;
checkBox.Text = "Checkbox";
checkBox.Checked += (_,_) => Debug.WriteLine($"IsChecked:{checkBox.IsChecked}");
checkBox.Unchecked += (_, _) => Debug.WriteLine($"IsChecked:{checkBox.IsChecked}");
```

<figure><img src="../../../../.gitbook/assets/24_06 44 43.gif" alt=""><figcaption><p>CheckBox redponding to Checked and Unchecked events by printing output</p></figcaption></figure>
