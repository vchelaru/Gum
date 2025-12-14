# Tutorial - Settings Screen

## Introduction

This document shows how to build a Settings screen using binding. The approach shown here could be used in a code-only project or a project that uses the Gum UI tool to create a screen. It creates a settings screen and a matching view model which stores properties for volume and full screen.

## Defining SettingsViewModel

The ViewModel is responsible for storing properties related to the game's settings. A typical game may have dozens of properties for settings, but this example includes a small number to keep the tutorial shorter.

```csharp
using Gum.Mvvm;

namespace BindingTutorial.ViewModels;

public class SettingsViewModel : ViewModel
{
    public float MusicVolume
    {
        get => Get<float>();
        set => Set(value);
    }

    public float SfxVolume
    {
        get => Get<float>();
        set => Set(value);
    }

    public bool IsFullScreen
    {
        get => Get<bool>();
        set => Set(value);
    }
}
```

Although the view model is a small class, it has a number of details that are worth discussing.

The uses `Gum.Mvvm.ViewModel` as its base class. This is not a requirement - if you are familiar with an existing framework that offers an `INotifyPropertyChanged` implementation (such as `CommunityToolkit.Mvvm`), then you are free to use this with Gum.

Our code above uses the built-in `Get` and `Set` methods which are specific to Gum's ViewModel class. These properties provide the following functionality:

* Notification of change whenever the Set method is called. This notification is necessary so that UI knows when to update what it is displaying.
* Property dependency using the `DependsOn` attribute - this is discusses in the [Binding Deep Dive](binding-deep-dive.md) page.

If any additional properties need to be added, they should also use `Get` and `Set` calls.

## Defining SettingsScreen

This section shows how to set up binding in a `SettingsScreen` class. The binding is the same whether you use the Gum UI tool or a code-only approach.

{% tabs %}
{% tab title="Code-Only" %}
```csharp
public class SettingsScreen : FrameworkElement
{
    Slider SfxSlider;
    Slider MusicSlider;
    CheckBox FullScreenCheckbox;
    Button BackToMainMenuButton;

    public SettingsScreen() : base(new ContainerRuntime())
    {
        CreateLayout();

        CreateBinding();
    }

    private void CreateLayout()
    {
        this.Dock(Gum.Wireframe.Dock.Fill);

        var panel = new StackPanel();
        this.AddChild(panel);
        panel.Anchor(Gum.Wireframe.Anchor.Center);

        var label = new Label();
        panel.AddChild(label);
        label.Text = "SFX Volume:";

        SfxSlider = new Slider();
        panel.AddChild(SfxSlider);
        SfxSlider.Width = 200;

        var musicLabel = new Label();
        panel.AddChild(musicLabel);
        musicLabel.Text = "Music Volume:";
        // add some padding:
        musicLabel.Y = 12;

        MusicSlider = new Slider();
        panel.AddChild(MusicSlider);
        MusicSlider.Width = 200;

        FullScreenCheckbox = new CheckBox();
        panel.AddChild(FullScreenCheckbox);
        FullScreenCheckbox.Text = "Full Screen";
        // Add some padding:
        FullScreenCheckbox.Y = 12;

        BackToMainMenuButton = new Button();
        panel.AddChild(BackToMainMenuButton);
        BackToMainMenuButton.Text = "Back to Main Menu";
        // Add some padding:
        BackToMainMenuButton.Y = 12;
    }

    private void CreateBinding()
    {
        SfxSlider.SetBinding(
            nameof(SfxSlider.Value),
            nameof(SettingsViewModel.SfxVolume));

        MusicSlider.SetBinding(
            nameof(MusicSlider.Value),
            nameof(SettingsViewModel.MusicVolume));

        FullScreenCheckbox.SetBinding(
            nameof(FullScreenCheckbox.IsChecked),
            nameof(SettingsViewModel.IsFullScreen));
    }
}
```
{% endtab %}

{% tab title="Gum UI tool (with code generation)" %}
```csharp
public partial class SettingsScreen
{
    void CustomInitialize()
    {
        SfxSlider.SetBinding(
            nameof(SfxSlider.Value),
            nameof(SettingsViewModel.SfxVolume));

        MusicSlider.SetBinding(
            nameof(MusicSlider.Value),
            nameof(SettingsViewModel.MusicVolume));

        FullScreenCheckbox.SetBinding(
            nameof(FullScreenCheckbox.IsChecked),
            nameof(SettingsViewModel.IsFullScreen));
    }
}
```
{% endtab %}
{% endtabs %}

The `SetBinding` method is used to associate the view property (such as `SfxSlider.Value`) with the ViewModel property (such as `SettingsViewModel.SfxVolume`).

SetBinding takes strings which means it would also be possible to pass the names of the properties. Using the `nameof` keyword is preferred to this since it reduces the chances of mistakes.

```csharp
// 🚫 Although this works, it's error prone and harder to maintain:
SfxSlider.SetBinding(
    "Value",
    "SfxVolume");

// ✅ Instead, use nameof to get compile-time checks and refactoring support:
SfxSlider.SetBinding(
    nameof(SfxSlider.Value),
    nameof(SettingsViewModel.SfxVolume));
```

Creating a local property for the ViewModel can also help simplify the code slightly. For example, we could modify the code to have a ViewModel property that is used in the SetBinding calls as shown in the following code block:

```csharp

SettingsViewModel ViewModel => (SettingsViewModel)BindingContext;

// this could be used to set up binding:

SfxSlider.SetBinding(
    nameof(SfxSlider.Value),
    nameof(ViewModel.SfxVolume));

MusicSlider.SetBinding(
    nameof(MusicSlider.Value),
    nameof(ViewModel.MusicVolume));

FullScreenCheckbox.SetBinding(
    nameof(FullScreenCheckbox.IsChecked),
    nameof(ViewModel.IsFullScreen));
```

This approach can also be used to handle events as shown in the [Task Screen tutorial](tutorial-task-screen.md).

## Using the SettingsScreen

Once the `SettingsScreen` is defined, we can instantiate it in our game. The following code shows how to create a `SettingsScreen` instance and assign its `BindingContext` to a `SettingsViewModel` instance. Note that we are setting values on the view model to verify that doing so changes the values on the UI:

```csharp
var screen = new SettingsScreen();
screen.AddToRoot();

var viewModel = new SettingsViewModel();
screen.BindingContext = viewModel;
viewModel.SfxVolume = 50;
viewModel.MusicVolume = 75;
viewModel.IsFullScreen = true;
```

We only needed to set the `BindingContext` on the `SettingsScreen`, which is the parent of the other controls. All children inherit the `BindingContext` of their parent, so we do not need to explicitly assign the `BindingContext` on each `Slider` and `CheckBox`.

<figure><img src="../../.gitbook/assets/14_08 03 38.png" alt=""><figcaption><p>Values set from the ViewModel</p></figcaption></figure>

Changes to the UI also immediately update the view model. For example, we can modify the ViewModel's `SfxVolume` to print output whenever it changes.

```csharp
public float SfxVolume
{
    get => Get<float>();
    set
    {
        // Set returns a true if the value changed
        if (Set(value))
        {
            System.Diagnostics.Debug.WriteLine($"SfxVolume set to {SfxVolume}");
        }
    }
}
```

Any change to the value results in output.

<figure><img src="../../.gitbook/assets/14_08 09 21.gif" alt=""><figcaption><p>Changing the slider updates the value on the ViewModel</p></figcaption></figure>
