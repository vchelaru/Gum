# Window

## Introduction

Window is a forms control which can be moved and resized with the cursor. It can be used for movable UI such as an inventory popup, or for floating toolbars in game editors and tools.

## Code Example

The following code creates a message box window.

```csharp
// Initialize
var window = new Window();
window.Anchor(Gum.Wireframe.Anchor.Center);
window.Width = 300;
window.Height = 200;
window.AddToRoot();

var textInstance = new Label();
textInstance.Dock(Gum.Wireframe.Dock.Top);
textInstance.Y = 24;
textInstance.Text = "Hello I am a message box";
window.AddChild(textInstance);

var button = new Button();
button.Anchor(Gum.Wireframe.Anchor.Bottom);
button.Y = -10;
button.Text = "Close";
window.AddChild(button);
button.Click += (_, _) =>
{
    window.RemoveFromRoot();
};

```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACn2RTWvCQBCG74L_YcgpUhvsx6mSgqa0BnqSgAgB2SRTs7i7UzYbIxX_ezcfaGpL9zbPvDvzsHscDgCcsHgrpfMERpc4bghX3HAm-Bda7OyZhoqrjCrwQWEFq6ZwR9NYtdybqTQn7do53opr_NBMYge9AJVB3QuveGZyO-phMrnABfJtbiy979NZlkW0JDLNsljVKgYPJlSFYSrFTuidJSiaSL_pvVC6u3KqkRfR56_sul79eE0jW9hG7CxQCIIQmAQGEouCbRESOsTOD9kg5yJz-yMu3klpDKnOeN4UjXLL_33COdmI7IVr29u7-qE6cBYNBBX4p1Wb7A0JBE93cOODuxnDZgT-c6yOsQJ7urtLlLTHV03y_AenqTMcnL4BEzWh8zkCAAA" target="_blank">Try on XnaFiddle.NET</a>

<figure><img src="../../.gitbook/assets/14_06 14 56.gif" alt=""><figcaption><p>Window responding to move and resize actions</p></figcaption></figure>

## Preventing Sizing and Moving

Resizing can be disabled by setting a Window's `ResizeMode` to `ResizeMode.NoResize`. By default this value is set to `ResizeMode.CanResize`. Note that the ResizeMode property does not affect whether a window can be moved by its title bar.

Window movement and resizing can be restricted by removing or modifying the IsEnabled property on the edges and corners of the window. By default each Window instance contains nine children which are used to resize and move the window. The items have the following names:

* TitleBarInstance (moves the entire window)
* BorderTopLeftInstance
* BorderTopRightInstance
* BorderBottomLeftInstance
* BorderBottomRightInstance
* BorderTopInstance
* BorderBottomInstance
* BorderLeftInstance
* BorderRightInstance

Any of these objects can be disabled to prevent the user from moving or resizing a window.

Note that changing any of these values will not update the visual appearance of the Window. These values only control the behavior of the control. Any changes to appereance must be either performed through the Visual object, or by creating a custom Window implementation.

## Code Example: Disabling Resizing with ResizeMode

The following code disabled resizing by setting ResizeMode:

```csharp
// Initialize
var window = new Window();
window.AddToRoot();
window.ResizeMode = ResizeMode.NoResize;
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACqvmUlBQ8ix2L81VslIoKSpN1QEJZOZllmQm5mRWpQJFlcoSixTKM_NS8ssVbBXyUssVwsEcDU3rmDyIuJ5jSkpIflB-fgmyYFBqMdAE3_yUVKA-BEfPLx_CsVbiquUCAI5YsKWBAAAA)

## Code Example: Forced Docking

The following code shows how to force dock a window to the right side of the screen. The user can still resize the window horizontally by grabbing the left edge of the window since the left side has not been disabled.

{% tabs %}
{% tab title="Code-only" %}
```csharp
// Initialize
var window = new Window();
window.AddToRoot();

window.Dock(Gum.Wireframe.Dock.Right);

var windowVisual = (WindowVisual)window.Visual;

// make it so the user cannot resize or move the window
// except for horizontally:
windowVisual.TitleBarInstance.IsEnabled = false;

windowVisual.BorderTopLeftInstance.IsEnabled = false;
windowVisual.BorderTopRightInstance.IsEnabled = false;
windowVisual.BorderBottomLeftInstance.IsEnabled = false;
windowVisual.BorderBottomRightInstance.IsEnabled = false;

windowVisual.BorderTopInstance.IsEnabled = false;
windowVisual.BorderBottomInstance.IsEnabled = false;
windowVisual.BorderRightInstance.IsEnabled = false;
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA52PQUvDQBCF_0qYUwplL94iPRiqEvBUQnNQD2t30g5udsrurAHF_-42K1q8SHN8j_nee_MBTbiPA1TiIy4hBnL7ANUjJFPdsR-CWmOvo5UthahtUNsreF4CORLSlt4RKnjTvhjJGR6LVeFwLLpJlIvrJ5d9dWNMyxtmOTfXvHstT0Udeey9HnCy1Ib2Bznd_Qbn9hRfdmd68R2U1U9wlqolsVhr37gg2u1QNeHW6ReLJuX06Rf8S9TsDfqWjw_YywxsGn4pV7MID3MaMzmrNI2d13Yp9d86-PwCD4OhIIMCAAA)
{% endtab %}

{% tab title="General" %}
```csharp
// Initialize
var window = new Window();
window.AddToRoot();

window.Dock(Gum.Wireframe.Dock.Right);

// make it so the user cannot resize or move the window
// except for horizontally:
window.GetFrameworkElement("TitleBarInstance").IsEnabled = false;

window.GetFrameworkElement("BorderTopLeftInstance").IsEnabled = false;
window.GetFrameworkElement("BorderTopRightInstance").IsEnabled = false;
window.GetFrameworkElement("BorderBottomLeftInstance").IsEnabled = false;
window.GetFrameworkElement("BorderBottomRightInstance").IsEnabled = false;

window.GetFrameworkElement("BorderTopInstance").IsEnabled = false;
window.GetFrameworkElement("BorderBottomInstance").IsEnabled = false;
window.GetFrameworkElement("BorderRightInstance").IsEnabled = false;
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA62PMQuDMBBG_4pkUigijpYOlbYidBLBxSU1ZxtMciWeFVr632t1cXTImJfj8b4Py_ts0CwhO8COSSNJciXfwBL24tYbpRE4egfPwOhV88MP9rVZeHgUosQCkdbwhE3nT9KwkhZayzXMKCzk_UGruwzo8v8d0XZnBRoM-fUQRXFcSlKQcpubnrhpYKFBmPdnw28KxBTUctXDFlmKVoAt8XmFltwa50UOlSkSoXbcuUhdp07rnTc6FG6cy74_Ts_NlgEDAAA)
{% endtab %}
{% endtabs %}

<figure><img src="../../.gitbook/assets/14_06 16 42.gif" alt=""><figcaption><p>Window resizing only from the left edge</p></figcaption></figure>

Each individual border and title bar instance can be independently disabled to customize the resize/movement behavior. For example, the following line is specifically responsible for disabling the ability to move the window:

```csharp
// Initialize
window.GetFrameworkElement("TitleBarInstance").IsEnabled = false;
```
