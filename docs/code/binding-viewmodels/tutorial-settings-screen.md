# Tutorial - Settings Screen

## Introduction

This document shows how to build a Settings screen using binding. The approach shown here could be used in a code-only project or a project that uses the Gum UI tool to create a screen. It creates a settings screen and a matching view model which stores properties for volume.

[Try the completed example on XnaFiddle.NET](https://xnafiddle.net/#code=H4sIAAAAAAAACq1W32_TMBB-719hIR4SCUUdCCGtbBJrWTWJAlq3AkI8uMm1tebale2kwLT_nbOTOk6asg7Rlzp3n-873w-fc83EkkykkGO6hnG-HvRyJ8JlcinVWu8JkqEURkneoRnBgubczJjOKdfJ7FUImRRFw_oXpmChkHUnnLBUSS0XJvkqaHJpVVup7h5RJ2NFNyuWeneCwyTjHEagWIHyQa-3yeecpSTlVGsyBWNQrGcMthOZASenxK979z2Cv2rDgktqyATNpzPJ8zU4ZQmxvyUYcnZOxmDeOuh5FA-8UpdKpIsKynOoVA-9fYbp4uf_tv99BBsQmf4kIoExkYvIk8Txj9AFbZSNnlePmN5w-svafv5sevn1lNx73enH_sOzwSGGIFDdHAGgweLkyBPoK6aHA8mbpgpAYOZ8ObznsAZhqgROOctA2UOVqzI2H-gc021PYwPmvgYh3PHvbyjdCraER2t6FMXo05xqiARsiW0YygSo61wYtoYojlsZHiqgBs3-krkJk1vKL5jI0HbUKh2sa1SSQrKsZaBl3awY9qZM76JG3zlRcsk4R8MeXFBFNlTgec-IdX5qaHr32QpCx5zJd1k2XDGeRQ4faN138k6kK6lanKUwGWKOQLV5uYtzyetiHO0b3XE6bKB238kN_MR2ILZgSVVBu0J1Cd4Vwu5w7uMvLH5DAPEyPFZmVmjqZb_fJKmr5MjTNPa0w7K2hfcUa_WGAFMLfZRcRQdx6oB-Q9zJy8ChoDmODWKwJQAF0gOBbDXckYdv7Xq0aXx3tbqmTjP29g7ktfZX36gV0LHGLzpB7XGTBBfxoeI5gjkA26w-mby6gBs-hJl5xIMQ-qTTN4bE4Zwfxf-PMdgfQ7EvlvbAsQ-KE7zT7X81W3bvjhEULIUJFXSJLbH0rxGHyddTUFZvl7dXbox72e691JwkjgoHyL23VRV-J2Fkb-J4QK70ROYa8OnF5hxwh1E5DOqylwZSAxmRBSiF6So74Eowwyhnv2Gv_J2_SQCwRC9I84k3A6WZFPjSa99ZupzM1Q3RGo7B68VJbPPeyGsp3fBr2Cn886xpyqexw1pVMHbslhedN1JDi_1WQODrfhciKBTEvHkduGhnfBik9m3THfbbTYYXT2QzfYPvAbKsFt05qNAeVLvo2LvVj3gwUnR7BH-j4pIhB6qioeR2gEsl8DG6BXVRPzprn539tqdOuOfnQ-8PaDoJ_4UMAAA)

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

    [DependsOn(nameof(SfxVolume))]
    public string SfxVolumeDisplay => $"SFX: {SfxVolume:N0}";

    [DependsOn(nameof(MusicVolume))]
    public string MusicVolumeDisplay => $"Music: {MusicVolume:N0}";
}
```

Although the view model is a small class, it has a number of details that are worth discussing.

The uses `Gum.Mvvm.ViewModel` as its base class. This is not a requirement - if you are familiar with an existing framework that offers an `INotifyPropertyChanged` implementation (such as `CommunityToolkit.Mvvm`), then you are free to use this with Gum.

Our code above uses the built-in `Get` and `Set` methods which are specific to Gum's ViewModel class. These properties provide the following functionality:

* Notification of change whenever the Set method is called. This notification is necessary so that UI knows when to update what it is displaying.
* Property dependency using the `DependsOn` attribute - this is discusses in the [View Model Property Dependency](view-model-property-dependency.md) page.

If any additional properties need to be added, they should also use `Get` and `Set` calls.

## Defining SettingsScreen

This section shows how to set up binding in a `SettingsScreen` class. The binding is the same whether you use the Gum UI tool or a code-only approach.

{% tabs %}
{% tab title="Code-Only" %}
```csharp
public class SettingsScreen : FrameworkElement
{
    Slider SfxSlider;
    Label SfxValueLabel;
    Slider MusicSlider;
    Label MusicValueLabel;

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

