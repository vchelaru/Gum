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
window.AddChild(button.Visual);
button.Click += (_, _) =>
{
    window.RemoveFromRoot();
};

```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACn2RXWuDMBSG7_0VB68s66T7uFpx0Dq2CrsqMhkIJepZDU1yRoy1rPS_L1ppXTeWu_Pk5c3Dyd4BcKPqpZbuAxhd47gFXHHDmeBfaKm7ZRoargpqIACFDSTd4I2mqTpyf6bykrRna_yEa_zQTGIP_RCVQT0IJ7wwpa26m0zOcIF8XRpLb4d0VhQxLYlM91iqWhWDOxOpyjCVYy_0yjIUXWR46T9RvrlwapEf0-ev7Hv79P0lje1gL1J3gUIQRMAkMJBYVWyNkNEudX_IhiUXhTesOHtntTGkeuN5N3TKR_7vCudkI3IQbm2vb9pF9eAkGgqq8E-rPvnGq5qJQVcoeL6BqwC81RhWIwgeU7VPFdjTVyxR0hafNcnTVxymrnNwvgF3GhpjPgIAAA" target="_blank">Try on XnaFiddle.NET</a>

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

Note that changing any of these values will not update the visual appearance of the Window. These values only control the behavior of the control. Any changes to apperance must be either performed through the Visual object, or by creating a custom Window implementation.

## Code Example: Disabling Resizing with ResizeMode

The following code disabled resizing by setting ResizeMode:

```csharp
// Initialize
var window = new Window();
window.AddToRoot();
window.ResizeMode = ResizeMode.NoResize;
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACqvmUlBQ8ix2L81VslIoKSpN1QEJZOZllmQm5mRWpQJFlcoSixTKM_NS8ssVbBXyUssVwsEcDU3rmDyIuJ5jSkpIflB-fgmyYFBqMdAE3_yUVKA-BEfPLx_CsVbiquUCAI5YsKWBAAAA" target="_blank">Try on XnaFiddle.NET</a>

## Code Example: Forced Docking

The following code shows how to force dock a window to the right side of the screen. The user can still resize the window horizontally by grabbing the left edge of the window since the left side has not been disabled.

{% tabs %}
{% tab title="Code-only" %}
```csharp
// Initialize
var window = new Window();
window.AddToRoot();

window.Dock(Dock.Right);

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
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACp2QMU_DMBCF9_yKU6ZUQuneioEIVEViqiK6sBzJhVh1fJV9aUoR_x3XLtABqNrF0r2n7947vycAaekWQ5_OQOxANwdBGSUKtdqTV9MtWhiVaXiEWzA0wioM2WT-bKKe3zVNxUtmCeK3fM_1Ojs8-VK9dhK9n3VPyg2o_dJsdTJPjnCcAjKdQo9rAiXgGKQjGBxZqNEYFrDkfFNgCz1vKdhxRQBpV9NGoPV2x1bt2Qhq_Tb7Khlj8kqJpgJtaZygqSkv3YPBF02N79eidnRy2JEp2DZkK948Uiv_gr9j4VMu5QoW4f6axEieD_2r7nV5l1Ln-qXJR_IJz-IpZ7YCAAA" target="_blank">Try on XnaFiddle.NET</a>
{% endtab %}

{% tab title="General" %}
```csharp
// Initialize
var window = new Window();
window.AddToRoot();

window.Dock(Dock.Right);

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
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACq2QMWvDMBCFd_-KQ5MDxdlTOtQ0DYZOwdDFi2qfa2FJF07nuE3pf68jQ-hQqAcvB3oPfffefSUAqgiHwakdCA94dxWMN2K0NRecVHXWDKPxDY3wAB5HeI2PdHNf-VnPHpumpCORRPEmP1Hdp9eRHc17J7O33YLTPYIRCATSIQwBGWrtPQkwhmktEIOjM0Z7hsWP-FHjSaCd7I7YXMiLtvZzd9t4QHlm7XAk7vcWHXpJK1UasZhrLnwQ7Wus1CYrwt7rN4vNVKrVNuDv4H9jcuIGuaTTC7byH2sZKd5lBVROIuRWyjXDFkVb2nO1VCuAFhRTyXfyA8CJg4kbAwAA" target="_blank">Try on XnaFiddle.NET</a>
{% endtab %}
{% endtabs %}

<figure><img src="../../.gitbook/assets/14_06 16 42.gif" alt=""><figcaption><p>Window resizing only from the left edge</p></figcaption></figure>

Each individual border and title bar instance can be independently disabled to customize the resize/movement behavior. For example, the following line is specifically responsible for disabling the ability to move the window:

```csharp
// Initialize
window.GetFrameworkElement("TitleBarInstance").IsEnabled = false;
```
