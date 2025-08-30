# Migrating to 2025 August

## Introduction

This page discusses breaking changes when migrating from `2025 July 28` to `2025 August`.

## Upgrading Gum Tool

An august version of the Gum tool has not yet been released as of this writing.

To upgrade the Gum tool to the latest version (2025 July):

1. Download Gum.zip from the release on Github: [https://github.com/vchelaru/Gum/releases/tag/Release\_July\_28\_2025](https://github.com/vchelaru/Gum/releases/tag/Release_July_28_2025)
2. Delete the old tool from your machine
3. Unzip the gum tool to the same location as to not break any file associations

## Upgrading Runtime

Upgrade your Gum NuGet packages to version 2025.8.26.1. For more information, see the NuGet packages for your particular platform:

* MonoGame - [https://www.nuget.org/packages/Gum.MonoGame/](https://www.nuget.org/packages/Gum.MonoGame/)
* KNI - [https://www.nuget.org/packages/Gum.KNI/](https://www.nuget.org/packages/Gum.KNI/)
* FNA - [https://www.nuget.org/packages/Gum.FNA/](https://www.nuget.org/packages/Gum.FNA/)
* raylib - [https://www.nuget.org/packages/Gum.raylib](https://www.nuget.org/packages/Gum.raylib)
* SkiaSharp - [https://www.nuget.org/packages/Gum.SkiaSharp/](https://www.nuget.org/packages/Gum.SkiaSharp/)

For other platforms you need to build Gum from source

See below for breaking changes and updates.

## TextRuntime Now Uses XNA BlendState Rather Than Gum BlendState

This matches the syntax for other runtime types, making all runtimes consistent.

## SliderVisual.NineSliceInstance Renamed to TrackBackground

This property was renamed to clearly indicate the usage of this NineSliceRuntime. The previous name was vague and caused confusion.

Change code from:

```csharp
var sliderVisual = (SliderVisual)MySlider.Visual;
var trackBackground = sliderVisual.NineSliceInstance;
```

to:

```csharp
var sliderVisual = (SliderVisual)MySlider.Visual;
var trackBackground = sliderVisual.TrackBackground;
```

## Upgrading Gum.MonoGame to .NET 8

The MonoGame runtime library for Gum has been upgraded from .NET 6 to .NET 8. Most projects will not be affected by this change since MonoGame 3.8.3 already requires game projects to be .NET 8 or newer. If your project is using an earlier version of MonoGame, such as MonoGame 3.8.1, then it may still be targeting .NET 6.0. You can upgrade your project to .NET 8 and it will still work with the older MonoGame version.

## Removal of MonoGameGum.Forms Namespace

This version of Gum begins the unification of the MonoGame Gum platform with other platforms such as raylib Gum. It begins the removal of the `MonoGameGum.Forms` namespace which is being replaced by the `Gum.Forms` namespace. Keep in mind that the entire `MonoGameGum` namespace is not being removed, only the Forms sub-namespace in MonoGameGum.

Gum Forms controls, enums, and supporting classes are moving from `MonoGameGum.Forms` to `Gum.Forms`. For example, Button has moved from `MonoGameGum.Forms.Controls.Button` to `Gum.Forms.Controls.Button`.  This change makes it easier to port Forms controls to new platforms, and makes game code more portable. It also allows documentation to be generalized and to apply to other platforms.

Internal code which references visuals (such as `RenderingLibrary.Graphics.Text` ) is being generalized by referencing the corresponding interfaces (such as `RenderingLibrary.Graphics.Text` ). This change makes also it easier to port Forms controls to new platforms.

Eventually Forms controls may migrate out of their platform-specific libraries (such as the Gum.MonoGame NuGet package) and into GumCommon, although additional refactoring is necessary before this migration occurs.

## Updating Projects

It is recommended that projects upgrade the following namespaces:

| Old Namespace                         | New Namespace                 |
| ------------------------------------- | ----------------------------- |
| MonoGameGum.Forms                     | Gum.Forms                     |
| MonoGameGum.Forms.Controls            | Gum.Forms.Controls            |
| MonoGameGum.Forms.Controls.Primitives | Gum.Forms.Controls.Primitives |
| MonoGameGum.Forms.Data                | Gum.Forms.Data                |
| MonoGameGum.Forms.DefaultVisuals      | Gum.Forms.DefaultVisuals      |

The current NuGet packages and source include a collection of compatibility controls. Therefore, as of August 2025 both of these blocks of code will work:

```csharp
// Old namespaces still exist:
using MonoGameGum.Forms.Controls;
...
var button = new Button(); // using the old MonoGameGum.Forms.Controls

// Also, explicitly using the old namespace is still supported:
var textBox = new MonoGameGum.Forms.Controls.TextBox(); 
```

The code above compiles and runs correctly, but it does produce warnings indicating that the `MonoGameGum.Forms` namespace is obsolete. Instead, the code should be modified as shown in the following block:

```csharp
// Old namespaces still exist:
using Gum.Forms.Controls;
...
var button = new Button(); // using the new Gum.Forms.Controls

// Also, explicitly using the old namespace is still supported:
var textBox = new Gum.Forms.Controls.TextBox(); 
```

## Required Updates

Although the Gum NuGet packages include compatibility classes to allow for incremental upgrades, some classes and situations require using the new namespaces.

### ❌ KeyEventArgs

The `KeyEventArgs` class has migrated to the new `Gum.Forms.Controls` namespace. Therefore, any code which uses `KeyEventArgs` must use the new namespace.

Users can either add a using statement to the top of their code, or can fully-qualify the `KeyEventArgs` type in-place. Qualifying in place is recommended since it will have minimal impact on the rest of the class.

{% tabs %}
{% tab title="Old Code" %}
```csharp
var button = new Button();
button.KeyDown += HandleKeyDown;
...
void HandleKeyDown(object sender, KeyEventArgs args)
{

}
```
{% endtab %}

{% tab title="New Code" %}
```csharp
var button = new Button();
button.KeyDown += HandleKeyDown;
...
void HandleKeyDown(object sender, Gum.Forms.Controls.KeyEventArgs args)
{

}
```
{% endtab %}
{% endtabs %}

### ❌ Version 2 Visuals

All version 2 visuals have been migrated to the new namespace. This breaking change was introduced without a compatibility layer because the version 2 visuals have been recently released so it is unlikely that they have widespread usage as of August 2028.

Therefore, any casting of the Visual object to the control-specific visual should be using the new namespace. For example, the following code shows how to access a new visual using the new namespace. As mentioned above, the change can happen by modifying the `using` statements at the top of your code, or by fully qualifying the Visual type as shown in the blocks of code below:

{% tabs %}
{% tab title="Old Code" %}
```csharp
var button = new Button();
var buttonVisual = (ButtonVisual)button.Visual;
buttonVisual.TextInstance.FontScale = 2;
```
{% endtab %}

{% tab title="New Code" %}
```csharp
var button = new Button();
var buttonVisual = (Gum.Forms.DefaultVisuals.ButtonVisual)button.Visual;
buttonVisual.TextInstance.FontScale = 2;
```
{% endtab %}
{% endtabs %}

### ❌ Casting Gum.Forms to MonoGameGum.Forms

Gum has added a compatibility layer to enable updating the NuGet package while still using old code. However, this compatiblity layer was achieved by using inheritance on controls. Specifically, controls with old namespaces (such as `MonoGameGum.Forms.Controls.Button`) inherit from controls with new namespaces (such as `Gum.Forms.Controls.Button`).

The old type can be casted to the new type, but new types cannot be casted to the old type. This can cause problems if you have some of your code using the old namespaces and some of your code using new namespaces.

The following code shows what is and is not supported:

```csharp
// This is supported:
Gum.Forms.Controls.Button button = new MonoGameGum.Forms.Controls.Button();
// This causes a compile error:
MonoGameGum.Forms.Controls.Button button = new Gum.Forms.Controls.Button();
// This causes a runtime exception:
var button = new Gum.Forms.Controls.Button();
var oldButtonType = (MonoGameGum.Forms.Controls.Button)button;
```

If it is difficult to migrate your entire game over to the new namespace, then you should consider either:

* Using an old NuGet package until you are ready to migrate
* Migrating your entire game away from MonoGameGum.Forms at one time when possible