        SfxValueLabel = new Label();
        panel.AddChild(SfxValueLabel);

        var musicLabel = new Label();
        panel.AddChild(musicLabel);
        musicLabel.Text = "Music Volume:";
        // add some padding:
        musicLabel.Y = 12;

        MusicSlider = new Slider();
        panel.AddChild(MusicSlider);
        MusicSlider.Width = 200;

        MusicValueLabel = new Label();
        panel.AddChild(MusicValueLabel);
    }

    private void CreateBinding()
    {
        SfxSlider.SetBinding(
            nameof(SfxSlider.Value),
            nameof(SettingsViewModel.SfxVolume));

        SfxValueLabel.SetBinding(
            nameof(SfxValueLabel.Text),
            nameof(SettingsViewModel.SfxVolumeDisplay));

        MusicSlider.SetBinding(
            nameof(MusicSlider.Value),
            nameof(SettingsViewModel.MusicVolume));

        MusicValueLabel.SetBinding(
            nameof(MusicValueLabel.Text),
            nameof(SettingsViewModel.MusicVolumeDisplay));
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

        SfxValueLabel.SetBinding(
            nameof(SfxValueLabel.Text),
            nameof(SettingsViewModel.SfxVolumeDisplay));

        MusicSlider.SetBinding(
            nameof(MusicSlider.Value),
            nameof(SettingsViewModel.MusicVolume));

        MusicValueLabel.SetBinding(
            nameof(MusicValueLabel.Text),
            nameof(SettingsViewModel.MusicVolumeDisplay));
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

Notice that `nameof` allows using either an instance to get a property (such as `SfxSlider.Value`) or a class name (such as `SettingsViewModel.SfxVolume`). In fact, we could have even replaced `SfxSlider.Value` with `Slider.Value` since Value is a property on the `Slider` class.

Creating a local property for the ViewModel can also help simplify the code slightly. For example, we could modify the code to have a ViewModel property that is used in the SetBinding calls as shown in the following code block:

```csharp

SettingsViewModel ViewModel => (SettingsViewModel)BindingContext;

// this could be used to set up binding:

SfxSlider.SetBinding(
    nameof(SfxSlider.Value),
    nameof(ViewModel.SfxVolume));

SfxValueLabel.SetBinding(
    nameof(SfxValueLabel.Text),
    nameof(ViewModel.SfxVolumeDisplay));

MusicSlider.SetBinding(
    nameof(MusicSlider.Value),
    nameof(ViewModel.MusicVolume));

MusicValueLabel.SetBinding(
    nameof(MusicValueLabel.Text),
    nameof(ViewModel.MusicVolumeDisplay));
```

This approach can also be used to handle events as shown in the [Task Screen tutorial](tutorial-task-screen.md).

## Using the SettingsScreen

Once the `SettingsScreen` is defined, we can instantiate it in our game. The following code shows how to create a `SettingsScreen` instance and assign its `BindingContext` to a `SettingsViewModel` instance. Note that we are setting initial values on the view model - these values are shown by our UI when we run the game:

```csharp
// Initialize
var screen = new SettingsScreen();
screen.AddToRoot();

var viewModel = new SettingsViewModel();
screen.BindingContext = viewModel;
viewModel.SfxVolume = 50;
viewModel.MusicVolume = 75;
```

We only needed to set the `BindingContext` on the `SettingsScreen`, which is the parent of the other controls. All children inherit the `BindingContext` of their parent, so we do not need to explicitly assign the `BindingContext` on each `Slider`.

<figure><img src="../../.gitbook/assets/14_08 03 38.png" alt=""><figcaption><p>Values set from the ViewModel</p></figcaption></figure>

Changes to the UI also immediately update the view model. The value labels next to each slider display the current value using `DependsOn` properties on the ViewModel. When the user drags a slider, the ViewModel's volume property updates, which triggers the display property to recalculate, which updates the label — all automatically through binding.

<figure><img src="../../.gitbook/assets/14_08 09 21.gif" alt=""><figcaption><p>Changing the slider updates the value on the ViewModel</p></figcaption></figure>
